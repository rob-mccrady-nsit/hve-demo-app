using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Stockroom.Data;
using Stockroom.Models;

namespace Stockroom.Pages.Products;

public sealed class CreateModel(StockroomDatabase database) : PageModel
{
    [BindProperty]
    public ProductInput Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        if (ModelState.IsValid == false)
        {
            return Page();
        }

        try
        {
            await database.CreateProductAsync(
                Input.Name.Trim(),
                Input.Sku.Trim(),
                Input.Category.Trim(),
                Input.QuantityInStock,
                Input.ReorderLevel,
                Input.UnitPrice,
                CleanOptional(Input.Supplier),
                HttpContext.RequestAborted);
        }
        catch (DuplicateSkuException)
        {
            ModelState.AddModelError("Input.Sku", "A product with this SKU already exists.");
            return Page();
        }

        TempData["FlashKind"] = "success";
        TempData["FlashMessage"] = $"Product '{Input.Name.Trim()}' added.";
        return RedirectToPage("/Products/Index");
    }

    private static string? CleanOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}