using Microsoft.AspNetCore.Mvc.RazorPages;
using Stockroom.Data;
using Stockroom.Models;

namespace Stockroom.Pages;

public sealed class IndexModel(StockroomDatabase database) : PageModel
{
    public DashboardSummary Summary { get; private set; } = null!;

    public async Task OnGetAsync() =>
        Summary = await database.GetDashboardAsync(HttpContext.RequestAborted);
}
