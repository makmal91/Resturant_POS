import React from 'react';
import { formatStockInUnit, formatStockQty } from './formStockHelpers';

export interface ProductStockHintProps {
  baseQuantity: number | null;
  conversionFactor: number;
  unitName: string;
  baseUnitName: string;
  loading?: boolean;
  hasWarehouse?: boolean;
  hasProduct?: boolean;
  warnExceeds?: boolean;
}

const ProductStockHint: React.FC<ProductStockHintProps> = ({
  baseQuantity,
  conversionFactor,
  unitName,
  baseUnitName,
  loading = false,
  hasWarehouse = true,
  hasProduct = true,
  warnExceeds = false,
}) => {
  if (!hasProduct || !hasWarehouse) return null;

  if (loading) {
    return <p className="mt-1 text-xs text-gray-400">Stock: loading…</p>;
  }

  if (baseQuantity == null) {
    return <p className="mt-1 text-xs text-gray-400">Stock: —</p>;
  }

  const factor = conversionFactor > 0 ? conversionFactor : 1;
  const displayUnit = unitName || baseUnitName || '';
  const showBaseSub =
    Boolean(displayUnit && baseUnitName && displayUnit !== baseUnitName && factor !== 1);

  return (
    <p className={`mt-1 text-xs leading-snug ${warnExceeds ? 'font-semibold text-red-600' : 'text-sky-700'}`}>
      <span className="font-medium text-gray-500">Stock:</span>{' '}
      {formatStockInUnit(baseQuantity, conversionFactor, unitName, baseUnitName)}
      {showBaseSub ? (
        <span className="text-gray-400"> ({formatStockQty(baseQuantity)} {baseUnitName})</span>
      ) : null}
    </p>
  );
};

export default ProductStockHint;
