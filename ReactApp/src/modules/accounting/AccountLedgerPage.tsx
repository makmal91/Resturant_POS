import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import DataTable, { type Column } from '../../components/DataTable';
import LedgerStatusBadge from '../../components/LedgerStatusBadge';
import LedgerViewToggle from '../../components/LedgerViewToggle';
import PermissionGate from '../../components/PermissionGate';
import { useGridExport } from '../../hooks/useGridExport';
import { getApiErrorMessage } from '../../services/api';
import { useBranchStore } from '../../stores/useBranchStore';
import { useBusinessCurrency } from '../../hooks/useBusinessCurrency';
import {
  accountLedgerService,
  type AccountLedgerEntry,
  type GlAccountListItem,
} from './accountLedgerService';

const PAGE_SIZE_OPTIONS = [25, 50, 100, 250];

const formatDate = (value: string) => {
  const d = new Date(value);
  return Number.isNaN(d.getTime())
    ? '—'
    : d.toLocaleDateString(undefined, {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
      });
};

export default function AccountLedgerPage() {
  const { fmt } = useBusinessCurrency();
  const [searchParams] = useSearchParams();
  const { selectedBranchId } = useBranchStore();
  const branchId = selectedBranchId ?? 0;

  const [accounts, setAccounts] = useState<GlAccountListItem[]>([]);
  const [accountId, setAccountId] = useState(0);
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [rows, setRows] = useState<AccountLedgerEntry[]>([]);
  const [accountName, setAccountName] = useState('');
  const [openingBalance, setOpeningBalance] = useState(0);
  const [closingBalance, setClosingBalance] = useState(0);
  const [totalDebit, setTotalDebit] = useState(0);
  const [totalCredit, setTotalCredit] = useState(0);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(50);
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [auditView, setAuditView] = useState(false);
  const [groupByChain, setGroupByChain] = useState(false);
  const [effectiveClosingBalance, setEffectiveClosingBalance] = useState(0);
  const [includesSubAccounts, setIncludesSubAccounts] = useState(false);

  const accountOptions = useMemo(() => {
    const byParent = new Map<number | null, GlAccountListItem[]>();
    for (const account of accounts) {
      const key = account.parentId ?? null;
      const list = byParent.get(key) ?? [];
      list.push(account);
      byParent.set(key, list);
    }

    const accountIds = new Set(accounts.map((a) => a.id));
    const roots = accounts
      .filter((a) => !a.parentId || !accountIds.has(a.parentId))
      .sort((a, b) => a.name.localeCompare(b.name));

    const rows: { id: number; label: string }[] = [];
    const walk = (parentId: number, depth: number) => {
      const children = (byParent.get(parentId) ?? []).slice().sort((a, b) => a.name.localeCompare(b.name));
      for (const child of children) {
        rows.push({
          id: child.id,
          label: `${'— '.repeat(depth)}${child.name} (${child.type})`,
        });
        walk(child.id, depth + 1);
      }
    };

    for (const root of roots) {
      rows.push({ id: root.id, label: `${root.name} (${root.type})` });
      walk(root.id, 1);
    }

    return rows;
  }, [accounts]);

  useEffect(() => {
    void accountLedgerService
      .listAccounts()
      .then(setAccounts)
      .catch(() => setAccounts([]));
  }, []);

  useEffect(() => {
    const id = Number(searchParams.get('accountId') ?? 0);
    if (id > 0) setAccountId(id);
    const from = searchParams.get('fromDate');
    const to = searchParams.get('toDate');
    if (from) setFromDate(from);
    if (to) setToDate(to);
  }, [searchParams]);

  const fetchLedger = useCallback(async () => {
    if (accountId <= 0) {
      setRows([]);
      setTotalRecords(0);
      setTotalPages(0);
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const res = await accountLedgerService.getLedger(
        accountId,
        currentPage,
        pageSize,
        fromDate || undefined,
        toDate || undefined,
        branchId > 0 ? branchId : undefined,
        { auditView, groupByChain },
      );
      setRows(res.entries);
      setAccountName(res.accountName);
      setOpeningBalance(res.openingBalance);
      setClosingBalance(res.closingBalance);
      setEffectiveClosingBalance(res.effectiveClosingBalance ?? res.closingBalance);
      setTotalDebit(res.totalDebit);
      setTotalCredit(res.totalCredit);
      setTotalRecords(res.totalRecords);
      setTotalPages(res.totalPages);
      setIncludesSubAccounts(Boolean(res.includesSubAccounts));
    } catch (err) {
      setRows([]);
      setError(getApiErrorMessage(err, 'Failed to load account ledger.'));
    } finally {
      setLoading(false);
    }
  }, [accountId, branchId, currentPage, pageSize, fromDate, toDate, auditView, groupByChain]);

  useEffect(() => {
    void fetchLedger();
  }, [fetchLedger]);

  const columns = useMemo<Column<AccountLedgerEntry>[]>(
    () => {
      const base: Column<AccountLedgerEntry>[] = [
      {
        key: 'date',
        header: 'Date',
        sortable: false,
        render: (value: string) => <span className="whitespace-nowrap">{formatDate(value)}</span>,
      },
      {
        key: 'referenceType',
        header: 'Type',
        sortable: false,
        render: (value: string, row: AccountLedgerEntry) =>
          row.isOpeningBalance ? (
            <span className="text-xs font-medium px-2 py-0.5 rounded bg-gray-100 text-gray-600">Opening</span>
          ) : (
            <span className="text-xs font-medium px-2 py-0.5 rounded bg-blue-50 text-blue-700">{value}</span>
          ),
      },
      ...(includesSubAccounts
        ? [{
            key: 'lineAccountName' as const,
            header: 'Account',
            sortable: false,
            render: (value: string | null | undefined, row: AccountLedgerEntry) =>
              row.isOpeningBalance ? '—' : (value?.trim() || '—'),
          }]
        : []),
      {
        key: 'description',
        header: 'Description',
        sortable: false,
        render: (value: string, row: AccountLedgerEntry) => (
          <span className="text-sm text-gray-700 inline-flex items-center flex-wrap gap-1">
            <span>{value || '—'}</span>
            {!row.isOpeningBalance && (
              <LedgerStatusBadge
                isSuperseded={row.isSuperseded}
                isReversal={row.isReversal}
                isReplacement={row.isReplacement}
              />
            )}
          </span>
        ),
      },
      {
        key: 'referenceId',
        header: 'Ref',
        sortable: false,
        render: (value: number | null | undefined, row: AccountLedgerEntry) =>
          row.isOpeningBalance ? '—' : value ?? '—',
      },
      {
        key: 'debit',
        header: 'Debit (In)',
        sortable: false,
        align: 'right',
        render: (value: number) => (
          <span className={value > 0 ? 'text-emerald-700 font-medium' : 'text-gray-400'}>
            {value > 0 ? fmt(value) : '—'}
          </span>
        ),
      },
      {
        key: 'credit',
        header: 'Credit (Out)',
        sortable: false,
        align: 'right',
        render: (value: number) => (
          <span className={value > 0 ? 'text-red-600 font-medium' : 'text-gray-400'}>
            {value > 0 ? fmt(value) : '—'}
          </span>
        ),
      },
      {
        key: 'runningBalance',
        header: 'Balance',
        sortable: false,
        align: 'right',
        render: (value: number) => <span className="font-semibold text-gray-800">{fmt(value)}</span>,
      },
    ];
      return base;
    },
    [fmt, includesSubAccounts]
  );

  const fetchExportPage = useCallback(
    async (pageNumber: number, exportPageSize: number) => {
      const res = await accountLedgerService.getLedger(
        accountId,
        pageNumber,
        exportPageSize,
        fromDate || undefined,
        toDate || undefined,
        branchId > 0 ? branchId : undefined,
        { auditView, groupByChain },
      );
      return { data: res.entries, totalRecords: res.totalRecords };
    },
    [accountId, branchId, fromDate, toDate, auditView, groupByChain]
  );

  const exportColumns = useMemo(
    () => [
      { key: 'date', header: 'Date' },
      { key: 'referenceType', header: 'Type' },
      ...(includesSubAccounts ? [{ key: 'lineAccountName', header: 'Account' }] : []),
      { key: 'description', header: 'Description' },
      { key: 'referenceId', header: 'Reference Id' },
      { key: 'debit', header: 'Debit' },
      { key: 'credit', header: 'Credit' },
      { key: 'runningBalance', header: 'Balance' },
    ],
    [includesSubAccounts]
  );

  const { exporting, onExport } = useGridExport(
    `account-ledger-${accountName || accountId}`,
    exportColumns,
    fetchExportPage,
    accountId > 0
  );

  if (branchId <= 0) {
    return (
      <div className="flex items-center justify-center h-64 text-gray-500 p-4 md:p-6">
        Please select a branch to view the account ledger.
      </div>
    );
  }

  return (
    <PermissionGate module="Account Ledger" action="View">
      <div className="space-y-4 p-4 md:p-6">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
          <div>
            <h1 className="text-2xl font-bold text-gray-800">Account Ledger</h1>
            <p className="text-sm text-gray-500 mt-0.5">
              Read-only transaction history
              {accountName ? ` — ${accountName}` : ''}
            </p>
          </div>
          <button
            type="button"
            onClick={() => void onExport()}
            disabled={loading || exporting || accountId <= 0}
            className="px-4 py-2 bg-emerald-50 border border-emerald-300 text-emerald-800 text-sm font-medium rounded-lg hover:bg-emerald-100 transition-colors disabled:opacity-60"
          >
            {exporting ? 'Exporting…' : 'Export CSV'}
          </button>
        </div>

        <div className="bg-white rounded-xl border border-gray-100 p-4">
          <div className="grid grid-cols-1 md:grid-cols-4 gap-3">
            <div className="md:col-span-2">
              <label className="text-xs text-gray-500 font-medium mb-1 block">Account</label>
              <select
                value={accountId}
                onChange={(e) => {
                  setAccountId(Number(e.target.value));
                  setCurrentPage(1);
                }}
                className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-400"
              >
                <option value={0}>Select account…</option>
                {accountOptions.map((a) => (
                  <option key={a.id} value={a.id}>
                    {a.label}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="text-xs text-gray-500 font-medium mb-1 block">From Date</label>
              <input
                type="date"
                value={fromDate}
                onChange={(e) => {
                  setFromDate(e.target.value);
                  setCurrentPage(1);
                }}
                className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-400"
              />
            </div>
            <div>
              <label className="text-xs text-gray-500 font-medium mb-1 block">To Date</label>
              <input
                type="date"
                value={toDate}
                onChange={(e) => {
                  setToDate(e.target.value);
                  setCurrentPage(1);
                }}
                className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-400"
              />
            </div>
          </div>
          <LedgerViewToggle
            auditView={auditView}
            groupByChain={groupByChain}
            onAuditViewChange={(value) => {
              setAuditView(value);
              setCurrentPage(1);
            }}
            onGroupByChainChange={(value) => {
              setGroupByChain(value);
              setCurrentPage(1);
            }}
          />
        </div>

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 text-sm rounded-lg px-4 py-3">{error}</div>
        )}

        {accountId > 0 && !loading && (
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div className="bg-gray-50 border border-gray-100 rounded-xl p-4 text-center">
              <p className="text-xs text-gray-500 font-medium uppercase">Opening</p>
              <p className="text-lg font-bold text-gray-800 mt-1">{fmt(openingBalance)}</p>
            </div>
            <div className="bg-emerald-50 border border-emerald-100 rounded-xl p-4 text-center">
              <p className="text-xs text-emerald-600 font-medium uppercase">Total Debit</p>
              <p className="text-lg font-bold text-emerald-700 mt-1">{fmt(totalDebit)}</p>
            </div>
            <div className="bg-red-50 border border-red-100 rounded-xl p-4 text-center">
              <p className="text-xs text-red-500 font-medium uppercase">Total Credit</p>
              <p className="text-lg font-bold text-red-600 mt-1">{fmt(totalCredit)}</p>
            </div>
            <div className="bg-blue-50 border border-blue-100 rounded-xl p-4 text-center">
              <p className="text-xs text-blue-600 font-medium uppercase">Closing</p>
              <p className="text-lg font-bold text-blue-700 mt-1">{fmt(closingBalance)}</p>
              {auditView && Math.abs(effectiveClosingBalance - closingBalance) <= 0.01 && (
                <p className="text-[10px] text-blue-500 mt-1">Matches clean effective balance</p>
              )}
            </div>
          </div>
        )}

        <DataTable
          columns={columns}
          data={rows}
          loading={loading}
          searchable={false}
          pagination
          serverSide
          emptyMessage={accountId <= 0 ? 'Select an account to view its ledger.' : 'No transactions in this period.'}
          totalRecords={totalRecords}
          totalPages={totalPages}
          currentPage={currentPage}
          pageSize={pageSize}
          pageSizeOptions={PAGE_SIZE_OPTIONS}
          onPageChange={setCurrentPage}
          onPageSizeChange={(size) => {
            setPageSize(size);
            setCurrentPage(1);
          }}
        />
      </div>
    </PermissionGate>
  );
}
