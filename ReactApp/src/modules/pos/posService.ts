import apiClient from '../../services/api';
import { toBaseQuantity } from '../product/unitPricing';

// ─── Types ─────────────────────────────────────────────────────────────────

export interface PosProductUnit {
  unitId: number;
  unitName: string;
  sellingPrice: number;
  wholesalePrice: number;
  conversionFactor: number;
  isBaseUnit: boolean;
  isDefaultSaleUnit?: boolean;
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
  useAutoUnitPricing?: boolean;
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
  stock?: number;
  allowNegativeStock?: boolean;
  baseUnitName?: string;
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
  allowNegativeStock?: boolean;
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
  isCreditSale?: boolean;
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
  isCreditSale?: boolean;
  branchId: number;
  branchName: string;
  branchAddress?: string;
  branchPhone?: string;
  branchEmail?: string;
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
  isManualPriceOverride?: boolean;
  calculatedUnitPrice?: number;
  availableUnits: PosProductUnit[];
  availableVariants: PosProductVariant[];
  stockBase: number;
  allowNegativeStock: boolean;
  baseUnitName: string;
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

export const getCartBaseQty = (
  cart: CartItem[],
  productId: number,
  variantId: number | null,
): number =>
  cart
    .filter((c) => c.productId === productId && (c.variantId ?? 0) === (variantId ?? 0))
    .reduce((sum, c) => sum + toBaseQuantity(c.quantity, c.conversionFactor), 0);

export const checkStockForCartLine = (
  cart: CartItem[],
  productId: number,
  variantId: number | null,
  variantName: string | null,
  productName: string,
  additionalQty: number,
  conversionFactor: number,
  stockBase: number,
  allowNegativeStock: boolean,
  baseUnitName: string,
  excludeCartKey?: string,
): string | null => {
  if (allowNegativeStock) return null;

  const cartForProduct = excludeCartKey
    ? cart.filter((c) => c.cartKey !== excludeCartKey)
    : cart;
  const alreadyInCart = getCartBaseQty(cartForProduct, productId, variantId);
  const needed = alreadyInCart + toBaseQuantity(additionalQty, conversionFactor);

  if (needed <= stockBase) return null;

  const remaining = Math.max(0, stockBase - alreadyInCart);
  const unitLabel = baseUnitName || 'base unit';
  const label = variantName ? `${productName} (${variantName})` : productName;
  return `Insufficient stock for ${label}. Required ${needed.toFixed(2)} ${unitLabel}, available ${remaining.toFixed(2)} ${unitLabel}.`;
};

export const validateCartStock = (cart: CartItem[]): string | null => {
  const grouped = new Map<string, {
    needed: number;
    stock: number;
    allowNegativeStock: boolean;
    label: string;
    unit: string;
  }>();

  for (const item of cart) {
    if (item.allowNegativeStock) continue;

    const key = `${item.productId}:${item.variantId ?? 0}`;
    const label = item.variantName ? `${item.productName} (${item.variantName})` : item.productName;
    const needed = toBaseQuantity(item.quantity, item.conversionFactor);
    const existing = grouped.get(key);

    if (existing) {
      existing.needed += needed;
    } else {
      grouped.set(key, {
        needed,
        stock: item.stockBase,
        allowNegativeStock: item.allowNegativeStock,
        label,
        unit: item.baseUnitName || 'base unit',
      });
    }
  }

  const errors: string[] = [];
  for (const entry of grouped.values()) {
    if (entry.needed > entry.stock) {
      errors.push(`${entry.label}: required ${entry.needed.toFixed(2)} ${entry.unit}, available ${entry.stock.toFixed(2)} ${entry.unit}`);
    }
  }

  if (errors.length === 0) return null;
  return errors.length === 1
    ? `Insufficient stock — ${errors[0]}.`
    : `Insufficient stock:\n${errors.map((line, i) => `${i + 1}. ${line}`).join('\n')}`;
};

// ─── Helper: resolve unit selling price from ProductUnit ────────────────────

export const resolveSaleUnitPrice = (
  pricingType: 'Retail' | 'Wholesale',
  unit: PosProductUnit | undefined,
  variant: PosProductVariant | null | undefined,
  productRetail: number,
  productWholesale: number,
  manualOverride?: number,
): number => {
  if (manualOverride != null && manualOverride >= 0) {
    return manualOverride;
  }

  if (pricingType === 'Wholesale') {
    return unit?.wholesalePrice ?? productWholesale;
  }

  // sellingPrice from API is already the effective price (auto or product override)
  if (unit?.sellingPrice != null) {
    return unit.sellingPrice;
  }

  if (variant?.sellingPriceOverride != null && variant.sellingPriceOverride > 0) {
    return variant.sellingPriceOverride;
  }

  return productRetail + (variant?.additionalPrice ?? 0);
};

/** Picks unit: preferred → matched (barcode) → default sale unit → base → first valid. */
export const resolveCartUnitId = (
  units: PosProductUnit[],
  preferredUnitId?: number | null,
  matchedUnitId?: number | null,
): number => {
  const isValid = (id: number) => {
    const u = units.find((x) => x.unitId === id);
    return u != null && u.conversionFactor > 0;
  };

  if (preferredUnitId != null && isValid(preferredUnitId)) return preferredUnitId;
  if (matchedUnitId != null && isValid(matchedUnitId)) return matchedUnitId;

  const fallback = units.find((u) => u.isDefaultSaleUnit && u.conversionFactor > 0)
    ?? units.find((u) => u.isBaseUnit && u.conversionFactor > 0)
    ?? units.find((u) => u.conversionFactor > 0);
  return fallback?.unitId ?? units[0]?.unitId ?? 0;
};

export const applyUnitToCartItem = (
  item: CartItem,
  unitId: number,
  pricingType: 'Retail' | 'Wholesale'
): CartItem => {
  const resolvedUnitId = resolveCartUnitId(item.availableUnits, unitId, item.unitId);
  const unit = item.availableUnits.find((u) => u.unitId === resolvedUnitId);
  if (!unit) return item;

  const variant =
    item.variantId != null
      ? item.availableVariants.find((v) => v.variantId === item.variantId) ?? null
      : null;
  const baseUnit = item.availableUnits.find((u) => u.isBaseUnit) ?? item.availableUnits[0];
  const productRetail = baseUnit?.sellingPrice ?? item.unitPrice;
  const productWholesale = baseUnit?.wholesalePrice ?? item.unitPrice;
  const unitPrice = resolveSaleUnitPrice(
    pricingType,
    unit,
    variant,
    productRetail,
    productWholesale,
    undefined,
  );

  const updated: CartItem = {
    ...item,
    cartKey: cartKey(item.productId, item.variantId, resolvedUnitId),
    unitId: resolvedUnitId,
    unitName: unit.unitName,
    conversionFactor: unit.conversionFactor > 0 ? unit.conversionFactor : 1,
    unitPrice,
    calculatedUnitPrice: pricingType === 'Wholesale'
      ? unit.wholesalePrice
      : unit.sellingPrice,
    isManualPriceOverride: false,
    lineTotal: 0,
  };
  updated.lineTotal = computeLineTotal(updated);
  return updated;
};

// ─── Helper: build CartItem from lookup ─────────────────────────────────────

export const lookupToCartItem = (
  lookup: PosProductLookup,
  pricingType: 'Retail' | 'Wholesale',
  preferredUnitId?: number | null,
): CartItem => {
  const unitId = resolveCartUnitId(
    lookup.availableUnits,
    preferredUnitId,
    lookup.matchedUnitId,
  );
  const unit = lookup.availableUnits.find((u) => u.unitId === unitId);
  const unitName = unit?.unitName ?? lookup.matchedUnitName ?? '';
  const conversionFactor = unit?.conversionFactor && unit.conversionFactor > 0
    ? unit.conversionFactor
    : 1;
  const variant =
    lookup.matchedVariantId != null
      ? lookup.availableVariants.find((v) => v.variantId === lookup.matchedVariantId) ?? null
      : null;
  const basePrice = resolveSaleUnitPrice(
    pricingType,
    unit,
    variant,
    lookup.retailPrice,
    lookup.wholesalePrice
  );

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
    calculatedUnitPrice: pricingType === 'Wholesale'
      ? (unit?.wholesalePrice ?? basePrice)
      : (unit?.sellingPrice ?? basePrice),
    isManualPriceOverride: false,
    discountPercent: lookup.isDiscountAllowed && lookup.discountType === 'Percentage' ? lookup.discountValue : 0,
    discountAmount: lookup.isDiscountAllowed && lookup.discountType === 'Fixed' ? lookup.discountValue : 0,
    taxPercent: 0,
    lineTotal: 0,
    itemNote: null,
    availableUnits: lookup.availableUnits,
    availableVariants: lookup.availableVariants,
    stockBase: lookup.stock ?? 0,
    allowNegativeStock: lookup.allowNegativeStock ?? false,
    baseUnitName: lookup.baseUnitName ?? lookup.availableUnits.find((u) => u.isBaseUnit)?.unitName ?? 'base unit',
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
  const baseUnit = group.units.find((u) => u.isBaseUnit) ?? group.units[0];
  // Manual product selection pre-selects the default sale unit (falls back to base).
  const defaultUnit = group.units.find((u) => u.isDefaultSaleUnit) ?? baseUnit;
  const variantLookup = variant
    ? group.variants.find((v) => v.variantId === variant.variantId) ?? variant
    : null;
  const stockBase = variant?.stock ?? group.stock;
  const productRetail = variant?.retailPrice ?? baseUnit?.sellingPrice ?? group.retailPrice;
  const productWholesale = variant?.wholesalePrice ?? baseUnit?.wholesalePrice ?? group.wholesalePrice;
  const price = pricingType === 'Wholesale' ? productWholesale : productRetail;

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
    retailPrice: productRetail,
    wholesalePrice: productWholesale,
    matchedUnitId: defaultUnit?.unitId ?? null,
    matchedUnitName: defaultUnit?.unitName ?? '',
    matchedUnitConversionFactor: defaultUnit?.conversionFactor ?? 1,
    matchedVariantId: variant?.variantId ?? null,
    matchedVariantName: variant?.variantName ?? null,
    matchedVariantSize: variant?.size ?? null,
    matchedVariantColor: variant?.color ?? null,
    matchedVariantSellingPrice: variantLookup ? price : null,
    stock: stockBase,
    allowNegativeStock: group.allowNegativeStock ?? false,
    baseUnitName: baseUnit?.unitName ?? 'base unit',
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
  getProductByBarcode: (barcode: string, branchId: number, warehouseId?: number) =>
    apiClient.get<PosProductLookup>(`/sales/product/barcode/${encodeURIComponent(barcode)}`, {
      params: { branchId, ...(warehouseId ? { warehouseId } : {}) },
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
