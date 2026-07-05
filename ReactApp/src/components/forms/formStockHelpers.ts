import { fromBaseQuantity } from '../../modules/product/unitPricing';

export const parseCurrentStockQuantity = (data: unknown): number => {
  const record = data as Record<string, unknown>;
  return Number(record.quantity ?? record.Quantity ?? 0);
};

export const formatStockQty = (value: number) => {
  const abs = Math.abs(value);
  const formatted = abs % 1 === 0 ? abs.toFixed(0) : abs.toFixed(3);
  return value < 0 ? `−${formatted}` : formatted;
};

export const formatStockInUnit = (
  baseQuantity: number,
  conversionFactor: number,
  unitName: string,
  baseUnitName: string,
): string => {
  const factor = conversionFactor > 0 ? conversionFactor : 1;
  const inUnit = fromBaseQuantity(baseQuantity, factor);
  const displayUnit = unitName || baseUnitName || '';
  if (!displayUnit) return formatStockQty(inUnit);
  return `${formatStockQty(inUnit)} ${displayUnit}`;
};

export type LineColumnAlign = 'left' | 'right' | 'center';

export const lineTableHeaderClass = (align: LineColumnAlign = 'left') => {
  const base = 'px-2 text-xs font-semibold text-gray-600';
  if (align === 'right') return `${base} text-right`;
  if (align === 'center') return `${base} text-center`;
  return base;
};

export const lineTableCellClass = (align: LineColumnAlign = 'left') => {
  const base = 'px-2 py-2.5';
  if (align === 'right') return `${base} text-right`;
  if (align === 'center') return `${base} text-center`;
  return base;
};

export const lineTableGridClass = 'grid items-center gap-x-3';

export const lineTableScrollWrapClass = 'min-h-0 flex-1 overflow-auto';

export const lineTableStickyHeaderClass =
  'sticky top-0 z-10 border-b border-gray-200 bg-gray-50 px-3 py-2.5 shadow-[0_1px_0_rgba(0,0,0,0.04)]';
