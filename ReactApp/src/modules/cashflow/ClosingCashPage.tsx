import React, { useCallback, useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { getApiErrorMessage } from '../../services/api';
import { useBranchStore } from '../../stores/useBranchStore';
import { useBusinessCurrency } from '../../hooks/useBusinessCurrency';
import {
  cashFlowService,
  type PosRegisterDto,
  type RegisterClosePreviewDto,
} from './cashFlowService';

export default function ClosingCashPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { selectedBranchId, getSelectedBranch } = useBranchStore();
  const { fmt, symbol } = useBusinessCurrency();
  const branchId = selectedBranchId ?? 0;
  const branchName = getSelectedBranch()?.name ?? `Branch ${branchId}`;

  const [registers, setRegisters] = useState<PosRegisterDto[]>([]);
  const [selectedRegisterId, setSelectedRegisterId] = useState<number | null>(null);
  const [preview, setPreview] = useState<RegisterClosePreviewDto | null>(null);
  const [physicalCash, setPhysicalCash] = useState('');
  const [mismatchReason, setMismatchReason] = useState('');
  const [notes, setNotes] = useState('');
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadRegisters = useCallback(async () => {
    if (branchId <= 0) return;
    try {
      const res = await cashFlowService.getRegisters(branchId);
      const openRegs = (res.data ?? []).filter((r) => r.hasOpenSession);
      setRegisters(openRegs);
      const fromQuery = Number(searchParams.get('registerId'));
      const initial =
        openRegs.find((r) => r.id === fromQuery)?.id ??
        openRegs.find((r) => r.isDefault)?.id ??
        openRegs[0]?.id ??
        null;
      setSelectedRegisterId(initial);
    } catch (e) {
      setError(getApiErrorMessage(e, 'Failed to load registers.'));
    }
  }, [branchId, searchParams]);

  const loadPreview = useCallback(async () => {
    if (branchId <= 0 || !selectedRegisterId) {
      setPreview(null);
      setLoading(false);
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const res = await cashFlowService.getClosePreview(branchId, selectedRegisterId);
      setPreview(res.data);
      if (res.data.isClosed) {
        setPhysicalCash('');
      }
    } catch (e) {
      setPreview(null);
      setError(getApiErrorMessage(e, 'Failed to load close preview.'));
    } finally {
      setLoading(false);
    }
  }, [branchId, selectedRegisterId]);

  useEffect(() => { void loadRegisters(); }, [loadRegisters]);
  useEffect(() => { void loadPreview(); }, [loadPreview]);

  const actual = parseFloat(physicalCash);
  const hasValidAmount = !isNaN(actual) && actual >= 0;
  const diff = hasValidAmount && preview ? actual - preview.expectedCash : null;
  const hasMismatch = diff !== null && Math.abs(diff) > 0.005;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedRegisterId) {
      setError('No open register selected.');
      return;
    }
    if (!hasValidAmount) {
      setError('Please enter a valid physical cash amount.');
      return;
    }
    if (hasMismatch && !mismatchReason.trim()) {
      setError('Cash mismatch detected. Please provide a reason before closing.');
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      await cashFlowService.closeRegister(
        branchId,
        selectedRegisterId,
        actual,
        mismatchReason.trim() || undefined,
        notes.trim() || undefined,
      );
      navigate('/cashflow');
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to close register.'));
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

  return (
    <div className="mx-auto mt-10 max-w-lg px-4">
      <div className="overflow-hidden rounded-2xl border border-gray-100 bg-white shadow-sm">
        <div className="bg-orange-500 px-6 py-5 text-white">
          <h1 className="text-xl font-bold">Close Cash Register</h1>
          <p className="mt-1 text-sm text-orange-100">
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
          {registers.length === 0 && !loading ? (
            <div className="rounded-xl border border-gray-200 bg-gray-50 p-4 text-sm text-gray-600">
              <p className="font-semibold">No open registers</p>
              <p className="mt-1">Open a register before closing.</p>
              <button
                type="button"
                onClick={() => navigate('/cashflow/opening')}
                className="mt-3 text-sm text-orange-600 underline"
              >
                Open register
              </button>
            </div>
          ) : (
            <>
              {registers.length > 1 && (
                <div>
                  <label className="mb-1.5 block text-sm font-medium text-gray-700">Register</label>
                  <select
                    value={selectedRegisterId ?? ''}
                    onChange={(e) => setSelectedRegisterId(Number(e.target.value))}
                    className="w-full rounded-xl border border-gray-200 px-4 py-3 text-gray-800 focus:outline-none focus:ring-2 focus:ring-orange-400"
                  >
                    {registers.map((r) => (
                      <option key={r.id} value={r.id}>
                        {r.name}
                      </option>
                    ))}
                  </select>
                </div>
              )}

              {loading ? (
                <div className="space-y-3">
                  {[...Array(4)].map((_, i) => (
                    <div key={i} className="h-8 animate-pulse rounded-lg bg-gray-100" />
                  ))}
                </div>
              ) : preview?.isClosed ? (
                <div className="rounded-xl border border-gray-200 bg-gray-50 p-4 text-sm text-gray-600">
                  <p className="font-semibold">Register already closed for today.</p>
                  <button
                    type="button"
                    onClick={() => navigate('/cashflow')}
                    className="mt-3 text-xs underline"
                  >
                    Go to dashboard
                  </button>
                </div>
              ) : preview ? (
                <form onSubmit={handleSubmit} className="space-y-5">
                  <div className="rounded-xl border border-gray-100 bg-gray-50 p-4 text-sm">
                    <p className="mb-3 font-semibold text-gray-700">{preview.registerName}</p>
                    <div className="grid grid-cols-2 gap-3">
                      {[
                        ['Opening', preview.openingBalance],
                        ['Cash Sales', preview.totalCashSales],
                        ['Expenses', preview.totalExpensesCash],
                        ['Cash In', preview.totalCashIn],
                        ['Cash Out', preview.totalCashOut],
                        ['Adjustments', preview.totalAdjustments],
                      ].map(([label, val]) => (
                        <div key={String(label)}>
                          <p className="text-xs text-gray-500">{label}</p>
                          <p className="font-semibold text-gray-800">{fmt(Number(val))}</p>
                        </div>
                      ))}
                    </div>
                    <div className="mt-4 border-t border-gray-200 pt-3">
                      <p className="text-xs text-gray-500">Expected cash in drawer</p>
                      <p className="text-2xl font-bold text-blue-700">{fmt(preview.expectedCash)}</p>
                    </div>
                  </div>

                  <div>
                    <label className="mb-1.5 block text-sm font-medium text-gray-700">
                      Physical cash counted <span className="text-red-500">*</span>
                    </label>
                    <div className="relative">
                      <span className="absolute left-3.5 top-1/2 -translate-y-1/2 font-medium text-gray-400">
                        {symbol}
                      </span>
                      <input
                        type="number"
                        min="0"
                        step="0.01"
                        value={physicalCash}
                        onChange={(e) => setPhysicalCash(e.target.value)}
                        required
                        className="w-full rounded-xl border border-gray-200 py-3 pl-8 pr-4 text-lg font-semibold focus:outline-none focus:ring-2 focus:ring-orange-400"
                      />
                    </div>
                  </div>

                  {diff !== null && (
                    <div
                      className={`rounded-xl border px-4 py-3 ${
                        hasMismatch
                          ? 'border-red-200 bg-red-50 text-red-800'
                          : 'border-emerald-200 bg-emerald-50 text-emerald-800'
                      }`}
                    >
                      <p className="text-sm font-medium">
                        {hasMismatch ? 'Cash mismatch detected' : 'Cash matches expected'}
                      </p>
                      <p className="mt-1 text-xl font-bold">
                        {diff >= 0 ? '+' : ''}
                        {fmt(diff)}
                      </p>
                    </div>
                  )}

                  {hasMismatch && (
                    <div>
                      <label className="mb-1.5 block text-sm font-medium text-gray-700">
                        Mismatch reason <span className="text-red-500">*</span>
                      </label>
                      <textarea
                        value={mismatchReason}
                        onChange={(e) => setMismatchReason(e.target.value)}
                        rows={2}
                        required
                        placeholder="Explain the over/short amount…"
                        className="w-full resize-none rounded-xl border border-gray-200 px-4 py-3 focus:outline-none focus:ring-2 focus:ring-orange-400"
                      />
                    </div>
                  )}

                  <div>
                    <label className="mb-1.5 block text-sm font-medium text-gray-700">Notes (optional)</label>
                    <textarea
                      value={notes}
                      onChange={(e) => setNotes(e.target.value)}
                      rows={2}
                      className="w-full resize-none rounded-xl border border-gray-200 px-4 py-3 focus:outline-none focus:ring-2 focus:ring-orange-400"
                    />
                  </div>

                  {error && (
                    <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">
                      {error}
                    </div>
                  )}

                  <div className="flex gap-3">
                    <button
                      type="button"
                      onClick={() => navigate('/cashflow')}
                      className="flex-1 rounded-xl border border-gray-200 py-3 font-medium text-gray-700 hover:bg-gray-50"
                    >
                      Cancel
                    </button>
                    <button
                      type="submit"
                      disabled={submitting}
                      className="flex-1 rounded-xl bg-orange-500 py-3 font-semibold text-white hover:bg-orange-600 disabled:opacity-60"
                    >
                      {submitting ? 'Closing…' : 'Close Register'}
                    </button>
                  </div>
                </form>
              ) : null}
            </>
          )}
        </div>
      </div>
    </div>
  );
}
