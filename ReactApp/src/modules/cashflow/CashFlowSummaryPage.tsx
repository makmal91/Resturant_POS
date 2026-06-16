import React, { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { getApiErrorMessage } from '../../services/api';
import { useBranchWriteAccess } from '../../hooks/useBranchWriteAccess';
import { hasBranchContext } from '../../types/permissions';
import { cashFlowService, type MonthlyCashSummaryDto } from './cashFlowService';

// ─── Helpers ──────────────────────────────────────────────────────────────────

const fmt = (n: number) =>
  new Intl.NumberFormat(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(n);

const MONTHS = [
  'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
  'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
];

// ─── Mini bar chart ───────────────────────────────────────────────────────────

function BarChart({ data }: { data: { date: string; cashIn: number; cashOut: number }[] }) {
  if (!data.length) return null;
  const maxVal = Math.max(...data.map((d) => Math.max(d.cashIn, d.cashOut)), 1);

  return (
    <div className="flex items-end gap-0.5 h-28 w-full overflow-x-auto py-1">
      {data.map((d) => {
        const inH = Math.max(2, (d.cashIn / maxVal) * 100);
        const outH = Math.max(2, (d.cashOut / maxVal) * 100);
        const dayLabel = new Date(d.date).getDate();
        return (
          <div key={d.date} className="flex flex-col items-center gap-0.5 flex-1 min-w-[14px]">
            <div className="flex items-end gap-0.5 h-24">
              <div
                title={`In: ${fmt(d.cashIn)}`}
                className="w-2 bg-emerald-400 rounded-t transition-all"
                style={{ height: `${inH}%` }}
              />
              <div
                title={`Out: ${fmt(d.cashOut)}`}
                className="w-2 bg-red-400 rounded-t transition-all"
                style={{ height: `${outH}%` }}
              />
            </div>
            <span className="text-[9px] text-gray-400">{dayLabel}</span>
          </div>
        );
      })}
    </div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function CashFlowSummaryPage() {
  const navigate = useNavigate();
  const { selectedBranchId } = useBranchWriteAccess();
  const hasBranch = hasBranchContext(selectedBranchId);
  const branchId  = hasBranch && selectedBranchId !== null ? selectedBranchId : 0;

  const now = new Date();
  const [year, setYear]   = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);

  const [summary, setSummary] = useState<MonthlyCashSummaryDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError]     = useState<string | null>(null);

  const load = useCallback(async () => {
    if (branchId <= 0) {
      setSummary(null);
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const res = await cashFlowService.getMonthlySummary(branchId, year, month);
      const data = res?.data;
      if (!data) throw new Error('Empty response from server.');
      // Ensure dailyTrend is always an array
      setSummary({ ...data, dailyTrend: Array.isArray(data.dailyTrend) ? data.dailyTrend : [] });
    } catch (e) {
      setError(getApiErrorMessage(e, 'Failed to load cash flow summary.'));
      setSummary(null);
    } finally {
      setLoading(false);
    }
  }, [branchId, year, month]);

  useEffect(() => { void load(); }, [load]);

  // Reset to current month if branch changes
  useEffect(() => {
    const n = new Date();
    setYear(n.getFullYear());
    setMonth(n.getMonth() + 1);
    setSummary(null);
  }, [branchId]);

  const prevMonth = () => {
    if (month === 1) { setYear((y) => y - 1); setMonth(12); }
    else setMonth((m) => m - 1);
  };

  const nextMonth = () => {
    if (month === 12) { setYear((y) => y + 1); setMonth(1); }
    else setMonth((m) => m + 1);
  };

  if (!hasBranch) {
    return (
      <div className="p-4 md:p-6">
        <div className="mb-8">
          <h1 className="mb-2 text-3xl font-bold text-gray-900">Cash Flow Summary</h1>
          <p className="text-gray-600">Monthly income and expense overview</p>
        </div>
        <div className="rounded-md border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-700">
          Select a branch from the header to load cash flow data.
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Cash Flow Summary</h1>
          <p className="text-gray-600 mt-1">
            Monthly income and expense overview
            {summary?.branchName ? ` — ${summary.branchName}` : ''}
          </p>
        </div>
        <button
          onClick={() => navigate('/cashflow')}
          className="inline-flex items-center rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 shadow-sm hover:bg-gray-50 transition-colors self-start"
        >
          ← Dashboard
        </button>
      </div>

      {/* Month navigation */}
      <div className="flex items-center gap-4">
        <button onClick={prevMonth} className="p-2 rounded-lg border border-gray-200 hover:bg-gray-50 text-gray-600 text-lg transition-colors">
          ‹
        </button>
        <span className="text-lg font-semibold text-gray-700 min-w-[140px] text-center">
          {MONTHS[month - 1]} {year}
        </span>
        <button
          onClick={nextMonth}
          disabled={year === now.getFullYear() && month === now.getMonth() + 1}
          className="p-2 rounded-lg border border-gray-200 hover:bg-gray-50 text-gray-600 text-lg disabled:opacity-40 transition-colors"
        >
          ›
        </button>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 rounded-xl px-5 py-3 text-sm">
          {error}
        </div>
      )}

      {loading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {[...Array(4)].map((_, i) => (
            <div key={i} className="h-28 bg-white rounded-xl border border-gray-100 animate-pulse" />
          ))}
        </div>
      ) : summary ? (
        <>
          {/* KPI cards */}
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
            {[
              { label: 'Total Cash In',  value: summary.totalCashIn,   color: 'text-emerald-700', bg: 'bg-emerald-50', icon: '📈' },
              { label: 'Total Cash Out', value: summary.totalCashOut,  color: 'text-red-600',     bg: 'bg-red-50',     icon: '📉' },
              { label: 'Total Sales',    value: summary.totalSales,    color: 'text-blue-700',    bg: 'bg-blue-50',    icon: '🛒' },
              { label: 'Total Expenses', value: summary.totalExpenses, color: 'text-orange-700',  bg: 'bg-orange-50',  icon: '💸' },
            ].map(({ label, value, color, bg, icon }) => (
              <div key={label} className={`${bg} rounded-xl p-5 border border-white shadow-sm`}>
                <div className="text-2xl mb-2">{icon}</div>
                <p className="text-xs text-gray-500 font-medium uppercase tracking-wide">{label}</p>
                <p className={`text-xl font-bold mt-1 ${color}`}>{fmt(value)}</p>
              </div>
            ))}
          </div>

          {/* Net cash flow */}
          <div
            className={`rounded-xl px-6 py-5 border ${
              summary.netCashFlow >= 0
                ? 'bg-emerald-50 border-emerald-200'
                : 'bg-red-50 border-red-200'
            }`}
          >
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Net Cash Flow for {MONTHS[month - 1]} {year}</p>
                <p className={`text-3xl font-extrabold mt-1 ${summary.netCashFlow >= 0 ? 'text-emerald-700' : 'text-red-600'}`}>
                  {summary.netCashFlow >= 0 ? '+' : ''}{fmt(summary.netCashFlow)}
                </p>
              </div>
              <span className="text-5xl">{summary.netCashFlow >= 0 ? '✅' : '⚠️'}</span>
            </div>
          </div>

          {/* Chart */}
          {summary.dailyTrend.length > 0 && (
            <div className="bg-white rounded-xl border border-gray-100 p-6">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-base font-semibold text-gray-700">Daily Trend</h2>
                <div className="flex items-center gap-4 text-xs text-gray-500">
                  <span className="flex items-center gap-1.5">
                    <span className="w-3 h-3 rounded-sm bg-emerald-400 inline-block" />
                    Cash In
                  </span>
                  <span className="flex items-center gap-1.5">
                    <span className="w-3 h-3 rounded-sm bg-red-400 inline-block" />
                    Cash Out
                  </span>
                </div>
              </div>
              <BarChart data={summary.dailyTrend} />
            </div>
          )}

          {/* Daily trend table */}
          {summary.dailyTrend.length > 0 && (
            <div className="bg-white rounded-xl border border-gray-100 overflow-hidden">
              <div className="px-5 py-4 border-b border-gray-50">
                <h2 className="text-base font-semibold text-gray-700">Daily Breakdown</h2>
              </div>
              <div className="overflow-x-auto max-h-80">
                <table className="w-full text-sm">
                  <thead className="sticky top-0">
                    <tr className="bg-gray-50 text-gray-600 uppercase text-xs tracking-wide">
                      <th className="text-left px-5 py-3">Date</th>
                      <th className="text-right px-4 py-3">Cash In</th>
                      <th className="text-right px-4 py-3">Cash Out</th>
                      <th className="text-right px-5 py-3">Net</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-50">
                    {summary.dailyTrend.map((d) => (
                      <tr key={d.date} className="hover:bg-gray-50/60">
                        <td className="px-5 py-2.5 text-gray-600">
                          {new Date(d.date).toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' })}
                        </td>
                        <td className="px-4 py-2.5 text-right text-emerald-600 font-medium">{fmt(d.cashIn)}</td>
                        <td className="px-4 py-2.5 text-right text-red-500 font-medium">{fmt(d.cashOut)}</td>
                        <td className={`px-5 py-2.5 text-right font-bold ${d.net >= 0 ? 'text-blue-700' : 'text-red-600'}`}>
                          {d.net >= 0 ? '+' : ''}{fmt(d.net)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                  <tfoot>
                    <tr className="bg-gray-50 font-bold text-gray-700 border-t border-gray-200">
                      <td className="px-5 py-3">Total</td>
                      <td className="px-4 py-3 text-right text-emerald-700">{fmt(summary.totalCashIn)}</td>
                      <td className="px-4 py-3 text-right text-red-600">{fmt(summary.totalCashOut)}</td>
                      <td className={`px-5 py-3 text-right ${summary.netCashFlow >= 0 ? 'text-blue-700' : 'text-red-600'}`}>
                        {summary.netCashFlow >= 0 ? '+' : ''}{fmt(summary.netCashFlow)}
                      </td>
                    </tr>
                  </tfoot>
                </table>
              </div>
            </div>
          )}

          {summary.dailyTrend.length === 0 && (
            <div className="text-center text-gray-400 py-12">
              <div className="text-4xl mb-3">📊</div>
              <p className="font-medium">No transactions this month.</p>
            </div>
          )}
        </>
      ) : null}
    </div>
  );
}
