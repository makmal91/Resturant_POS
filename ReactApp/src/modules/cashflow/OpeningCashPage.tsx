import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { getApiErrorMessage } from '../../services/api';
import { useBranchStore } from '../../stores/useBranchStore';
import { cashFlowService, type CashRegisterDto } from './cashFlowService';

const fmt = (n: number) =>
  new Intl.NumberFormat(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(n);

export default function OpeningCashPage() {
  const navigate = useNavigate();
  const { selectedBranchId, getSelectedBranch } = useBranchStore();
  const branchId = selectedBranchId ?? 0;
  const branchName = getSelectedBranch()?.name ?? `Branch ${branchId}`;

  const [amount, setAmount] = useState('');
  const [notes, setNotes] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [existing, setExisting] = useState<CashRegisterDto | null | undefined>(undefined);
  const [checkingExisting, setCheckingExisting] = useState(true);

  useEffect(() => {
    if (branchId <= 0) return;
    cashFlowService
      .getTodayRegister(branchId)
      .then((r) => setExisting(r.data))
      .catch(() => setExisting(null))
      .finally(() => setCheckingExisting(false));
  }, [branchId]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const parsed = parseFloat(amount);
    if (isNaN(parsed) || parsed < 0) {
      setError('Please enter a valid amount (0 or more).');
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      await cashFlowService.openCash(branchId, parsed, notes || undefined);
      navigate('/cashflow');
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to open cash register.'));
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

  if (checkingExisting) {
    return <div className="flex items-center justify-center h-64 text-gray-400">Checking register…</div>;
  }

  return (
    <div className="max-w-lg mx-auto mt-10 px-4">
      <div className="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden">
        {/* Header */}
        <div className="bg-emerald-600 px-6 py-5 text-white">
          <h1 className="text-xl font-bold">Open Cash Register</h1>
          <p className="text-emerald-100 text-sm mt-1">
            {branchName} —{' '}
            {new Date().toLocaleDateString(undefined, { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}
          </p>
        </div>

        <div className="p-6 space-y-5">
          {/* Already open */}
          {existing && !existing.isClosed && (
            <div className="bg-amber-50 border border-amber-200 rounded-xl p-4 text-amber-800 text-sm">
              <p className="font-semibold mb-1">Register already open</p>
              <p>
                Opening balance: <span className="font-bold">{fmt(existing.openingCash)}</span>
              </p>
              <button
                onClick={() => navigate('/cashflow')}
                className="mt-3 text-amber-700 underline text-xs"
              >
                Go to dashboard
              </button>
            </div>
          )}

          {/* Already closed */}
          {existing?.isClosed && (
            <div className="bg-gray-50 border border-gray-200 rounded-xl p-4 text-gray-600 text-sm">
              <p className="font-semibold mb-1">Register already closed for today</p>
              <p>
                Closing cash: <span className="font-bold">{fmt(existing.closingCash ?? 0)}</span>
              </p>
              <button
                onClick={() => navigate('/cashflow')}
                className="mt-3 text-gray-500 underline text-xs"
              >
                Go to dashboard
              </button>
            </div>
          )}

          {!existing && (
            <form onSubmit={handleSubmit} className="space-y-5">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1.5">
                  Opening Cash Amount <span className="text-red-500">*</span>
                </label>
                <div className="relative">
                  <span className="absolute left-3.5 top-1/2 -translate-y-1/2 text-gray-400 font-medium">$</span>
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    value={amount}
                    onChange={(e) => setAmount(e.target.value)}
                    placeholder="0.00"
                    required
                    className="w-full pl-8 pr-4 py-3 border border-gray-200 rounded-xl text-gray-800 text-lg font-semibold focus:outline-none focus:ring-2 focus:ring-emerald-400 focus:border-transparent transition"
                  />
                </div>
                <p className="text-xs text-gray-400 mt-1">Enter 0 if starting with no cash.</p>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1.5">Notes (optional)</label>
                <textarea
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                  rows={2}
                  placeholder="Any remarks for this shift…"
                  className="w-full px-4 py-3 border border-gray-200 rounded-xl text-gray-700 focus:outline-none focus:ring-2 focus:ring-emerald-400 transition resize-none"
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
                  disabled={submitting}
                  className="flex-1 py-3 bg-emerald-600 text-white rounded-xl font-semibold hover:bg-emerald-700 disabled:opacity-60 transition-colors"
                >
                  {submitting ? 'Opening…' : 'Open Register'}
                </button>
              </div>
            </form>
          )}
        </div>
      </div>
    </div>
  );
}
