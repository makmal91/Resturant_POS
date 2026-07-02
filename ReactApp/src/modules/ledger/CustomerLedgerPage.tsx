import React, { useCallback, useEffect, useMemo, useState } from 'react';
import DataTable, { type Column } from '../../components/DataTable';
import LedgerViewToggle from '../../components/LedgerViewToggle';
import { useGridExport } from '../../hooks/useGridExport';
import { getApiErrorMessage } from '../../services/api';
import { partyLedgerExportColumns } from '../../utils/gridExportColumns';
import { useBranchStore } from '../../stores/useBranchStore';
import { useBusinessCurrency } from '../../hooks/useBusinessCurrency';
import { customerService, type CustomerListItem } from '../customer/customerService';
import PartyLedgerEntryDescription from './PartyLedgerEntryDescription';
import {
  partyLedgerService,
  type PartyLedgerEntry,
} from './partyLedgerService';

const LEDGER_PAGE_SIZE_OPTIONS = [25, 50, 100, 250, 500];

const formatDate = (value: string) => {
  const datePart = value.includes('T') ? value.split('T')[0] : value.slice(0, 10);
  const [year, month, day] = datePart.split('-').map(Number);
  if (!year || !month || !day) return '—';
  return new Date(year, month - 1, day).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
};

export default function CustomerLedgerPage() {
  const { fmt } = useBusinessCurrency();
  const { selectedBranchId } = useBranchStore();
  const branchId = selectedBranchId ?? 0;

  const [customers, setCustomers] = useState<CustomerListItem[]>([]);
  const [customerSearch, setCustomerSearch] = useState('');
  const [customerId, setCustomerId] = useState(0);
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [rows, setRows] = useState<PartyLedgerEntry[]>([]);
  const [partyName, setPartyName] = useState('');
  const [currentBalance, setCurrentBalance] = useState(0);
  const [periodClosingBalance, setPeriodClosingBalance] = useState(0);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(50);
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [totalDebit, setTotalDebit] = useState(0);
  const [totalCredit, setTotalCredit] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [auditView, setAuditView] = useState(false);
  const [groupByChain, setGroupByChain] = useState(false);

  useEffect(() => {
    if (branchId <= 0) {
      setCustomers([]);
      return;
    }

    const timer = window.setTimeout(() => {
      void customerService
        .getForLedgerFilter(branchId, customerSearch)
        .then(setCustomers)
        .catch(() => setCustomers([]));
    }, customerSearch ? 300 : 0);

    return () => window.clearTimeout(timer);
  }, [branchId, customerSearch]);

  useEffect(() => {
    if (branchId <= 0 || customerId > 0) return;
    const walkIn = customers.find((c) => c.isWalkIn);
    if (walkIn) setCustomerId(walkIn.id);
  }, [branchId, customers, customerId]);

  const fetchLedger = useCallback(async () => {
    if (branchId <= 0 || customerId <= 0) {
      setRows([]);
      setTotalRecords(0);
      setTotalPages(0);
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const res = await partyLedgerService.getCustomerLedger(
        branchId,
        customerId,
        currentPage,
        pageSize,
        fromDate || undefined,
        toDate || undefined,
        { auditView, groupByChain },
      );
      setRows(res.data.entries);
      setPartyName(res.data.partyName);
      setCurrentBalance(res.data.currentBalance);
      setPeriodClosingBalance(res.data.periodClosingBalance);
      setTotalRecords(res.data.totalRecords);
      setTotalPages(res.data.totalPages);
      setTotalDebit(res.data.totalDebit);
      setTotalCredit(res.data.totalCredit);
    } catch (err) {
      setRows([]);
      setError(getApiErrorMessage(err, 'Failed to load customer ledger.'));
    } finally {
      setLoading(false);
    }
  }, [branchId, customerId, currentPage, pageSize, fromDate, toDate, auditView, groupByChain]);

  const fetchExportPage = useCallback(async (pageNumber: number, exportPageSize: number) => {
    const res = await partyLedgerService.getCustomerLedger(
      branchId,
      customerId,
      pageNumber,
      exportPageSize,
      fromDate || undefined,
      toDate || undefined,
      { auditView, groupByChain },
    );
    return { data: res.data.entries, totalRecords: res.data.totalRecords };
  }, [branchId, customerId, fromDate, toDate, auditView, groupByChain]);

  const exportFilename = useMemo(() => {
    const slug = partyName.trim().replace(/\s+/g, '-').toLowerCase() || 'customer';
    const range = fromDate || toDate ? `-${fromDate || 'start'}-${toDate || 'end'}` : '';
    return `customer-ledger-${slug}${range}`;
  }, [partyName, fromDate, toDate]);

  const { exporting, onExport } = useGridExport(
    exportFilename,
    partyLedgerExportColumns,
    fetchExportPage,
    branchId > 0 && customerId > 0,
  );

  useEffect(() => {
    void fetchLedger();
  }, [fetchLedger]);

  useEffect(() => {
    setCurrentPage(1);
  }, [customerId, fromDate, toDate, pageSize]);

  const handlePageSizeChange = useCallback((size: number) => {
    setPageSize(size);
    setCurrentPage(1);
  }, []);

  const columns = useMemo<Column<PartyLedgerEntry>[]>(
    () => [
      {
        key: 'date',
        header: 'Date',
        sortable: false,
        render: (value: string) => <span className="text-gray-600 whitespace-nowrap">{formatDate(value)}</span>,
      },
      {
        key: 'description',
        header: 'Description',
        sortable: false,
        render: (_: string, row: PartyLedgerEntry) => (
          <PartyLedgerEntryDescription row={row} />
        ),
      },
      {
        key: 'in',
        header: 'In',
        sortable: false,
        render: (_: unknown, row: PartyLedgerEntry) => (
          <span className="tabular-nums text-green-600">{row.credit > 0 ? fmt(row.credit) : '—'}</span>
        ),
      },
      {
        key: 'out',
        header: 'Out',
        sortable: false,
        render: (_: unknown, row: PartyLedgerEntry) => (
          <span className="tabular-nums text-red-600">{row.debit > 0 ? fmt(row.debit) : '—'}</span>
        ),
      },
      {
        key: 'runningBalance',
        header: 'Running Balance',
        sortable: false,
        render: (value: number) => <span className="tabular-nums font-semibold text-gray-800">{fmt(value)}</span>,
      },
    ],
    [fmt]
  );

  const footerRow = useMemo(() => {
    if (totalRecords <= 0) return undefined;
    return {
      label: 'Total',
      values: {
        description: 'Total',
        in: <span className="tabular-nums font-bold text-green-600">{fmt(totalCredit)}</span>,
        out: <span className="tabular-nums font-bold text-red-600">{fmt(totalDebit)}</span>,
        runningBalance: <span className="tabular-nums font-bold text-blue-800">{fmt(periodClosingBalance)}</span>,
      },
    };
  }, [totalRecords, totalDebit, totalCredit, periodClosingBalance, fmt]);

  if (branchId <= 0) {
    return <div className="flex items-center justify-center h-64 text-gray-500">Please select a branch first.</div>;
  }

  return (
    <div className="flex h-[calc(100dvh-7.5rem)] min-h-[28rem] flex-col gap-4 overflow-hidden p-4 md:p-6">
      <div className="shrink-0 space-y-4">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-gray-800">Customer Ledger</h1>
          <p className="text-sm text-gray-500 mt-0.5">Read-only receivable history — use Receivables screen to add or edit receipts</p>
        </div>
        <div className="flex flex-wrap gap-2">
          {customerId > 0 && (
            <button
              type="button"
              onClick={() => void onExport()}
              disabled={loading || exporting}
              className="px-4 py-2 bg-emerald-50 border border-emerald-300 text-emerald-800 text-sm font-medium rounded-lg hover:bg-emerald-100 transition-colors disabled:opacity-60"
            >
              {exporting ? 'Exporting…' : 'Export CSV'}
            </button>
          )}
        </div>
      </div>

      <div className="bg-white rounded-xl border border-gray-100 p-4 grid grid-cols-1 md:grid-cols-5 gap-4">
        <div>
          <label className="text-xs text-gray-500 font-medium mb-1 block">Search Customer</label>
          <input
            type="text"
            value={customerSearch}
            onChange={(e) => setCustomerSearch(e.target.value)}
            placeholder="Name, code, or phone"
            className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-400"
          />
        </div>
        <div>
          <label className="text-xs text-gray-500 font-medium mb-1 block">Customer</label>
          <select
            value={customerId || ''}
            onChange={(e) => setCustomerId(Number(e.target.value))}
            className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-400"
          >
            <option value="">Select customer</option>
            {customers.map((c) => (
              <option key={c.id} value={c.id}>
                {c.customerCode ? `${c.customerCode} — ` : ''}{c.name}{c.isWalkIn ? ' (Walk-in)' : ''}
              </option>
            ))}
          </select>
        </div>
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
        {customerId > 0 && (
          <div className="flex items-end">
            <div className="w-full rounded-lg bg-blue-50 border border-blue-100 px-4 py-2">
              <p className="text-xs text-blue-600">Outstanding — {partyName}</p>
              <p className="text-lg font-bold text-blue-900">{fmt(currentBalance)}</p>
            </div>
          </div>
        )}
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

      {error && <div className="bg-red-50 border border-red-200 text-red-600 rounded-lg px-4 py-3 text-sm">{error}</div>}
      </div>

      <div className="min-h-0 flex-1 flex flex-col">
      {!customerId ? (
        <div className="bg-white rounded-xl border border-gray-200 p-12 text-center text-gray-400">
          Select a customer to view ledger entries.
        </div>
      ) : (
        <DataTable
          columns={columns}
          data={rows}
          loading={loading}
          searchable={false}
          serverSide
          fillHeight
          currentPage={currentPage}
          pageSize={pageSize}
          pageSizeOptions={LEDGER_PAGE_SIZE_OPTIONS}
          totalRecords={totalRecords}
          totalPages={totalPages}
          onPageChange={setCurrentPage}
          onPageSizeChange={handlePageSizeChange}
          footerRow={footerRow}
          emptyMessage="No ledger entries found for this customer."
        />
      )}
      </div>

    </div>
  );
}
