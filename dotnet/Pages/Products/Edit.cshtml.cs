using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Stockroom.Data;
using Stockroom.Models;

namespace Stockroom.Pages.Products;

public sealed class EditModel(StockroomDatabase database) : PageModel
{
    [BindProperty]
    public ProductInput Input { get; set; } = new();

    public Product? Product { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Product = await database.GetProductAsync(id, HttpContext.RequestAborted);
        if (Product is null)
        {
            return NotFound();
        }

        Input = new ProductInput
        {
            Name = Product.Name,
            Sku = Product.Sku,
            Category = Product.Category,
            QuantityInStock = Product.QuantityInStock,
            ReorderLevel = Product.ReorderLevel,
            UnitPrice = Product.UnitPrice,
            Supplier = Product.Supplier
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        Product = await database.GetProductAsync(id, HttpContext.RequestAborted);
        if (Product is null)
        {
            return NotFound();
        }

        if (ModelState.IsValid == false)
        {
            return Page();
        }

        var updated = await database.UpdateProductAsync(
            id,
            Input.Name.Trim(),
            Input.Category.Trim(),
            Input.QuantityInStock,
            Input.ReorderLevel,
            Input.UnitPrice,
            string.IsNullOrWhiteSpace(Input.Supplier) ? null : Input.Supplier.Trim(),
            HttpContext.RequestAborted);
        TempData["FlashKind"] = updated ? "success" : "error";
        TempData["FlashMessage"] = updated
            ? $"Product '{Input.Name.Trim()}' updated."
            : "Product was not found.";
        return RedirectToPage("/Products/Index");
    }
}