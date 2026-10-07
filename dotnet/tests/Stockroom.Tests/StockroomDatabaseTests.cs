using Stockroom.Data;
using Stockroom.Models;

namespace Stockroom.Tests;

public sealed class StockroomDatabaseTests : IAsyncLifetime
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(), $"stockroom-tests-{Guid.NewGuid():N}.db");
    private readonly StockroomDatabase _database;

    public StockroomDatabaseTests()
    {
        _database = new StockroomDatabase(_databasePath);
    }

    public Task InitializeAsync() => _database.InitializeAsync();

    public Task DisposeAsync()
    {
        File.Delete(_databasePath);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ProductSearchFindsNameSkuAndCategory()
    {
        await AddProductAsync("Blue Notebook", "NOTE-1", "Stationery");

        Assert.Single(await _database.GetProductsAsync("NOTE-1"));
        Assert.Single(await _database.GetProductsAsync("Stationery"));
        Assert.Empty(await _database.GetProductsAsync("missing"));
    }

    [Fact]
    public async Task DuplicateSkuIsRejected()
    {
        await AddProductAsync("Blue Notebook", "NOTE-1", "Stationery");

        await Assert.ThrowsAsync<DuplicateSkuException>(() =>
            AddProductAsync("Red Notebook", "NOTE-1", "Stationery"));
    }

    [Fact]
    public async Task RecordingSaleDeductsStockAndKeepsPriceSnapshot()
    {
        var product = await AddProductAsync("Blue Notebook", "NOTE-1", "Stationery");

        var result = await _database.RecordSaleAsync(product.Id, 2);
        var updatedProduct = await _database.GetProductAsync(product.Id);
        var sales = await _database.GetSalesAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(3, updatedProduct!.QuantityInStock);
        Assert.Equal(21m, result.Sale!.TotalAmount);
        Assert.Equal(10.50m, sales[0].UnitPriceAtSale);
        Assert.Equal(21m, sales[0].TotalAmount);
    }

    [Fact]
    public async Task OversellIsRejectedWithoutChangingStockOrCreatingSale()
    {
        var product = await AddProductAsync("Blue Notebook", "NOTE-1", "Stationery", quantity: 2);

        var result = await _database.RecordSaleAsync(product.Id, 3);

        Assert.False(result.IsSuccess);
        Assert.Contains("Not enough stock", result.Error);
        Assert.Equal(2, (await _database.GetProductAsync(product.Id))!.QuantityInStock);
        Assert.Empty(await _database.GetSalesAsync());
    }

    private Task<Product> AddProductAsync(
        string name,
        string sku,
        string category,
        int quantity = 5) =>
        _database.CreateProductAsync(name, sku, category, quantity, 2, 10.50m, null);
}