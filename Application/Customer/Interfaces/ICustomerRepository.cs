using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Customer.DTOs;
using CustomerEntity = POSSystem.Domain.Customer;

namespace POSSystem.Application.Customer.Interfaces;

public interface ICustomerRepository
{
    Task<PagedResultDto<CustomerEntity>> GetPagedAsync(CustomerFilterDto filter);
    Task<CustomerEntity?> GetByIdAsync(int id, int businessId, int branchId);
    Task<CustomerEntity?> GetWalkInAsync(int businessId, int branchId);
    Task<List<CustomerEntity>> SearchAsync(string query, int businessId, int branchId, int take = 10);
    Task<bool> PhoneExistsAsync(string phone, int businessId, int branchId, int? excludeId = null);
    Task<bool> CustomerCodeExistsAsync(string customerCode, int businessId, int branchId, int? excludeId = null);
    Task AddAsync(CustomerEntity customer);
    Task SaveChangesAsync();
}
