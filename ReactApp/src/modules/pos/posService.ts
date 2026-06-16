import apiClient from '../../services/api';

// ─── Types ─────────────────────────────────────────────────────────────────

export interface PosProductUnit {
  unitId: number;
  unitName: string;
  sellingPrice: number;
  wholesalePrice: number;
  conversionFactor: number;
  isBaseUnit: boolean;
}

export interface PosProductVariant {
  variantId: number;
  variantName: string;
  size: string;
  color: string;
  sku: string;
  sellingPriceOverride: number;
  additionalPrice: number;
}

export interface PosProductLookup {
  productId: number;
  productName: string;
  productCode: string;
  sku: string;
  isVariantEnabled: boolean;
  isDiscountAllowed: boolean;
  discountType: 'Percentage' | 'Fixed' | null;
  discountValue: number;
  barcode: string;
  retailPrice: number;
  wholesalePrice: number;
  matchedUnitId: number | null;
  matchedUnitName: string;
  matchedUnitConversionFactor: number;
  matchedVariantId: number | null;
  matchedVariantName: string | null;
  matchedVariantSize: string | null;
  matchedVariantColor: string | null;
  matchedVariantSellingPrice: number | null;
  availableUnits: PosProductUnit[];
  availableVariants: PosProductVariant[];
}

export interface PosCustomer {
  id: number;
  name: string;
  phone: string;
  email: string;
}

// ─── Grouped search result types ────────────────────────────────────────────

export interface PosSearchVariantRow {
  variantId: number;
  variantName: string;
  size: string;
  color: string;
  sku: string;
  barcode: string;
  retailPrice: number;
  wholesalePrice: number;
  stock: number;
}

export interface PosSearchGroup {
  productId: number;
  productName: string;
  productCode: string;
  categoryName: string;
  brandName: string;
  isVariantEnabled: boolean;
  retailPrice: number;
  wholesalePrice: number;
  stock: number;
  isDiscountAllowed: boolean;
  discountType: 'Percentage' | 'Fixed' | null;
  discountValue: number;
  units: PosProductUnit[];
  variants: PosSearchVariantRow[];
}

export interface SaleInvoiceItem {
  productId: number;
  variantId?: number | null;
  unitId: number;
  quantity: number;
  conversionFactor: number;
  unitPrice: number;
  discountPercent: number;
  discountAmount: number;
  taxPercent: number;
  itemNote?: string | null;
}

export interface CreateSaleInvoicePayload {
  customerId?: number | null;
  warehouseId: number;
  pricingType: 'Retail' | 'Wholesale';
  paymentMethod: 'Cash' | 'Card' | 'Mixed';
  paidAmount: number;
  cashAmount: number;
  cardAmount: number;
  discountAmount: number;
  notes?: string;
  cashierName?: string;
  businessId: number;
  branchId: number;
  items: SaleInvoiceItem[];
}

export interface HoldBillPayload {
  heldNote?: string;
  customerId?: number | null;
  warehouseId: number;
  pricingType: 'Retail' | 'Wholesale';
  discountAmount: number;
  notes?: string;
  businessId: number;
  branchId: number;
  items: SaleInvoiceItem[];
}

export interface SaleInvoiceDto {
  id: number;
  invoiceNo: string;
  customerId: number | null;
  customerName: string | null;
  customerPhone: string | null;
  warehouseId: number;
  warehouseName: string;
  saleDate: string;
  subTotal: number;
  discountAmount: number;
  taxAmount: number;
  grandTotal: number;
  paidAmount: number;
  returnAmount: number;
  paymentMethod: 'Cash' | 'Card' | 'Mixed';
  cashAmount: number;
  cardAmount: number;
  status: 'Draft' | 'Completed' | 'Held' | 'Cancelled' | 'Returned' | 'Voided';
  voidedAt: string | null;
  voidedByName: string | null;
  pricingType: 'Retail' | 'Wholesale';
  notes: string | null;
  heldNote: string | null;
  cashierName: string | null;
  branchId: number;
  branchName: string;
  createdDate: string;
  items: SaleInvoiceItemResult[];
}

export interface SaleInvoiceItemResult {
  id: number;
  productId: number;
  productName: string;
  productCode: string;
  variantId: number | null;
  variantName: string | null;
  variantSize: string | null;
  variantColor: string | null;
  unitId: number;
  unitName: string;
  quantity: number;
  conversionFactor: number;
  baseQuantity: number;
  unitPrice: number;
  discountPercent: number;
  discountAmount: number;
  taxPercent: number;
  taxAmount: number;
  lineTotal: number;
  itemNote: string | null;
}

