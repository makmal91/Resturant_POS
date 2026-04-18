using System.ComponentModel.DataAnnotations;

namespace POSSystem.Application.Recipe.DTOs;

public class CreateRecipeDto
{
    [Required]
    public int MenuItemId { get; set; }
    [Required]
    public int IngredientId { get; set; }
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal QuantityRequired { get; set; }
    [Required]
    public string Unit { get; set; } = string.Empty;
    [Required]
    public int BranchId { get; set; }
}

public class UpdateRecipeDto
{
    [Required]
    public int Id { get; set; }
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal QuantityRequired { get; set; }
    [Required]
    public string Unit { get; set; } = string.Empty;
}