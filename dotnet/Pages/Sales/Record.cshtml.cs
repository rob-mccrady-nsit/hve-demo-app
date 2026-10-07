using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Stockroom.Data;
using Stockroom.Models;

namespace Stockroom.Pages.Sales;

public sealed class RecordModel(StockroomDatabase database) : PageModel
{
    [BindProperty]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a product.")]
    public int ProductId { get; set; }

    [BindProperty]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity sold must be a positive whole number.")]
    public int Quantity { get; set; } = 1;

    public IReadOnlyList<Product> Products { get; private set; } = [];

    public async Task OnGetAsync() => await LoadProductsAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadProductsAsync();
        if (ModelState.IsValid == false)
        {
            return Page();
        }

        var result = await database.RecordSaleAsync(
            ProductId, Quantity, HttpContext.RequestAborted);
        if (result.IsSuccess == false)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "The sale could not be recorded.");
            return Page();
        }

        TempData["FlashKind"] = "success";
        TempData["FlashMessage"] =
            $"Sale recorded: {Quantity} x '{result.Sale!.ProductName}' for {result.Sale.TotalAmount:C}.";
        return RedirectToPage("/Sales/Index");
    }

    private async Task LoadProductsAsync() =>
        Products = await database.GetProductsAsync(cancellationToken: HttpContext.RequestAborted);
}