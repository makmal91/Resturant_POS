using POSSystem.Domain;

namespace POSSystem.Application.Navigation.Interfaces;

public interface INavigationMenuRepository
{
    Task<IReadOnlyList<AppMenu>> GetAllActiveAsync();
}
