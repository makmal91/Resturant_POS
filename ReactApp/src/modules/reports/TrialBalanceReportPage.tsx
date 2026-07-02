import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import PermissionGate from '../../components/PermissionGate';
import { useBusinessCurrency } from '../../hooks/useBusinessCurrency';
import { useCurrentBranch } from '../../hooks/useCurrentBranch';
import { getApiErrorMessage } from '../../services/api';
import { hasBranchContext } from '../../types/permissions';
import { exportTrialBalanceExcel, printTrialBalancePdf } from './trialBalanceExport';
import {
  trialBalanceService,
  type TrialBalanceAccountLevel,
  type TrialBalanceReport,
  type TrialBalanceRow,
} from './trialBalanceService';
import './reports.css';

const monthStart = () => {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-01`;
};

const todayStr = () => {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
};

function isRowVisible(row: TrialBalanceRow, allRows: TrialBalanceRow[], collapsed: Set<number>) {
  let parentId = row.parentAccountId ?? null;
  while (parentId != null) {
    if (collapsed.has(parentId)) return false;
    parentId = allRows.find((r) => r.accountId === parentId)?.parentAccountId ?? null;
  }
  return true;
}

export default function TrialBalanceReportPage() {
  const navigate = useNavigate();
  const { fmt } = useBusinessCurrency();
  const { branchId, showBranchSelector, getSelectedBranch } = useCurrentBranch();
  const hasBranch = hasBranchContext(branchId);

  const [fromDate, setFromDate] = useState(monthStart);
  const [toDate, setToDate] = useState(todayStr);
  const [accountLevel, setAccountLevel] = useState<TrialBalanceAccountLevel>('ParentAndChild');
  const [showZeroBalance, setShowZeroBalance] = useState(false);
  const [report, setReport] = useState<TrialBalanceReport | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [collapsed, setCollapsed] = useState<Set<number>>(new Set());

  const load = useCallback(async () => {
    if (!hasBranch || !branchId) {
      setReport(null);
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const data = await trialBalanceService.getReport({
        fromDate,
        toDate,
        branchId,
        accountLevel,
        showZeroBalance,
      });
      setReport(data);
      setCollapsed(new Set());
    } catch (err) {
      setReport(null);
      setError(getApiErrorMessage(err, 'Failed to load trial balance.'));
    } finally {
      setLoading(false);
    }
  }, [hasBranch, branchId, fromDate, toDate, accountLevel, showZeroBalance]);

  useEffect(() => {
    void load();
  }, [load]);

  const rows = report?.rows ?? [];
  const visibleRows = useMemo(() => {
    if (accountLevel === 'ParentOnly') return rows;
    return rows.filter((row) => isRowVisible(row, rows, collapsed));
  }, [rows, collapsed, accountLevel]);

  const toggleCollapse = (accountId: number) => {
    setCollapsed((prev) => {
      const next = new Set(prev);
      if (next.has(accountId)) next.delete(accountId);
      else next.add(accountId);
      return next;
    });
  };

  const branchLabel = getSelectedBranch()?.name ?? '';

  const onDrillDown = (accountId: number) => {
    const params = new URLSearchParams();
    params.set('accountId', String(accountId));
    if (fromDate) params.set('fromDate', fromDate);
    if (toDate) params.set('toDate', toDate);
    navigate(`/accounting/ledger?${params.toString()}`);
  };

  if (!hasBranch) {
    return (
      <div className="flex items-center justify-center h-64 text-gray-500 p-4 md:p-6">
        Please select a branch to view the trial balance.
      </div>
    );
  }

  return (
    <PermissionGate module="Trial Balance Report" action="View">
      <div className="print-area p-4 md:p-6 space-y-4">
        <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-3">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Trial Balance</h1>
            <p className="text-sm text-gray-500 mt-1">
              Account balances for the selected period
              {showBranchSelector && branchLabel ? ` — ${branchLabel}` : ''}
            </p>
          </div>
          <div className="flex flex-wrap gap-2 print:hidden">
            <button
              type="button"
              onClick={() => report && exportTrialBalanceExcel(`trial-balance-${fromDate}-${toDate}`, report.rows, report.totalDebit, report.totalCredit)}
              disabled={loading || !report?.rows.length}
              className="px-4 py-2 bg-emerald-50 border border-emerald-300 text-emerald-800 text-sm font-medium rounded-lg hover:bg-emerald-100 disabled:opacity-60"
            >
              Export Excel
            </button>
            <button
              type="button"
              onClick={printTrialBalancePdf}
              disabled={loading || !report?.rows.length}
              className="px-4 py-2 bg-white border border-gray-300 text-gray-700 text-sm font-medium rounded-lg hover:bg-gray-50 disabled:opacity-60"
            >
              Export PDF
            </button>
            <button
              type="button"
              onClick={() => void load()}
              disabled={loading}
              className="px-4 py-2 bg-white border border-gray-300 text-gray-700 text-sm font-medium rounded-lg hover:bg-gray-50 disabled:opacity-60"
            >
              {loading ? 'Loading…' : 'Refresh'}
            </button>
          </div>
        </div>

        <div className="bg-white rounded-xl border border-gray-100 p-4 print:hidden">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-3">
            <div>
              <label className="text-xs text-gray-500 font-medium mb-1 block">From Date</label>
              <input
                type="date"
                value={fromDate}
                onChange={(e) => setFromDate(e.target.value)}
                className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-400"
              />
            </div>
            <div>
              <label className="text-xs text-gray-500 font-medium mb-1 block">To Date</label>
              <input
                type="date"
                value={toDate}
                onChange={(e) => setToDate(e.target.value)}
                className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-400"
              />
            </div>
            <div>
              <label className="text-xs text-gray-500 font-medium mb-1 block">Account Level</label>
              <select
                value={accountLevel}
                onChange={(e) => setAccountLevel(e.target.value as TrialBalanceAccountLevel)}
                className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-400"
              >
                <option value="ParentOnly">Parent Only</option>
                <option value="ParentAndChild">Parent + Child Accounts</option>
              </select>
            </div>
            <div className="flex items-end">
              <label className="inline-flex items-center gap-2 text-sm text-gray-700 cursor-pointer pb-2">
                <input
                  type="checkbox"
                  checked={showZeroBalance}
                  onChange={(e) => setShowZeroBalance(e.target.checked)}
                  className="rounded border-gray-300 text-blue-600 focus:ring-blue-400"
                />
                Show zero balance accounts
              </label>
            </div>
          </div>
        </div>

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 text-sm rounded-lg px-4 py-3">{error}</div>
        )}

        {report && !report.isBalanced && report.balanceMessage && (
          <div className="bg-amber-50 border border-amber-300 text-amber-900 text-sm rounded-lg px-4 py-3 font-medium">
            {report.balanceMessage}
          </div>
        )}

        {report?.isBalanced && report.rows.length > 0 && (
          <div className="bg-emerald-50 border border-emerald-200 text-emerald-800 text-sm rounded-lg px-4 py-2">
            Trial balance is balanced — Total Debit equals Total Credit.
          </div>
        )}

        <div className="bg-white rounded-xl border border-gray-100 overflow-hidden flex flex-col max-h-[calc(100dvh-18rem)]">
          <div className="overflow-auto flex-1 min-h-0">
            <table className="w-full text-sm">
              <thead className="sticky top-0 z-10 bg-gray-50 text-xs uppercase tracking-wide text-gray-600">
                <tr>
                  <th className="text-left px-4 py-3 font-semibold">Account Code</th>
                  <th className="text-left px-4 py-3 font-semibold">Account Name</th>
                  <th className="text-right px-4 py-3 font-semibold">Debit</th>
                  <th className="text-right px-4 py-3 font-semibold">Credit</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {loading ? (
                  <tr>
                    <td colSpan={4} className="px-4 py-10 text-center text-gray-400">Loading…</td>
                  </tr>
                ) : visibleRows.length === 0 ? (
                  <tr>
                    <td colSpan={4} className="px-4 py-10 text-center text-gray-400">No accounts for the selected filters.</td>
                  </tr>
                ) : (
                  visibleRows.map((row) => (
                    <tr key={row.accountId} className="hover:bg-gray-50/80">
                      <td className="px-4 py-2.5 font-mono text-xs text-gray-500">{row.accountCode}</td>
                      <td className="px-4 py-2.5 text-gray-800">
                        <div className="flex items-center gap-1" style={{ paddingLeft: `${row.level * 1.25}rem` }}>
                          {accountLevel === 'ParentAndChild' && row.hasChildren ? (
                            <button
                              type="button"
                              onClick={() => toggleCollapse(row.accountId)}
                              className="w-5 h-5 shrink-0 rounded border border-gray-200 text-gray-500 text-xs hover:bg-gray-100 print:hidden"
                              aria-label={collapsed.has(row.accountId) ? 'Expand' : 'Collapse'}
                            >
                              {collapsed.has(row.accountId) ? '+' : '−'}
                            </button>
                          ) : (
                            <span className="w-5 shrink-0" />
                          )}
                          <button
                            type="button"
                            onClick={() => onDrillDown(row.accountId)}
                            className="text-left hover:text-blue-700 hover:underline print:no-underline print:text-gray-800"
                            title="Open account ledger"
                          >
                            {row.accountName}
                          </button>
                        </div>
                      </td>
                      <td className="px-4 py-2.5 text-right tabular-nums text-emerald-700 font-medium">
                        {row.debit > 0 ? fmt(row.debit) : '—'}
                      </td>
                      <td className="px-4 py-2.5 text-right tabular-nums text-red-600 font-medium">
                        {row.credit > 0 ? fmt(row.credit) : '—'}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
              {report && report.rows.length > 0 && (
                <tfoot>
                  <tr className="sticky bottom-0 bg-blue-50 border-t-2 border-blue-200 font-bold text-gray-900">
                    <td className="px-4 py-3" colSpan={2}>Total</td>
                    <td className="px-4 py-3 text-right tabular-nums text-emerald-800">{fmt(report.totalDebit)}</td>
                    <td className="px-4 py-3 text-right tabular-nums text-red-700">{fmt(report.totalCredit)}</td>
                  </tr>
                </tfoot>
              )}
            </table>
          </div>
        </div>
      </div>
    </PermissionGate>
  );
}
