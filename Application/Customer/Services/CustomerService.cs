using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Customer.DTOs;
using POSSystem.Application.Customer.Interfaces;
using CustomerEntity = POSSystem.Domain.Customer;

namespace POSSystem.Application.Customer.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repo;

    public CustomerService(ICustomerRepository repo) => _repo = repo;

    public async Task<PagedResultDto<CustomerListDto>> GetCustomersPagedAsync(CustomerFilterDto filter)
    {
        var paged = await _repo.GetPagedAsync(filter);
        return new PagedResultDto<CustomerListDto>
        {
            Data       = paged.Data.Select(MapList).ToList(),
            TotalRecords = paged.TotalRecords,
            TotalPages = paged.TotalPages,
            CurrentPage = paged.CurrentPage
        };
    }

    public async Task<CustomerDetailDto?> GetByIdAsync(int id, int businessId, int branchId)
    {
        var c = await _repo.GetByIdAsync(id, businessId, branchId);
        return c == null ? null : MapDetail(c);
    }

    public async Task<CustomerDetailDto?> GetWalkInCustomerAsync(int businessId, int branchId)
    {
        var c = await _repo.GetWalkInAsync(businessId, branchId);
        return c == null ? null : MapDetail(c);
    }

    public async Task<List<CustomerListDto>> SearchCustomersAsync(string query, int businessId, int branchId)
    {
        var list = await _repo.SearchAsync(query, businessId, branchId, 10);
        return list.Select(MapList).ToList();
    }

    public async Task<CustomerDetailDto> CreateAsync(CreateCustomerDto dto)
    {
        Validate(dto.Name, dto.CreditLimit);

        if (!string.IsNullOrWhiteSpace(dto.Phone))
        {
            if (await _repo.PhoneExistsAsync(dto.Phone, dto.BusinessId, dto.BranchId))
                throw new InvalidOperationException($"Phone number '{dto.Phone}' is already registered.");
        }

        var seq  = await _repo.GetNextCodeSequenceAsync(dto.BusinessId, dto.BranchId);
        var code = $"CUST-{seq:D5}";

        var entity = new CustomerEntity
        {
            CustomerCode  = code,
            Name          = dto.Name.Trim(),
            Phone         = dto.Phone?.Trim(),
            Email         = dto.Email?.Trim(),
            Address       = dto.Address?.Trim(),
            City          = dto.City?.Trim(),
            CNIC          = dto.CNIC?.Trim(),
            CustomerType  = dto.CustomerType,
            Status        = dto.Status,
            OpeningBalance = dto.OpeningBalance,
            CreditLimit   = dto.CreditLimit,
            BusinessId    = dto.BusinessId,
            BranchId      = dto.BranchId
        };

        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        return MapDetail(entity);
    }

    public async Task<CustomerDetailDto?> UpdateAsync(int id, UpdateCustomerDto dto)
    {
        var entity = await _repo.GetByIdAsync(id, dto.BusinessId, dto.BranchId);
        if (entity == null) return null;

        Validate(dto.Name, dto.CreditLimit);

        if (!string.IsNullOrWhiteSpace(dto.Phone))
        {
            if (await _repo.PhoneExistsAsync(dto.Phone, dto.BusinessId, dto.BranchId, id))
                throw new InvalidOperationException($"Phone number '{dto.Phone}' is already registered.");
        }

        entity.Name          = dto.Name.Trim();
        entity.Phone         = dto.Phone?.Trim();
        entity.Email         = dto.Email?.Trim();
        entity.Address       = dto.Address?.Trim();
        entity.City          = dto.City?.Trim();
        entity.CNIC          = dto.CNIC?.Trim();
        entity.CustomerType  = dto.CustomerType;
        entity.Status        = dto.Status;
        entity.OpeningBalance = dto.OpeningBalance;
        entity.CreditLimit   = dto.CreditLimit;

        await _repo.SaveChangesAsync();
        return MapDetail(entity);
    }

    public async Task DeleteAsync(int id, int businessId, int branchId)
    {
        var entity = await _repo.GetByIdAsync(id, businessId, branchId);
        if (entity == null) throw new InvalidOperationException("Customer not found.");
        if (entity.IsWalkIn) throw new InvalidOperationException("The walk-in customer cannot be deleted.");

        entity.IsDeleted   = true;
        await _repo.SaveChangesAsync();
    }

    public async Task<CustomerDetailDto> QuickCreateAsync(QuickCreateCustomerDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Customer name is required.");

        if (!string.IsNullOrWhiteSpace(dto.Phone))
        {
            if (await _repo.PhoneExistsAsync(dto.Phone, dto.BusinessId, dto.BranchId))
                throw new InvalidOperationException($"Phone '{dto.Phone}' is already registered.");
        }

        var seq  = await _repo.GetNextCodeSequenceAsync(dto.BusinessId, dto.BranchId);
        var entity = new CustomerEntity
        {
            CustomerCode = $"CUST-{seq:D5}",
            Name         = dto.Name.Trim(),
            Phone        = dto.Phone?.Trim(),
            CustomerType = Domain.CustomerType.Retail,
            Status       = true,
            BusinessId   = dto.BusinessId,
            BranchId     = dto.BranchId
        };

        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        return MapDetail(entity);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static void Validate(string name, decimal creditLimit)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Customer name is required.");
        if (creditLimit < 0)
            throw new InvalidOperationException("Credit limit cannot be negative.");
    }

    private static CustomerListDto MapList(CustomerEntity c) => new()
    {
        Id           = c.Id,
        CustomerCode = c.CustomerCode,
        Name         = c.Name,
        Phone        = c.Phone,
        Email        = c.Email,
        City         = c.City,
        CustomerType = c.CustomerType,
        Status       = c.Status,
        CreditLimit  = c.CreditLimit,
        IsWalkIn     = c.IsWalkIn,
        CreatedAt  = c.CreatedAt
    };

    private static CustomerDetailDto MapDetail(CustomerEntity c) => new()
    {
        Id             = c.Id,
        CustomerCode   = c.CustomerCode,
        Name           = c.Name,
        Phone          = c.Phone,
        Email          = c.Email,
        Address        = c.Address,
        City           = c.City,
        CNIC           = c.CNIC,
        CustomerType   = c.CustomerType,
        Status         = c.Status,
        OpeningBalance = c.OpeningBalance,
        CreditLimit    = c.CreditLimit,
        LoyaltyPoints  = c.LoyaltyPoints,
        IsWalkIn       = c.IsWalkIn,
        CreatedAt    = c.CreatedAt,
        ModifiedAt   = c.ModifiedAt
    };
}
