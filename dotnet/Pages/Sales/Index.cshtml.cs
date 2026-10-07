using Microsoft.AspNetCore.Mvc.RazorPages;
using Stockroom.Data;
using Stockroom.Models;

namespace Stockroom.Pages.Sales;

public sealed class IndexModel(StockroomDatabase database) : PageModel
{
    public IReadOnlyList<Sale> Sales { get; private set; } = [];

    public async Task OnGetAsync() =>
        Sales = await database.GetSalesAsync(HttpContext.RequestAborted);
}