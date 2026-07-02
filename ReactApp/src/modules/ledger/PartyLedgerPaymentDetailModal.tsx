import React from 'react';
import { useBusinessCurrency } from '../../hooks/useBusinessCurrency';
import { LEDGER_TYPE_LABELS, type PartyPaymentDetail } from './partyLedgerService';

interface PartyLedgerPaymentDetailModalProps {
  payment: PartyPaymentDetail | null;
  loading: boolean;
  error: string | null;
  onClose: () => void;
}

const PartyLedgerPaymentDetailModal: React.FC<PartyLedgerPaymentDetailModalProps> = ({
  payment,
  loading,
  error,
  onClose,
}) => {
  const { fmt } = useBusinessCurrency();

  if (!payment && !loading && !error) return null;

  const formatDate = (value: string) => {
    const d = new Date(value);
    return Number.isNaN(d.getTime())
      ? '—'
      : d.toLocaleString(undefined, {
          year: 'numeric',
          month: 'short',
          day: 'numeric',
          hour: '2-digit',
          minute: '2-digit',
        });
  };

  return (
    <div className="fixed inset-0 z-[60] flex items-center justify-center p-4">
      <button
        type="button"
        className="absolute inset-0 bg-black/40"
        aria-label="Close payment details"
        onClick={onClose}
      />
      <div className="relative w-full max-w-lg rounded-xl bg-white shadow-xl border border-gray-200">
        <div className="flex items-center justify-between border-b border-gray-100 px-5 py-4">
          <div>
            <h2 className="text-lg font-semibold text-gray-900">Payment Details</h2>
            <p className="text-xs text-gray-500 mt-0.5">Read-only voucher details</p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg p-2 text-gray-400 hover:bg-gray-100 hover:text-gray-600"
            aria-label="Close"
          >
            <svg className="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <div className="px-5 py-4 space-y-4 max-h-[70vh] overflow-y-auto">
          {loading && <p className="text-sm text-gray-500">Loading payment…</p>}
          {error && <p className="text-sm text-red-600">{error}</p>}

          {payment && !loading && (
            <>
              <dl className="grid grid-cols-2 gap-3 text-sm">
                <div>
                  <dt className="text-gray-500">Payment #</dt>
                  <dd className="font-medium text-gray-900">{payment.id}</dd>
                </div>
                <div>
                  <dt className="text-gray-500">Date</dt>
                  <dd className="font-medium text-gray-900">{formatDate(payment.paymentDate)}</dd>
                </div>
                <div>
                  <dt className="text-gray-500">Amount</dt>
                  <dd className="font-semibold text-gray-900">{fmt(payment.amount)}</dd>
                </div>
                <div>
                  <dt className="text-gray-500">Method</dt>
                  <dd className="font-medium text-gray-900">{payment.paymentType}</dd>
                </div>
                <div>
                  <dt className="text-gray-500">Category</dt>
                  <dd className="font-medium text-gray-900">
                    {LEDGER_TYPE_LABELS[payment.category ?? 'AgainstInvoice'] ?? payment.category}
                  </dd>
                </div>
                {payment.referenceNo && (
                  <div className="col-span-2">
                    <dt className="text-gray-500">Reference</dt>
                    <dd className="font-mono text-xs text-gray-800">{payment.referenceNo}</dd>
                  </div>
                )}
              </dl>

              {payment.allocations.length > 0 ? (
                <div>
                  <p className="text-xs font-medium uppercase tracking-wide text-gray-500 mb-2">
                    Invoice Allocations
                  </p>
                  <div className="border border-gray-200 rounded-lg overflow-hidden">
                    <table className="min-w-full text-sm">
                      <thead className="bg-gray-50 text-left text-xs text-gray-500">
                        <tr>
                          <th className="px-3 py-2">Invoice</th>
                          <th className="px-3 py-2 text-right">Applied</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-gray-100">
                        {payment.allocations.map((row) => (
                          <tr key={row.id || row.invoiceId}>
                            <td className="px-3 py-2">{row.invoiceNo || `#${row.invoiceId}`}</td>
                            <td className="px-3 py-2 text-right tabular-nums">{fmt(row.appliedAmount)}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              ) : (
                <p className="text-sm text-gray-600 rounded-lg bg-gray-50 border border-gray-100 px-3 py-2">
                  No invoice allocations — recorded as advance payment.
                </p>
              )}

              {payment.notes && (
                <div>
                  <p className="text-xs font-medium uppercase tracking-wide text-gray-500 mb-1">Notes</p>
                  <p className="text-sm text-gray-700">{payment.notes}</p>
                </div>
              )}
            </>
          )}
        </div>

        <div className="border-t border-gray-100 px-5 py-3 flex justify-end">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-gray-100 rounded-lg hover:bg-gray-200"
          >
            Close
          </button>
        </div>
      </div>
    </div>
  );
};

export default PartyLedgerPaymentDetailModal;
