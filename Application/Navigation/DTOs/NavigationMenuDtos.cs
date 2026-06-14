namespace POSSystem.Application.Navigation.DTOs;

public class NavigationMenuDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Route { get; set; }
    public string? Icon { get; set; }
    public string? ModuleName { get; set; }
    public int? ParentId { get; set; }
    public int DisplayOrder { get; set; }
}
