import React, { useEffect, useMemo } from 'react';
import type { OutstandingInvoiceOption } from '../../modules/ledger/partyLedgerService';

export type AllocationMode = 'auto' | 'manual';

export interface InvoiceAllocationRow {
  invoiceId: number;
  invoiceNo: string;
  invoiceDate: string;
  invoiceTotal: number;
  paidAmount: number;
  balanceDue: number;
  selected: boolean;
  applyAmount: string;
}

interface PaymentAllocationGridProps {
  invoices: OutstandingInvoiceOption[];
  mode: AllocationMode;
  paymentAmount: string;
  loading?: boolean;
  fmt: (value: number) => string;
  rows: InvoiceAllocationRow[];
  onRowsChange: (rows: InvoiceAllocationRow[]) => void;
  onModeChange: (mode: AllocationMode) => void;
  accent?: 'blue' | 'orange';
}

const formatDate = (value: string) => {
  const d = new Date(value);
  return Number.isNaN(d.getTime())
    ? '—'
    : d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
};

const buildRowsFromInvoices = (invoices: OutstandingInvoiceOption[]): InvoiceAllocationRow[] =>
  invoices.map((inv) => ({
    invoiceId: inv.invoiceId,
    invoiceNo: inv.invoiceNo,
    invoiceDate: inv.invoiceDate,
    invoiceTotal: inv.invoiceTotal,
    paidAmount: inv.paidAmount,
    balanceDue: inv.balanceDue,
    selected: false,
    applyAmount: '',
  }));

const computeAutoAllocations = (
  invoices: OutstandingInvoiceOption[],
  paymentAmount: number,
): InvoiceAllocationRow[] => {
  let remaining = paymentAmount;

  return invoices.map((inv) => {
    const applied = remaining > 0 ? Math.min(inv.balanceDue, remaining) : 0;
    remaining -= applied;

    return {
      invoiceId: inv.invoiceId,
      invoiceNo: inv.invoiceNo,
      invoiceDate: inv.invoiceDate,
      invoiceTotal: inv.invoiceTotal,
      paidAmount: inv.paidAmount,
      balanceDue: inv.balanceDue,
      selected: applied > 0,
      applyAmount: applied > 0 ? applied.toFixed(2) : '',
    };
  });
};

export const buildInitialAllocationRows = (
  invoices: OutstandingInvoiceOption[],
  mode: AllocationMode,
  paymentAmount: string,
): InvoiceAllocationRow[] => {
  const parsed = parseFloat(paymentAmount);
  if (mode === 'auto' && !isNaN(parsed) && parsed > 0) {
    return computeAutoAllocations(invoices, parsed);
  }
  return buildRowsFromInvoices(invoices);
};

export const buildEditAllocationRows = (
  invoices: OutstandingInvoiceOption[],
  existingAllocations: { invoiceId: number; invoiceNo?: string; appliedAmount: number }[],
): InvoiceAllocationRow[] => {
  const rowsById = new Map(
    buildRowsFromInvoices(invoices).map((row) => [row.invoiceId, row]),
  );

  for (const alloc of existingAllocations) {
    if (!rowsById.has(alloc.invoiceId)) {
      rowsById.set(alloc.invoiceId, {
        invoiceId: alloc.invoiceId,
        invoiceNo: alloc.invoiceNo || `#${alloc.invoiceId}`,
        invoiceDate: '',
        invoiceTotal: alloc.appliedAmount,
        paidAmount: 0,
        balanceDue: alloc.appliedAmount,
        selected: true,
        applyAmount: alloc.appliedAmount.toFixed(2),
      });
    }
  }

  return Array.from(rowsById.values()).map((row) => {
    const alloc = existingAllocations.find((item) => item.invoiceId === row.invoiceId);
    if (!alloc) return row;
    return {
      ...row,
      selected: true,
      applyAmount: alloc.appliedAmount.toFixed(2),
    };
  });
};

