import React, { useEffect, useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useFormModal } from '../../contexts/FormModalContext';
import { getApiErrorMessage } from '../../services/api';
import { useBranchStore } from '../../stores/useBranchStore';
import {
  cashFlowService,
  type DailyCashSummaryDto,
  type BranchCashSummaryDto,
  type CashRegisterDto,
} from './cashFlowService';

// ─── Helpers ──────────────────────────────────────────────────────────────────

const fmt = (n: number) =>
  new Intl.NumberFormat(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(n);

const today = () => new Date().toISOString().slice(0, 10);

// ─── Sub-components ───────────────────────────────────────────────────────────

function StatCard({
  label,
  value,
  color = 'text-gray-800',
  bg = 'bg-white',
  icon,
}: {
  label: string;
  value: string;
  color?: string;
  bg?: string;
  icon: React.ReactNode;
}) {
  return (
    <div className={`${bg} rounded-xl shadow-sm border border-gray-100 p-5 flex items-center gap-4`}>
      <div className="flex-shrink-0 w-12 h-12 rounded-full bg-gray-50 flex items-center justify-center text-2xl">
        {icon}
      </div>
      <div>
        <p className="text-xs text-gray-500 font-medium uppercase tracking-wide">{label}</p>
        <p className={`text-xl font-bold mt-0.5 ${color}`}>{value}</p>
      </div>
    </div>
  );
}

function DiffBadge({ diff }: { diff: number | null }) {
  if (diff === null) return <span className="text-gray-400 text-sm">—</span>;
  const positive = diff >= 0;
  return (
    <span
      className={`inline-flex items-center gap-1 text-sm font-semibold px-2 py-0.5 rounded-full ${
        positive ? 'bg-emerald-50 text-emerald-700' : 'bg-red-50 text-red-600'
      }`}
    >
      {positive ? '▲' : '▼'} {fmt(Math.abs(diff))}
    </span>
  );
}

// ─── Main Page ────────────────────────────────────────────────────────────────

export default function CashFlowDashboardPage() {
  const navigate = useNavigate();
  const { openForm, isOpen } = useFormModal();
  const { selectedBranchId } = useBranchStore();
  const branchId = selectedBranchId ?? 0;

  const [summary, setSummary] = useState<DailyCashSummaryDto | null>(null);
  const [register, setRegister] = useState<CashRegisterDto | null | undefined>(undefined);
  const [branches, setBranches] = useState<BranchCashSummaryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (branchId <= 0) return;
    setLoading(true);
    setError(null);
    try {
      const [sumRes, regRes, brRes] = await Promise.all([
        cashFlowService.getDailySummary(branchId),
        cashFlowService.getTodayRegister(branchId),
        cashFlowService.getBranchSummary(today()),
      ]);
      setSummary(sumRes.data);
      setRegister(regRes.data);
      setBranches(brRes.data);
    } catch (e) {
      setError(getApiErrorMessage(e, 'Failed to load cash flow data.'));
    } finally {
      setLoading(false);
    }
  }, [branchId]);

  useEffect(() => { load(); }, [load]);
  useEffect(() => {
    if (!isOpen) {
      void load();
    }
  }, [isOpen, load]);

  if (branchId <= 0) {
    return (
      <div className="flex items-center justify-center h-64 text-gray-500">
        Please select a branch to view cash flow.
      </div>
    );
  }

  return (
    <div className="space-y-6 p-4 md:p-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-gray-800">Cash Flow Dashboard</h1>
          <p className="text-sm text-gray-500 mt-0.5">
            {new Date().toLocaleDateString(undefined, { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}
          </p>
        </div>
        <div className="flex gap-2">
          {!register && !loading && (
            <button
              onClick={() => navigate('/cashflow/opening')}
              className="px-4 py-2 bg-emerald-600 text-white text-sm font-medium rounded-lg hover:bg-emerald-700 transition-colors"
            >
              Open Cash Register
            </button>
          )}
          {register && !register.isClosed && (
            <button
              onClick={() => navigate('/cashflow/closing')}
              className="px-4 py-2 bg-orange-500 text-white text-sm font-medium rounded-lg hover:bg-orange-600 transition-colors"
            >
              Close Cash Register
            </button>
          )}
          <button
            onClick={() => openForm('cashTransaction')}
            className="px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 transition-colors"
          >
            Record Transaction
          </button>
          <button
            onClick={() => navigate('/cashflow/ledger')}
            className="px-4 py-2 bg-white border border-gray-200 text-gray-700 text-sm font-medium rounded-lg hover:bg-gray-50 transition-colors"
          >
            View Ledger
          </button>
          <button
            onClick={() => navigate('/cashflow/summary')}
            className="px-4 py-2 bg-white border border-gray-200 text-gray-700 text-sm font-medium rounded-lg hover:bg-gray-50 transition-colors"
          >
            Reports
          </button>
        </div>
      </div>

      {/* Register status banner */}
      {!loading && (
        <div
          className={`rounded-xl px-5 py-3 text-sm font-medium flex items-center gap-2 ${
            register?.isClosed
              ? 'bg-gray-100 text-gray-600'
              : register
              ? 'bg-emerald-50 text-emerald-700 border border-emerald-200'
              : 'bg-amber-50 text-amber-700 border border-amber-200'
          }`}
        >
          <span className="text-base">
            {register?.isClosed ? '🔒' : register ? '🟢' : '🟡'}
          </span>
          {register?.isClosed
            ? 'Cash register is closed for today.'
            : register
            ? `Cash register is OPEN — Opening balance: ${fmt(register.openingCash)}`
            : 'Cash register has not been opened yet. Open it to start tracking cash.'}
        </div>
      )}

      {/* Error */}
      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 rounded-xl px-5 py-3 text-sm">
          {error}
        </div>
      )}

      {/* Skeleton / Stats */}
      {loading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
          {[...Array(4)].map((_, i) => (
            <div key={i} className="bg-white rounded-xl border border-gray-100 p-5 h-24 animate-pulse" />
          ))}
        </div>
      ) : summary ? (
        <>
          <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
            <StatCard label="Opening Cash" value={fmt(summary.openingCash)} icon="💼" />
            <StatCard
              label="Total Cash Sales"
              value={fmt(summary.totalCashSales)}
              color="text-emerald-700"
              icon="💰"
            />
            <StatCard
              label="Total Expenses"
              value={fmt(summary.totalExpensesCash)}
              color="text-red-600"
              icon="💸"
            />
            <StatCard
              label="Expected Closing"
              value={fmt(summary.expectedClosingCash)}
              color="text-blue-700"
              icon="🏦"
            />
          </div>

          {/* Detailed breakdown card */}
          <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-6">
            <h2 className="text-base font-semibold text-gray-700 mb-4">Today's Breakdown</h2>
            <div className="grid grid-cols-2 md:grid-cols-3 xl:grid-cols-6 gap-4 text-center">
              {[
                { label: 'Cash In', val: summary.totalCashIn, pos: true },
                { label: 'Cash Out', val: summary.totalCashOut, pos: false },
                { label: 'Card Sales', val: summary.totalCardSales, pos: true },
                { label: 'Bank Transfers', val: summary.totalBankTransfers, pos: false },
                { label: 'Expected Close', val: summary.expectedClosingCash, pos: true },
                { label: 'Actual Close', val: summary.actualClosingCash ?? null, pos: true },
              ].map(({ label, val, pos }) => (
                <div key={label} className="bg-gray-50 rounded-lg p-3">
                  <p className="text-xs text-gray-500 font-medium">{label}</p>
                  <p className={`text-lg font-bold mt-1 ${val === null ? 'text-gray-400' : pos ? 'text-gray-800' : 'text-red-600'}`}>
                    {val === null ? '—' : fmt(val)}
                  </p>
                </div>
              ))}
            </div>

            {summary.isClosed && summary.difference !== null && (
              <div className="mt-4 pt-4 border-t border-gray-100 flex items-center gap-3">
                <span className="text-sm text-gray-600 font-medium">Cash Difference:</span>
                <DiffBadge diff={summary.difference} />
                <span className="text-xs text-gray-400">
                  {summary.difference > 0 ? '(Over)' : summary.difference < 0 ? '(Short)' : '(Exact)'}
                </span>
              </div>
            )}
          </div>
        </>
      ) : null}

      {/* All-branch table */}
      {branches.length > 0 && (
        <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-6">
          <h2 className="text-base font-semibold text-gray-700 mb-4">Branch Cash Overview — Today</h2>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-gray-50 text-gray-600 uppercase text-xs tracking-wide">
                  <th className="text-left px-4 py-3 rounded-l-lg">Branch</th>
                  <th className="text-right px-4 py-3">Opening</th>
                  <th className="text-right px-4 py-3">Cash In</th>
                  <th className="text-right px-4 py-3">Cash Out</th>
                  <th className="text-right px-4 py-3">Net Position</th>
                  <th className="text-center px-4 py-3 rounded-r-lg">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {branches.map((b) => (
                  <tr key={b.branchId} className="hover:bg-gray-50/50 transition-colors">
                    <td className="px-4 py-3 font-medium text-gray-800">{b.branchName}</td>
                    <td className="px-4 py-3 text-right text-gray-600">{fmt(b.openingCash)}</td>
                    <td className="px-4 py-3 text-right text-emerald-600 font-medium">{fmt(b.todayCashIn)}</td>
                    <td className="px-4 py-3 text-right text-red-500 font-medium">{fmt(b.todayCashOut)}</td>
                    <td className="px-4 py-3 text-right font-bold text-blue-700">{fmt(b.netPosition)}</td>
                    <td className="px-4 py-3 text-center">
                      <span
                        className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                          b.isOpenForDay
                            ? 'bg-emerald-100 text-emerald-700'
                            : 'bg-gray-100 text-gray-500'
                        }`}
                      >
                        {b.isOpenForDay ? 'Open' : 'Not Started'}
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
