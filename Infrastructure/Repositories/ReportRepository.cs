using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Reports.DTOs;
using POSSystem.Application.Reports.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly POSDbContext _db;

    public ReportRepository(POSDbContext db) => _db = db;

    public async Task<ReportPagedResultDto<SalesReportRowDto>> GetSalesReportAsync(ReportFilterDto filter)
    {
        var (pageNumber, pageSize) = filter.Normalize();
        var (from, toExclusive) = filter.ResolveDateRange();

        var query = _db.SaleInvoices
            .AsNoTracking()
            .Where(i => i.BusinessId == filter.BusinessId
                     && !i.IsDeleted
                     && i.Status == SaleInvoiceStatus.Completed
                     && i.SaleDate >= from
                     && i.SaleDate < toExclusive);

        if (filter.BranchId > 0)
            query = query.Where(i => i.BranchId == filter.BranchId);

        if (filter.CustomerId is > 0)
            query = query.Where(i => i.CustomerId == filter.CustomerId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(i =>
                i.InvoiceNo.ToLower().Contains(term) ||
                (i.Customer != null && i.Customer.Name.ToLower().Contains(term)) ||
                (i.CashierName != null && i.CashierName.ToLower().Contains(term)));
        }

        var projected = query.Select(i => new SalesReportRowDto
        {
            Id = i.Id,
            InvoiceNo = i.InvoiceNo,
            SaleDate = i.SaleDate,
            CustomerId = i.CustomerId,
            CustomerName = i.Customer != null ? i.Customer.Name : "Walk-in",
            SubTotal = i.SubTotal,
            DiscountAmount = i.DiscountAmount,
            TaxAmount = i.TaxAmount,
            GrandTotal = i.GrandTotal,
            PaidAmount = i.PaidAmount,
            BalanceDue = i.GrandTotal - i.PaidAmount,
            PaymentMethod = i.PaymentMethod.ToString(),
            IsCreditSale = i.IsCreditSale,
            CashAmount = i.CashAmount,
            CardAmount = i.CardAmount,
            Status = i.Status.ToString(),
            CashierName = i.CashierName
        });

        projected = ApplySalesSort(projected, filter.SortColumn, filter.IsDescending());

        return await PaginateAsync(projected, pageNumber, pageSize);
    }

    public async Task<ReportPagedResultDto<PurchaseReportRowDto>> GetPurchaseReportAsync(ReportFilterDto filter)
    {
        var (pageNumber, pageSize) = filter.Normalize();
        var (from, toExclusive) = filter.ResolveDateRange();

        var query = _db.Purchases
            .AsNoTracking()
            .Where(p => p.BusinessId == filter.BusinessId
                     && !p.IsDeleted
                     && p.Status == PurchaseStatus.Posted
                     && p.PurchaseDate >= from
                     && p.PurchaseDate < toExclusive);

        if (filter.BranchId > 0)
            query = query.Where(p => p.BranchId == filter.BranchId);

        if (filter.SupplierId is > 0)
            query = query.Where(p => p.SupplierId == filter.SupplierId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(p =>
                p.InvoiceNo.ToLower().Contains(term) ||
                p.Supplier.Name.ToLower().Contains(term));
        }

        var projected = query.Select(p => new PurchaseReportRowDto
        {
            Id = p.Id,
            InvoiceNo = p.InvoiceNo,
            PurchaseDate = p.PurchaseDate,
            SupplierId = p.SupplierId,
            SupplierName = p.Supplier.Name,
            TotalAmount = p.TotalAmount,
            PaidAmount = p.Payments.Sum(x => x.Amount),
            BalanceDue = p.TotalAmount - p.Payments.Sum(x => x.Amount),
            Status = p.Status.ToString(),
            IsCreditPurchase = p.IsCreditPurchase
        });

        projected = ApplyPurchaseSort(projected, filter.SortColumn, filter.IsDescending());

        return await PaginateAsync(projected, pageNumber, pageSize);
    }

    public async Task<ReportPagedResultDto<CustomerOutstandingRowDto>> GetCustomerOutstandingReportAsync(ReportFilterDto filter)
    {
        var (pageNumber, pageSize) = filter.Normalize();

        var paidByInvoice = _db.InvoicePayments
            .AsNoTracking()
            .Where(p => p.BusinessId == filter.BusinessId
                     && p.Module == InvoicePaymentModule.Sale
                     && p.SaleInvoiceId != null);

        if (filter.BranchId > 0)
            paidByInvoice = paidByInvoice.Where(p => p.BranchId == filter.BranchId);

        var paidTotals = paidByInvoice
            .GroupBy(p => p.SaleInvoiceId!.Value)
            .Select(g => new { InvoiceId = g.Key, Paid = g.Sum(x => x.Amount) });

        var invoiceQuery = _db.SaleInvoices
            .AsNoTracking()
            .Where(i => i.BusinessId == filter.BusinessId
                     && !i.IsDeleted
                     && i.Status == SaleInvoiceStatus.Completed
                     && i.CustomerId != null);

        if (filter.BranchId > 0)
            invoiceQuery = invoiceQuery.Where(i => i.BranchId == filter.BranchId);

        if (filter.CustomerId is > 0)
            invoiceQuery = invoiceQuery.Where(i => i.CustomerId == filter.CustomerId);

        var invoiceBalances = invoiceQuery
            .GroupJoin(
                paidTotals,
                inv => inv.Id,
                paid => paid.InvoiceId,
                (inv, paidGroup) => new { inv, paidGroup })
            .SelectMany(
                x => x.paidGroup.DefaultIfEmpty(),
                (x, paid) => new
                {
                    x.inv.CustomerId,
                    Balance = x.inv.GrandTotal - (paid != null ? paid.Paid : 0m),
                    x.inv.SaleDate
                })
            .Where(x => x.Balance > 0)
            .GroupBy(x => x.CustomerId!.Value)
            .Select(g => new
            {
                CustomerId = g.Key,
                InvoiceOutstanding = g.Sum(x => x.Balance),
                OutstandingInvoices = g.Count(),
                LastSaleDate = g.Max(x => x.SaleDate)
            });

        var customerQuery = _db.Customers
            .AsNoTracking()
            .Where(c => c.BusinessId == filter.BusinessId && !c.IsDeleted);

        if (filter.BranchId > 0)
            customerQuery = customerQuery.Where(c => c.BranchId == filter.BranchId);

        if (filter.CustomerId is > 0)
            customerQuery = customerQuery.Where(c => c.Id == filter.CustomerId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            customerQuery = customerQuery.Where(c =>
                c.Name.ToLower().Contains(term) ||
                c.CustomerCode.ToLower().Contains(term) ||
                (c.Phone != null && c.Phone.Contains(filter.Search!.Trim())));
        }

        var projected = customerQuery
            .GroupJoin(
                invoiceBalances,
                c => c.Id,
                b => b.CustomerId,
                (c, balanceGroup) => new { c, balanceGroup })
            .SelectMany(
                x => x.balanceGroup.DefaultIfEmpty(),
                (x, balance) => new CustomerOutstandingRowDto
                {
                    CustomerId = x.c.Id,
                    CustomerCode = x.c.CustomerCode,
                    CustomerName = x.c.Name,
                    Phone = x.c.Phone,
                    OpeningBalance = x.c.OpeningBalance,
                    OutstandingInvoices = balance != null ? balance.OutstandingInvoices : 0,
                    InvoiceOutstanding = balance != null ? balance.InvoiceOutstanding : 0m,
                    OutstandingAmount = x.c.OpeningBalance + (balance != null ? balance.InvoiceOutstanding : 0m),
                    LastSaleDate = balance != null ? balance.LastSaleDate : null
                })
            .Where(r => r.OutstandingAmount > 0 || r.OpeningBalance > 0);

        projected = ApplyCustomerOutstandingSort(projected, filter.SortColumn, filter.IsDescending());

        return await PaginateAsync(projected, pageNumber, pageSize);
    }

    public async Task<ReportPagedResultDto<SupplierPayableRowDto>> GetSupplierPayableReportAsync(ReportFilterDto filter)
    {
        var (pageNumber, pageSize) = filter.Normalize();

        var paidByPurchase = _db.InvoicePayments
            .AsNoTracking()
            .Where(p => p.BusinessId == filter.BusinessId
                     && p.Module == InvoicePaymentModule.Purchase
                     && p.PurchaseId != null);

        if (filter.BranchId > 0)
            paidByPurchase = paidByPurchase.Where(p => p.BranchId == filter.BranchId);

        var paidTotals = paidByPurchase
            .GroupBy(p => p.PurchaseId!.Value)
            .Select(g => new { PurchaseId = g.Key, Paid = g.Sum(x => x.Amount) });

        var purchaseQuery = _db.Purchases
            .AsNoTracking()
            .Where(p => p.BusinessId == filter.BusinessId
                     && !p.IsDeleted
                     && p.Status == PurchaseStatus.Posted);

        if (filter.BranchId > 0)
            purchaseQuery = purchaseQuery.Where(p => p.BranchId == filter.BranchId);

        if (filter.SupplierId is > 0)
            purchaseQuery = purchaseQuery.Where(p => p.SupplierId == filter.SupplierId);

        var purchaseBalances = purchaseQuery
            .GroupJoin(
                paidTotals,
                pur => pur.Id,
                paid => paid.PurchaseId,
                (pur, paidGroup) => new { pur, paidGroup })
            .SelectMany(
                x => x.paidGroup.DefaultIfEmpty(),
                (x, paid) => new
                {
                    x.pur.SupplierId,
                    Balance = x.pur.TotalAmount - (paid != null ? paid.Paid : 0m),
                    x.pur.PurchaseDate
                })
            .Where(x => x.Balance > 0)
            .GroupBy(x => x.SupplierId)
            .Select(g => new
            {
                SupplierId = g.Key,
                InvoicePayable = g.Sum(x => x.Balance),
                OutstandingInvoices = g.Count(),
                LastPurchaseDate = g.Max(x => x.PurchaseDate)
            });

        var supplierQuery = _db.Suppliers
            .AsNoTracking()
            .Where(s => s.BusinessId == filter.BusinessId && !s.IsDeleted);

        if (filter.BranchId > 0)
            supplierQuery = supplierQuery.Where(s => s.BranchId == filter.BranchId);

        if (filter.SupplierId is > 0)
            supplierQuery = supplierQuery.Where(s => s.Id == filter.SupplierId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            supplierQuery = supplierQuery.Where(s =>
                s.Name.ToLower().Contains(term) ||
                s.SupplierCode.ToLower().Contains(term) ||
                s.Phone.Contains(filter.Search!.Trim()));
        }

        var projected = supplierQuery
            .GroupJoin(
                purchaseBalances,
                s => s.Id,
                b => b.SupplierId,
                (s, balanceGroup) => new { s, balanceGroup })
            .SelectMany(
                x => x.balanceGroup.DefaultIfEmpty(),
                (x, balance) => new SupplierPayableRowDto
                {
                    SupplierId = x.s.Id,
                    SupplierCode = x.s.SupplierCode,
                    SupplierName = x.s.Name,
                    Phone = x.s.Phone,
                    OutstandingInvoices = balance != null ? balance.OutstandingInvoices : 0,
                    InvoicePayable = balance != null ? balance.InvoicePayable : 0m,
                    PayableAmount = balance != null ? balance.InvoicePayable : 0m,
                    LastPurchaseDate = balance != null ? balance.LastPurchaseDate : null
                })
            .Where(r => r.PayableAmount > 0);

        projected = ApplySupplierPayableSort(projected, filter.SortColumn, filter.IsDescending());

        return await PaginateAsync(projected, pageNumber, pageSize);
    }

    public async Task<ReportPagedResultDto<ProfitLossRowDto>> GetProfitLossReportAsync(ReportFilterDto filter)
    {
        var (pageNumber, pageSize) = filter.Normalize();
        var (from, toExclusive) = filter.ResolveDateRange();

        var salesQuery = _db.SaleInvoices
            .AsNoTracking()
            .Where(i => i.BusinessId == filter.BusinessId
                     && !i.IsDeleted
                     && i.Status == SaleInvoiceStatus.Completed
                     && i.SaleDate >= from
                     && i.SaleDate < toExclusive);

        if (filter.BranchId > 0)
            salesQuery = salesQuery.Where(i => i.BranchId == filter.BranchId);

        var salesByDay = await salesQuery
            .GroupBy(i => i.SaleDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Revenue = g.Sum(x => x.GrandTotal),
                Discounts = g.Sum(x => x.DiscountAmount),
                Tax = g.Sum(x => x.TaxAmount),
                SalesCount = g.Count()
            })
            .ToListAsync();

        var cogsQuery = _db.SaleInvoiceItems
            .AsNoTracking()
            .Where(item => item.BusinessId == filter.BusinessId
                        && !item.IsDeleted
                        && item.SaleInvoice.Status == SaleInvoiceStatus.Completed
                        && item.SaleInvoice.SaleDate >= from
                        && item.SaleInvoice.SaleDate < toExclusive);

        if (filter.BranchId > 0)
            cogsQuery = cogsQuery.Where(item => item.BranchId == filter.BranchId);

        var cogsByDay = await cogsQuery
            .GroupBy(item => item.SaleInvoice.SaleDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                CostOfGoodsSold = g.Sum(x => x.BaseQuantity * x.Product.CostPrice)
            })
            .ToListAsync();

        var expenseQuery = _db.Expenses
            .AsNoTracking()
            .Where(e => e.BusinessId == filter.BusinessId
                     && !e.IsDeleted
                     && e.ExpenseDate >= from
                     && e.ExpenseDate < toExclusive);

        if (filter.BranchId > 0)
            expenseQuery = expenseQuery.Where(e => e.BranchId == filter.BranchId);

        var expensesByDay = await expenseQuery
            .GroupBy(e => e.ExpenseDate.Date)
            .Select(g => new { Date = g.Key, Expenses = g.Sum(x => x.Amount) })
            .ToListAsync();

        var cogsMap = cogsByDay.ToDictionary(x => x.Date, x => x.CostOfGoodsSold);
        var expenseMap = expensesByDay.ToDictionary(x => x.Date, x => x.Expenses);

        var rows = salesByDay
            .Select(s =>
            {
                cogsMap.TryGetValue(s.Date, out var cogs);
                expenseMap.TryGetValue(s.Date, out var expenses);
                var grossProfit = s.Revenue - cogs;
                return new ProfitLossRowDto
                {
                    Date = s.Date,
                    Revenue = s.Revenue,
                    Discounts = s.Discounts,
                    Tax = s.Tax,
                    CostOfGoodsSold = cogs,
                    GrossProfit = grossProfit,
                    Expenses = expenses,
                    NetProfit = grossProfit - expenses,
                    SalesCount = s.SalesCount
                };
            })
            .Concat(expensesByDay
                .Where(e => salesByDay.All(s => s.Date != e.Date))
                .Select(e =>
                {
                    cogsMap.TryGetValue(e.Date, out var cogs);
                    return new ProfitLossRowDto
                    {
                        Date = e.Date,
                        Revenue = 0m,
                        Discounts = 0m,
                        Tax = 0m,
                        CostOfGoodsSold = cogs,
                        GrossProfit = -cogs,
                        Expenses = e.Expenses,
                        NetProfit = -cogs - e.Expenses,
                        SalesCount = 0
                    };
                }))
            .Concat(cogsByDay
                .Where(c => salesByDay.All(s => s.Date != c.Date) && expensesByDay.All(e => e.Date != c.Date))
                .Select(c => new ProfitLossRowDto
                {
                    Date = c.Date,
                    Revenue = 0m,
                    Discounts = 0m,
                    Tax = 0m,
                    CostOfGoodsSold = c.CostOfGoodsSold,
                    GrossProfit = -c.CostOfGoodsSold,
                    Expenses = 0m,
                    NetProfit = -c.CostOfGoodsSold,
                    SalesCount = 0
                }))
            .ToList();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            rows = rows.Where(r => r.Date.ToString("yyyy-MM-dd").Contains(term)).ToList();
        }

        rows = ApplyProfitLossSort(rows, filter.SortColumn, filter.IsDescending());

        var totalRecords = rows.Count;
        var paged = rows
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return ReportPagedResultDto<ProfitLossRowDto>.Create(paged, totalRecords, pageNumber, pageSize);
    }

    private static async Task<ReportPagedResultDto<T>> PaginateAsync<T>(
        IQueryable<T> query, int pageNumber, int pageSize)
    {
        var totalRecords = await query.CountAsync();
        var data = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return ReportPagedResultDto<T>.Create(data, totalRecords, pageNumber, pageSize);
    }

    private static IQueryable<SalesReportRowDto> ApplySalesSort(
        IQueryable<SalesReportRowDto> query, string? sortColumn, bool descending)
    {
        return (sortColumn ?? "saleDate").ToLowerInvariant() switch
        {
            "invoiceno" or "invoice" => descending
                ? query.OrderByDescending(r => r.InvoiceNo)
                : query.OrderBy(r => r.InvoiceNo),
            "customername" or "customer" => descending
                ? query.OrderByDescending(r => r.CustomerName)
                : query.OrderBy(r => r.CustomerName),
            "grandtotal" or "total" => descending
                ? query.OrderByDescending(r => r.GrandTotal)
                : query.OrderBy(r => r.GrandTotal),
            "paidamount" or "paid" => descending
                ? query.OrderByDescending(r => r.PaidAmount)
                : query.OrderBy(r => r.PaidAmount),
            "balancedue" or "balance" => descending
                ? query.OrderByDescending(r => r.BalanceDue)
                : query.OrderBy(r => r.BalanceDue),
            "paymentmethod" => descending
                ? query.OrderByDescending(r => r.PaymentMethod)
                : query.OrderBy(r => r.PaymentMethod),
            _ => descending
                ? query.OrderByDescending(r => r.SaleDate).ThenByDescending(r => r.Id)
                : query.OrderBy(r => r.SaleDate).ThenBy(r => r.Id)
        };
    }

    private static IQueryable<PurchaseReportRowDto> ApplyPurchaseSort(
        IQueryable<PurchaseReportRowDto> query, string? sortColumn, bool descending)
    {
        return (sortColumn ?? "purchaseDate").ToLowerInvariant() switch
        {
            "invoiceno" or "invoice" => descending
                ? query.OrderByDescending(r => r.InvoiceNo)
                : query.OrderBy(r => r.InvoiceNo),
            "suppliername" or "supplier" => descending
                ? query.OrderByDescending(r => r.SupplierName)
                : query.OrderBy(r => r.SupplierName),
            "totalamount" or "total" => descending
                ? query.OrderByDescending(r => r.TotalAmount)
                : query.OrderBy(r => r.TotalAmount),
            "paidamount" or "paid" => descending
                ? query.OrderByDescending(r => r.PaidAmount)
                : query.OrderBy(r => r.PaidAmount),
            "balancedue" or "balance" => descending
                ? query.OrderByDescending(r => r.BalanceDue)
                : query.OrderBy(r => r.BalanceDue),
            _ => descending
                ? query.OrderByDescending(r => r.PurchaseDate).ThenByDescending(r => r.Id)
                : query.OrderBy(r => r.PurchaseDate).ThenBy(r => r.Id)
        };
    }

    private static IQueryable<CustomerOutstandingRowDto> ApplyCustomerOutstandingSort(
        IQueryable<CustomerOutstandingRowDto> query, string? sortColumn, bool descending)
    {
        return (sortColumn ?? "outstandingAmount").ToLowerInvariant() switch
        {
            "customercode" or "code" => descending
                ? query.OrderByDescending(r => r.CustomerCode)
                : query.OrderBy(r => r.CustomerCode),
            "customername" or "customer" or "name" => descending
                ? query.OrderByDescending(r => r.CustomerName)
                : query.OrderBy(r => r.CustomerName),
            "openingbalance" => descending
                ? query.OrderByDescending(r => r.OpeningBalance)
                : query.OrderBy(r => r.OpeningBalance),
            "invoiceoutstanding" => descending
                ? query.OrderByDescending(r => r.InvoiceOutstanding)
                : query.OrderBy(r => r.InvoiceOutstanding),
            "outstandinginvoices" or "invoices" => descending
                ? query.OrderByDescending(r => r.OutstandingInvoices)
                : query.OrderBy(r => r.OutstandingInvoices),
            "lastsaledate" => descending
                ? query.OrderByDescending(r => r.LastSaleDate)
                : query.OrderBy(r => r.LastSaleDate),
            _ => descending
                ? query.OrderByDescending(r => r.OutstandingAmount).ThenByDescending(r => r.CustomerName)
                : query.OrderBy(r => r.OutstandingAmount).ThenBy(r => r.CustomerName)
        };
    }

    private static IQueryable<SupplierPayableRowDto> ApplySupplierPayableSort(
        IQueryable<SupplierPayableRowDto> query, string? sortColumn, bool descending)
    {
        return (sortColumn ?? "payableAmount").ToLowerInvariant() switch
        {
            "suppliercode" or "code" => descending
                ? query.OrderByDescending(r => r.SupplierCode)
                : query.OrderBy(r => r.SupplierCode),
            "suppliername" or "supplier" or "name" => descending
                ? query.OrderByDescending(r => r.SupplierName)
                : query.OrderBy(r => r.SupplierName),
            "invoicepayable" => descending
                ? query.OrderByDescending(r => r.InvoicePayable)
                : query.OrderBy(r => r.InvoicePayable),
            "outstandinginvoices" or "invoices" => descending
                ? query.OrderByDescending(r => r.OutstandingInvoices)
                : query.OrderBy(r => r.OutstandingInvoices),
            "lastpurchasedate" => descending
                ? query.OrderByDescending(r => r.LastPurchaseDate)
                : query.OrderBy(r => r.LastPurchaseDate),
            _ => descending
                ? query.OrderByDescending(r => r.PayableAmount).ThenByDescending(r => r.SupplierName)
                : query.OrderBy(r => r.PayableAmount).ThenBy(r => r.SupplierName)
        };
    }

    private static List<ProfitLossRowDto> ApplyProfitLossSort(
        List<ProfitLossRowDto> rows, string? sortColumn, bool descending)
    {
        return (sortColumn ?? "date").ToLowerInvariant() switch
        {
            "revenue" => descending
                ? rows.OrderByDescending(r => r.Revenue).ToList()
                : rows.OrderBy(r => r.Revenue).ToList(),
            "costofgoodssold" or "cogs" => descending
                ? rows.OrderByDescending(r => r.CostOfGoodsSold).ToList()
                : rows.OrderBy(r => r.CostOfGoodsSold).ToList(),
            "grossprofit" => descending
                ? rows.OrderByDescending(r => r.GrossProfit).ToList()
                : rows.OrderBy(r => r.GrossProfit).ToList(),
            "expenses" => descending
                ? rows.OrderByDescending(r => r.Expenses).ToList()
                : rows.OrderBy(r => r.Expenses).ToList(),
            "netprofit" or "profit" => descending
                ? rows.OrderByDescending(r => r.NetProfit).ToList()
                : rows.OrderBy(r => r.NetProfit).ToList(),
            "salescount" => descending
                ? rows.OrderByDescending(r => r.SalesCount).ToList()
                : rows.OrderBy(r => r.SalesCount).ToList(),
            _ => descending
                ? rows.OrderByDescending(r => r.Date).ToList()
                : rows.OrderBy(r => r.Date).ToList()
        };
    }
}
