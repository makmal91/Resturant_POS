/**
 * Converts an entered quantity (in a given unit) to base-unit stock.
 *
 * The base unit is the SMALLEST sellable unit and stock is stored in it.
 * ConversionFactor = number of BASE units contained in 1 of this unit
 * (base unit = 1). So selling/buying a larger unit multiplies:
 * e.g. base = PCS, 1 Package = 3 PCS → 1 Package = 3 base PCS.
 */
export const toBaseQuantity = (quantity: number, conversionFactor: number): number => {
  const factor = conversionFactor > 0 ? conversionFactor : 1;
  return quantity * factor;
};

/**
 * Converts base-unit stock back to a quantity expressed in another unit.
 * unitQty = baseQuantity ÷ factor.
 */
export const fromBaseQuantity = (baseQuantity: number, conversionFactor: number): number => {
  const factor = conversionFactor > 0 ? conversionFactor : 1;
  return baseQuantity / factor;
};
