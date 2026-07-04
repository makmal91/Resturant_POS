import React, { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useFormModal } from '../../contexts/FormModalContext';
import { getApiErrorMessage } from '../../services/api';
import { useBranchStore } from '../../stores/useBranchStore';
import { useBusinessCurrency } from '../../hooks/useBusinessCurrency';
import {
  cashFlowService,
  type DailyCashSummaryDto,
  type BranchCashSummaryDto,
  type RegisterDashboardDto,
} from './cashFlowService';

const today = () => new Date().toISOString().slice(0, 10);

function DiffBadge({ diff }: { diff: number | null }) {
  if (diff === null) return <span className="text-sm text-gray-400">—</span>;
  const positive = diff >= 0;
  return (
    <span
      className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-sm font-semibold ${
        positive ? 'bg-emerald-50 text-emerald-700' : 'bg-red-50 text-red-600'
      }`}
    >
      {positive ? '▲' : '▼'} {Math.abs(diff).toFixed(2)}
    </span>
  );
}

export default function CashFlowDashboardPage() {
  const navigate = useNavigate();
  const { isOpen } = useFormModal();
  const { selectedBranchId } = useBranchStore();
  const { fmt } = useBusinessCurrency();
  const branchId = selectedBranchId ?? 0;

  const [summary, setSummary] = useState<DailyCashSummaryDto | null>(null);
  const [registerDash, setRegisterDash] = useState<RegisterDashboardDto | null>(null);
  const [branches, setBranches] = useState<BranchCashSummaryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const openSessions = registerDash?.openSessions ?? [];
  const registers = registerDash?.registers ?? [];
  const hasAnyOpen = openSessions.length > 0;
  const defaultOpen = openSessions.find((s) =>
    registers.find((r) => r.id === s.posRegisterId)?.isDefault,
  ) ?? openSessions[0];

  const sessionTotals = openSessions.reduce(
    (acc, s) => ({
      opening: acc.opening + (s.openingBalance ?? 0),
      sales: acc.sales + (s.totalCashSales ?? 0),
      expenses: acc.expenses + (s.totalExpensesCash ?? 0),
      expected: acc.expected + (s.expectedClosing ?? 0),
    }),
    { opening: 0, sales: 0, expenses: 0, expected: 0 },
  );

  const summaryCards: [string, number, string][] = hasAnyOpen
    ? [
        ['Opening Cash', sessionTotals.opening, 'text-gray-800'],
        ['Cash Sales', sessionTotals.sales, 'text-emerald-700'],
        ['Expenses', sessionTotals.expenses, 'text-red-600'],
        ['Expected Closing', sessionTotals.expected, 'text-blue-700'],
      ]
    : summary
      ? [
          ['Opening Cash', summary.openingCash, 'text-gray-800'],
          ['Cash Sales', summary.totalCashSales, 'text-emerald-700'],
          ['Expenses', summary.totalExpensesCash, 'text-red-600'],
          ['Expected Closing', summary.expectedClosingCash, 'text-blue-700'],
        ]
      : [];

  const load = useCallback(async () => {
    if (branchId <= 0) return;
    setLoading(true);
    setError(null);
    try {
      const [sumRes, dashRes, brRes] = await Promise.all([
        cashFlowService.getDailySummary(branchId),
        cashFlowService.getRegisterDashboard(branchId),
        cashFlowService.getBranchSummary(today()),
      ]);
      setSummary(sumRes.data);
      setRegisterDash(dashRes.data);
      setBranches(brRes.data);
    } catch (e) {
      setError(getApiErrorMessage(e, 'Failed to load cash flow data.'));
    } finally {
      setLoading(false);
    }
  }, [branchId]);

  useEffect(() => { void load(); }, [load]);
  useEffect(() => {
    if (!isOpen) void load();
  }, [isOpen, load]);

  if (branchId <= 0) {
    return (
      <div className="flex h-64 items-center justify-center text-gray-500">
        Please select a branch to view cash flow.
      </div>
    );
  }

  return (
    <div className="space-y-6 p-4 md:p-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-800">Cash Flow Dashboard</h1>
          <p className="mt-0.5 text-sm text-gray-500">
            {new Date().toLocaleDateString(undefined, {
              weekday: 'long',
              year: 'numeric',
              month: 'long',
              day: 'numeric',
            })}
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          {!hasAnyOpen && !loading && (
            <button
              onClick={() => navigate('/cashflow/opening')}
              className="rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-700"
            >
              Open Register
            </button>
          )}
          {hasAnyOpen && (
            <button
              onClick={() =>
                navigate(
                  `/cashflow/closing${defaultOpen ? `?registerId=${defaultOpen.posRegisterId}` : ''}`,
                )
              }
              className="rounded-lg bg-orange-500 px-4 py-2 text-sm font-medium text-white hover:bg-orange-600"
            >
              Close Register
            </button>
          )}
          <button
            onClick={() => navigate('/cashflow/register-history')}
            className="rounded-lg border border-gray-200 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
          >
            Register History
          </button>
          <button
            onClick={() => navigate('/cashflow/ledger')}
            className="rounded-lg border border-gray-200 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
          >
            View Ledger
          </button>
        </div>
      </div>

      {!loading && (
        <div
          className={`flex items-center gap-2 rounded-xl px-5 py-3 text-sm font-medium ${
            hasAnyOpen
              ? 'border border-emerald-200 bg-emerald-50 text-emerald-700'
              : 'border border-amber-200 bg-amber-50 text-amber-700'
          }`}
        >
          <span>{hasAnyOpen ? '🟢' : '🟡'}</span>
          {hasAnyOpen
            ? `${openSessions.length} register(s) open — track cash per counter below`
            : 'No register open. Open a drawer to start the day.'}
        </div>
      )}

      {error && (
        <div className="rounded-xl border border-red-200 bg-red-50 px-5 py-3 text-sm text-red-700">
          {error}
        </div>
      )}

      {!loading && registers.length > 0 && (
        <div className="overflow-hidden rounded-xl border border-gray-100 bg-white shadow-sm">
          <div className="border-b border-gray-100 px-6 py-4">
            <h2 className="text-base font-semibold text-gray-700">Registers</h2>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-gray-50 text-xs uppercase tracking-wide text-gray-600">
                  <th className="rounded-l-lg px-4 py-3 text-left">Counter</th>
                  <th className="px-4 py-3 text-left">Cash Account</th>
                  <th className="px-4 py-3 text-right">Current Balance</th>
                  <th className="rounded-r-lg px-4 py-3 text-center">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {registers.map((r) => {
                  const session = openSessions.find((s) => s.posRegisterId === r.id);
                  return (
                    <tr key={r.id} className="hover:bg-gray-50/50">
                      <td className="px-4 py-3 font-medium text-gray-800">
                        {r.name}
                        {r.isDefault && (
                          <span className="ml-2 text-xs text-gray-400">(default)</span>
                        )}
                      </td>
                      <td className="px-4 py-3 text-gray-600">{r.linkedCashAccountName}</td>
                      <td className="px-4 py-3 text-right font-semibold text-blue-700">
                        {r.currentBalance != null ? fmt(r.currentBalance) : '—'}
                      </td>
                      <td className="px-4 py-3 text-center">
                        {session ? (
                          <div className="flex flex-col items-center gap-1">
                            <span className="inline-flex rounded-full bg-emerald-100 px-2.5 py-0.5 text-xs font-medium text-emerald-700">
                              Open
                            </span>
                            <button
                              type="button"
                              onClick={() => navigate(`/cashflow/closing?registerId=${r.id}`)}
                              className="text-xs text-orange-600 underline"
                            >
                              Close
                            </button>
                          </div>
                        ) : (
                          <button
                            type="button"
                            onClick={() => navigate('/cashflow/opening')}
                            className="text-xs text-emerald-600 underline"
                          >
                            Open
                          </button>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {loading ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
          {[...Array(4)].map((_, i) => (
            <div key={i} className="h-24 animate-pulse rounded-xl border border-gray-100 bg-white p-5" />
          ))}
        </div>
      ) : summaryCards.length > 0 ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
          {summaryCards.map(([label, val, color]) => (
            <div key={label} className="rounded-xl border border-gray-100 bg-white p-5 shadow-sm">
              <p className="text-xs font-medium uppercase tracking-wide text-gray-500">{label}</p>
              <p className={`mt-1 text-xl font-bold ${color}`}>{fmt(Number(val))}</p>
            </div>
          ))}
        </div>
      ) : null}

      {summary?.isClosed && summary.difference != null && (
        <div className="flex items-center gap-3 rounded-xl border border-gray-100 bg-white px-5 py-4">
          <span className="text-sm font-medium text-gray-600">Last close difference:</span>
          <DiffBadge diff={summary.difference} />
        </div>
      )}

      {branches.length > 0 && (
        <div className="rounded-xl border border-gray-100 bg-white p-6 shadow-sm">
          <h2 className="mb-4 text-base font-semibold text-gray-700">Branch Cash Overview — Today</h2>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-gray-50 text-xs uppercase tracking-wide text-gray-600">
                  <th className="rounded-l-lg px-4 py-3 text-left">Branch</th>
                  <th className="px-4 py-3 text-right">Opening</th>
                  <th className="px-4 py-3 text-right">Cash In</th>
                  <th className="px-4 py-3 text-right">Cash Out</th>
                  <th className="px-4 py-3 text-right">Net</th>
                  <th className="rounded-r-lg px-4 py-3 text-center">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {branches.map((b) => (
                  <tr key={b.branchId}>
                    <td className="px-4 py-3 font-medium">{b.branchName}</td>
                    <td className="px-4 py-3 text-right">{fmt(b.openingCash)}</td>
                    <td className="px-4 py-3 text-right text-emerald-600">{fmt(b.todayCashIn)}</td>
                    <td className="px-4 py-3 text-right text-red-500">{fmt(b.todayCashOut)}</td>
                    <td className="px-4 py-3 text-right font-bold text-blue-700">{fmt(b.netPosition)}</td>
                    <td className="px-4 py-3 text-center">
                      <span
                        className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${
                          b.status === 'Open'
                            ? 'bg-emerald-100 text-emerald-700'
                            : b.status === 'Closed'
                              ? 'bg-amber-100 text-amber-700'
                              : 'bg-gray-100 text-gray-500'
                        }`}
                      >
                        {b.status === 'Open' ? 'Open' : b.status === 'Closed' ? 'Closed' : 'Not Started'}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