export interface SaleLedgerEntry {
  id: number;
  type: string;
  productId: number;
  productName: string;
  variantId: number | null;
  variantName: string | null;
  warehouseId: number;
  warehouseName: string;
  quantityInBaseUnit: number;
  unitPrice: number;
  totalAmount: number;
  date: string;
  remarks: string;
}

export interface VoidInvoicePayload {
  businessId: number;
  branchId: number;
  voidedByName?: string;
  reason?: string;
}

export interface UpdateSaleInvoicePayload {
  customerId?: number | null;
  warehouseId: number;
  pricingType: 'Retail' | 'Wholesale';
  paymentMethod: 'Cash' | 'Card' | 'Mixed';
  paidAmount: number;
  cashAmount: number;
  cardAmount: number;
  discountAmount: number;
  notes?: string;
  cashierName?: string;
  businessId: number;
  branchId: number;
  items: SaleInvoiceItem[];
}

// ─── Cart item (local state only) ──────────────────────────────────────────

export interface CartItem {
  cartKey: string;
  productId: number;
  productName: string;
  productCode: string;
  barcode: string;
  variantId: number | null;
  variantName: string | null;
  variantSize: string | null;
  variantColor: string | null;
  unitId: number;
  unitName: string;
  conversionFactor: number;
  quantity: number;
  unitPrice: number;
  discountPercent: number;
  discountAmount: number;
  taxPercent: number;
  lineTotal: number;
  itemNote: string | null;
  availableUnits: PosProductUnit[];
  availableVariants: PosProductVariant[];
}

// ─── Helper: derive cart key ────────────────────────────────────────────────

export const cartKey = (productId: number, variantId: number | null, unitId: number): string =>
  `${productId}-${variantId ?? 0}-${unitId}`;

// ─── Helper: compute line total ─────────────────────────────────────────────

export const computeLineTotal = (item: Pick<CartItem, 'quantity' | 'unitPrice' | 'discountPercent' | 'discountAmount' | 'taxPercent'>): number => {
  const gross = item.quantity * item.unitPrice;
  const disc = item.discountAmount > 0 ? item.discountAmount : (gross * item.discountPercent) / 100;
  const net = gross - disc;
  const tax = item.taxPercent > 0 ? (net * item.taxPercent) / 100 : 0;
  return Math.round((net + tax) * 100) / 100;
};

// ─── Helper: build CartItem from lookup ─────────────────────────────────────

export const lookupToCartItem = (lookup: PosProductLookup, pricingType: 'Retail' | 'Wholesale'): CartItem => {
  const unitId = lookup.matchedUnitId ?? lookup.availableUnits.find(u => u.isBaseUnit)?.unitId ?? 0;
  const unitName = lookup.matchedUnitName ?? lookup.availableUnits.find(u => u.isBaseUnit)?.unitName ?? '';
  const conversionFactor = lookup.matchedUnitConversionFactor > 0 ? lookup.matchedUnitConversionFactor : 1;
  const basePrice = pricingType === 'Wholesale' ? lookup.wholesalePrice : lookup.retailPrice;

  const item: CartItem = {
    cartKey: cartKey(lookup.productId, lookup.matchedVariantId ?? null, unitId),
    productId: lookup.productId,
    productName: lookup.productName,
    productCode: lookup.productCode,
    barcode: lookup.barcode,
    variantId: lookup.matchedVariantId ?? null,
    variantName: lookup.matchedVariantName ?? null,
    variantSize: lookup.matchedVariantSize ?? null,
    variantColor: lookup.matchedVariantColor ?? null,
    unitId,
    unitName,
    conversionFactor,
    quantity: 1,
    unitPrice: basePrice,
    discountPercent: lookup.isDiscountAllowed && lookup.discountType === 'Percentage' ? lookup.discountValue : 0,
    discountAmount: lookup.isDiscountAllowed && lookup.discountType === 'Fixed' ? lookup.discountValue : 0,
    taxPercent: 0,
    lineTotal: 0,
    itemNote: null,
    availableUnits: lookup.availableUnits,
    availableVariants: lookup.availableVariants
  };
  item.lineTotal = computeLineTotal(item);
  return item;
};

// ─── Helper: build a PosProductLookup from a grouped search result ──────────

