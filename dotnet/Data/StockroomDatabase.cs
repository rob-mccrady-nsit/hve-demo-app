using System.Globalization;
using Microsoft.Data.Sqlite;
using Stockroom.Models;

namespace Stockroom.Data;

public sealed class StockroomDatabase
{
    private readonly string _databasePath;
    private readonly string _connectionString;

    public StockroomDatabase(string databasePath)
    {
        _databasePath = Path.GetFullPath(databasePath);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            ForeignKeys = true,
            Pooling = false
        }.ToString();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_databasePath);
        if (string.IsNullOrEmpty(directory) == false)
        {
            Directory.CreateDirectory(directory);
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                sku TEXT NOT NULL UNIQUE,
                category TEXT NOT NULL,
                quantity_in_stock INTEGER NOT NULL DEFAULT 0,
                reorder_level INTEGER NOT NULL DEFAULT 5,
                unit_price REAL NOT NULL,
                supplier TEXT
            );

            CREATE TABLE IF NOT EXISTS sales (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                product_id INTEGER NOT NULL,
                quantity_sold INTEGER NOT NULL,
                unit_price_at_sale REAL NOT NULL,
                total_amount REAL NOT NULL,
                sale_date TEXT NOT NULL,
                FOREIGN KEY (product_id) REFERENCES products(id)
            );

            CREATE INDEX IF NOT EXISTS ix_sales_sale_date ON sales(sale_date DESC, id DESC);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<DashboardSummary> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var totalProducts = Convert.ToInt32(await ExecuteScalarAsync(
            connection, "SELECT COUNT(*) FROM products;", cancellationToken));
        var stockValue = ToDecimal(await ExecuteScalarAsync(
            connection,
            "SELECT COALESCE(SUM(quantity_in_stock * unit_price), 0) FROM products;",
            cancellationToken));
        var totalRevenue = ToDecimal(await ExecuteScalarAsync(
            connection, "SELECT COALESCE(SUM(total_amount), 0) FROM sales;", cancellationToken));
        var lowStockProducts = await ReadProductsAsync(
            connection,
            "SELECT * FROM products WHERE quantity_in_stock <= reorder_level ORDER BY quantity_in_stock, name;",
            null,
            cancellationToken);
        var recentSales = await ReadSalesAsync(
            connection,
            """
            SELECT sales.id, sales.product_id, sales.quantity_sold, sales.unit_price_at_sale,
                   sales.total_amount, sales.sale_date, products.name, products.sku
            FROM sales
            JOIN products ON products.id = sales.product_id
            ORDER BY sales.sale_date DESC, sales.id DESC
            LIMIT 5;
            """,
            cancellationToken);

        return new DashboardSummary(
            totalProducts, stockValue, totalRevenue, lowStockProducts, recentSales);
    }

    public async Task<IReadOnlyList<Product>> GetProductsAsync(
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var sql = string.IsNullOrWhiteSpace(search)
            ? "SELECT * FROM products ORDER BY name;"
            : "SELECT * FROM products WHERE name LIKE $search OR sku LIKE $search OR category LIKE $search ORDER BY name;";
        return await ReadProductsAsync(connection, sql, search, cancellationToken);
    }

    public async Task<Product?> GetProductAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        return await ReadProductAsync(connection, null, id, cancellationToken);
    }

    public async Task<Product> CreateProductAsync(
        string name,
        string sku,
        string category,
        int quantityInStock,
        int reorderLevel,
        decimal unitPrice,
        string? supplier,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO products (name, sku, category, quantity_in_stock, reorder_level, unit_price, supplier)
            VALUES ($name, $sku, $category, $quantity, $reorderLevel, $unitPrice, $supplier);
            SELECT last_insert_rowid();
            """;
        AddProductParameters(command, name, sku, category, quantityInStock, reorderLevel, unitPrice, supplier);

        try
        {
            var id = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            return new Product(id, name, sku, category, quantityInStock, reorderLevel, unitPrice, supplier);
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            throw new DuplicateSkuException(sku, exception);
        }
    }

    public async Task<bool> UpdateProductAsync(
        int id,
        string name,
        string category,
        int quantityInStock,
        int reorderLevel,
        decimal unitPrice,
        string? supplier,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE products
            SET name = $name, category = $category, quantity_in_stock = $quantity,
                reorder_level = $reorderLevel, unit_price = $unitPrice, supplier = $supplier
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$category", category);
        command.Parameters.AddWithValue("$quantity", quantityInStock);
        command.Parameters.AddWithValue("$reorderLevel", reorderLevel);
        command.Parameters.AddWithValue("$unitPrice", (double)unitPrice);
        command.Parameters.AddWithValue("$supplier", (object?)supplier ?? DBNull.Value);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> DeleteProductAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM products WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<IReadOnlyList<Sale>> GetSalesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        return await ReadSalesAsync(
            connection,
            """
            SELECT sales.id, sales.product_id, sales.quantity_sold, sales.unit_price_at_sale,
                   sales.total_amount, sales.sale_date, products.name, products.sku
            FROM sales
            JOIN products ON products.id = sales.product_id
            ORDER BY sales.sale_date DESC, sales.id DESC;
            """,
            cancellationToken);
    }

    public async Task<SaleResult> RecordSaleAsync(
        int productId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            return new SaleResult(false, "Quantity sold must be a positive whole number.", null);
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        var product = await ReadProductAsync(connection, transaction, productId, cancellationToken);
        if (product is null)
        {
            return new SaleResult(false, "Selected product was not found.", null);
        }

        if (quantity > product.QuantityInStock)
        {
            return new SaleResult(
                false,
                $"Not enough stock. Only {product.QuantityInStock} unit(s) of '{product.Name}' available.",
                null);
        }

        var totalAmount = decimal.Round(product.UnitPrice * quantity, 2, MidpointRounding.ToEven);
        var saleDate = DateTime.Now;
        await using (var insert = connection.CreateCommand())
        {
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO sales (product_id, quantity_sold, unit_price_at_sale, total_amount, sale_date)
                VALUES ($productId, $quantity, $unitPrice, $total, $saleDate);
                """;
            insert.Parameters.AddWithValue("$productId", product.Id);
            insert.Parameters.AddWithValue("$quantity", quantity);
            insert.Parameters.AddWithValue("$unitPrice", (double)product.UnitPrice);
            insert.Parameters.AddWithValue("$total", (double)totalAmount);
            insert.Parameters.AddWithValue("$saleDate", saleDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var update = connection.CreateCommand())
        {
            update.Transaction = transaction;
            update.CommandText = """
                UPDATE products
                SET quantity_in_stock = quantity_in_stock - $quantity
                WHERE id = $productId AND quantity_in_stock >= $quantity;
                """;
            update.Parameters.AddWithValue("$productId", product.Id);
            update.Parameters.AddWithValue("$quantity", quantity);
            if (await update.ExecuteNonQueryAsync(cancellationToken) != 1)
            {
                return new SaleResult(false, "Stock changed before the sale could be recorded. Try again.", null);
            }
        }

        transaction.Commit();
        return new SaleResult(
            true,
            null,
            new Sale(0, product.Id, quantity, product.UnitPrice, totalAmount, saleDate, product.Name, product.Sku));
    }

    public async Task<IReadOnlyList<ProductRevenue>> GetTopProductsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT products.name, SUM(sales.quantity_sold), SUM(sales.total_amount)
            FROM sales
            JOIN products ON products.id = sales.product_id
            GROUP BY products.id
            ORDER BY SUM(sales.total_amount) DESC
            LIMIT 10;
            """;
        var results = new List<ProductRevenue>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new ProductRevenue(
                reader.GetString(0), reader.GetInt32(1), ToDecimal(reader.GetValue(2))));
        }

        return results;
    }

    public async Task<IReadOnlyList<CategoryRevenue>> GetRevenueByCategoryAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT products.category, SUM(sales.total_amount)
            FROM sales
            JOIN products ON products.id = sales.product_id
            GROUP BY products.category
            ORDER BY SUM(sales.total_amount) DESC;
            """;
        var results = new List<CategoryRevenue>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new CategoryRevenue(reader.GetString(0), ToDecimal(reader.GetValue(1))));
        }

        return results;
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static async Task<object?> ExecuteScalarAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync(cancellationToken);
    }

    private static async Task<IReadOnlyList<Product>> ReadProductsAsync(
        SqliteConnection connection,
        string sql,
        string? search,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        if (string.IsNullOrWhiteSpace(search) == false)
        {
            command.Parameters.AddWithValue("$search", $"%{search.Trim()}%");
        }

        var products = new List<Product>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            products.Add(ReadProduct(reader));
        }

        return products;
    }

    private static async Task<Product?> ReadProductAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        int id,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT * FROM products WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadProduct(reader) : null;
    }

    private static async Task<IReadOnlyList<Sale>> ReadSalesAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var sales = new List<Sale>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            sales.Add(new Sale(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetInt32(2),
                ToDecimal(reader.GetValue(3)),
                ToDecimal(reader.GetValue(4)),
                DateTime.ParseExact(reader.GetString(5), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                reader.GetString(6),
                reader.GetString(7)));
        }

        return sales;
    }

    private static Product ReadProduct(SqliteDataReader reader) => new(
        reader.GetInt32(0),
        reader.GetString(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetInt32(4),
        reader.GetInt32(5),
        ToDecimal(reader.GetValue(6)),
        reader.IsDBNull(7) ? null : reader.GetString(7));

    private static decimal ToDecimal(object? value) =>
        value is null or DBNull ? 0 : Convert.ToDecimal(value, CultureInfo.InvariantCulture);

    private static void AddProductParameters(
        SqliteCommand command,
        string name,
        string sku,
        string category,
        int quantityInStock,
        int reorderLevel,
        decimal unitPrice,
        string? supplier)
    {
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$sku", sku);
        command.Parameters.AddWithValue("$category", category);
        command.Parameters.AddWithValue("$quantity", quantityInStock);
        command.Parameters.AddWithValue("$reorderLevel", reorderLevel);
        command.Parameters.AddWithValue("$unitPrice", (double)unitPrice);
        command.Parameters.AddWithValue("$supplier", (object?)supplier ?? DBNull.Value);
    }
}