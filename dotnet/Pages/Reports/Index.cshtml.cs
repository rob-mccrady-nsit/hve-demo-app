using Microsoft.AspNetCore.Mvc.RazorPages;
using Stockroom.Data;
using Stockroom.Models;

namespace Stockroom.Pages.Reports;

public sealed class IndexModel(StockroomDatabase database) : PageModel
{
    public IReadOnlyList<ProductRevenue> TopProducts { get; private set; } = [];

    public IReadOnlyList<CategoryRevenue> RevenueByCategory { get; private set; } = [];

    public async Task OnGetAsync()
    {
        TopProducts = await database.GetTopProductsAsync(HttpContext.RequestAborted);
        RevenueByCategory = await database.GetRevenueByCategoryAsync(HttpContext.RequestAborted);
    }
}