using POSSystem.Application.Accounting.Interfaces;
using POSSystem.Application.Branch.Interfaces;
using POSSystem.Application.Common.Constants;
using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Common.Interfaces;
using POSSystem.Application.Customer.DTOs;
using POSSystem.Application.Customer.Interfaces;
using CustomerEntity = POSSystem.Domain.Customer;

namespace POSSystem.Application.Customer.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repo;
    private readonly IBranchRepository _branchRepo;
    private readonly ICodeGeneratorService _codeGenerator;
    private readonly IGlAccountService _glAccountService;

    public CustomerService(
        ICustomerRepository repo,
        IBranchRepository branchRepo,
        ICodeGeneratorService codeGenerator,
        IGlAccountService glAccountService)
    {
        _repo = repo;
        _branchRepo = branchRepo;
        _codeGenerator = codeGenerator;
        _glAccountService = glAccountService;
    }

    public async Task<PagedResultDto<CustomerListDto>> GetCustomersPagedAsync(CustomerFilterDto filter)
    {
        var paged = await _repo.GetPagedAsync(filter);
        var items = new List<CustomerListDto>();
        foreach (var customer in paged.Data)
            items.Add(await MapListAsync(customer));

        return new PagedResultDto<CustomerListDto>
        {
            Data       = items,
            TotalRecords = paged.TotalRecords,
            TotalPages = paged.TotalPages,
            CurrentPage = paged.CurrentPage
        };
    }

    public async Task<CustomerDetailDto?> GetByIdAsync(int id, int businessId, int branchId)
    {
        var c = await _repo.GetByIdAsync(id, businessId, branchId);
        return c == null ? null : await MapDetailAsync(c);
    }

    public async Task<CustomerDetailDto?> GetWalkInCustomerAsync(int businessId, int branchId)
    {
        var c = await _repo.GetWalkInAsync(businessId, branchId);
        if (c == null) return null;

        if (!c.AccountId.HasValue)
        {
            c.AccountId = await _glAccountService.CreateCustomerReceivableAccountAsync(
                c.BusinessId, c.BranchId, c.Name, c.CustomerCode);
            await _repo.SaveChangesAsync();
        }

        return await MapDetailAsync(c);
    }

    public async Task<List<CustomerListDto>> SearchCustomersAsync(string query, int businessId, int branchId)
    {
        var list = await _repo.SearchAsync(query, businessId, branchId, 10);
        var results = new List<CustomerListDto>();
        foreach (var customer in list)
            results.Add(await MapListAsync(customer));
        return results;
    }

    public async Task<CustomerDetailDto> CreateAsync(CreateCustomerDto dto)
    {
        Validate(dto.Name, dto.CreditLimit);
        await ValidateLocationAsync(dto.CountryId, dto.CityId);

        if (!string.IsNullOrWhiteSpace(dto.Phone))
        {
            if (await _repo.PhoneExistsAsync(dto.Phone, dto.BusinessId, dto.BranchId))
                throw new InvalidOperationException($"Phone number '{dto.Phone}' is already registered.");
        }

        var code = await ResolveCustomerCodeAsync(dto.CustomerCode, dto.BusinessId, dto.BranchId);

        var entity = new CustomerEntity
        {
            CustomerCode  = code,
            Name          = dto.Name.Trim(),
            Phone         = dto.Phone?.Trim(),
            Email         = dto.Email?.Trim(),
            Address       = dto.Address?.Trim(),
            CountryId     = dto.CountryId,
            CityId        = dto.CityId,
            CNIC          = dto.CNIC?.Trim(),
            CustomerType  = dto.CustomerType,
            Status        = dto.Status,
            OpeningBalance = dto.OpeningBalance,
            CreditLimit   = dto.CreditLimit,
            BusinessId    = dto.BusinessId,
            BranchId      = dto.BranchId
        };

        entity.AccountId = await _glAccountService.CreateCustomerReceivableAccountAsync(
            dto.BusinessId, dto.BranchId, entity.Name, code);

        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        return await MapDetailAsync(entity);
    }

    public async Task<CustomerDetailDto?> UpdateAsync(int id, UpdateCustomerDto dto)
    {
        var entity = await _repo.GetByIdAsync(id, dto.BusinessId, dto.BranchId);
        if (entity == null) return null;

        Validate(dto.Name, dto.CreditLimit);
        await ValidateLocationAsync(dto.CountryId, dto.CityId);

        if (!string.IsNullOrWhiteSpace(dto.Phone))
        {
            if (await _repo.PhoneExistsAsync(dto.Phone, dto.BusinessId, dto.BranchId, id))
                throw new InvalidOperationException($"Phone number '{dto.Phone}' is already registered.");
        }

        entity.Name          = dto.Name.Trim();
        entity.Phone         = dto.Phone?.Trim();
        entity.Email         = dto.Email?.Trim();
        entity.Address       = dto.Address?.Trim();
        entity.CountryId     = dto.CountryId;
        entity.CityId        = dto.CityId;
        entity.CNIC          = dto.CNIC?.Trim();
        entity.CustomerType  = dto.CustomerType;
        entity.Status        = dto.Status;
        entity.OpeningBalance = dto.OpeningBalance;
        entity.CreditLimit   = dto.CreditLimit;

        if (!entity.AccountId.HasValue)
        {
            entity.AccountId = await _glAccountService.CreateCustomerReceivableAccountAsync(
                entity.BusinessId, entity.BranchId, entity.Name, entity.CustomerCode);
        }

        await _repo.SaveChangesAsync();
        return await MapDetailAsync(entity);
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

        var code = await _codeGenerator.GenerateAsync(CodeModuleNames.Customer, dto.BranchId);
        var entity = new CustomerEntity
        {
            CustomerCode = code,
            Name         = dto.Name.Trim(),
            Phone        = dto.Phone?.Trim(),
            CustomerType = Domain.CustomerType.Retail,
            Status       = true,
            BusinessId   = dto.BusinessId,
            BranchId     = dto.BranchId
        };

        entity.AccountId = await _glAccountService.CreateCustomerReceivableAccountAsync(
            dto.BusinessId, dto.BranchId, entity.Name, code);

        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        return await MapDetailAsync(entity);
    }

    private async Task<string> ResolveCustomerCodeAsync(string? requestedCode, int businessId, int branchId)
    {
        var code = await _codeGenerator.ResolveAsync(CodeModuleNames.Customer, branchId, requestedCode);

        if (await _repo.CustomerCodeExistsAsync(code, businessId, branchId))
            throw new InvalidOperationException($"Customer code '{code}' already exists in this branch.");

        return code;
    }

    private async Task ValidateLocationAsync(int? countryId, int? cityId)
    {
        if (cityId.HasValue && cityId.Value > 0)
        {
            if (!countryId.HasValue || countryId.Value <= 0)
                throw new InvalidOperationException("Country is required when city is selected.");

            if (!await _branchRepo.CountryExistsAsync(countryId.Value))
                throw new InvalidOperationException("Invalid country selected.");

            if (!await _branchRepo.CityBelongsToCountryAsync(cityId.Value, countryId.Value))
                throw new InvalidOperationException("Invalid city for the selected country.");
        }
        else if (countryId.HasValue && countryId.Value > 0)
        {
            if (!await _branchRepo.CountryExistsAsync(countryId.Value))
                throw new InvalidOperationException("Invalid country selected.");
        }
    }

    private static void Validate(string name, decimal creditLimit)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Customer name is required.");
        if (creditLimit < 0)
            throw new InvalidOperationException("Credit limit cannot be negative.");
    }

    private async Task<CustomerListDto> MapListAsync(CustomerEntity c) => new()
    {
        Id           = c.Id,
        CustomerCode = c.CustomerCode,
        Name         = c.Name,
        Phone        = c.Phone,
        Email        = c.Email,
        CountryId    = c.CountryId,
        CityId       = c.CityId,
        CityName     = c.CityId.HasValue ? await _branchRepo.GetCityNameByIdAsync(c.CityId.Value) : null,
        CustomerType = c.CustomerType,
        Status       = c.Status,
        CreditLimit  = c.CreditLimit,
        IsWalkIn     = c.IsWalkIn,
        AccountId    = c.AccountId,
        CreatedAt  = c.CreatedAt
    };

    private async Task<CustomerDetailDto> MapDetailAsync(CustomerEntity c) => new()
    {
        Id             = c.Id,
        CustomerCode   = c.CustomerCode,
        Name           = c.Name,
        Phone          = c.Phone,
        Email          = c.Email,
        Address        = c.Address,
        CountryId      = c.CountryId,
        CityId         = c.CityId,
        CityName       = c.CityId.HasValue ? await _branchRepo.GetCityNameByIdAsync(c.CityId.Value) : null,
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
