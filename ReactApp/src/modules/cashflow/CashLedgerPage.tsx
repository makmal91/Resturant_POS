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
  type LedgerResponse,
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
      });
};

const TYPE_COLORS: Record<CashFlowTransactionType, string> = {
  Sale: 'bg-emerald-100 text-emerald-700',
  Expense: 'bg-red-100 text-red-600',
  CashIn: 'bg-blue-100 text-blue-700',
  CashOut: 'bg-orange-100 text-orange-700',
  BankTransfer: 'bg-purple-100 text-purple-700',
  OpeningBalance: 'bg-gray-100 text-gray-600',
  OpeningStockVoucher: 'bg-teal-100 text-teal-700',
  ClosingBalance: 'bg-gray-100 text-gray-700',
  Reversal: 'bg-amber-100 text-amber-800',
};

const TYPE_LABELS: Record<CashFlowTransactionType, string> = {
  Sale: 'Sale',
  Expense: 'Expense',
  CashIn: 'Cash In',
  CashOut: 'Cash Out',
  BankTransfer: 'Bank Transfer',
  OpeningBalance: 'Opening',
  OpeningStockVoucher: 'Opening Stock',
  ClosingBalance: 'Closing',
  Reversal: 'Reversal',
};

const TX_TYPES: CashFlowTransactionType[] = [
  'Sale',
  'Expense',
  'CashIn',
  'CashOut',
  'BankTransfer',
  'OpeningBalance',
  'OpeningStockVoucher',
  'ClosingBalance',
  'Reversal',
];

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function CashLedgerPage() {
  const navigate = useNavigate();
  const { isOpen } = useFormModal();
  const { selectedBranchId } = useBranchStore();
  const branchId = selectedBranchId ?? 0;

  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);

  const [rows, setRows] = useState<CashFlowTransactionDto[]>([]);
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [totalDebit, setTotalDebit] = useState(0);
  const [totalCredit, setTotalCredit] = useState(0);
  const [netTotal, setNetTotal] = useState(0);
  const [periodOpeningBalance, setPeriodOpeningBalance] = useState(0);
  const [closingBalance, setClosingBalance] = useState(0);
  const [accountName, setAccountName] = useState('');
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
        key: 'accountName',
        header: 'Account',
        sortable: false,
        render: (value: string | null) => (
          <span className="text-gray-700 max-w-[180px] truncate block" title={value?.trim() || undefined}>
            {value?.trim() || '—'}
          </span>
        ),
      },
      {
        key: 'description',
        header: 'Description',
        sortable: false,
        render: (value: string | null) => (
          <span className="text-gray-600 max-w-[240px] truncate block" title={value ?? undefined}>
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
        key: 'debit',
        header: 'In',
        sortable: false,
        width: '110px',
        render: (value: number) => (
          <span className="tabular-nums text-emerald-600">{value > 0 ? fmt(value) : '—'}</span>
        ),
      },
      {
        key: 'credit',
        header: 'Out',
        sortable: false,
        width: '110px',
        render: (value: number) => (
          <span className="tabular-nums text-red-600">{value > 0 ? fmt(value) : '—'}</span>
        ),
      },
      {
        key: 'runningBalance',
        header: 'Balance',
        sortable: false,
        width: '130px',
        render: (value: number) => (
          <span className="tabular-nums font-semibold text-gray-800">{fmt(value)}</span>
        ),
      },
    ],
    [],
  );

  const footerRow = useMemo(() => {
    if (totalRecords <= 0) return undefined;
    return {
      label: 'Total',
      values: {
        transactionType: 'Total',
        debit: <span className="tabular-nums font-bold text-emerald-600">{fmt(totalDebit)}</span>,
        credit: <span className="tabular-nums font-bold text-red-600">{fmt(totalCredit)}</span>,
        runningBalance: (
          <span className="tabular-nums font-bold text-blue-800">{fmt(closingBalance)}</span>
        ),
      },
    };
  }, [totalRecords, totalDebit, totalCredit, closingBalance]);

  const fetchLedger = useCallback(async () => {
    if (branchId <= 0) {
      setRows([]);
      setTotalRecords(0);
      setTotalPages(0);
      setTotalDebit(0);
      setTotalCredit(0);
      setNetTotal(0);
      setPeriodOpeningBalance(0);
      setClosingBalance(0);
      setAccountName('');
      setLoading(false);
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const res = await cashFlowService.getLedger(branchId, {
        fromDate,
        toDate,
        page: currentPage,
        pageSize,
      });
      const payload = res.data as LedgerResponse & Record<string, unknown>;
      setRows(
        (payload.transactions ?? []).map((row) => {
          const isInflow = Boolean(row.isInflow ?? row.IsInflow ?? false);
          const displayAmount = Number(row.displayAmount ?? row.DisplayAmount ?? Math.abs(Number(row.amount ?? 0)));
          const debit = Number(row.debit ?? row.Debit ?? (isInflow ? displayAmount : 0));
          const credit = Number(row.credit ?? row.Credit ?? (isInflow ? 0 : displayAmount));
          return {
            ...row,
            accountName: String(row.accountName ?? row.AccountName ?? ''),
            runningBalance: Number(row.runningBalance ?? row.RunningBalance ?? 0),
            displayAmount,
            isInflow,
            debit,
            credit,
          };
        })
      );
      setTotalRecords(payload.totalRecords);
      setTotalPages(payload.totalPages);
      setTotalDebit(Number(payload.totalDebit ?? payload.totalOut ?? 0));
      setTotalCredit(Number(payload.totalCredit ?? payload.totalIn ?? 0));
      setNetTotal(Number(payload.netTotal ?? 0));
      setPeriodOpeningBalance(Number(payload.periodOpeningBalance ?? 0));
      setClosingBalance(
        Number(payload.closingBalance ?? Number(payload.periodOpeningBalance ?? 0) + Number(payload.netTotal ?? 0))
      );
      setAccountName(String(payload.accountName ?? payload.AccountName ?? ''));
    } catch (e) {
      setError(getApiErrorMessage(e, 'Failed to load ledger.'));
      setRows([]);
      setTotalRecords(0);
      setTotalPages(0);
      setTotalDebit(0);
      setTotalCredit(0);
      setNetTotal(0);
      setPeriodOpeningBalance(0);
      setClosingBalance(0);
      setAccountName('');
    } finally {
      setLoading(false);
    }
  }, [branchId, fromDate, toDate, currentPage, pageSize]);

  const fetchExportPage = useCallback(async (pageNumber: number, exportPageSize: number) => {
    const res = await cashFlowService.getLedger(branchId, {
      fromDate,
      toDate,
      page: pageNumber,
      pageSize: exportPageSize,
    });
    return { data: res.data.transactions, totalRecords: res.data.totalRecords };
  }, [branchId, fromDate, toDate]);

  const exportFilename = useMemo(() => `cash-ledger-${fromDate}-${toDate}`, [fromDate, toDate]);

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
    <div className="flex h-[calc(100dvh-7.5rem)] min-h-[28rem] flex-col gap-4 overflow-hidden">
      <div className="shrink-0 space-y-4">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-gray-800">
            Cash Flow Ledger{accountName ? ` — ${accountName}` : ''}
          </h1>
          <p className="text-sm text-gray-500 mt-0.5">Cash account movements from the general ledger</p>
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
            onClick={() => navigate('/cashflow')}
            className="px-4 py-2 bg-white border border-gray-200 text-gray-700 text-sm font-medium rounded-lg hover:bg-gray-50 transition-colors"
          >
            Dashboard
          </button>
        </div>
      </div>

      <div className="bg-white rounded-xl border border-gray-100 p-4">
        <div className="grid grid-cols-2 md:grid-cols-2 gap-3">
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
      </div>

      {!loading && totalRecords > 0 && (
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div className="bg-emerald-50 border border-emerald-100 rounded-xl p-4 text-center">
            <p className="text-xs text-emerald-600 font-medium uppercase">Total In</p>
            <p className="text-lg font-bold text-emerald-700 mt-1">{fmt(totalDebit)}</p>
          </div>
          <div className="bg-red-50 border border-red-100 rounded-xl p-4 text-center">
            <p className="text-xs text-red-500 font-medium uppercase">Total Out</p>
            <p className="text-lg font-bold text-red-600 mt-1">{fmt(totalCredit)}</p>
          </div>
          <div className="bg-blue-50 border border-blue-100 rounded-xl p-4 text-center">
            <p className="text-xs text-blue-600 font-medium uppercase">Balance</p>
            <p className={`text-lg font-bold mt-1 ${netTotal >= 0 ? 'text-blue-700' : 'text-red-600'}`}>
              {fmt(closingBalance)}
            </p>
          </div>
        </div>
      )}

      {!loading && periodOpeningBalance !== 0 && (
        <div className="bg-gray-50 border border-gray-200 rounded-xl px-4 py-2 text-sm text-gray-600">
          Opening balance before selected period: <span className="font-semibold text-gray-800">{fmt(periodOpeningBalance)}</span>
        </div>
      )}

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 rounded-xl px-5 py-3 text-sm">
          {error}
        </div>
      )}
      </div>

      <div className="min-h-0 flex-1 flex flex-col">
      <DataTable
        data={rows}
        columns={columns}
        loading={loading}
        searchable={false}
        pagination
        serverSide
        fillHeight
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
        footerRow={footerRow}
        emptyMessage="No transactions found for the selected filters."
      />
      </div>
    </div>
  );
}
