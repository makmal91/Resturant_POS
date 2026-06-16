import React, { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { getApiErrorMessage } from '../../services/api';
import { useBranchStore } from '../../stores/useBranchStore';
import {
  cashFlowService,
  type CashFlowTransactionDto,
  type CashFlowTransactionType,
  type CashFlowPaymentMethod,
} from './cashFlowService';

// ─── Helpers ──────────────────────────────────────────────────────────────────

const fmt = (n: number) =>
  new Intl.NumberFormat(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(n);

const formatDate = (s: string) => {
  const d = new Date(s);
  return isNaN(d.getTime())
    ? '—'
    : d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
};

const TYPE_COLORS: Record<CashFlowTransactionType, string> = {
  Sale:           'bg-emerald-100 text-emerald-700',
  Expense:        'bg-red-100 text-red-600',
  CashIn:         'bg-blue-100 text-blue-700',
  CashOut:        'bg-orange-100 text-orange-700',
  BankTransfer:   'bg-purple-100 text-purple-700',
  OpeningBalance: 'bg-gray-100 text-gray-600',
  ClosingBalance: 'bg-gray-100 text-gray-700',
};

const TYPE_LABELS: Record<CashFlowTransactionType, string> = {
  Sale:           'Sale',
  Expense:        'Expense',
  CashIn:         'Cash In',
  CashOut:        'Cash Out',
  BankTransfer:   'Bank Transfer',
  OpeningBalance: 'Opening',
  ClosingBalance: 'Closing',
};

const isInflow = (type: CashFlowTransactionType) =>
  type === 'Sale' || type === 'CashIn' || type === 'OpeningBalance';

const TX_TYPES: CashFlowTransactionType[] = [
  'Sale', 'Expense', 'CashIn', 'CashOut', 'BankTransfer', 'OpeningBalance', 'ClosingBalance',
];

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function CashLedgerPage() {
  const navigate = useNavigate();
  const { selectedBranchId } = useBranchStore();
  const branchId = selectedBranchId ?? 0;

  const today = new Date().toISOString().slice(0, 10);
  const [fromDate, setFromDate]       = useState(today);
  const [toDate, setToDate]           = useState(today);
  const [typeFilter, setTypeFilter]   = useState<CashFlowTransactionType | ''>('');
  const [methodFilter, setMethod]     = useState<CashFlowPaymentMethod | ''>('');
  const [page, setPage]               = useState(1);
  const pageSize                      = 50;

  const [rows, setRows]     = useState<CashFlowTransactionDto[]>([]);
  const [total, setTotal]   = useState(0);
  const [pages, setPages]   = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError]   = useState<string | null>(null);

  const load = useCallback(async () => {
    if (branchId <= 0) return;
    setLoading(true);
    setError(null);
    try {
      const res = await cashFlowService.getLedger(branchId, {
        fromDate,
        toDate,
        transactionType: typeFilter || null,
        paymentMethod: methodFilter || null,
        page,
        pageSize,
      });
      setRows(res.data.transactions);
      setTotal(res.data.totalRecords);
      setPages(res.data.totalPages);
    } catch (e) {
      setError(getApiErrorMessage(e, 'Failed to load ledger.'));
    } finally {
      setLoading(false);
    }
  }, [branchId, fromDate, toDate, typeFilter, methodFilter, page, pageSize]);

  useEffect(() => { load(); }, [load]);

  const totalIn  = rows.filter((r) => isInflow(r.transactionType as CashFlowTransactionType)).reduce((a, r) => a + r.amount, 0);
  const totalOut = rows.filter((r) => !isInflow(r.transactionType as CashFlowTransactionType)).reduce((a, r) => a + r.amount, 0);

  return (
    <div className="space-y-4 p-4 md:p-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <h1 className="text-2xl font-bold text-gray-800">Cash Flow Ledger</h1>
        <div className="flex gap-2">
          <button
            onClick={() => navigate('/cashflow/transaction')}
            className="px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 transition-colors"
          >
            + New Transaction
          </button>
          <button
            onClick={() => navigate('/cashflow')}
            className="px-4 py-2 bg-white border border-gray-200 text-gray-700 text-sm font-medium rounded-lg hover:bg-gray-50 transition-colors"
          >
            Dashboard
          </button>
        </div>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-xl border border-gray-100 p-4">
        <div className="grid grid-cols-2 md:grid-cols-4 xl:grid-cols-5 gap-3">
          <div>
            <label className="text-xs text-gray-500 font-medium mb-1 block">From Date</label>
            <input
              type="date"
              value={fromDate}
              onChange={(e) => { setFromDate(e.target.value); setPage(1); }}
              className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-400"
            />
          </div>
          <div>
            <label className="text-xs text-gray-500 font-medium mb-1 block">To Date</label>
            <input
              type="date"
              value={toDate}
              onChange={(e) => { setToDate(e.target.value); setPage(1); }}
              className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-400"
            />
          </div>
          <div>
            <label className="text-xs text-gray-500 font-medium mb-1 block">Type</label>
            <select
              value={typeFilter}
              onChange={(e) => { setTypeFilter(e.target.value as CashFlowTransactionType | ''); setPage(1); }}
              className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-400"
            >
              <option value="">All Types</option>
              {TX_TYPES.map((t) => (
                <option key={t} value={t}>{TYPE_LABELS[t]}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="text-xs text-gray-500 font-medium mb-1 block">Method</label>
            <select
              value={methodFilter}
              onChange={(e) => { setMethod(e.target.value as CashFlowPaymentMethod | ''); setPage(1); }}
              className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-400"
            >
              <option value="">All Methods</option>
              <option value="Cash">Cash</option>
              <option value="Bank">Bank</option>
              <option value="Wallet">Wallet</option>
            </select>
          </div>
          <div className="flex items-end">
            <button
              onClick={() => { setPage(1); load(); }}
              className="w-full px-3 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 transition-colors"
            >
              Apply
            </button>
          </div>
        </div>
      </div>

      {/* Summary bar */}
      {!loading && rows.length > 0 && (
        <div className="grid grid-cols-3 gap-4">
          <div className="bg-emerald-50 border border-emerald-100 rounded-xl p-4 text-center">
            <p className="text-xs text-emerald-600 font-medium uppercase">Total In (page)</p>
            <p className="text-lg font-bold text-emerald-700 mt-1">{fmt(totalIn)}</p>
          </div>
          <div className="bg-red-50 border border-red-100 rounded-xl p-4 text-center">
            <p className="text-xs text-red-500 font-medium uppercase">Total Out (page)</p>
            <p className="text-lg font-bold text-red-600 mt-1">{fmt(totalOut)}</p>
          </div>
          <div className="bg-blue-50 border border-blue-100 rounded-xl p-4 text-center">
            <p className="text-xs text-blue-600 font-medium uppercase">Net (page)</p>
            <p className={`text-lg font-bold mt-1 ${totalIn - totalOut >= 0 ? 'text-blue-700' : 'text-red-600'}`}>
              {fmt(totalIn - totalOut)}
            </p>
          </div>
        </div>
      )}

      {/* Error */}
      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 rounded-xl px-5 py-3 text-sm">
          {error}
        </div>
      )}

      {/* Table */}
      <div className="bg-white rounded-xl border border-gray-100 overflow-hidden">
        {loading ? (
          <div className="p-8 text-center text-gray-400">Loading…</div>
        ) : rows.length === 0 ? (
          <div className="p-12 text-center text-gray-400">
            <div className="text-4xl mb-3">📭</div>
            <p className="font-medium">No transactions found</p>
            <p className="text-sm mt-1">Adjust filters or record a new transaction.</p>
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-gray-50 text-gray-600 uppercase text-xs tracking-wide border-b border-gray-100">
                    <th className="text-left px-5 py-3">Date</th>
                    <th className="text-left px-4 py-3">Type</th>
                    <th className="text-left px-4 py-3">Method</th>
                    <th className="text-left px-4 py-3">Description</th>
                    <th className="text-left px-4 py-3">Reference</th>
                    <th className="text-left px-4 py-3">Branch</th>
                    <th className="text-right px-5 py-3">Amount</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-50">
                  {rows.map((row) => {
                    const inflow = isInflow(row.transactionType as CashFlowTransactionType);
                    return (
                      <tr key={row.id} className="hover:bg-gray-50/60 transition-colors">
                        <td className="px-5 py-3 text-gray-600 whitespace-nowrap">
                          {formatDate(row.transactionDate)}
                        </td>
                        <td className="px-4 py-3">
                          <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${TYPE_COLORS[row.transactionType as CashFlowTransactionType] ?? 'bg-gray-100 text-gray-600'}`}>
                            {TYPE_LABELS[row.transactionType as CashFlowTransactionType] ?? row.transactionType}
                          </span>
                        </td>
                        <td className="px-4 py-3 text-gray-500">{row.paymentMethod}</td>
                        <td className="px-4 py-3 text-gray-600 max-w-[180px] truncate">
                          {row.description ?? '—'}
                        </td>
                        <td className="px-4 py-3 text-gray-500 font-mono text-xs">
                          {row.referenceNo ?? '—'}
                        </td>
                        <td className="px-4 py-3 text-gray-500">{row.branchName}</td>
                        <td className={`px-5 py-3 text-right font-bold ${inflow ? 'text-emerald-600' : 'text-red-500'}`}>
                          {inflow ? '+' : '−'}{fmt(row.amount)}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>

            {/* Pagination */}
            {pages > 1 && (
              <div className="flex items-center justify-between px-5 py-3 border-t border-gray-100 text-sm text-gray-500">
                <span>
                  Showing {(page - 1) * pageSize + 1}–{Math.min(page * pageSize, total)} of {total}
                </span>
                <div className="flex gap-1">
                  <button
                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                    disabled={page === 1}
                    className="px-3 py-1.5 rounded-lg border border-gray-200 disabled:opacity-40 hover:bg-gray-50 transition-colors"
                  >
                    ‹
                  </button>
                  {Array.from({ length: Math.min(5, pages) }, (_, i) => {
                    const p = Math.max(1, Math.min(pages - 4, page - 2)) + i;
                    return (
                      <button
                        key={p}
                        onClick={() => setPage(p)}
                        className={`px-3 py-1.5 rounded-lg border transition-colors ${p === page ? 'bg-blue-600 text-white border-blue-600' : 'border-gray-200 hover:bg-gray-50'}`}
                      >
                        {p}
                      </button>
                    );
                  })}
                  <button
                    onClick={() => setPage((p) => Math.min(pages, p + 1))}
                    disabled={page === pages}
                    className="px-3 py-1.5 rounded-lg border border-gray-200 disabled:opacity-40 hover:bg-gray-50 transition-colors"
                  >
                    ›
                  </button>
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}
