import React, { useEffect, useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { getApiErrorMessage } from '../../services/api';
import { useBranchStore } from '../../stores/useBranchStore';
import { cashFlowService, type DailyCashSummaryDto } from './cashFlowService';

const fmt = (n: number) =>
  new Intl.NumberFormat(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(n);

export default function ClosingCashPage() {
  const navigate = useNavigate();
  const { selectedBranchId, getSelectedBranch } = useBranchStore();
  const branchId = selectedBranchId ?? 0;
  const branchName = getSelectedBranch()?.name ?? `Branch ${branchId}`;

  const [actualCash, setActualCash] = useState('');
  const [notes, setNotes] = useState('');
  const [summary, setSummary] = useState<DailyCashSummaryDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadSummary = useCallback(async () => {
    if (branchId <= 0) return;
    setLoading(true);
    try {
      const res = await cashFlowService.getDailySummary(branchId);
      setSummary(res.data);
    } catch (e) {
      setError(getApiErrorMessage(e, 'Failed to load today summary.'));
    } finally {
      setLoading(false);
    }
  }, [branchId]);

  useEffect(() => { loadSummary(); }, [loadSummary]);

  const actual = parseFloat(actualCash);
  const hasValidAmount = !isNaN(actual) && actual >= 0;
  const diff = hasValidAmount && summary ? actual - summary.expectedClosingCash : null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!hasValidAmount) {
      setError('Please enter a valid cash amount.');
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      await cashFlowService.closeCash(branchId, actual, notes || undefined);
      navigate('/cashflow');
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to close cash register.'));
    } finally {
      setSubmitting(false);
    }
  };

  if (branchId <= 0) {
    return (
      <div className="flex items-center justify-center h-64 text-gray-500">
        Please select a branch first.
      </div>
    );
  }

  return (
    <div className="max-w-lg mx-auto mt-10 px-4">
      <div className="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden">
        {/* Header */}
        <div className="bg-orange-500 px-6 py-5 text-white">
          <h1 className="text-xl font-bold">Close Cash Register</h1>
          <p className="text-orange-100 text-sm mt-1">
            {branchName} —{' '}
            {new Date().toLocaleDateString(undefined, { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}
          </p>
        </div>

        <div className="p-6 space-y-5">
          {loading ? (
            <div className="space-y-3">
              {[...Array(4)].map((_, i) => (
                <div key={i} className="h-8 bg-gray-100 rounded-lg animate-pulse" />
              ))}
            </div>
          ) : summary?.isClosed ? (
            <div className="bg-gray-50 border border-gray-200 rounded-xl p-4 text-gray-600 text-sm">
              <p className="font-semibold">Register already closed for today.</p>
              <p className="mt-1">
                Closing cash: <span className="font-bold">{fmt(summary.closingCash ?? 0)}</span> | Difference:{' '}
                <span
                  className={`font-bold ${
                    (summary.difference ?? 0) >= 0 ? 'text-emerald-600' : 'text-red-600'
                  }`}
                >
                  {fmt(summary.difference ?? 0)}
                </span>
              </p>
              <button
                onClick={() => navigate('/cashflow')}
                className="mt-3 text-gray-500 underline text-xs"
              >
                Go to dashboard
              </button>
            </div>
          ) : (
            <>
              {/* Today's summary */}
              {summary && (
                <div className="bg-gray-50 rounded-xl p-4 space-y-2 text-sm">
                  <h3 className="font-semibold text-gray-700 mb-2">Today's Summary</h3>
                  {[
                    ['Opening Cash', fmt(summary.openingCash)],
                    ['+ Cash Sales', fmt(summary.totalCashSales)],
                    ['+ Cash In', fmt(summary.totalCashIn)],
                    ['− Expenses (cash)', fmt(summary.totalExpensesCash)],
                    ['− Cash Out', fmt(summary.totalCashOut)],
                    ['− Bank Transfers', fmt(summary.totalBankTransfers)],
                  ].map(([label, val]) => (
                    <div key={label} className="flex justify-between text-gray-600">
                      <span>{label}</span>
                      <span className="font-medium">{val}</span>
                    </div>
                  ))}
                  <div className="border-t border-gray-200 pt-2 flex justify-between font-bold text-gray-800">
                    <span>Expected Closing Cash</span>
                    <span className="text-blue-700">{fmt(summary.expectedClosingCash)}</span>
                  </div>
                </div>
              )}

              <form onSubmit={handleSubmit} className="space-y-5">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1.5">
                    Actual Cash in Drawer <span className="text-red-500">*</span>
                  </label>
                  <div className="relative">
                    <span className="absolute left-3.5 top-1/2 -translate-y-1/2 text-gray-400 font-medium">$</span>
                    <input
                      type="number"
                      min="0"
                      step="0.01"
                      value={actualCash}
                      onChange={(e) => setActualCash(e.target.value)}
                      placeholder="0.00"
                      required
                      className="w-full pl-8 pr-4 py-3 border border-gray-200 rounded-xl text-gray-800 text-lg font-semibold focus:outline-none focus:ring-2 focus:ring-orange-400 focus:border-transparent transition"
                    />
                  </div>
                </div>

                {/* Live difference indicator */}
                {hasValidAmount && summary && diff !== null && (
                  <div
                    className={`rounded-xl px-4 py-3 border text-sm font-medium ${
                      diff === 0
                        ? 'bg-emerald-50 border-emerald-200 text-emerald-700'
                        : diff > 0
                        ? 'bg-blue-50 border-blue-200 text-blue-700'
                        : 'bg-red-50 border-red-200 text-red-600'
                    }`}
                  >
                    {diff === 0 && '✅ Exact match — no difference.'}
                    {diff > 0 && `📈 OVER by ${fmt(diff)} — you have more cash than expected.`}
                    {diff < 0 && `⚠️ SHORT by ${fmt(Math.abs(diff))} — you have less cash than expected.`}
                  </div>
                )}

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1.5">Notes (optional)</label>
                  <textarea
                    value={notes}
                    onChange={(e) => setNotes(e.target.value)}
                    rows={2}
                    placeholder="Explain any discrepancy…"
                    className="w-full px-4 py-3 border border-gray-200 rounded-xl text-gray-700 focus:outline-none focus:ring-2 focus:ring-orange-400 transition resize-none"
                  />
                </div>

                {error && (
                  <div className="bg-red-50 border border-red-200 text-red-600 rounded-xl px-4 py-3 text-sm">
                    {error}
                  </div>
                )}

                <div className="flex gap-3">
                  <button
                    type="button"
                    onClick={() => navigate('/cashflow')}
                    className="flex-1 py-3 border border-gray-200 rounded-xl text-gray-700 font-medium hover:bg-gray-50 transition-colors"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    disabled={submitting || !hasValidAmount}
                    className="flex-1 py-3 bg-orange-500 text-white rounded-xl font-semibold hover:bg-orange-600 disabled:opacity-60 transition-colors"
                  >
                    {submitting ? 'Closing…' : 'Close Register'}
                  </button>
                </div>
              </form>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