export const groupRowToLookup = (
  group: PosSearchGroup,
  variant: PosSearchVariantRow | null,
  pricingType: 'Retail' | 'Wholesale'
): PosProductLookup => {
  const baseUnit = group.units.find(u => u.isBaseUnit) ?? group.units[0];
  const price = pricingType === 'Wholesale'
    ? (variant?.wholesalePrice ?? group.wholesalePrice)
    : (variant?.retailPrice   ?? group.retailPrice);

  return {
    productId: group.productId,
    productName: group.productName,
    productCode: group.productCode,
    sku: variant?.sku ?? '',
    isVariantEnabled: group.isVariantEnabled,
    isDiscountAllowed: group.isDiscountAllowed,
    discountType: group.discountType,
    discountValue: group.discountValue,
    barcode: variant?.barcode ?? '',
    retailPrice: variant?.retailPrice ?? group.retailPrice,
    wholesalePrice: variant?.wholesalePrice ?? group.wholesalePrice,
    matchedUnitId: baseUnit?.unitId ?? null,
    matchedUnitName: baseUnit?.unitName ?? '',
    matchedUnitConversionFactor: baseUnit?.conversionFactor ?? 1,
    matchedVariantId: variant?.variantId ?? null,
    matchedVariantName: variant?.variantName ?? null,
    matchedVariantSize: variant?.size ?? null,
    matchedVariantColor: variant?.color ?? null,
    matchedVariantSellingPrice: variant ? price : null,
    availableUnits: group.units,
    availableVariants: group.variants.map(v => ({
      variantId: v.variantId,
      variantName: v.variantName,
      size: v.size,
      color: v.color,
      sku: v.sku,
      sellingPriceOverride: v.retailPrice,
      additionalPrice: 0,
    })),
  };
};

// ─── API calls ──────────────────────────────────────────────────────────────

const bh = (branchId: number) => ({ headers: { 'X-Branch-Id': String(branchId) } });

export const posService = {
  getProductByBarcode: (barcode: string, branchId: number) =>
    apiClient.get<PosProductLookup>(`/sales/product/barcode/${encodeURIComponent(barcode)}`, {
      params: { branchId },
      ...bh(branchId),
    }),

  searchProducts: (q: string, branchId: number) =>
    apiClient.get<PosProductLookup[]>('/sales/products/search', {
      params: { q, branchId },
      ...bh(branchId),
    }),

  searchProductsGrouped: (q: string, branchId: number, warehouseId?: number) =>
    apiClient.get<PosSearchGroup[]>('/sales/products/search-grouped', {
      params: { q, branchId, ...(warehouseId ? { warehouseId } : {}) },
      ...bh(branchId),
    }),

  searchCustomers: (q: string, branchId: number) =>
    apiClient.get<PosCustomer[]>('/sales/customers/search', {
      params: { q, branchId },
      ...bh(branchId),
    }),

  createInvoice: (payload: CreateSaleInvoicePayload) =>
    apiClient.post<SaleInvoiceDto>('/sales/invoice', payload, bh(payload.branchId)),

  holdBill: (payload: HoldBillPayload) =>
    apiClient.post<SaleInvoiceDto>('/sales/hold', payload, bh(payload.branchId)),

  getHeldBills: (branchId: number) =>
    apiClient.get<SaleInvoiceDto[]>('/sales/held', {
      params: { branchId },
      ...bh(branchId),
    }),

  cancelHeldBill: (id: number, branchId: number) =>
    apiClient.delete(`/sales/held/${id}`, {
      params: { branchId },
      ...bh(branchId),
    }),

  getInvoiceById: (id: number, branchId: number) =>
    apiClient.get<SaleInvoiceDto>(`/sales/invoice/${id}`, {
      params: { branchId },
      ...bh(branchId),
    }),

  voidInvoice: (id: number, payload: VoidInvoicePayload) =>
    apiClient.post<SaleInvoiceDto>(`/sales/invoice/${id}/void`, payload, bh(payload.branchId)),

  updateInvoice: (id: number, payload: UpdateSaleInvoicePayload) =>
    apiClient.put<SaleInvoiceDto>(`/sales/invoice/${id}`, payload, bh(payload.branchId)),

  getInvoiceLedgerHistory: (id: number, branchId: number) =>
    apiClient.get<SaleLedgerEntry[]>(`/sales/invoice/${id}/ledger`, {
      params: { branchId },
      ...bh(branchId),
    }),
};
