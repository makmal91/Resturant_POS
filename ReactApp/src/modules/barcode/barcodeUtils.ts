import { ProductBarcodePayload, ProductUnitPayload, ProductVariantPayload } from '../product/productService';
import { ProductPrintDetails } from './barcodeService';

export const generateBarcodeValue = (productId: number, unitId: number, variantId?: number | null): string =>
  `AKH${String(productId).padStart(5, '0')}${String(unitId).padStart(2, '0')}${String(variantId ?? 0).padStart(2, '0')}`;

export const resolveDefaultUnit = (units: ProductUnitPayload[]): ProductUnitPayload | null => {
  if (units.length === 0) return null;
  return units.find((unit) => unit.isBaseUnit) ?? units[0];
};

export const resolveBarcodeValue = (
  details: ProductPrintDetails,
  productUnitId: number,
  variantId?: number | null,
): string => {
  const { barcodes, productId } = details;
  const active = barcodes.filter((barcode) => barcode.barcodeValue.trim());

  const exact = active.find(
    (barcode) => matchesUnit(barcode, productUnitId, details.units) && matchesVariant(barcode, variantId, details.variants),
  );
  if (exact) return exact.barcodeValue;

  const unitOnly = active.find(
    (barcode) => matchesUnit(barcode, productUnitId, details.units) && !barcode.variantId && !barcode.variantName,
  );
  if (unitOnly) return unitOnly.barcodeValue;

  if (variantId) {
    const variantOnly = active.find(
      (barcode) => !barcode.unitId && !barcode.unitName && matchesVariant(barcode, variantId, details.variants),
    );
    if (variantOnly) return variantOnly.barcodeValue;
  }

  const primary = active.find((barcode) => barcode.isPrimary);
  if (primary) return primary.barcodeValue;

  return generateBarcodeValue(productId, productUnitId, variantId);
};

export const productNeedsSelection = (
  details: ProductPrintDetails,
  unitFeatureEnabled: boolean,
  variantFeatureEnabled: boolean,
): boolean => {
  const activeBarcodes = details.barcodes.filter((barcode) => barcode.barcodeValue.trim());
  const showUnit = unitFeatureEnabled && details.hasMultipleUnits;
  const showVariant = variantFeatureEnabled && details.hasVariants;
  return showUnit || showVariant || activeBarcodes.length > 1;
};

export const getBarcodesForSelection = (
  details: ProductPrintDetails,
  productUnitId: number,
  variantId?: number | null,
): ProductBarcodePayload[] => {
  const active = details.barcodes.filter((barcode) => barcode.barcodeValue.trim());
  const exact = active.filter(
    (barcode) => matchesUnit(barcode, productUnitId, details.units) && matchesVariant(barcode, variantId, details.variants),
  );
  if (exact.length > 0) return exact;

  const unitOnly = active.filter(
    (barcode) => matchesUnit(barcode, productUnitId, details.units) && !barcode.variantId && !barcode.variantName,
  );
  if (unitOnly.length > 0) return unitOnly;

  if (variantId) {
    const variantOnly = active.filter(
      (barcode) => !barcode.unitId && !barcode.unitName && matchesVariant(barcode, variantId, details.variants),
    );
    if (variantOnly.length > 0) return variantOnly;
  }

  return active;
};

const matchesUnit = (barcode: ProductBarcodePayload, productUnitId: number, units: ProductUnitPayload[]): boolean => {
  if (barcode.unitId) return barcode.unitId === productUnitId;
  if (barcode.unitName) {
    const unit = units.find((item) => item.id === productUnitId);
    return unit ? unit.unitName.toLowerCase() === barcode.unitName.toLowerCase() : false;
  }
  const defaultUnit = resolveDefaultUnit(units);
  return defaultUnit?.id === productUnitId;
};

