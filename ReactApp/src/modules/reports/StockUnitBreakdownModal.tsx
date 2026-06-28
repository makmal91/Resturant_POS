import React, { useEffect, useState } from 'react';
import { getApiErrorMessage } from '../../services/api';
import { fmtQty } from './reportFormatters';
import { reportService, type StockUnitBreakdownResponse } from './reportService';

export interface StockUnitBreakdownModalProps {
  open: boolean;
  onClose: () => void;
  branchId: number;
  productId: number;
  productName: string;
  closingBalance: number;
  baseUnitName?: string;
  warehouseId?: number;
  toDate: string;
}

const StockUnitBreakdownModal: React.FC<StockUnitBreakdownModalProps> = ({
  open,
  onClose,
  branchId,
  productId,
  productName,
  closingBalance,
  baseUnitName,
  warehouseId,
  toDate,
}) => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [data, setData] = useState<StockUnitBreakdownResponse | null>(null);

  useEffect(() => {
    if (!open || branchId <= 0 || productId <= 0) {
      setData(null);
      setError(null);
      return;
    }

    let cancelled = false;
    setLoading(true);
    setError(null);

    void reportService
      .getStockUnitBreakdown(branchId, productId, {
        warehouseId,
        toDate,
      })
      .then((res) => {
        if (!cancelled) setData(res.data);
      })
      .catch((err) => {
        if (!cancelled) {
          setData(null);
          setError(getApiErrorMessage(err, 'Failed to load unit breakdown.'));
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [open, branchId, productId, warehouseId, toDate]);

  if (!open) return null;

  const baseLabel = data?.baseUnitName || baseUnitName || 'base unit';
  const baseQty = data?.closingBalance ?? closingBalance;
  const lines = data?.units ?? [];

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/45 p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby="stock-unit-breakdown-title"
      onClick={onClose}
    >
      <div
        className="w-full max-w-md rounded-xl bg-white shadow-xl dark:bg-gray-900"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-start justify-between border-b border-gray-200 px-5 py-4 dark:border-gray-700">
          <div>
            <h2 id="stock-unit-breakdown-title" className="text-lg font-semibold text-gray-900 dark:text-gray-100">
              Stock by Unit
            </h2>
            <p className="mt-0.5 text-sm text-gray-500 dark:text-gray-400">{productName}</p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg p-1 text-gray-400 hover:bg-gray-100 hover:text-gray-600 dark:hover:bg-gray-800"
            aria-label="Close"
          >
            ×
          </button>
        </div>

        <div className="px-5 py-4">
          <p className="mb-4 text-sm text-gray-600 dark:text-gray-300">
            Ledger balance:{' '}
            <span className="font-semibold tabular-nums text-gray-900 dark:text-gray-100">
              {fmtQty(baseQty)} {baseLabel}
            </span>
          </p>

          {loading && (
            <p className="py-6 text-center text-sm text-gray-500">Loading unit breakdown…</p>
          )}

          {error && (
            <p className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>
          )}

          {!loading && !error && lines.length > 0 && (
            <div className="overflow-hidden rounded-lg border border-gray-200 dark:border-gray-700">
              <table className="w-full text-sm">
                <thead className="bg-gray-50 dark:bg-gray-800">
                  <tr>
                    <th className="px-4 py-2.5 text-left text-xs font-semibold uppercase tracking-wide text-gray-500">
                      Unit
                    </th>
                    <th className="px-4 py-2.5 text-right text-xs font-semibold uppercase tracking-wide text-gray-500">
                      Quantity
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-800">
                  {lines.map((line) => (
                    <tr
                      key={line.unitId}
                      className={line.isBaseUnit ? 'bg-blue-50/50 dark:bg-blue-950/30' : undefined}
                    >
                      <td className="px-4 py-2.5 font-medium text-gray-800 dark:text-gray-200">
                        {line.unitName}
                        {line.isBaseUnit && (
                          <span className="ml-2 inline-flex rounded-full bg-blue-100 px-1.5 py-0.5 text-[10px] font-semibold text-blue-700 dark:bg-blue-900 dark:text-blue-200">
                            Base
                          </span>
                        )}
                      </td>
                      <td className="px-4 py-2.5 text-right tabular-nums font-semibold text-gray-900 dark:text-gray-100">
                        {fmtQty(line.quantity)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {!loading && !error && lines.length === 0 && (
            <p className="py-4 text-center text-sm text-gray-500">No units configured for this product.</p>
          )}
        </div>

        <div className="border-t border-gray-200 px-5 py-3 text-right dark:border-gray-700">
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 dark:border-gray-600 dark:text-gray-200 dark:hover:bg-gray-800"
          >
            Close
          </button>
        </div>
      </div>
    </div>
  );
};

export default StockUnitBreakdownModal;
