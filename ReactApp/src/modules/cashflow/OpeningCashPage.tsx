import React, { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { getApiErrorMessage } from '../../services/api';
import { useBranchStore } from '../../stores/useBranchStore';
import { useBusinessCurrency } from '../../hooks/useBusinessCurrency';
import {
  cashFlowService,
  type PosRegisterDto,
  type RegisterOpeningHintDto,
} from './cashFlowService';

export default function OpeningCashPage() {
  const navigate = useNavigate();
  const { selectedBranchId, getSelectedBranch } = useBranchStore();
  const { fmt, symbol, loading: currencyLoading } = useBusinessCurrency();
  const branchId = selectedBranchId ?? 0;
  const branchName = getSelectedBranch()?.name ?? `Branch ${branchId}`;

  const [registers, setRegisters] = useState<PosRegisterDto[]>([]);
  const [selectedRegisterId, setSelectedRegisterId] = useState<number | null>(null);
  const [hint, setHint] = useState<RegisterOpeningHintDto | null>(null);
  const [overrideOpening, setOverrideOpening] = useState(false);
  const [amount, setAmount] = useState('');
  const [overrideReason, setOverrideReason] = useState('');
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadRegisters = useCallback(async () => {
    if (branchId <= 0) return;
    setLoading(true);
    setError(null);
    try {
      const res = await cashFlowService.getRegisters(branchId);
      const list = res.data ?? [];
      setRegisters(list);
      const defaultReg = list.find((r) => r.isDefault) ?? list[0];
      setSelectedRegisterId(defaultReg?.id ?? null);
    } catch (e) {
      setError(getApiErrorMessage(e, 'Failed to load registers.'));
    } finally {
      setLoading(false);
    }
  }, [branchId]);

  const loadHint = useCallback(async () => {
    if (branchId <= 0 || !selectedRegisterId) return;
    try {
      const res = await cashFlowService.getOpeningHint(branchId, selectedRegisterId);
      const data = res.data;
      setHint(data);
      if (data.hasOpenSessionToday) return;
      if (!data.isFirstTime && !overrideOpening) {
        setAmount(String(data.suggestedOpeningBalance));
      } else if (data.isFirstTime) {
        setAmount('');
      }
    } catch (e) {
      setError(getApiErrorMessage(e, 'Failed to load opening hint.'));
    }
  }, [branchId, selectedRegisterId, overrideOpening]);

  useEffect(() => { void loadRegisters(); }, [loadRegisters]);
  useEffect(() => { void loadHint(); }, [loadHint]);

  useEffect(() => {
    if (!hint || hint.isFirstTime || overrideOpening) return;
    setAmount(String(hint.suggestedOpeningBalance));
  }, [hint, overrideOpening]);

  const parsed = parseFloat(amount);
  const hasValidAmount = !isNaN(parsed) && parsed >= 0;
  const suggested = hint?.suggestedOpeningBalance ?? 0;
  const needsOverrideReason =
    overrideOpening && hasValidAmount && parsed !== suggested && !hint?.isFirstTime;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedRegisterId) {
      setError('Please select a register.');
      return;
    }
    if (!hasValidAmount) {
      setError('Please enter a valid amount (0 or more).');
      return;
    }
    if (needsOverrideReason && !overrideReason.trim()) {
      setError('Override reason is required when opening balance differs from last closing.');
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      await cashFlowService.openRegister(
        branchId,
        selectedRegisterId,
        parsed,
        overrideOpening,
        overrideReason.trim() || undefined,
      );
      navigate('/cashflow');
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to open register.'));
    } finally {
      setSubmitting(false);
    }
  };

  if (branchId <= 0) {
    return (
      <div className="flex h-64 items-center justify-center text-gray-500">
        Please select a branch first.
      </div>
    );
  }

  if (loading || currencyLoading) {
    return <div className="flex h-64 items-center justify-center text-gray-400">Loading…</div>;
  }

  return (
    <div className="mx-auto mt-10 max-w-lg px-4">
      <div className="overflow-hidden rounded-2xl border border-gray-100 bg-white shadow-sm">
        <div className="bg-emerald-600 px-6 py-5 text-white">
          <h1 className="text-xl font-bold">Open Cash Register</h1>
          <p className="mt-1 text-sm text-emerald-100">
            {branchName} —{' '}
            {new Date().toLocaleDateString(undefined, {
              weekday: 'long',
              year: 'numeric',
              month: 'long',
              day: 'numeric',
            })}
          </p>
        </div>

        <div className="space-y-5 p-6">
          {registers.length === 0 ? (
            <div className="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
              No registers configured for this branch. Restart the API to seed the default register, or add one from settings.
            </div>
          ) : (
            <>
              <div>
                <label className="mb-1.5 block text-sm font-medium text-gray-700">Register</label>
                <select
                  value={selectedRegisterId ?? ''}
                  onChange={(e) => {
                    setSelectedRegisterId(Number(e.target.value));
                    setOverrideOpening(false);
                    setOverrideReason('');
                  }}
                  className="w-full rounded-xl border border-gray-200 px-4 py-3 text-gray-800 focus:border-transparent focus:outline-none focus:ring-2 focus:ring-emerald-400"
                >
                  {registers.map((r) => (
                    <option key={r.id} value={r.id} disabled={!r.isActive}>
                      {r.name}
                      {r.hasOpenSession ? ' (Open)' : ''}
                    </option>
                  ))}
                </select>
              </div>

              {hint?.hasOpenSessionToday && hint.openSession ? (
                <div className="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
                  <p className="font-semibold">Register already open</p>
                  <p className="mt-1">
                    Opening balance: <span className="font-bold">{fmt(hint.openSession.openingBalance)}</span>
                    {hint.openSession.isOpeningOverride && (
                      <span className="ml-2 text-xs text-amber-700">(manual override)</span>
                    )}
                  </p>
                  <button
                    type="button"
                    onClick={() => navigate('/cashflow')}
                    className="mt-3 text-xs text-amber-700 underline"
                  >
                    Go to dashboard
                  </button>
                </div>
              ) : (
                <form onSubmit={handleSubmit} className="space-y-5">
                  {!hint?.isFirstTime && (
                    <div className="rounded-xl border border-blue-100 bg-blue-50 p-4 text-sm text-blue-900">
                      <p className="font-medium">Last closing balance</p>
                      <p className="mt-1 text-2xl font-bold">{fmt(hint?.lastClosingBalance ?? 0)}</p>
                      {hint?.lastClosedAt && (
                        <p className="mt-1 text-xs text-blue-700">
                          Closed {new Date(hint.lastClosedAt).toLocaleString()}
                        </p>
                      )}
                      <p className="mt-2 text-xs text-blue-700">
                        Opening balance is auto-filled from the previous closing.
                      </p>
                    </div>
                  )}

                  {hint?.isFirstTime && (
                    <div className="rounded-xl border border-gray-200 bg-gray-50 p-4 text-sm text-gray-700">
                      <p className="font-medium">First-time opening</p>
                      <p className="mt-1 text-xs">Enter the physical cash count in the drawer to start tracking.</p>
                    </div>
                  )}

                  {!hint?.isFirstTime && (
                    <label className="flex cursor-pointer items-center gap-3 rounded-xl border border-gray-200 px-4 py-3">
                      <input
                        type="checkbox"
                        checked={overrideOpening}
                        onChange={(e) => setOverrideOpening(e.target.checked)}
                        className="h-4 w-4 rounded border-gray-300 text-emerald-600"
                      />
                      <span className="text-sm text-gray-700">Override opening balance</span>
                    </label>
                  )}

                  <div>
                    <label className="mb-1.5 block text-sm font-medium text-gray-700">
                      Opening Balance {!hint?.isFirstTime && !overrideOpening && (
                        <span className="font-normal text-gray-400">(auto)</span>
                      )}
                      <span className="text-red-500"> *</span>
                    </label>
                    <div className="relative">
                      <span className="absolute left-3.5 top-1/2 -translate-y-1/2 font-medium text-gray-400">
                        {symbol}
                      </span>
                      <input
                        type="number"
                        min="0"
                        step="0.01"
                        value={amount}
                        onChange={(e) => setAmount(e.target.value)}
                        readOnly={!hint?.isFirstTime && !overrideOpening}
                        required
                        className={`w-full rounded-xl border border-gray-200 py-3 pl-8 pr-4 text-lg font-semibold text-gray-800 transition focus:border-transparent focus:outline-none focus:ring-2 focus:ring-emerald-400 ${
                          !hint?.isFirstTime && !overrideOpening ? 'bg-gray-50' : ''
                        }`}
                      />
                    </div>
                  </div>

                  {needsOverrideReason && (
                    <div>
                      <label className="mb-1.5 block text-sm font-medium text-gray-700">
                        Override reason <span className="text-red-500">*</span>
                      </label>
                      <textarea
                        value={overrideReason}
                        onChange={(e) => setOverrideReason(e.target.value)}
                        rows={2}
                        required
                        placeholder="Why does opening differ from last closing?"
                        className="w-full resize-none rounded-xl border border-gray-200 px-4 py-3 text-gray-700 focus:outline-none focus:ring-2 focus:ring-emerald-400"
                      />
                    </div>
                  )}

                  {error && (
                    <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">
                      {error}
                    </div>
                  )}

                  <div className="flex gap-3">
                    <button
                      type="button"
                      onClick={() => navigate('/cashflow')}
                      className="flex-1 rounded-xl border border-gray-200 py-3 font-medium text-gray-700 transition-colors hover:bg-gray-50"
                    >
                      Cancel
                    </button>
                    <button
                      type="submit"
                      disabled={submitting}
                      className="flex-1 rounded-xl bg-emerald-600 py-3 font-semibold text-white transition-colors hover:bg-emerald-700 disabled:opacity-60"
                    >
                      {submitting ? 'Opening…' : 'Open Register'}
                    </button>
                  </div>
                </form>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
}