const matchesVariant = (
  barcode: ProductBarcodePayload,
  variantId: number | null | undefined,
  variants: ProductVariantPayload[],
): boolean => {
  if (!variantId) return !barcode.variantId && !barcode.variantName;
  if (barcode.variantId) return barcode.variantId === variantId;
  if (barcode.variantName) {
    const variant = variants.find((item) => item.id === variantId);
    return variant ? variant.variantName.toLowerCase() === barcode.variantName.toLowerCase() : false;
  }
  return false;
};

export const resolveUnitPrice = (
  details: ProductPrintDetails,
  unit: ProductUnitPayload,
  variant?: ProductVariantPayload | null,
): number => {
  // Each unit stores its own manual sale price; fall back to the product price.
  const base = unit.sellingPrice != null
    ? Number(unit.sellingPrice)
    : Number(details.sellingPrice);

  if (variant?.sellingPriceOverride != null) return Number(variant.sellingPriceOverride);
  return base + Number(variant?.additionalPrice ?? 0);
};

const shouldShowUnitOnLabel = (unitName?: string | null): boolean => {
  const unit = unitName?.trim().toLowerCase();
  if (!unit) return false;
  return unit !== 'piece' && unit !== 'pcs' && unit !== 'pc';
};

export const buildLabelTitle = (
  productName: string,
  unitName?: string | null,
  variantName?: string | null,
): string => {
  const name = productName.trim();
  const variant = variantName?.trim();
  const unit = shouldShowUnitOnLabel(unitName) ? unitName!.trim() : null;

  if (variant && unit) return `${name} - ${variant} (${unit.toUpperCase()})`;
  if (variant) return `${name} - ${variant}`;
  if (unit) return `${name} (${unit.toUpperCase()})`;
  return name;
};

export const formatLabelPrice = (value: number, symbol: string, currencyCode: string): string => {
  const amount = new Intl.NumberFormat(undefined, {
    minimumFractionDigits: value % 1 === 0 ? 0 : 2,
    maximumFractionDigits: 2,
  }).format(value);
  const sym = (symbol || currencyCode).trim();
  return `${sym} ${amount}`;
};

/** Reference-style price e.g. 57,900/- */
export const formatLabelPriceSlash = (value: number): string => {
  const amount = new Intl.NumberFormat(undefined, {
    minimumFractionDigits: value % 1 === 0 ? 0 : 2,
    maximumFractionDigits: 2,
  }).format(value);
  return `${amount}/-`;
};

export const formatBarcodeNumberSpaced = (barcode: string): string =>
  barcode.replace(/\s+/g, '').split('').join(' ');

export const formatLabelDate = (date: Date): string => {
  const day = String(date.getDate()).padStart(2, '0');
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const year = date.getFullYear();
  return `${day}/${month}/${year}`;
};

export const formatLabelTime = (date: Date): string =>
  date.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit', hour12: true });

export interface PrintQueueRow {
  key: string;
  productId: number;
  productName: string;
  sku: string;
  productUnitId: number;
  unitName?: string | null;
  variantId?: number | null;
  variantName?: string | null;
  barcode: string;
  price: number;
  qty: number;
}

export const buildPrintQueueRow = (
  details: ProductPrintDetails,
  productUnitId: number,
  variantId?: number | null,
  qty = 1,
  barcodeOverride?: string,
): PrintQueueRow | null => {
  const unit = details.units.find((item) => item.id === productUnitId) ?? resolveDefaultUnit(details.units);
  if (!unit?.id) return null;

  const variant = variantId ? details.variants.find((item) => item.id === variantId) ?? null : null;
  const barcode = barcodeOverride?.trim() || resolveBarcodeValue(details, unit.id, variantId);

  return {
    key: `${details.productId}:${unit.id}:${variantId ?? 0}:${barcode}`,
    productId: details.productId,
    productName: details.name,
    sku: details.sku,
    productUnitId: unit.id,
    unitName: unit.unitName,
    variantId: variant?.id ?? null,
    variantName: variant?.variantName ?? null,
    barcode,
    price: resolveUnitPrice(details, unit, variant),
    qty: Math.max(1, qty),
  };
};
