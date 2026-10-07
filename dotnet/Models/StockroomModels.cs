namespace Stockroom.Models;

public sealed record Product(
    int Id,
    string Name,
    string Sku,
    string Category,
    int QuantityInStock,
    int ReorderLevel,
    decimal UnitPrice,
    string? Supplier);

public sealed record Sale(
    int Id,
    int ProductId,
    int QuantitySold,
    decimal UnitPriceAtSale,
    decimal TotalAmount,
    DateTime SaleDate,
    string ProductName,
    string Sku);

public sealed record DashboardSummary(
    int TotalProducts,
    decimal StockValue,
    decimal TotalRevenue,
    IReadOnlyList<Product> LowStockProducts,
    IReadOnlyList<Sale> RecentSales);

public sealed record ProductRevenue(string Name, int TotalSold, decimal TotalRevenue);

public sealed record CategoryRevenue(string Category, decimal TotalRevenue);

public sealed record SaleResult(bool IsSuccess, string? Error, Sale? Sale);

public sealed class DuplicateSkuException(string sku, Exception innerException)
    : Exception($"A product with SKU '{sku}' already exists.", innerException);