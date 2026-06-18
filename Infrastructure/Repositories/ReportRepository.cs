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

        var invoiceRows = await invoiceQuery
            .Select(inv => new
            {
                CustomerId = inv.CustomerId!.Value,
                Balance = inv.GrandTotal - (
                    _db.InvoicePayments
                        .AsNoTracking()
                        .Where(p => p.BusinessId == filter.BusinessId
                                 && p.Module == InvoicePaymentModule.Sale
                                 && p.SaleInvoiceId == inv.Id
                                 && (filter.BranchId <= 0 || p.BranchId == filter.BranchId))
                        .Sum(p => (decimal?)p.Amount) ?? 0m),
                inv.SaleDate
            })
            .Where(x => x.Balance > 0)
            .ToListAsync();

        var balanceByCustomer = invoiceRows
            .GroupBy(x => x.CustomerId)
            .ToDictionary(
                g => g.Key,
                g => new PartyInvoiceBalanceAgg
                {
                    PartyId = g.Key,
                    InvoiceBalance = g.Sum(x => x.Balance),
                    OutstandingInvoices = g.Count(),
                    LastDate = g.Max(x => x.SaleDate)
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

        var customers = await customerQuery
            .Select(c => new
            {
                c.Id,
                c.CustomerCode,
                c.Name,
                c.Phone,
                c.OpeningBalance
            })
            .ToListAsync();

        var rows = customers
            .Select(c =>
            {
                balanceByCustomer.TryGetValue(c.Id, out var agg);
                var invoiceOutstanding = agg?.InvoiceBalance ?? 0m;
                return new CustomerOutstandingRowDto
                {
                    CustomerId = c.Id,
                    CustomerCode = c.CustomerCode,
                    CustomerName = c.Name,
                    Phone = c.Phone,
                    OpeningBalance = c.OpeningBalance,
                    OutstandingInvoices = agg?.OutstandingInvoices ?? 0,
                    InvoiceOutstanding = invoiceOutstanding,
                    OutstandingAmount = c.OpeningBalance + invoiceOutstanding,
                    LastSaleDate = agg?.LastDate
                };
            })
            .Where(r => r.OutstandingAmount > 0 || r.OpeningBalance > 0)
            .AsQueryable();

        rows = ApplyCustomerOutstandingSort(rows, filter.SortColumn, filter.IsDescending());

        return PaginateList(rows.ToList(), pageNumber, pageSize);
    }

    public async Task<ReportPagedResultDto<SupplierPayableRowDto>> GetSupplierPayableReportAsync(ReportFilterDto filter)
    {
        var (pageNumber, pageSize) = filter.Normalize();

        var purchaseQuery = _db.Purchases
            .AsNoTracking()
            .Where(p => p.BusinessId == filter.BusinessId
                     && !p.IsDeleted
                     && p.Status == PurchaseStatus.Posted);

        if (filter.BranchId > 0)
            purchaseQuery = purchaseQuery.Where(p => p.BranchId == filter.BranchId);

        if (filter.SupplierId is > 0)
            purchaseQuery = purchaseQuery.Where(p => p.SupplierId == filter.SupplierId);

        var purchaseRows = await purchaseQuery
            .Select(pur => new
            {
                pur.SupplierId,
                Balance = pur.TotalAmount - (
                    _db.InvoicePayments
                        .AsNoTracking()
                        .Where(p => p.BusinessId == filter.BusinessId
                                 && p.Module == InvoicePaymentModule.Purchase
                                 && p.PurchaseId == pur.Id
                                 && (filter.BranchId <= 0 || p.BranchId == filter.BranchId))
                        .Sum(p => (decimal?)p.Amount) ?? 0m),
                pur.PurchaseDate
            })
            .Where(x => x.Balance > 0)
            .ToListAsync();

        var balanceBySupplier = purchaseRows
            .GroupBy(x => x.SupplierId)
            .ToDictionary(
                g => g.Key,
                g => new PartyInvoiceBalanceAgg
                {
                    PartyId = g.Key,
                    InvoiceBalance = g.Sum(x => x.Balance),
                    OutstandingInvoices = g.Count(),
                    LastDate = g.Max(x => x.PurchaseDate)
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

        var suppliers = await supplierQuery
            .Select(s => new
            {
                s.Id,
                s.SupplierCode,
                s.Name,
                s.Phone
            })
            .ToListAsync();

        var rows = suppliers
            .Where(s => balanceBySupplier.ContainsKey(s.Id))
            .Select(s =>
            {
                var agg = balanceBySupplier[s.Id];
                return new SupplierPayableRowDto
                {
                    SupplierId = s.Id,
                    SupplierCode = s.SupplierCode,
                    SupplierName = s.Name,
                    Phone = s.Phone,
                    OutstandingInvoices = agg.OutstandingInvoices,
                    InvoicePayable = agg.InvoiceBalance,
                    PayableAmount = agg.InvoiceBalance,
                    LastPurchaseDate = agg.LastDate
                };
            })
            .AsQueryable();

        rows = ApplySupplierPayableSort(rows, filter.SortColumn, filter.IsDescending());

        return PaginateList(rows.ToList(), pageNumber, pageSize);
    }

    public async Task<ProfitLossReportPagedResultDto> GetProfitLossReportAsync(ReportFilterDto filter)
    {
        var (pageNumber, pageSize) = filter.Normalize();
        var (from, toExclusive) = filter.ResolveDateRange();

        var rows = await BuildProfitLossDailyRowsAsync(filter, from, toExclusive);
        rows = ApplyProfitLossGroupBy(rows, filter.GroupBy);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            rows = rows.Where(r => r.Date.ToString("yyyy-MM-dd").Contains(term)
                              || r.Date.ToString("yyyy-MM").Contains(term)).ToList();
        }

        rows = ApplyProfitLossSort(rows, filter.SortColumn, filter.IsDescending());

        var summary = BuildProfitLossSummary(rows);

        var totalRecords = rows.Count;
        var paged = rows
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = ReportPagedResultDto<ProfitLossRowDto>.Create(paged, totalRecords, pageNumber, pageSize);
        return new ProfitLossReportPagedResultDto
        {
            Data = result.Data,
            TotalRecords = result.TotalRecords,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            Summary = summary,
        };
    }

    public async Task<ProfitLossStatementDto> GetProfitLossStatementAsync(ReportFilterDto filter)
    {
        var (from, toExclusive) = filter.ResolveDateRange();
        var toInclusive = toExclusive.AddDays(-1);

        var rows = await BuildProfitLossDailyRowsAsync(filter, from, toExclusive);
        var summary = BuildProfitLossSummary(rows);

        var expenseQuery = _db.Expenses
            .AsNoTracking()
            .Where(e => e.BusinessId == filter.BusinessId
                     && !e.IsDeleted
                     && e.ExpenseDate >= from
                     && e.ExpenseDate < toExclusive);

        if (filter.BranchId > 0)
            expenseQuery = expenseQuery.Where(e => e.BranchId == filter.BranchId);

        var expenseLines = await expenseQuery
            .GroupBy(e => new { e.ExpenseCategoryId, CategoryName = e.ExpenseCategory.Name })
            .Select(g => new ProfitLossExpenseLineDto
            {
                CategoryId = g.Key.ExpenseCategoryId,
                CategoryName = g.Key.CategoryName,
                Amount = g.Sum(x => x.Amount),
            })
            .OrderByDescending(x => x.Amount)
            .ThenBy(x => x.CategoryName)
            .ToListAsync();

        var branchName = filter.BranchId > 0
            ? await _db.Branches.AsNoTracking()
                .Where(b => b.Id == filter.BranchId)
                .Select(b => b.Name)
                .FirstOrDefaultAsync() ?? "Unknown"
            : "All Branches";

        return new ProfitLossStatementDto
        {
            BranchId = filter.BranchId,
            BranchName = branchName,
            FromDate = from,
            ToDate = toInclusive,
            Summary = summary,
            ExpenseLines = expenseLines,
        };
    }

    private async Task<List<ProfitLossRowDto>> BuildProfitLossDailyRowsAsync(
        ReportFilterDto filter, DateTime from, DateTime toExclusive)
    {
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

        return rows;
    }

    private static ProfitLossReportSummaryDto BuildProfitLossSummary(List<ProfitLossRowDto> rows) =>
        new()
        {
            TotalRevenue = rows.Sum(r => r.Revenue),
            TotalDiscounts = rows.Sum(r => r.Discounts),
            TotalTax = rows.Sum(r => r.Tax),
            TotalCostOfGoodsSold = rows.Sum(r => r.CostOfGoodsSold),
            TotalGrossProfit = rows.Sum(r => r.GrossProfit),
            TotalExpenses = rows.Sum(r => r.Expenses),
            TotalNetProfit = rows.Sum(r => r.NetProfit),
            TotalSalesCount = rows.Sum(r => r.SalesCount),
        };

    private static List<ProfitLossRowDto> ApplyProfitLossGroupBy(List<ProfitLossRowDto> rows, string? groupBy)
    {
        return (groupBy ?? "day").ToLowerInvariant() switch
        {
            "month" => rows
                .GroupBy(r => new DateTime(r.Date.Year, r.Date.Month, 1))
                .Select(g => new ProfitLossRowDto
                {
                    Date = g.Key,
                    Revenue = g.Sum(r => r.Revenue),
                    Discounts = g.Sum(r => r.Discounts),
                    Tax = g.Sum(r => r.Tax),
                    CostOfGoodsSold = g.Sum(r => r.CostOfGoodsSold),
                    GrossProfit = g.Sum(r => r.GrossProfit),
                    Expenses = g.Sum(r => r.Expenses),
                    NetProfit = g.Sum(r => r.NetProfit),
                    SalesCount = g.Sum(r => r.SalesCount),
                })
                .ToList(),
            "year" => rows
                .GroupBy(r => new DateTime(r.Date.Year, 1, 1))
                .Select(g => new ProfitLossRowDto
                {
                    Date = g.Key,
                    Revenue = g.Sum(r => r.Revenue),
                    Discounts = g.Sum(r => r.Discounts),
                    Tax = g.Sum(r => r.Tax),
                    CostOfGoodsSold = g.Sum(r => r.CostOfGoodsSold),
                    GrossProfit = g.Sum(r => r.GrossProfit),
                    Expenses = g.Sum(r => r.Expenses),
                    NetProfit = g.Sum(r => r.NetProfit),
                    SalesCount = g.Sum(r => r.SalesCount),
                })
                .ToList(),
            _ => rows,
        };
    }

    public async Task<ProductWiseSalesReportPagedResultDto> GetProductWiseSalesReportAsync(ReportFilterDto filter)
    {
        var (pageNumber, pageSize) = filter.Normalize();
        var (from, toExclusive) = filter.ResolveDateRange();

        var itemQuery = _db.SaleInvoiceItems
            .AsNoTracking()
            .Where(i => i.BusinessId == filter.BusinessId
                     && !i.IsDeleted
                     && !i.SaleInvoice.IsDeleted
                     && i.SaleInvoice.Status == SaleInvoiceStatus.Completed
                     && i.SaleInvoice.SaleDate >= from
                     && i.SaleInvoice.SaleDate < toExclusive);

        if (filter.BranchId > 0)
            itemQuery = itemQuery.Where(i => i.BranchId == filter.BranchId);

        if (filter.ProductId is > 0)
            itemQuery = itemQuery.Where(i => i.ProductId == filter.ProductId);

        if (filter.CategoryId is > 0)
            itemQuery = itemQuery.Where(i => i.Product.CategoryId == filter.CategoryId);

        if (filter.SubCategoryId is > 0)
            itemQuery = itemQuery.Where(i => i.Product.SubCategoryId == filter.SubCategoryId);

        if (filter.BrandId is > 0)
            itemQuery = itemQuery.Where(i => i.Product.BrandId == filter.BrandId);

        var totalInvoices = await itemQuery
            .Select(i => i.SaleInvoiceId)
            .Distinct()
            .CountAsync();

        var grouped = itemQuery
            .GroupBy(i => new
            {
                i.ProductId,
                i.Product.ProductName,
                i.Product.ProductCode,
                i.Product.SKU,
                i.Product.CategoryId,
                CategoryName = i.Product.Category.Name,
                i.Product.SubCategoryId,
                SubCategoryName = i.Product.SubCategory != null ? i.Product.SubCategory.Name : null,
                i.Product.BrandId,
                BrandName = i.Product.Brand != null ? i.Product.Brand.Name : null,
            })
            .Select(g => new ProductWiseSalesReportRowDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                ProductCode = g.Key.ProductCode,
                Sku = g.Key.SKU,
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.CategoryName,
                SubCategoryId = g.Key.SubCategoryId,
                SubCategoryName = g.Key.SubCategoryName,
                BrandId = g.Key.BrandId,
                BrandName = g.Key.BrandName,
                TotalQuantity = g.Sum(x => x.Quantity),
                TotalBaseQuantity = g.Sum(x => x.BaseQuantity),
                TotalAmount = g.Sum(x => x.LineTotal),
                TotalDiscount = g.Sum(x => x.DiscountAmount),
                TotalTax = g.Sum(x => x.TaxAmount),
                TotalCost = g.Sum(x => x.BaseQuantity * x.Product.CostPrice),
                GrossProfit = g.Sum(x => x.LineTotal) - g.Sum(x => x.BaseQuantity * x.Product.CostPrice),
                InvoiceCount = g.Select(x => x.SaleInvoiceId).Distinct().Count(),
            });

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            grouped = grouped.Where(r =>
                r.ProductName.ToLower().Contains(term) ||
                r.ProductCode.ToLower().Contains(term) ||
                r.Sku.ToLower().Contains(term) ||
                r.CategoryName.ToLower().Contains(term) ||
                (r.SubCategoryName != null && r.SubCategoryName.ToLower().Contains(term)) ||
                (r.BrandName != null && r.BrandName.ToLower().Contains(term)));
        }

        var summaryRow = await grouped
            .GroupBy(_ => 1)
            .Select(g => new ProductWiseSalesReportSummaryDto
            {
                TotalProducts = g.Count(),
                TotalQuantity = g.Sum(x => x.TotalQuantity),
                TotalAmount = g.Sum(x => x.TotalAmount),
                TotalDiscount = g.Sum(x => x.TotalDiscount),
                TotalTax = g.Sum(x => x.TotalTax),
                TotalCost = g.Sum(x => x.TotalCost),
                GrossProfit = g.Sum(x => x.GrossProfit),
                TotalInvoices = totalInvoices,
            })
            .FirstOrDefaultAsync() ?? new ProductWiseSalesReportSummaryDto { TotalInvoices = totalInvoices };

        grouped = ApplyProductWiseSalesSort(grouped, filter.SortColumn, filter.IsDescending());
        var paged = await PaginateAsync(grouped, pageNumber, pageSize);

        return new ProductWiseSalesReportPagedResultDto
        {
            Data = paged.Data,
            TotalRecords = paged.TotalRecords,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalPages = paged.TotalPages,
            Summary = summaryRow,
        };
    }

    public async Task<AgingReportPagedResultDto<ReceivableAgingRowDto>> GetReceivableAgingReportAsync(ReportFilterDto filter)
    {
        var (pageNumber, pageSize) = filter.Normalize();
        var asOfDate = (filter.ToDate ?? DateTime.UtcNow).Date;

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

        if (filter.FromDate.HasValue)
            invoiceQuery = invoiceQuery.Where(i => i.SaleDate >= filter.FromDate.Value.Date);

        if (filter.ToDate.HasValue)
            invoiceQuery = invoiceQuery.Where(i => i.SaleDate < filter.ToDate.Value.Date.AddDays(1));

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            invoiceQuery = invoiceQuery.Where(i =>
                i.InvoiceNo.ToLower().Contains(term) ||
                (i.Customer != null && i.Customer.Name.ToLower().Contains(term)));
        }

        var invoiceRows = await invoiceQuery
            .Select(inv => new
            {
                inv.Id,
                inv.InvoiceNo,
                InvoiceDate = inv.SaleDate,
                CustomerId = inv.CustomerId!.Value,
                CustomerName = inv.Customer != null ? inv.Customer.Name : "Unknown",
                TotalAmount = inv.GrandTotal,
                PaidAmount = _db.InvoicePayments
                    .AsNoTracking()
                    .Where(p => p.BusinessId == filter.BusinessId
                             && p.Module == InvoicePaymentModule.Sale
                             && p.SaleInvoiceId == inv.Id
                             && (filter.BranchId <= 0 || p.BranchId == filter.BranchId))
                    .Sum(p => (decimal?)p.Amount) ?? 0m
            })
            .ToListAsync();

        var allRows = invoiceRows
            .Select(r => BuildReceivableAgingRow(r.Id, r.CustomerId, r.CustomerName, r.InvoiceNo, r.InvoiceDate, r.TotalAmount, r.PaidAmount, asOfDate))
            .Where(r => r.Outstanding > 0)
            .ToList();

        var summary = BuildAgingSummary(allRows.Select(r => (r.Outstanding, r.AgingBucket)), asOfDate);

        var filtered = allRows
            .Where(r => MatchesAgingBucketFilter(r.DaysOverdue, filter.AgingBucket))
            .AsQueryable();

        filtered = ApplyReceivableAgingSort(filtered, filter.SortColumn, filter.IsDescending());
        var paged = PaginateList(filtered.ToList(), pageNumber, pageSize);

        return new AgingReportPagedResultDto<ReceivableAgingRowDto>
        {
            Data = paged.Data,
            TotalRecords = paged.TotalRecords,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalPages = paged.TotalPages,
            Summary = summary
        };
    }

    public async Task<AgingReportPagedResultDto<PayableAgingRowDto>> GetPayableAgingReportAsync(ReportFilterDto filter)
    {
        var (pageNumber, pageSize) = filter.Normalize();
        var asOfDate = (filter.ToDate ?? DateTime.UtcNow).Date;

        var purchaseQuery = _db.Purchases
            .AsNoTracking()
            .Where(p => p.BusinessId == filter.BusinessId
                     && !p.IsDeleted
                     && p.Status == PurchaseStatus.Posted);

        if (filter.BranchId > 0)
            purchaseQuery = purchaseQuery.Where(p => p.BranchId == filter.BranchId);

        if (filter.SupplierId is > 0)
            purchaseQuery = purchaseQuery.Where(p => p.SupplierId == filter.SupplierId);

        if (filter.FromDate.HasValue)
            purchaseQuery = purchaseQuery.Where(p => p.PurchaseDate >= filter.FromDate.Value.Date);

        if (filter.ToDate.HasValue)
            purchaseQuery = purchaseQuery.Where(p => p.PurchaseDate < filter.ToDate.Value.Date.AddDays(1));

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            purchaseQuery = purchaseQuery.Where(p =>
                p.InvoiceNo.ToLower().Contains(term) ||
                p.Supplier.Name.ToLower().Contains(term));
        }

        var purchaseRows = await purchaseQuery
            .Select(pur => new
            {
                pur.Id,
                pur.InvoiceNo,
                InvoiceDate = pur.PurchaseDate,
                pur.SupplierId,
                SupplierName = pur.Supplier.Name,
                TotalAmount = pur.TotalAmount,
                PaidAmount = _db.InvoicePayments
                    .AsNoTracking()
                    .Where(p => p.BusinessId == filter.BusinessId
                             && p.Module == InvoicePaymentModule.Purchase
                             && p.PurchaseId == pur.Id
                             && (filter.BranchId <= 0 || p.BranchId == filter.BranchId))
                    .Sum(p => (decimal?)p.Amount) ?? 0m
            })
            .ToListAsync();

        var allRows = purchaseRows
            .Select(r => BuildPayableAgingRow(r.Id, r.SupplierId, r.SupplierName, r.InvoiceNo, r.InvoiceDate, r.TotalAmount, r.PaidAmount, asOfDate))
            .Where(r => r.Outstanding > 0)
            .ToList();

        var summary = BuildAgingSummary(allRows.Select(r => (r.Outstanding, r.AgingBucket)), asOfDate);

        var filtered = allRows
            .Where(r => MatchesAgingBucketFilter(r.DaysOverdue, filter.AgingBucket))
            .AsQueryable();

        filtered = ApplyPayableAgingSort(filtered, filter.SortColumn, filter.IsDescending());
        var paged = PaginateList(filtered.ToList(), pageNumber, pageSize);

        return new AgingReportPagedResultDto<PayableAgingRowDto>
        {
            Data = paged.Data,
            TotalRecords = paged.TotalRecords,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalPages = paged.TotalPages,
            Summary = summary
        };
    }

    private static ReceivableAgingRowDto BuildReceivableAgingRow(
        int invoiceId, int customerId, string customerName, string invoiceNo,
        DateTime invoiceDate, decimal totalAmount, decimal paidAmount, DateTime asOfDate)
    {
        var outstanding = totalAmount - paidAmount;
        var daysOverdue = CalculateDaysOverdue(invoiceDate, asOfDate);
        return new ReceivableAgingRowDto
        {
            InvoiceId = invoiceId,
            CustomerId = customerId,
            CustomerName = customerName,
            InvoiceNo = invoiceNo,
            InvoiceDate = invoiceDate,
            TotalAmount = totalAmount,
            PaidAmount = paidAmount,
            Outstanding = outstanding,
            DaysOverdue = daysOverdue,
            AgingBucket = ResolveAgingBucket(daysOverdue)
        };
    }

    private static PayableAgingRowDto BuildPayableAgingRow(
        int invoiceId, int supplierId, string supplierName, string invoiceNo,
        DateTime invoiceDate, decimal totalAmount, decimal paidAmount, DateTime asOfDate)
    {
        var outstanding = totalAmount - paidAmount;
        var daysOverdue = CalculateDaysOverdue(invoiceDate, asOfDate);
        return new PayableAgingRowDto
        {
            InvoiceId = invoiceId,
            SupplierId = supplierId,
            SupplierName = supplierName,
            InvoiceNo = invoiceNo,
            InvoiceDate = invoiceDate,
            TotalAmount = totalAmount,
            PaidAmount = paidAmount,
            Outstanding = outstanding,
            DaysOverdue = daysOverdue,
            AgingBucket = ResolveAgingBucket(daysOverdue)
        };
    }

    private static int CalculateDaysOverdue(DateTime invoiceDate, DateTime asOfDate)
    {
        var days = (asOfDate - invoiceDate.Date).Days;
        return Math.Max(0, days);
    }

    private static string ResolveAgingBucket(int daysOverdue) => daysOverdue switch
    {
        <= 30 => "0-30",
        <= 60 => "31-60",
        <= 90 => "61-90",
        _ => "90+"
    };

    private static bool MatchesAgingBucketFilter(int daysOverdue, string? bucketFilter)
    {
        if (string.IsNullOrWhiteSpace(bucketFilter))
            return true;

        var normalized = bucketFilter.Trim().ToLowerInvariant();
        var bucket = ResolveAgingBucket(daysOverdue);
        return normalized switch
        {
            "0-30" or "0to30" or "030" => bucket == "0-30",
            "31-60" or "31to60" or "3160" => bucket == "31-60",
            "61-90" or "61to90" or "6190" => bucket == "61-90",
            "90+" or "90plus" or "90 plus" => bucket == "90+",
            _ => bucket.Equals(bucketFilter, StringComparison.OrdinalIgnoreCase)
        };
    }

    private static AgingReportSummaryDto BuildAgingSummary(
        IEnumerable<(decimal Outstanding, string Bucket)> rows, DateTime asOfDate)
    {
        var list = rows.ToList();
        return new AgingReportSummaryDto
        {
            AsOfDate = asOfDate,
            TotalOutstanding = list.Sum(r => r.Outstanding),
            Bucket0To30 = list.Where(r => r.Bucket == "0-30").Sum(r => r.Outstanding),
            Bucket31To60 = list.Where(r => r.Bucket == "31-60").Sum(r => r.Outstanding),
            Bucket61To90 = list.Where(r => r.Bucket == "61-90").Sum(r => r.Outstanding),
            Bucket90Plus = list.Where(r => r.Bucket == "90+").Sum(r => r.Outstanding)
        };
    }

    private static IQueryable<ReceivableAgingRowDto> ApplyReceivableAgingSort(
        IQueryable<ReceivableAgingRowDto> query, string? sortColumn, bool descending)
    {
        return (sortColumn ?? "daysOverdue").ToLowerInvariant() switch
        {
            "customername" or "customer" => descending
                ? query.OrderByDescending(r => r.CustomerName).ThenByDescending(r => r.DaysOverdue)
                : query.OrderBy(r => r.CustomerName).ThenBy(r => r.DaysOverdue),
            "invoiceno" or "invoice" => descending
                ? query.OrderByDescending(r => r.InvoiceNo)
                : query.OrderBy(r => r.InvoiceNo),
            "invoicedate" or "date" => descending
                ? query.OrderByDescending(r => r.InvoiceDate)
                : query.OrderBy(r => r.InvoiceDate),
            "totalamount" or "total" => descending
                ? query.OrderByDescending(r => r.TotalAmount)
                : query.OrderBy(r => r.TotalAmount),
            "paidamount" or "paid" => descending
                ? query.OrderByDescending(r => r.PaidAmount)
                : query.OrderBy(r => r.PaidAmount),
            "outstanding" or "balance" => descending
                ? query.OrderByDescending(r => r.Outstanding)
                : query.OrderBy(r => r.Outstanding),
            "agingbucket" or "bucket" => descending
                ? query.OrderByDescending(r => r.DaysOverdue)
                : query.OrderBy(r => r.DaysOverdue),
            _ => descending
                ? query.OrderByDescending(r => r.DaysOverdue).ThenByDescending(r => r.InvoiceDate)
                : query.OrderBy(r => r.DaysOverdue).ThenBy(r => r.InvoiceDate)
        };
    }

    private static IQueryable<PayableAgingRowDto> ApplyPayableAgingSort(
        IQueryable<PayableAgingRowDto> query, string? sortColumn, bool descending)
    {
        return (sortColumn ?? "daysOverdue").ToLowerInvariant() switch
        {
            "suppliername" or "supplier" => descending
                ? query.OrderByDescending(r => r.SupplierName).ThenByDescending(r => r.DaysOverdue)
                : query.OrderBy(r => r.SupplierName).ThenBy(r => r.DaysOverdue),
            "invoiceno" or "invoice" => descending
                ? query.OrderByDescending(r => r.InvoiceNo)
                : query.OrderBy(r => r.InvoiceNo),
            "invoicedate" or "date" => descending
                ? query.OrderByDescending(r => r.InvoiceDate)
                : query.OrderBy(r => r.InvoiceDate),
            "totalamount" or "total" => descending
                ? query.OrderByDescending(r => r.TotalAmount)
                : query.OrderBy(r => r.TotalAmount),
            "paidamount" or "paid" => descending
                ? query.OrderByDescending(r => r.PaidAmount)
                : query.OrderBy(r => r.PaidAmount),
            "outstanding" or "balance" => descending
                ? query.OrderByDescending(r => r.Outstanding)
                : query.OrderBy(r => r.Outstanding),
            "agingbucket" or "bucket" => descending
                ? query.OrderByDescending(r => r.DaysOverdue)
                : query.OrderBy(r => r.DaysOverdue),
            _ => descending
                ? query.OrderByDescending(r => r.DaysOverdue).ThenByDescending(r => r.InvoiceDate)
                : query.OrderBy(r => r.DaysOverdue).ThenBy(r => r.InvoiceDate)
        };
    }

    private static ReportPagedResultDto<T> PaginateList<T>(
        List<T> rows, int pageNumber, int pageSize)
    {
        var totalRecords = rows.Count;
        var paged = rows
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return ReportPagedResultDto<T>.Create(paged, totalRecords, pageNumber, pageSize);
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

    private static IQueryable<ProductWiseSalesReportRowDto> ApplyProductWiseSalesSort(
        IQueryable<ProductWiseSalesReportRowDto> query, string? sortColumn, bool descending)
    {
        return (sortColumn ?? "totalAmount").ToLowerInvariant() switch
        {
            "productname" or "product" => descending
                ? query.OrderByDescending(r => r.ProductName)
                : query.OrderBy(r => r.ProductName),
            "productcode" or "code" => descending
                ? query.OrderByDescending(r => r.ProductCode)
                : query.OrderBy(r => r.ProductCode),
            "sku" => descending
                ? query.OrderByDescending(r => r.Sku)
                : query.OrderBy(r => r.Sku),
            "categoryname" or "category" => descending
                ? query.OrderByDescending(r => r.CategoryName)
                : query.OrderBy(r => r.CategoryName),
            "subcategoryname" or "subcategory" => descending
                ? query.OrderByDescending(r => r.SubCategoryName)
                : query.OrderBy(r => r.SubCategoryName),
            "brandname" or "brand" => descending
                ? query.OrderByDescending(r => r.BrandName)
                : query.OrderBy(r => r.BrandName),
            "totalquantity" or "quantity" or "qty" => descending
                ? query.OrderByDescending(r => r.TotalQuantity)
                : query.OrderBy(r => r.TotalQuantity),
            "totalamount" or "amount" or "sales" => descending
                ? query.OrderByDescending(r => r.TotalAmount)
                : query.OrderBy(r => r.TotalAmount),
            "totaldiscount" or "discount" => descending
                ? query.OrderByDescending(r => r.TotalDiscount)
                : query.OrderBy(r => r.TotalDiscount),
            "totaltax" or "tax" => descending
                ? query.OrderByDescending(r => r.TotalTax)
                : query.OrderBy(r => r.TotalTax),
            "totalcost" or "cost" => descending
                ? query.OrderByDescending(r => r.TotalCost)
                : query.OrderBy(r => r.TotalCost),
            "grossprofit" or "profit" => descending
                ? query.OrderByDescending(r => r.GrossProfit)
                : query.OrderBy(r => r.GrossProfit),
            "invoicecount" or "invoices" => descending
                ? query.OrderByDescending(r => r.InvoiceCount)
                : query.OrderBy(r => r.InvoiceCount),
            _ => descending
                ? query.OrderByDescending(r => r.TotalAmount).ThenByDescending(r => r.ProductName)
                : query.OrderBy(r => r.TotalAmount).ThenBy(r => r.ProductName),
        };
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

    private sealed class PartyInvoiceBalanceAgg
    {
        public int PartyId { get; init; }
        public decimal InvoiceBalance { get; init; }
        public int OutstandingInvoices { get; init; }
        public DateTime LastDate { get; init; }
    }
}
