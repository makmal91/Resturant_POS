using POSSystem.Application.Sales.DTOs;
using POSSystem.Application.Sales.Interfaces;
using POSSystem.Application.Stock.Interfaces;
using POSSystem.Domain;
using ProductEntity = POSSystem.Domain.Product;

namespace POSSystem.Application.Sales.Services;

public class SalesService : ISalesService
{
    private readonly ISalesRepository _salesRepository;
    private readonly IStockLedgerRepository _stockLedgerRepository;

    public SalesService(ISalesRepository salesRepository, IStockLedgerRepository stockLedgerRepository)
    {
        _salesRepository = salesRepository;
        _stockLedgerRepository = stockLedgerRepository;
    }

    public async Task<PosProductLookupDto?> GetProductByBarcodeAsync(string barcode, int businessId, int branchId)
    {
        var product = await _salesRepository.GetProductByBarcodeAsync(barcode, businessId, branchId);
        if (product == null) return null;

        var matchedBarcode = product.Barcodes.FirstOrDefault(b => b.BarcodeValue == barcode);
        return MapProductToLookup(product, matchedBarcode, barcode);
    }

    public async Task<List<PosProductLookupDto>> SearchProductsAsync(string query, int businessId, int branchId)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<PosProductLookupDto>();

        var products = await _salesRepository.SearchProductsAsync(query.Trim(), businessId, branchId, 20);
        return products.Select(p => MapProductToLookup(p, null, string.Empty)).ToList();
    }

    public async Task<List<PosSearchGroupDto>> SearchProductsGroupedAsync(
        string query, int businessId, int branchId, int? warehouseId)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<PosSearchGroupDto>();

        var products = await _salesRepository.SearchProductsAsync(query.Trim(), businessId, branchId, 20);
        if (products.Count == 0) return new List<PosSearchGroupDto>();

        // Fetch stock for all found products in one query
        var productIds = products.Select(p => p.Id).Distinct();
        var stockMap = await _stockLedgerRepository.GetStockForProductsAsync(businessId, branchId, productIds, warehouseId);

        var result = new List<PosSearchGroupDto>();
        foreach (var p in products)
        {
            var baseUnit = p.Units.FirstOrDefault(u => u.IsBaseUnit) ?? p.Units.FirstOrDefault();
            var noVariantStock = stockMap.TryGetValue($"{p.Id}:0", out var s) ? s : 0m;

            var group = new PosSearchGroupDto
            {
                ProductId      = p.Id,
                ProductName    = p.ProductName,
                ProductCode    = p.ProductCode,
                CategoryName   = p.Category?.Name ?? string.Empty,
                BrandName      = p.Brand?.Name ?? string.Empty,
                IsVariantEnabled = p.IsVariantEnabled,
                RetailPrice    = p.SellingPrice,
                WholesalePrice = p.WholesalePrice,
                Stock          = noVariantStock,
                IsDiscountAllowed = p.IsDiscountAllowed,
                DiscountType   = p.DiscountType,
                DiscountValue  = p.DiscountValue,
                Units = p.Units
                    .Where(u => !u.IsDeleted)
                    .Select(u => new PosProductUnitDto
                    {
                        UnitId = u.Id,
                        UnitName = u.UnitName,
                        SellingPrice = u.SellingPrice ?? p.SellingPrice,
                        WholesalePrice = u.WholesalePrice ?? p.WholesalePrice,
                        ConversionFactor = u.ConversionFactor,
                        IsBaseUnit = u.IsBaseUnit
                    }).ToList()
            };

            if (p.IsVariantEnabled)
            {
                group.Variants = p.Variants
                    .Where(v => !v.IsDeleted && v.Status)
                    .Select(v =>
                    {
                        var variantStock = stockMap.TryGetValue($"{p.Id}:{v.Id}", out var vs) ? vs : 0m;
                        // Find barcode for this variant
                        var barcode = p.Barcodes
                            .FirstOrDefault(b => b.ProductVariantId == v.Id && !b.IsDeleted)?.BarcodeValue
                            ?? string.Empty;

                        decimal retailPrice  = v.SellingPriceOverride ?? (p.SellingPrice + v.AdditionalPrice);
                        decimal whlsPrice    = p.WholesalePrice + v.AdditionalPrice;

                        return new PosSearchVariantRowDto
                        {
                            VariantId     = v.Id,
                            VariantName   = v.VariantName,
                            Size          = v.Size,
                            Color         = v.Color,
                            SKU           = v.SKU,
                            Barcode       = barcode,
                            RetailPrice   = retailPrice,
                            WholesalePrice = whlsPrice,
                            Stock         = variantStock
                        };
                    }).ToList();
            }

            result.Add(group);
        }

        return result;
    }

    public async Task<List<PosCustomerDto>> SearchCustomersAsync(string query, int businessId, int branchId)
    {
        var customers = await _salesRepository.SearchCustomersAsync(query, businessId, branchId, 10);
        return customers.Select(c => new PosCustomerDto
        {
            Id = c.Id,
            Name = c.Name,
            Phone = c.Phone ?? string.Empty,
            Email = c.Email ?? string.Empty
        }).ToList();
    }

    public async Task<SaleInvoiceDto> CreateSaleInvoiceAsync(CreateSaleInvoiceDto dto)
    {
        ValidateInvoiceDto(dto.BranchId, dto.WarehouseId, dto.Items);

        var invoiceNo = await GenerateInvoiceNoAsync(dto.BusinessId, dto.BranchId);
        var invoice = BuildInvoice(invoiceNo, dto);
        invoice.Status = SaleInvoiceStatus.Completed;

        await _salesRepository.AddAsync(invoice);
        await _salesRepository.SaveChangesAsync();

        // Write stock ledger — one SaleEntry (stock OUT) per invoice item
        foreach (var item in invoice.Items.Where(i => !i.IsDeleted))
        {
            await _stockLedgerRepository.AddAsync(new StockLedger
            {
                ProductId          = item.ProductId,
                VariantId          = item.VariantId,
                WarehouseId        = invoice.WarehouseId,
                Type               = StockLedgerType.SaleEntry,
                ReferenceId        = invoice.Id,
                QuantityInBaseUnit = -item.BaseQuantity,   // negative = stock OUT
                UnitPrice          = item.UnitPrice,
                TotalAmount        = item.LineTotal,
                Date               = invoice.SaleDate,
                Remarks            = $"Sale — Invoice: {invoice.InvoiceNo}",
                BusinessId         = dto.BusinessId,
                BranchId           = dto.BranchId
            });
        }
        await _stockLedgerRepository.SaveChangesAsync();

        var created = await _salesRepository.GetByIdAsync(invoice.Id, dto.BusinessId, dto.BranchId);
        return MapInvoiceDto(created!);
    }

    public async Task<SaleInvoiceDto> HoldBillAsync(HoldBillDto dto)
    {
        if (dto.BranchId <= 0) throw new InvalidOperationException("BranchId is required.");
        if (dto.Items == null || dto.Items.Count == 0) throw new InvalidOperationException("At least one item is required.");

        var invoiceNo = await GenerateInvoiceNoAsync(dto.BusinessId, dto.BranchId);
        var createDto = new CreateSaleInvoiceDto
        {
            CustomerId = dto.CustomerId,
            WarehouseId = dto.WarehouseId,
            PricingType = dto.PricingType,
            DiscountAmount = dto.DiscountAmount,
            Notes = dto.Notes,
            BusinessId = dto.BusinessId,
            BranchId = dto.BranchId,
            Items = dto.Items
        };

        var invoice = BuildInvoice(invoiceNo, createDto);
        invoice.Status = SaleInvoiceStatus.Held;
        invoice.HeldNote = dto.HeldNote;

        await _salesRepository.AddAsync(invoice);
        await _salesRepository.SaveChangesAsync();

        var created = await _salesRepository.GetByIdAsync(invoice.Id, dto.BusinessId, dto.BranchId);
        return MapInvoiceDto(created!);
    }

    public async Task<List<SaleInvoiceDto>> GetHeldBillsAsync(int businessId, int branchId)
    {
        var bills = await _salesRepository.GetHeldBillsAsync(businessId, branchId);
        return bills.Select(MapInvoiceDto).ToList();
    }

    public async Task<SaleInvoiceDto?> GetInvoiceByIdAsync(int id, int businessId, int branchId)
    {
        var invoice = await _salesRepository.GetByIdAsync(id, businessId, branchId);
        return invoice == null ? null : MapInvoiceDto(invoice);
    }

    public async Task CancelHeldBillAsync(int id, int businessId, int branchId)
    {
        var invoice = await _salesRepository.GetByIdAsync(id, businessId, branchId);
        if (invoice == null) throw new InvalidOperationException("Invoice not found.");
        if (invoice.Status != SaleInvoiceStatus.Held)
            throw new InvalidOperationException("Only held bills can be cancelled.");

        invoice.Status = SaleInvoiceStatus.Cancelled;
        invoice.UpdatedDate = DateTime.UtcNow;
        await _salesRepository.SaveChangesAsync();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<string> GenerateInvoiceNoAsync(int businessId, int branchId)
    {
        var seq = await _salesRepository.GetNextInvoiceNumberAsync(businessId, branchId);
        return $"SI-{DateTime.UtcNow:yyyyMMdd}-{seq:D5}";
    }

    private static SaleInvoice BuildInvoice(string invoiceNo, CreateSaleInvoiceDto dto)
    {
        var invoice = new SaleInvoice
        {
            InvoiceNo = invoiceNo,
            CustomerId = dto.CustomerId,
            WarehouseId = dto.WarehouseId,
            SaleDate = DateTime.UtcNow,
            PricingType = dto.PricingType,
            PaymentMethod = dto.PaymentMethod,
            PaidAmount = dto.PaidAmount,
            CashAmount = dto.CashAmount,
            CardAmount = dto.CardAmount,
            Notes = dto.Notes,
            CashierName = dto.CashierName,
            BusinessId = dto.BusinessId,
            BranchId = dto.BranchId
        };

        decimal subTotal = 0;
        decimal totalDiscount = 0;
        decimal totalTax = 0;

        foreach (var i in dto.Items)
        {
            var lineDiscount = i.DiscountAmount > 0
                ? i.DiscountAmount
                : i.DiscountPercent > 0 ? i.UnitPrice * i.Quantity * i.DiscountPercent / 100 : 0;

            var grossLine = i.UnitPrice * i.Quantity;
            var netAfterDiscount = grossLine - lineDiscount;
            var lineTax = i.TaxPercent > 0 ? netAfterDiscount * i.TaxPercent / 100 : 0;
            var lineTotal = netAfterDiscount + lineTax;

            subTotal += grossLine;
            totalDiscount += lineDiscount;
            totalTax += lineTax;

            var cf = i.ConversionFactor > 0 ? i.ConversionFactor : 1m;
            invoice.Items.Add(new SaleInvoiceItem
            {
                ProductId        = i.ProductId,
                VariantId        = i.VariantId,
                UnitId           = i.UnitId,
                Quantity         = i.Quantity,
                ConversionFactor = cf,
                BaseQuantity     = i.Quantity * cf,
                UnitPrice        = i.UnitPrice,
                DiscountPercent  = i.DiscountPercent,
                DiscountAmount   = lineDiscount,
                TaxPercent       = i.TaxPercent,
                TaxAmount        = lineTax,
                LineTotal        = lineTotal,
                ItemNote         = i.ItemNote,
                BusinessId       = dto.BusinessId,
                BranchId         = dto.BranchId
            });
        }

        // Apply overall bill discount on top of line discounts
        totalDiscount += dto.DiscountAmount;

        invoice.SubTotal = subTotal;
        invoice.DiscountAmount = totalDiscount;
        invoice.TaxAmount = totalTax;
        invoice.GrandTotal = subTotal - totalDiscount + totalTax;
        invoice.ReturnAmount = dto.PaidAmount > invoice.GrandTotal
            ? dto.PaidAmount - invoice.GrandTotal
            : 0;

        return invoice;
    }

    private static void ValidateInvoiceDto(int branchId, int warehouseId, List<CreateSaleInvoiceItemDto> items)
    {
        if (branchId <= 0) throw new InvalidOperationException("BranchId is required.");
        if (warehouseId <= 0) throw new InvalidOperationException("WarehouseId is required.");
        if (items == null || items.Count == 0) throw new InvalidOperationException("At least one item is required.");
        foreach (var item in items)
        {
            if (item.ProductId <= 0) throw new InvalidOperationException("ProductId is required for all items.");
            if (item.UnitId <= 0) throw new InvalidOperationException("UnitId is required for all items.");
            if (item.Quantity <= 0) throw new InvalidOperationException("Quantity must be greater than zero.");
        }
    }

    // ─── Mapping ──────────────────────────────────────────────────────────────

    private static PosProductLookupDto MapProductToLookup(ProductEntity product, ProductBarcode? matchedBarcode, string barcodeValue)
    {
        var matchedVariant = matchedBarcode?.ProductVariantId.HasValue == true
            ? product.Variants.FirstOrDefault(v => v.Id == matchedBarcode.ProductVariantId)
            : null;

        var matchedUnit = matchedBarcode?.ProductUnitId.HasValue == true
            ? product.Units.FirstOrDefault(u => u.Id == matchedBarcode.ProductUnitId)
            : product.Units.FirstOrDefault(u => u.IsBaseUnit);

        decimal retailPrice = matchedVariant?.SellingPriceOverride ?? product.SellingPrice;
        if (matchedUnit?.SellingPrice.HasValue == true) retailPrice = matchedUnit.SellingPrice.Value;

        decimal wholesalePrice = product.WholesalePrice;
        if (matchedUnit?.WholesalePrice.HasValue == true) wholesalePrice = matchedUnit.WholesalePrice.Value;

        return new PosProductLookupDto
        {
            ProductId = product.Id,
            ProductName = product.ProductName,
            ProductCode = product.ProductCode,
            SKU = product.SKU,
            IsVariantEnabled = product.IsVariantEnabled,
            IsDiscountAllowed = product.IsDiscountAllowed,
            DiscountType = product.DiscountType,
            DiscountValue = product.DiscountValue,
            Barcode = barcodeValue,
            RetailPrice = retailPrice,
            WholesalePrice = wholesalePrice,
            MatchedUnitId = matchedUnit?.Id,
            MatchedUnitName = matchedUnit?.UnitName ?? string.Empty,
            MatchedUnitConversionFactor = matchedUnit?.ConversionFactor ?? 1,
            MatchedVariantId = matchedVariant?.Id,
            MatchedVariantName = matchedVariant?.VariantName,
            MatchedVariantSize = matchedVariant?.Size,
            MatchedVariantColor = matchedVariant?.Color,
            MatchedVariantSellingPrice = matchedVariant?.SellingPriceOverride,
            AvailableUnits = product.Units
                .Where(u => !u.IsDeleted)
                .Select(u => new PosProductUnitDto
                {
                    UnitId = u.Id,
                    UnitName = u.UnitName,
                    SellingPrice = u.SellingPrice ?? product.SellingPrice,
                    WholesalePrice = u.WholesalePrice ?? product.WholesalePrice,
                    ConversionFactor = u.ConversionFactor,
                    IsBaseUnit = u.IsBaseUnit
                }).ToList(),
            AvailableVariants = product.Variants
                .Where(v => !v.IsDeleted && v.Status)
                .Select(v => new PosProductVariantDto
                {
                    VariantId = v.Id,
                    VariantName = v.VariantName,
                    Size = v.Size,
                    Color = v.Color,
                    SKU = v.SKU,
                    SellingPriceOverride = v.SellingPriceOverride ?? (product.SellingPrice + v.AdditionalPrice),
                    AdditionalPrice = v.AdditionalPrice
                }).ToList()
        };
    }

    private static SaleInvoiceDto MapInvoiceDto(SaleInvoice inv) => new()
    {
        Id = inv.Id,
        InvoiceNo = inv.InvoiceNo,
        CustomerId = inv.CustomerId,
        CustomerName = inv.Customer?.Name,
        CustomerPhone = inv.Customer?.Phone,
        WarehouseId = inv.WarehouseId,
        WarehouseName = inv.Warehouse?.Name ?? string.Empty,
        SaleDate = inv.SaleDate,
        SubTotal = inv.SubTotal,
        DiscountAmount = inv.DiscountAmount,
        TaxAmount = inv.TaxAmount,
        GrandTotal = inv.GrandTotal,
        PaidAmount = inv.PaidAmount,
        ReturnAmount = inv.ReturnAmount,
        PaymentMethod = inv.PaymentMethod,
        CashAmount = inv.CashAmount,
        CardAmount = inv.CardAmount,
        Status = inv.Status,
        PricingType = inv.PricingType,
        Notes = inv.Notes,
        HeldNote = inv.HeldNote,
        CashierName = inv.CashierName,
        BranchId = inv.BranchId,
        BranchName = inv.Branch?.Name ?? string.Empty,
        CreatedDate = inv.CreatedDate,
        Items = inv.Items
            .Where(i => !i.IsDeleted)
            .Select(i => new SaleInvoiceItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product?.ProductName ?? string.Empty,
                ProductCode = i.Product?.ProductCode ?? string.Empty,
                VariantId = i.VariantId,
                VariantName = i.Variant?.VariantName,
                VariantSize = i.Variant?.Size,
                VariantColor = i.Variant?.Color,
                UnitId = i.UnitId,
                UnitName = i.Unit?.UnitName ?? string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                DiscountPercent = i.DiscountPercent,
                DiscountAmount = i.DiscountAmount,
                TaxPercent = i.TaxPercent,
                TaxAmount = i.TaxAmount,
                LineTotal = i.LineTotal,
                ItemNote = i.ItemNote
            }).ToList()
    };
}
