import React, { useCallback, useEffect, useMemo, useState } from 'react';
import DataTable, { type Column } from '../../components/DataTable';
import LedgerViewToggle from '../../components/LedgerViewToggle';
import { useGridExport } from '../../hooks/useGridExport';
import { getApiErrorMessage } from '../../services/api';
import { supplierLedgerExportColumns } from '../../utils/gridExportColumns';
import { useBranchStore } from '../../stores/useBranchStore';
import { useBusinessCurrency } from '../../hooks/useBusinessCurrency';
import { supplierService, type SupplierItem } from '../supplier/supplierService';
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

const formatPayableBalance = (value: number, fmt: (amount: number) => string) => (
  <span className="tabular-nums font-semibold text-gray-800">{fmt(value)}</span>
);

export default function SupplierLedgerPage() {
  const { fmt } = useBusinessCurrency();
  const { selectedBranchId } = useBranchStore();
  const branchId = selectedBranchId ?? 0;

  const [suppliers, setSuppliers] = useState<SupplierItem[]>([]);
  const [supplierId, setSupplierId] = useState(0);
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
  const [expandedRowKeys, setExpandedRowKeys] = useState<Set<string>>(new Set());
  const [auditView, setAuditView] = useState(false);
  const [groupByChain, setGroupByChain] = useState(false);

  const toggleRowExpanded = useCallback((rowKey: string) => {
    setExpandedRowKeys((prev) => {
      const next = new Set(prev);
      if (next.has(rowKey)) next.delete(rowKey);
      else next.add(rowKey);
      return next;
    });
  }, []);

  const getRowKey = useCallback(
    (row: PartyLedgerEntry) => String(row.paymentId ?? row.id),
    [],
  );

  useEffect(() => {
    if (branchId <= 0) return;
    supplierService
      .getAllActive(branchId)
      .then((res) => setSuppliers(Array.isArray(res.data) ? res.data : []))
      .catch(() => setSuppliers([]));
  }, [branchId]);

  const fetchLedger = useCallback(async () => {
    if (branchId <= 0 || supplierId <= 0) {
      setRows([]);
      setTotalRecords(0);
      setTotalPages(0);
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const res = await partyLedgerService.getSupplierLedger(
        branchId,
        supplierId,
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
      setExpandedRowKeys(new Set());
    } catch (err) {
      setRows([]);
      setError(getApiErrorMessage(err, 'Failed to load supplier ledger.'));
    } finally {
      setLoading(false);
    }
  }, [branchId, supplierId, currentPage, pageSize, fromDate, toDate, auditView, groupByChain]);

  const fetchExportPage = useCallback(async (pageNumber: number, exportPageSize: number) => {
    const res = await partyLedgerService.getSupplierLedger(
      branchId,
      supplierId,
      pageNumber,
      exportPageSize,
      fromDate || undefined,
      toDate || undefined,
      { auditView, groupByChain },
    );
    return { data: res.data.entries, totalRecords: res.data.totalRecords };
  }, [branchId, supplierId, fromDate, toDate, auditView, groupByChain]);

  const exportFilename = useMemo(() => {
    const slug = partyName.trim().replace(/\s+/g, '-').toLowerCase() || 'supplier';
    const range = fromDate || toDate ? `-${fromDate || 'start'}-${toDate || 'end'}` : '';
    return `supplier-ledger-${slug}${range}`;
  }, [partyName, fromDate, toDate]);

  const { exporting, onExport } = useGridExport(
    exportFilename,
    supplierLedgerExportColumns,
    fetchExportPage,
    branchId > 0 && supplierId > 0,
  );

  useEffect(() => {
    void fetchLedger();
  }, [fetchLedger]);

  const handlePageSizeChange = useCallback((size: number) => {
    setPageSize(size);
    setCurrentPage(1);
  }, []);

  useEffect(() => {
    setCurrentPage(1);
  }, [supplierId, fromDate, toDate, pageSize]);

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
          <PartyLedgerEntryDescription
            row={row}
            expanded={expandedRowKeys.has(getRowKey(row))}
            onToggle={() => toggleRowExpanded(getRowKey(row))}
            fmt={fmt}
          />
        ),
      },
      {
        key: 'debit',
        header: 'In',
        sortable: false,
        render: (value: number) => (
          <span className="tabular-nums text-green-600">{value > 0 ? fmt(value) : '—'}</span>
        ),
      },
      {
        key: 'credit',
        header: 'Out',
        sortable: false,
        render: (value: number) => (
          <span className="tabular-nums text-red-600">{value > 0 ? fmt(value) : '—'}</span>
        ),
      },
      {
        key: 'runningBalance',
        header: 'Running Balance',
        sortable: false,
        render: (value: number) => formatPayableBalance(value, fmt),
      },
    ],
    [fmt, expandedRowKeys, getRowKey, toggleRowExpanded]
  );

  const footerRow = useMemo(() => {
    if (totalRecords <= 0) return undefined;
    return {
      label: 'Total',
      values: {
        description: 'Total',
        debit: <span className="tabular-nums font-bold text-green-600">{fmt(totalDebit)}</span>,
        credit: <span className="tabular-nums font-bold text-red-600">{fmt(totalCredit)}</span>,
        runningBalance: formatPayableBalance(periodClosingBalance, fmt),
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
          <h1 className="text-2xl font-bold text-gray-800">Supplier Ledger</h1>
          <p className="text-sm text-gray-500 mt-0.5">Read-only payable history — use Payables screen to add or edit payments</p>
        </div>
        <div className="flex flex-wrap gap-2">
          {supplierId > 0 && (
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

      <div className="bg-white rounded-xl border border-gray-100 p-4 grid grid-cols-1 md:grid-cols-4 gap-4">
        <div>
          <label className="text-xs text-gray-500 font-medium mb-1 block">Supplier</label>
          <select
            value={supplierId || ''}
            onChange={(e) => setSupplierId(Number(e.target.value))}
            className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-400"
          >
            <option value="">Select supplier</option>
            {suppliers.map((s) => (
              <option key={s.id} value={s.id}>{s.name}</option>
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
        {supplierId > 0 && (
          <div className="flex items-end">
            <div className="w-full rounded-lg bg-orange-50 border border-orange-100 px-4 py-2">
              <p className="text-xs text-orange-600">Payable — {partyName}</p>
              <div className="text-lg font-bold text-orange-900">
                {formatPayableBalance(currentBalance, fmt)}
              </div>
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
      {!supplierId ? (
        <div className="bg-white rounded-xl border border-gray-200 p-12 text-center text-gray-400">
          Select a supplier to view ledger entries.
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
          emptyMessage="No ledger entries found for this supplier."
        />
      )}
      </div>

    </div>
  );
}
