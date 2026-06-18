import React, { useCallback, useEffect, useMemo, useState } from 'react';
import DataTable, { type Column } from '../../components/DataTable';
import { useFormModal } from '../../contexts/FormModalContext';
import { useGridExport } from '../../hooks/useGridExport';
import { getApiErrorMessage } from '../../services/api';
import { partyLedgerExportColumns } from '../../utils/gridExportColumns';
import { useBranchStore } from '../../stores/useBranchStore';
import { useBusinessCurrency } from '../../hooks/useBusinessCurrency';
import { customerService, type CustomerListItem } from '../customer/customerService';
import { LEDGER_TYPE_LABELS, partyLedgerService, type PartyLedgerEntry } from './partyLedgerService';

const formatDate = (value: string) => {
  const d = new Date(value);
  return Number.isNaN(d.getTime())
    ? '—'
    : d.toLocaleDateString(undefined, {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      });
};

export default function CustomerLedgerPage() {
  const { fmt } = useBusinessCurrency();
  const { openForm, isOpen } = useFormModal();
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
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

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
        toDate || undefined
      );
      setRows(res.data.entries);
      setPartyName(res.data.partyName);
      setCurrentBalance(res.data.currentBalance);
      setTotalRecords(res.data.totalRecords);
      setTotalPages(res.data.totalPages);
    } catch (err) {
      setRows([]);
      setError(getApiErrorMessage(err, 'Failed to load customer ledger.'));
    } finally {
      setLoading(false);
    }
  }, [branchId, customerId, currentPage, pageSize, fromDate, toDate]);

  const fetchExportPage = useCallback(async (pageNumber: number, exportPageSize: number) => {
    const res = await partyLedgerService.getCustomerLedger(
      branchId,
      customerId,
      pageNumber,
      exportPageSize,
      fromDate || undefined,
      toDate || undefined,
    );
    return { data: res.data.entries, totalRecords: res.data.totalRecords };
  }, [branchId, customerId, fromDate, toDate]);

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
    if (!isOpen) {
      void fetchLedger();
    }
  }, [isOpen, fetchLedger]);

  useEffect(() => {
    setCurrentPage(1);
  }, [customerId, fromDate, toDate]);

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
          <div>
            <p className="text-gray-800 text-sm">{row.description}</p>
            <p className="text-xs text-gray-400">{LEDGER_TYPE_LABELS[row.type] ?? row.type}</p>
          </div>
        ),
      },
      {
        key: 'debit',
        header: 'Debit',
        sortable: false,
        render: (value: number) => (
          <span className="tabular-nums text-red-600">{value > 0 ? fmt(value) : '—'}</span>
        ),
      },
      {
        key: 'credit',
        header: 'Credit',
        sortable: false,
        render: (value: number) => (
          <span className="tabular-nums text-green-600">{value > 0 ? fmt(value) : '—'}</span>
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

  if (branchId <= 0) {
    return <div className="flex items-center justify-center h-64 text-gray-500">Please select a branch first.</div>;
  }

  return (
    <div className="space-y-4 p-4 md:p-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-gray-800">Customer Ledger</h1>
          <p className="text-sm text-gray-500 mt-0.5">Receivable transactions and running balance</p>
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
          <button
            type="button"
            onClick={() => openForm('receivePayment', customerId > 0 ? { customerId } : undefined)}
            className="px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 transition-colors"
          >
            + Receive Payment
          </button>
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

      {error && <div className="bg-red-50 border border-red-200 text-red-600 rounded-lg px-4 py-3 text-sm">{error}</div>}

      {!customerId ? (
        <div className="bg-white rounded-xl border border-gray-200 p-12 text-center text-gray-400">
          Select a customer to view ledger entries.
        </div>
      ) : (
        <DataTable
          columns={columns}
          data={rows}
          loading={loading}
          currentPage={currentPage}
          pageSize={pageSize}
          totalRecords={totalRecords}
          totalPages={totalPages}
          onPageChange={setCurrentPage}
          onPageSizeChange={setPageSize}
          emptyMessage="No ledger entries found for this customer."
        />
      )}
    </div>
  );
}
