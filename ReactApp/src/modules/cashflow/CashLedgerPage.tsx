import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import DataTable, { type Column } from '../../components/DataTable';
import { useFormModal } from '../../contexts/FormModalContext';
import { useGridExport } from '../../hooks/useGridExport';
import { getApiErrorMessage } from '../../services/api';
import { cashLedgerExportColumns } from '../../utils/gridExportColumns';
import { useBranchStore } from '../../stores/useBranchStore';
import {
  cashFlowService,
  type CashFlowTransactionDto,
  type CashFlowTransactionType,
  type CashFlowPaymentMethod,
} from './cashFlowService';

// ─── Helpers ──────────────────────────────────────────────────────────────────

const fmt = (n: number) =>
  new Intl.NumberFormat(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(n);

const formatDate = (s: string) => {
  const d = new Date(s);
  return isNaN(d.getTime())
    ? '—'
    : d.toLocaleDateString(undefined, {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      });
};

const TYPE_COLORS: Record<CashFlowTransactionType, string> = {
  Sale: 'bg-emerald-100 text-emerald-700',
  Expense: 'bg-red-100 text-red-600',
  CashIn: 'bg-blue-100 text-blue-700',
  CashOut: 'bg-orange-100 text-orange-700',
  BankTransfer: 'bg-purple-100 text-purple-700',
  OpeningBalance: 'bg-gray-100 text-gray-600',
  ClosingBalance: 'bg-gray-100 text-gray-700',
};

const TYPE_LABELS: Record<CashFlowTransactionType, string> = {
  Sale: 'Sale',
  Expense: 'Expense',
  CashIn: 'Cash In',
  CashOut: 'Cash Out',
  BankTransfer: 'Bank Transfer',
  OpeningBalance: 'Opening',
  ClosingBalance: 'Closing',
};

const isInflow = (type: CashFlowTransactionType) =>
  type === 'Sale' || type === 'CashIn' || type === 'OpeningBalance';

const TX_TYPES: CashFlowTransactionType[] = [
  'Sale',
  'Expense',
  'CashIn',
  'CashOut',
  'BankTransfer',
  'OpeningBalance',
  'ClosingBalance',
];

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function CashLedgerPage() {
  const navigate = useNavigate();
  const { openForm, isOpen } = useFormModal();
  const { selectedBranchId } = useBranchStore();
  const branchId = selectedBranchId ?? 0;

  const today = new Date().toISOString().slice(0, 10);
  const [fromDate, setFromDate] = useState(today);
  const [toDate, setToDate] = useState(today);
  const [typeFilter, setTypeFilter] = useState<CashFlowTransactionType | ''>('');
  const [methodFilter, setMethod] = useState<CashFlowPaymentMethod | ''>('');
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);

  const [rows, setRows] = useState<CashFlowTransactionDto[]>([]);
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [totalIn, setTotalIn] = useState(0);
  const [totalOut, setTotalOut] = useState(0);
  const [netTotal, setNetTotal] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const columns = useMemo<Column<CashFlowTransactionDto>[]>(
    () => [
      {
        key: 'transactionDate',
        header: 'Date',
        sortable: false,
        render: (value: string) => (
          <span className="text-gray-600 whitespace-nowrap">{formatDate(value)}</span>
        ),
      },
      {
        key: 'transactionType',
        header: 'Type',
        sortable: false,
        render: (value: string) => {
          const type = value as CashFlowTransactionType;
          return (
            <span
              className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                TYPE_COLORS[type] ?? 'bg-gray-100 text-gray-600'
              }`}
            >
              {TYPE_LABELS[type] ?? value}
            </span>
          );
        },
      },
      {
        key: 'paymentMethod',
        header: 'Method',
        sortable: false,
        render: (value: string) => <span className="text-gray-500">{value}</span>,
      },
      {
        key: 'description',
        header: 'Description',
        sortable: false,
        render: (value: string | null) => (
          <span className="text-gray-600 max-w-[200px] truncate block" title={value ?? undefined}>
            {value?.trim() || '—'}
          </span>
        ),
      },
      {
        key: 'referenceNo',
        header: 'Reference',
        sortable: false,
        render: (value: string | null) => (
          <span className="text-gray-500 font-mono text-xs">{value?.trim() || '—'}</span>
        ),
      },
      {
        key: 'branchName',
        header: 'Branch',
        sortable: false,
        render: (value: string) => <span className="text-gray-500">{value}</span>,
      },
      {
        key: 'amount',
        header: 'Amount',
        sortable: false,
        width: '120px',
        render: (value: number, item) => {
          const inflow = isInflow(item.transactionType as CashFlowTransactionType);
          return (
            <span className={`font-bold ${inflow ? 'text-emerald-600' : 'text-red-500'}`}>
              {inflow ? '+' : '−'}
              {fmt(value)}
            </span>
          );
        },
      },
    ],
    [],
  );

  const fetchLedger = useCallback(async () => {
    if (branchId <= 0) {
      setRows([]);
      setTotalRecords(0);
      setTotalPages(0);
      setTotalIn(0);
      setTotalOut(0);
      setNetTotal(0);
      setLoading(false);
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const res = await cashFlowService.getLedger(branchId, {
        fromDate,
        toDate,
        transactionType: typeFilter || null,
        paymentMethod: methodFilter || null,
        page: currentPage,
        pageSize,
      });
      setRows(res.data.transactions);
      setTotalRecords(res.data.totalRecords);
      setTotalPages(res.data.totalPages);
      setTotalIn(Number(res.data.totalIn ?? 0));
      setTotalOut(Number(res.data.totalOut ?? 0));
      setNetTotal(Number(res.data.netTotal ?? 0));
    } catch (e) {
      setError(getApiErrorMessage(e, 'Failed to load ledger.'));
      setRows([]);
      setTotalRecords(0);
      setTotalPages(0);
      setTotalIn(0);
      setTotalOut(0);
      setNetTotal(0);
    } finally {
      setLoading(false);
    }
  }, [branchId, fromDate, toDate, typeFilter, methodFilter, currentPage, pageSize]);

  const fetchExportPage = useCallback(async (pageNumber: number, exportPageSize: number) => {
    const res = await cashFlowService.getLedger(branchId, {
      fromDate,
      toDate,
      transactionType: typeFilter || null,
      paymentMethod: methodFilter || null,
      page: pageNumber,
      pageSize: exportPageSize,
    });
    return { data: res.data.transactions, totalRecords: res.data.totalRecords };
  }, [branchId, fromDate, toDate, typeFilter, methodFilter]);

  const exportFilename = useMemo(() => {
    const typePart = typeFilter ? `-${typeFilter.toLowerCase()}` : '';
    const methodPart = methodFilter ? `-${methodFilter.toLowerCase()}` : '';
    return `cash-ledger-${fromDate}-${toDate}${typePart}${methodPart}`;
  }, [fromDate, toDate, typeFilter, methodFilter]);

  const { exporting, onExport } = useGridExport(
    exportFilename,
    cashLedgerExportColumns,
    fetchExportPage,
    branchId > 0,
  );

  useEffect(() => {
    void fetchLedger();
  }, [fetchLedger]);

  useEffect(() => {
    if (!isOpen) {
      void fetchLedger();
    }
  }, [isOpen, fetchLedger]);

  if (branchId <= 0) {
    return (
      <div className="flex items-center justify-center h-64 text-gray-500 p-4 md:p-6">
        Please select a branch to view the cash ledger.
      </div>
    );
  }

  return (
    <div className="space-y-4 p-4 md:p-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-gray-800">Cash Flow Ledger</h1>
          <p className="text-sm text-gray-500 mt-0.5">All cash movements for the selected filters</p>
        </div>
        <div className="flex gap-2">
          <button
            type="button"
            onClick={() => void onExport()}
            disabled={loading || exporting}
            className="px-4 py-2 bg-emerald-50 border border-emerald-300 text-emerald-800 text-sm font-medium rounded-lg hover:bg-emerald-100 transition-colors disabled:opacity-60"
          >
            {exporting ? 'Exporting…' : 'Export CSV'}
          </button>
          <button
            type="button"
            onClick={() => openForm('cashTransaction')}
            className="px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 transition-colors"
          >
            + New Transaction
          </button>
          <button
            type="button"
            onClick={() => navigate('/cashflow')}
            className="px-4 py-2 bg-white border border-gray-200 text-gray-700 text-sm font-medium rounded-lg hover:bg-gray-50 transition-colors"
          >
            Dashboard
          </button>
        </div>
      </div>

      <div className="bg-white rounded-xl border border-gray-100 p-4">
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
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
          <div>
            <label className="text-xs text-gray-500 font-medium mb-1 block">Type</label>
            <select
              value={typeFilter}
              onChange={(e) => {
                setTypeFilter(e.target.value as CashFlowTransactionType | '');
                setCurrentPage(1);
              }}
              className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-400"
            >
              <option value="">All Types</option>
              {TX_TYPES.map((t) => (
                <option key={t} value={t}>
                  {TYPE_LABELS[t]}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="text-xs text-gray-500 font-medium mb-1 block">Method</label>
            <select
              value={methodFilter}
              onChange={(e) => {
                setMethod(e.target.value as CashFlowPaymentMethod | '');
                setCurrentPage(1);
              }}
              className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-400"
            >
              <option value="">All Methods</option>
              <option value="Cash">Cash</option>
              <option value="Bank">Bank</option>
              <option value="Wallet">Wallet</option>
            </select>
          </div>
        </div>
      </div>

      {!loading && totalRecords > 0 && (
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div className="bg-emerald-50 border border-emerald-100 rounded-xl p-4 text-center">
            <p className="text-xs text-emerald-600 font-medium uppercase">Total In</p>
            <p className="text-lg font-bold text-emerald-700 mt-1">{fmt(totalIn)}</p>
          </div>
          <div className="bg-red-50 border border-red-100 rounded-xl p-4 text-center">
            <p className="text-xs text-red-500 font-medium uppercase">Total Out</p>
            <p className="text-lg font-bold text-red-600 mt-1">{fmt(totalOut)}</p>
          </div>
          <div className="bg-blue-50 border border-blue-100 rounded-xl p-4 text-center">
            <p className="text-xs text-blue-600 font-medium uppercase">Net</p>
            <p className={`text-lg font-bold mt-1 ${netTotal >= 0 ? 'text-blue-700' : 'text-red-600'}`}>
              {fmt(netTotal)}
            </p>
          </div>
        </div>
      )}

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 rounded-xl px-5 py-3 text-sm">
          {error}
        </div>
      )}

      <DataTable
        data={rows}
        columns={columns}
        loading={loading}
        searchable={false}
        pagination
        serverSide
        totalRecords={totalRecords}
        totalPages={totalPages}
        currentPage={currentPage}
        pageSize={pageSize}
        pageSizeOptions={[10, 25, 50, 100]}
        onPageChange={setCurrentPage}
        onPageSizeChange={(size) => {
          setPageSize(size);
          setCurrentPage(1);
        }}
        emptyMessage="No transactions found for the selected filters."
      />
    </div>
  );
}
