using POSSystem.Application.Modules.DTOs;
using POSSystem.Application.Modules.Interfaces;

namespace POSSystem.Application.Modules.Services;

public class ModuleService : IModuleService
{
    private readonly IModuleRepository _repository;

    public ModuleService(IModuleRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<ModuleListItemDto>> GetModulesAsync() =>
        _repository.GetAllAsync();
}
