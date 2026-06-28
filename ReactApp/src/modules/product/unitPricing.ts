import type { ProductUnitPayload } from './productService';

/** Child unit price = base price ÷ conversion factor (child units in 1 base unit). */
export const calculateAutoUnitPrice = (
  basePrice: number,
  conversionFactor: number,
  isBaseUnit: boolean,
): number => {
  if (isBaseUnit) return basePrice;
  const factor = conversionFactor > 0 ? conversionFactor : 1;
  return Math.round((basePrice / factor) * 100) / 100;
};

/** Converts entered quantity to base-unit stock. */
export const toBaseQuantity = (quantity: number, conversionFactor: number): number => {
  const factor = conversionFactor > 0 ? conversionFactor : 1;
  return quantity / factor;
};

/** Recalculates non-overridden alternate units when auto pricing is enabled. */
export const recalculateUnitPrices = (
  units: ProductUnitPayload[],
  baseCostPrice: number,
  baseSellingPrice: number,
  baseWholesalePrice: number,
  useAutoUnitPricing = true,
): ProductUnitPayload[] =>
  units.map((unit) => {
    if (unit.isBaseUnit) {
      return {
        ...unit,
        costPrice: baseCostPrice,
        sellingPrice: baseSellingPrice,
        wholesalePrice: baseWholesalePrice,
        isPriceOverridden: false,
      };
    }

    const calculatedSelling = calculateAutoUnitPrice(baseSellingPrice, unit.conversionFactor, false);
    const calculatedWholesale = calculateAutoUnitPrice(baseWholesalePrice, unit.conversionFactor, false);
    const calculatedCost = calculateAutoUnitPrice(baseCostPrice, unit.conversionFactor, false);

    if (!useAutoUnitPricing || unit.isPriceOverridden) {
      return {
        ...unit,
        calculatedSellingPrice: calculatedSelling,
        calculatedWholesalePrice: calculatedWholesale,
      };
    }

    return {
      ...unit,
      costPrice: calculatedCost,
      sellingPrice: calculatedSelling,
      wholesalePrice: calculatedWholesale,
      calculatedSellingPrice: calculatedSelling,
      calculatedWholesalePrice: calculatedWholesale,
      isPriceOverridden: false,
    };
  });
