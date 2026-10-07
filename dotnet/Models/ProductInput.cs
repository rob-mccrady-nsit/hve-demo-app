using System.ComponentModel.DataAnnotations;

namespace Stockroom.Models;

public sealed class ProductInput
{
    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(60)]
    public string Sku { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string Category { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int QuantityInStock { get; set; }

    [Range(0, int.MaxValue)]
    public int ReorderLevel { get; set; } = 5;

    [Range(typeof(decimal), "0", "1000000000")]
    public decimal UnitPrice { get; set; }

    [StringLength(120)]
    public string? Supplier { get; set; }
}