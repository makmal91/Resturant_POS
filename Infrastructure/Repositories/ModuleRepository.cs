using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Modules.DTOs;
using POSSystem.Application.Modules.Interfaces;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Repositories;

public class ModuleRepository : IModuleRepository
{
    private readonly POSDbContext _context;

    public ModuleRepository(POSDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ModuleListItemDto>> GetAllAsync()
    {
        var modules = await _context.PermissionModules
            .AsNoTracking()
            .Where(m => !m.IsDeleted && m.IsActive)
            .OrderBy(m => m.DisplayOrder)
            .ThenBy(m => m.ModuleName)
            .Select(m => new ModuleListItemDto
            {
                Id = m.Id,
                ModuleName = m.ModuleName,
                ModuleKey = m.ModuleKey,
                ParentModuleId = m.ParentModuleId,
                DisplayOrder = m.DisplayOrder,
                IsActive = m.IsActive
            })
            .ToListAsync();

        return BuildTree(modules);
    }

    public async Task<IReadOnlyList<(int Id, string ModuleKey, string ModuleName, int? ParentModuleId, int DisplayOrder)>> GetAssignableModulesAsync()
    {
        return await _context.PermissionModules
            .AsNoTracking()
            .Where(m => !m.IsDeleted && m.IsActive && m.ModuleKey != string.Empty)
            .OrderBy(m => m.DisplayOrder)
            .ThenBy(m => m.ModuleName)
            .Select(m => new ValueTuple<int, string, string, int?, int>(
                m.Id,
                m.ModuleKey,
                m.ModuleName,
                m.ParentModuleId,
                m.DisplayOrder))
            .ToListAsync();
    }

    public async Task<(int Id, string ModuleKey, string ModuleName)?> GetByIdAsync(int moduleId)
    {
        var module = await _context.PermissionModules
            .AsNoTracking()
            .Where(m => m.Id == moduleId && !m.IsDeleted && m.IsActive)
            .Select(m => new { m.Id, m.ModuleKey, m.ModuleName })
            .FirstOrDefaultAsync();

        return module == null ? null : (module.Id, module.ModuleKey, module.ModuleName);
    }

    private static IReadOnlyList<ModuleListItemDto> BuildTree(IReadOnlyList<ModuleListItemDto> modules)
    {
        var lookup = modules.ToLookup(m => m.ParentModuleId);
        return BuildChildren(lookup, null);
    }

    private static IReadOnlyList<ModuleListItemDto> BuildChildren(
        ILookup<int?, ModuleListItemDto> lookup,
        int? parentId)
    {
        return lookup[parentId]
            .Select(module =>
            {
                var children = BuildChildren(lookup, module.Id);
                return new ModuleListItemDto
                {
                    Id = module.Id,
                    ModuleName = module.ModuleName,
                    ModuleKey = module.ModuleKey,
                    ParentModuleId = module.ParentModuleId,
                    DisplayOrder = module.DisplayOrder,
                    IsActive = module.IsActive,
                    Children = children
                };
            })
            .ToList();
    }
}
