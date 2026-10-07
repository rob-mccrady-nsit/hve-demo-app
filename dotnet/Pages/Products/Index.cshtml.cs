using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Stockroom.Data;
using Stockroom.Models;

namespace Stockroom.Pages.Products;

public sealed class IndexModel(StockroomDatabase database) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public IReadOnlyList<Product> Products { get; private set; } = [];

    public async Task OnGetAsync() =>
        Products = await database.GetProductsAsync(Search, HttpContext.RequestAborted);

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            var deleted = await database.DeleteProductAsync(id, HttpContext.RequestAborted);
            TempData["FlashKind"] = deleted ? "success" : "error";
            TempData["FlashMessage"] = deleted ? "Product deleted." : "Product was not found.";
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            TempData["FlashKind"] = "error";
            TempData["FlashMessage"] = "A product with recorded sales cannot be deleted.";
        }

        return RedirectToPage("/Products/Index", new { Search });
    }
}