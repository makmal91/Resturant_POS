import { LabelDimensions } from './labelSize';

const MM_TO_PX = 96 / 25.4;

export interface BarcodeLabelContentHints {
  showVariantLine?: boolean;
  showPrice?: boolean;
  showCompany?: boolean;
}

export interface BarcodeLabelScale {
  paddingMm: number;
  headerGapMm: number;
  sectionGapMm: number;
  barcodeTextGapMm: number;
  priceGapMm: number;
  productNamePx: number;
  variantPx: number;
  barcodeTextPx: number;
  pricePx: number;
  companyPx: number;
  barcodeWidth: number;
  barcodeHeight: number;
  barcodeMaxHeightMm: number;
}

export interface VariantUnitLineOptions {
  showVariant?: boolean;
  showUnit?: boolean;
}

export const buildVariantUnitLine = (
  variantName?: string | null,
  unitName?: string | null,
  options: VariantUnitLineOptions = {},
): string | null => {
  const showVariant = options.showVariant ?? true;
  const showUnit = options.showUnit ?? true;
  const variant = showVariant ? variantName?.trim() : '';
  const unit = showUnit ? unitName?.trim() : '';

  if (variant && unit) return `${variant} (${unit.toUpperCase()})`;
  if (variant) return variant;
  if (unit) return unit.toUpperCase();
  return null;
};

const pxToMm = (px: number, lineHeight = 1.15): number => (px * lineHeight) / MM_TO_PX;

/** Auto-scales typography and CODE128 dimensions to fit label height with even spacing. */
export const computeBarcodeLabelScale = (
  size: LabelDimensions,
  hints: BarcodeLabelContentHints = {},
): BarcodeLabelScale => {
  const { labelWidth, labelHeight } = size;
  const compact = labelHeight <= 22;
  const large = labelHeight >= 35;
  const showVariantLine = hints.showVariantLine ?? true;
  const showPrice = hints.showPrice ?? true;
  const showCompany = hints.showCompany ?? false;

  const paddingMm = 1.5;
  const headerGapMm = 0.35;
  const sectionGapMm = compact ? 0.7 : 0.9;
  const barcodeTextGapMm = 0.65;
  const priceGapMm = 1;

  const productNamePx = compact ? 10 : large ? 12 : 11;
  const variantPx = compact ? 8 : large ? 10 : 9;
  const barcodeTextPx = compact ? 9 : 10;
  const pricePx = compact ? 10 : 11;
  const companyPx = compact ? 7 : 8;

  const companyMm = showCompany ? pxToMm(companyPx) + headerGapMm : 0;
  const productMm = pxToMm(productNamePx);
  const variantMm = showVariantLine ? pxToMm(variantPx) : 0;
  const headerBlockMm = companyMm + productMm + variantMm + (showVariantLine ? headerGapMm : 0);
  const barcodeTextMm = pxToMm(barcodeTextPx, 1.2) + barcodeTextGapMm;
  const priceBlockMm = showPrice ? pxToMm(pricePx, 1.1) + priceGapMm : 0;

  const verticalGapsMm = sectionGapMm * (showPrice ? 2 : 1);
  const reservedMm = paddingMm * 2 + headerBlockMm + barcodeTextMm + priceBlockMm + verticalGapsMm;
  const availableBarcodeMm = Math.max(4, labelHeight - reservedMm);
  const barcodeHeight = Math.round(
    Math.min(
      large ? 46 : compact ? 22 : 34,
      Math.max(compact ? 14 : 18, availableBarcodeMm * MM_TO_PX * 0.92),
    ),
  );

  const barcodeWidth = Math.min(
    compact ? 1.15 : large ? 1.65 : 1.35,
    (labelWidth - paddingMm * 2) / (compact ? 34 : 40),
  );

  return {
    paddingMm,
    headerGapMm,
    sectionGapMm,
    barcodeTextGapMm,
    priceGapMm,
    productNamePx,
    variantPx,
    barcodeTextPx,
    pricePx,
    companyPx,
    barcodeWidth,
    barcodeHeight,
    barcodeMaxHeightMm: Number((barcodeHeight / MM_TO_PX).toFixed(2)),
  };
};

export const truncateBarcodeForLabel = (barcode: string, maxLength = 24): string => {
  const value = barcode.trim();
  if (value.length <= maxLength) return value;
  return `${value.slice(0, maxLength - 1)}…`;
};
