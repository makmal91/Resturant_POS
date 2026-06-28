import React, { useEffect, useMemo, useState } from 'react';
import { ProductPrintDetails } from './barcodeService';
import {
  buildPrintQueueRow,
  getBarcodesForSelection,
  PrintQueueRow,
  resolveDefaultUnit,
  resolveBarcodeValue,
} from './barcodeUtils';

interface AddProductModalProps {
  details: ProductPrintDetails;
  unitFeatureEnabled: boolean;
  variantFeatureEnabled: boolean;
  onClose: () => void;
  onAdd: (row: PrintQueueRow) => void;
}

const AddProductModal: React.FC<AddProductModalProps> = ({
  details,
  unitFeatureEnabled,
  variantFeatureEnabled,
  onClose,
  onAdd,
}) => {
  const defaultUnit = resolveDefaultUnit(details.units);
  const activeVariants = details.variants.filter((variant) => variant.status);

  const [unitId, setUnitId] = useState<number>(defaultUnit?.id ?? 0);
  const [variantId, setVariantId] = useState<number | null>(() => {
    if (!variantFeatureEnabled || !details.hasVariants || activeVariants.length === 0) return null;
    return activeVariants[0]?.id ?? null;
  });
  const [barcodeValue, setBarcodeValue] = useState('');
  const [qty, setQty] = useState(1);
  const [validationError, setValidationError] = useState('');

  const showUnit = unitFeatureEnabled && details.hasMultipleUnits && details.units.length > 1;
  const showVariant = variantFeatureEnabled && details.hasVariants && activeVariants.length > 1;

  const barcodeOptions = useMemo(
    () => getBarcodesForSelection(details, unitId, variantId),
    [details, unitId, variantId],
  );

  const showBarcode = barcodeOptions.length > 1;

  useEffect(() => {
    if (barcodeOptions.length === 0) {
      setBarcodeValue(resolveBarcodeValue(details, unitId, variantId));
      return;
    }
    setBarcodeValue((current) => {
      if (current && barcodeOptions.some((item) => item.barcodeValue === current)) {
        return current;
      }
      return barcodeOptions[0].barcodeValue;
    });
  }, [barcodeOptions, details, unitId, variantId]);

  const previewRow = useMemo(() => {
    if (!unitId) return null;
    if (showVariant && !variantId) return null;
    return buildPrintQueueRow(details, unitId, variantId, qty, barcodeValue);
  }, [details, unitId, variantId, qty, barcodeValue, showVariant]);

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    setValidationError('');

    if (!unitId) {
      setValidationError('Please select a unit.');
      return;
    }
    if (showVariant && !variantId) {
      setValidationError('Please select a variant.');
      return;
    }
    if (qty <= 0) {
      setValidationError('Quantity must be greater than 0.');
      return;
    }

    const row = buildPrintQueueRow(details, unitId, variantId, qty, barcodeValue);
    if (!row) {
      setValidationError('Unable to build label for this selection.');
      return;
    }

    onAdd(row);
    onClose();
  };

  const formatBarcodeOption = (barcode: (typeof barcodeOptions)[0]): string => {
    const parts = [barcode.barcodeValue];
    if (barcode.unitName) parts.push(barcode.unitName);
    if (barcode.variantName) parts.push(barcode.variantName);
    if (barcode.isPrimary) parts.push('Primary');
    return parts.join(' · ');
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4" role="dialog" aria-modal="true">
      <div className="w-full max-w-md rounded-xl border border-gray-200 bg-white shadow-xl">
        <div className="border-b border-gray-200 px-5 py-4">
          <h2 className="text-lg font-semibold text-gray-900">Add to Print Queue</h2>
          <p className="mt-1 text-sm text-gray-600">{details.name}</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4 px-5 py-4">
          {showUnit && (
            <div>
              <label className="mb-1 block text-xs font-semibold uppercase tracking-wide text-gray-500">Unit</label>
              <select
                value={unitId}
                onChange={(event) => setUnitId(Number(event.target.value))}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm"
              >
                {details.units.map((unit) => (
                  <option key={unit.id} value={unit.id ?? 0}>
                    {unit.unitName}{unit.isBaseUnit ? ' (Base)' : ''}
                  </option>
                ))}
              </select>
            </div>
          )}

          {showVariant && (
            <div>
              <label className="mb-1 block text-xs font-semibold uppercase tracking-wide text-gray-500">Variant</label>
              <select
                value={variantId ?? 0}
                onChange={(event) => setVariantId(Number(event.target.value) || null)}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm"
              >
                <option value={0}>Select variant</option>
                {activeVariants.map((variant) => (
                  <option key={variant.id} value={variant.id ?? 0}>
                    {variant.variantName}
                    {variant.size ? ` · ${variant.size}` : ''}
                    {variant.color ? ` · ${variant.color}` : ''}
                  </option>
                ))}
              </select>
            </div>
          )}

          {showBarcode && (
            <div>
              <label className="mb-1 block text-xs font-semibold uppercase tracking-wide text-gray-500">Barcode</label>
              <select
                value={barcodeValue}
                onChange={(event) => setBarcodeValue(event.target.value)}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm font-mono"
              >
                {barcodeOptions.map((barcode) => (
                  <option key={`${barcode.id ?? barcode.barcodeValue}`} value={barcode.barcodeValue}>
                    {formatBarcodeOption(barcode)}
                  </option>
                ))}
              </select>
            </div>
          )}

          {!showBarcode && previewRow && (
            <div className="rounded-lg border border-gray-200 bg-gray-50 px-3 py-2">
              <div className="text-xs font-semibold uppercase tracking-wide text-gray-500">Barcode</div>
              <div className="mt-1 font-mono text-sm text-gray-900">{previewRow.barcode}</div>
            </div>
          )}

          <div>
            <label className="mb-1 block text-xs font-semibold uppercase tracking-wide text-gray-500">Qty to Print</label>
            <input
              type="number"
              min={1}
              value={qty}
              onChange={(event) => setQty(Math.max(1, Number(event.target.value)))}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm"
            />
          </div>

          {previewRow && (
            <div className="rounded-lg border border-blue-100 bg-blue-50 px-3 py-2 text-sm text-blue-900">
              Price: <span className="font-semibold">{previewRow.price.toFixed(2)}</span>
            </div>
          )}

          {validationError && (
            <p className="text-sm text-red-600">{validationError}</p>
          )}

          <div className="flex justify-end gap-2 border-t border-gray-100 pt-4">
            <button
              type="button"
              onClick={onClose}
              className="rounded-md border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              className="rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700"
            >
              Add to Queue
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default AddProductModal;
