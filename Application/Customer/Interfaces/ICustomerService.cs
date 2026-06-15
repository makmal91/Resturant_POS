using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Customer.DTOs;

namespace POSSystem.Application.Customer.Interfaces;

public interface ICustomerService
{
    Task<PagedResultDto<CustomerListDto>> GetCustomersPagedAsync(CustomerFilterDto filter);
    Task<CustomerDetailDto?> GetByIdAsync(int id, int businessId, int branchId);
    Task<CustomerDetailDto?> GetWalkInCustomerAsync(int businessId, int branchId);
    Task<List<CustomerListDto>> SearchCustomersAsync(string query, int businessId, int branchId);
    Task<CustomerDetailDto> CreateAsync(CreateCustomerDto dto);
    Task<CustomerDetailDto?> UpdateAsync(int id, UpdateCustomerDto dto);
    Task DeleteAsync(int id, int businessId, int branchId);
    Task<CustomerDetailDto> QuickCreateAsync(QuickCreateCustomerDto dto);
}