const PaymentAllocationGrid: React.FC<PaymentAllocationGridProps> = ({
  invoices,
  mode,
  paymentAmount,
  loading = false,
  fmt,
  rows,
  onRowsChange,
  onModeChange,
  accent = 'blue',
}) => {
  const parsedPayment = parseFloat(paymentAmount);
  const hasValidPayment = !isNaN(parsedPayment) && parsedPayment > 0;

  const totalPending = useMemo(
    () => invoices.reduce((sum, inv) => sum + inv.balanceDue, 0),
    [invoices],
  );

  const totalApplied = useMemo(
    () =>
      rows.reduce((sum, row) => {
        if (mode === 'manual' && !row.selected) return sum;
        const val = parseFloat(row.applyAmount);
        return sum + (isNaN(val) ? 0 : val);
      }, 0),
    [rows, mode],
  );

  const remaining = hasValidPayment ? parsedPayment - totalApplied : 0;

  useEffect(() => {
    if (mode !== 'auto' || !hasValidPayment) return;
    onRowsChange(computeAutoAllocations(invoices, parsedPayment));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [mode, paymentAmount, invoices, hasValidPayment, parsedPayment]);

  const accentRing = accent === 'orange' ? 'focus:ring-orange-400' : 'focus:ring-blue-400';
  const accentText = accent === 'orange' ? 'text-orange-700' : 'text-blue-700';
  const accentBg = accent === 'orange' ? 'bg-orange-50 border-orange-100' : 'bg-blue-50 border-blue-100';

  const toggleRow = (invoiceId: number, checked: boolean) => {
    onRowsChange(
      rows.map((row) => {
        if (row.invoiceId !== invoiceId) return row;
        return {
          ...row,
          selected: checked,
          applyAmount: checked && !row.applyAmount ? String(row.balanceDue) : row.applyAmount,
        };
      }),
    );
  };

  const updateApplyAmount = (invoiceId: number, value: string) => {
    onRowsChange(
      rows.map((row) =>
        row.invoiceId === invoiceId
          ? { ...row, applyAmount: value, selected: value.trim() !== '' || row.selected }
          : row,
      ),
    );
  };

  return (
    <div className="md:col-span-2 space-y-4">
      <div className={`rounded-lg border px-4 py-3 ${accentBg}`}>
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <p className="text-xs font-medium uppercase tracking-wide text-gray-500">Total Pending</p>
            <p className={`text-lg font-bold ${accentText}`}>{fmt(totalPending)}</p>
          </div>
          {hasValidPayment && (
            <div className="text-right">
              <p className="text-xs font-medium uppercase tracking-wide text-gray-500">Remaining</p>
              <p className={`text-lg font-bold ${remaining < -0.005 ? 'text-red-600' : accentText}`}>
                {fmt(Math.max(remaining, 0))}
                {remaining < -0.005 && ' (over allocated)'}
              </p>
            </div>
          )}
        </div>
      </div>

      <div>
        <p className="text-sm font-medium text-gray-800 mb-2">Allocation Mode</p>
        <div className="flex flex-wrap gap-4">
          <label className="inline-flex items-center gap-2 text-sm text-gray-700 cursor-pointer">
            <input
              type="radio"
              name="allocationMode"
              checked={mode === 'auto'}
              onChange={() => onModeChange('auto')}
              className={accentRing}
            />
            Auto Apply (FIFO)
          </label>
          <label className="inline-flex items-center gap-2 text-sm text-gray-700 cursor-pointer">
            <input
              type="radio"
              name="allocationMode"
              checked={mode === 'manual'}
              onChange={() => onModeChange('manual')}
              className={accentRing}
            />
            Manual Apply
          </label>
        </div>
      </div>

      <div className="border border-gray-200 rounded-lg overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full text-sm">
            <thead className="bg-gray-50 text-left text-xs font-medium text-gray-500 uppercase">
              <tr>
                {mode === 'manual' && <th className="px-3 py-2 w-10">Select</th>}
                <th className="px-3 py-2">Invoice No</th>
                <th className="px-3 py-2">Date</th>
                <th className="px-3 py-2 text-right">Total</th>
                <th className="px-3 py-2 text-right">Paid</th>
                <th className="px-3 py-2 text-right">Balance</th>
                <th className="px-3 py-2 text-right">Apply Amount</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {loading ? (
                <tr>
                  <td colSpan={mode === 'manual' ? 7 : 6} className="px-3 py-6 text-center text-gray-400">
                    Loading invoices…
                  </td>
                </tr>
              ) : rows.length === 0 ? (
                <tr>
                  <td colSpan={mode === 'manual' ? 7 : 6} className="px-3 py-6 text-center text-gray-400">
                    No outstanding invoices. Payment will be recorded as advance.
                  </td>
                </tr>
              ) : (
                rows.map((row) => {
                  const isPaid = row.balanceDue <= 0.005;
                  const applyVal = parseFloat(row.applyAmount);
                  const overBalance = !isNaN(applyVal) && applyVal > row.balanceDue + 0.005;

                  return (
                    <tr key={row.invoiceId} className={isPaid ? 'bg-gray-50 opacity-60' : 'bg-white'}>
                      {mode === 'manual' && (
                        <td className="px-3 py-2">
                          <input
                            type="checkbox"
                            checked={row.selected}
                            disabled={isPaid}
                            onChange={(e) => toggleRow(row.invoiceId, e.target.checked)}
                            className={accentRing}
                          />
                        </td>
                      )}
                      <td className="px-3 py-2 font-medium text-gray-900">{row.invoiceNo}</td>
                      <td className="px-3 py-2 text-gray-600">{formatDate(row.invoiceDate)}</td>
                      <td className="px-3 py-2 text-right text-gray-700">{fmt(row.invoiceTotal)}</td>
                      <td className="px-3 py-2 text-right text-gray-700">{fmt(row.paidAmount)}</td>
                      <td className="px-3 py-2 text-right font-medium text-gray-900">{fmt(row.balanceDue)}</td>
                      <td className="px-3 py-2 text-right">
                        {mode === 'manual' ? (
                          <input
                            type="number"
                            min="0"
                            step="0.01"
                            max={row.balanceDue}
                            value={row.applyAmount}
                            disabled={isPaid || !row.selected}
                            onChange={(e) => updateApplyAmount(row.invoiceId, e.target.value)}
                            className={`w-28 px-2 py-1 border rounded text-right text-sm ${
                              overBalance ? 'border-red-300' : 'border-gray-300'
                            }`}
                          />
                        ) : (
                          <span className={row.selected ? 'font-medium text-gray-900' : 'text-gray-400'}>
                            {row.applyAmount ? fmt(parseFloat(row.applyAmount)) : '—'}
                          </span>
                        )}
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};

export default PaymentAllocationGrid;
