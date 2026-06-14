using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Navigation.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Repositories;

public class NavigationMenuRepository : INavigationMenuRepository
{
    private readonly POSDbContext _context;

    public NavigationMenuRepository(POSDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AppMenu>> GetAllActiveAsync()
    {
        return await _context.Menus
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.ParentId ?? m.Id)
            .ThenBy(m => m.DisplayOrder)
            .ThenBy(m => m.Name)
            .ToListAsync();
    }
}
