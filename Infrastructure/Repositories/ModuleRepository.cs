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
        var modules = await GetModuleRowsAsync();
        var forms = await GetFormRowsAsync();
        return BuildTree(modules, forms);
    }

    public async Task<IReadOnlyList<ModuleListItemDto>> GetSidebarModulesAsync()
    {
        var modules = await GetModuleRowsAsync();
        return BuildTree(modules, Array.Empty<(int, int, string, string, string?, int)>());
    }

    public async Task<IReadOnlyList<(int Id, string ModuleKey, string ModuleName, int? ParentModuleId, int DisplayOrder, string? Route, string? Icon)>> GetAllModulesFlatAsync()
    {
        return await _context.PermissionModules
            .AsNoTracking()
            .Where(m => !m.IsDeleted && m.IsActive)
            .OrderBy(m => m.DisplayOrder)
            .ThenBy(m => m.ModuleName)
            .Select(m => new ValueTuple<int, string, string, int?, int, string?, string?>(
                m.Id,
                m.ModuleKey,
                m.ModuleName,
                m.ParentModuleId,
                m.DisplayOrder,
                m.Route,
                m.Icon))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<(int Id, string ModuleKey, string ModuleName, int? ParentModuleId, int DisplayOrder)>> GetAssignableModulesAsync()
    {
        return await _context.PermissionModules
            .AsNoTracking()
            .Where(m => !m.IsDeleted && m.IsActive)
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

    public async Task<IReadOnlyList<(int Id, int ModuleId, string FormName, string FormCode, string? Route, int SortOrder)>> GetAllFormsAsync()
    {
        return await _context.ModuleForms
            .AsNoTracking()
            .Where(f => !f.IsDeleted && f.IsActive)
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.FormName)
            .Select(f => new ValueTuple<int, int, string, string, string?, int>(
                f.Id,
                f.ModuleId,
                f.FormName,
                f.FormCode,
                f.Route,
                f.SortOrder))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<FormPermissionItemDto>> GetFormPermissionsForRoleAsync(int roleId)
    {
        return await _context.RoleFormPermissions
            .AsNoTracking()
            .Where(rfp => rfp.RoleId == roleId && !rfp.IsDeleted)
            .Select(rfp => new FormPermissionItemDto
            {
                FormId = rfp.FormId,
                ModuleId = rfp.Form.ModuleId,
                FormName = rfp.Form.FormName,
                FormCode = rfp.Form.FormCode,
                CanView = rfp.CanView,
                CanCreate = rfp.CanCreate,
                CanEdit = rfp.CanEdit,
                CanDelete = rfp.CanDelete
            })
            .ToListAsync();
    }

    public async Task ReplaceFormPermissionsAsync(int roleId, IReadOnlyList<SaveFormPermissionItemDto> formPermissions)
    {
        var existing = await _context.RoleFormPermissions
            .Where(rfp => rfp.RoleId == roleId)
            .ToListAsync();

        _context.RoleFormPermissions.RemoveRange(existing);

        foreach (var item in formPermissions)
        {
            await _context.RoleFormPermissions.AddAsync(new Domain.RoleFormPermission
            {
                RoleId = roleId,
                FormId = item.FormId,
                CanView = item.CanView,
                CanCreate = item.CanCreate,
                CanEdit = item.CanEdit,
                CanDelete = item.CanDelete,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
    }

    private async Task<IReadOnlyList<(int Id, string ModuleKey, string ModuleName, int? ParentModuleId, int DisplayOrder, string? Route, string? Icon)>> GetModuleRowsAsync()
    {
        return await GetAllModulesFlatAsync();
    }

    private async Task<IReadOnlyList<(int Id, int ModuleId, string FormName, string FormCode, string? Route, int SortOrder)>> GetFormRowsAsync()
    {
        return await GetAllFormsAsync();
    }

    private static IReadOnlyList<ModuleListItemDto> BuildTree(
        IReadOnlyList<(int Id, string ModuleKey, string ModuleName, int? ParentModuleId, int DisplayOrder, string? Route, string? Icon)> modules,
        IReadOnlyList<(int Id, int ModuleId, string FormName, string FormCode, string? Route, int SortOrder)> forms)
    {
        var formLookup = forms.ToLookup(f => f.ModuleId);
        var moduleDtos = modules
            .Select(m => new ModuleListItemDto
            {
                Id = m.Id,
                ModuleName = m.ModuleName,
                ModuleKey = m.ModuleKey,
                ParentModuleId = m.ParentModuleId,
                Route = m.Route,
                Icon = m.Icon,
                DisplayOrder = m.DisplayOrder,
                IsActive = true,
                Forms = formLookup[m.Id]
                    .Select(f => new ModuleFormDto
                    {
                        Id = f.Id,
                        ModuleId = f.ModuleId,
                        FormName = f.FormName,
                        FormCode = f.FormCode,
                        Route = f.Route,
                        SortOrder = f.SortOrder,
                        IsActive = true
                    })
                    .ToList()
            })
            .ToList();

        var lookup = moduleDtos.ToLookup(m => m.ParentModuleId);
        return BuildChildren(lookup, null);
    }

    private static IReadOnlyList<ModuleListItemDto> BuildChildren(
        ILookup<int?, ModuleListItemDto> lookup,
        int? parentId)
    {
        return lookup[parentId]
            .OrderBy(m => m.DisplayOrder)
            .ThenBy(m => m.ModuleName)
            .Select(module =>
            {
                var children = BuildChildren(lookup, module.Id);
                return new ModuleListItemDto
                {
                    Id = module.Id,
                    ModuleName = module.ModuleName,
                    ModuleKey = module.ModuleKey,
                    ParentModuleId = module.ParentModuleId,
                    Route = module.Route,
                    Icon = module.Icon,
                    DisplayOrder = module.DisplayOrder,
                    IsActive = module.IsActive,
                    Forms = module.Forms,
                    Children = children
                };
            })
            .ToList();
    }
}
