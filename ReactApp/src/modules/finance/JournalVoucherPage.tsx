import React, { useCallback, useEffect, useMemo, useState } from 'react';
import DataTable, { type Column } from '../../components/DataTable';
import Badge from '../../components/Badge';
import PermissionGate from '../../components/PermissionGate';
import { useFormModal } from '../../contexts/FormModalContext';
import { useBusinessCurrency } from '../../hooks/useBusinessCurrency';
import { useModuleCrudAccess } from '../../hooks/useModuleCrudAccess';
import { hasBranchContext } from '../../types/permissions';
import { getApiErrorMessage } from '../../services/api';
import { useBranchStore } from '../../stores/useBranchStore';
import {
  journalVoucherService,
  type JournalVoucherDto,
  type JournalVoucherType,
} from './journalVoucherService';

const formatDate = (value: string) => {
  const d = new Date(value);
  return Number.isNaN(d.getTime())
    ? '—'
    : d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
};

const TYPE_LABELS: Record<JournalVoucherType, string> = {
  CashIn: 'Cash In',
  CashOut: 'Cash Out',
};

const TYPE_VARIANT: Record<JournalVoucherType, 'success' | 'warning'> = {
  CashIn: 'success',
  CashOut: 'warning',
};

export default function JournalVoucherPage() {
  const { fmt } = useBusinessCurrency();
  const { openForm, isOpen } = useFormModal();
  const { canAdd, getWriteBlockMessage } = useModuleCrudAccess('Cash Flow');
  const { selectedBranchId } = useBranchStore();
  const branchId = selectedBranchId ?? 0;
  const hasBranchSelection = hasBranchContext(selectedBranchId);

  const [items, setItems] = useState<JournalVoucherDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [typeFilter, setTypeFilter] = useState<JournalVoucherType | ''>('');
  const [notification, setNotification] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  const showNotification = useCallback((type: 'success' | 'error', message: string) => {
    setNotification({ type, message });
    setTimeout(() => setNotification(null), 4000);
  }, []);

  const fetchVouchers = useCallback(async () => {
    if (!hasBranchSelection || branchId <= 0) {
      setItems([]);
      setTotalRecords(0);
      setTotalPages(0);
      return;
    }

    setLoading(true);
    try {
      const res = await journalVoucherService.list(branchId, currentPage, pageSize, {
        fromDate: fromDate || undefined,
        toDate: toDate || undefined,
        transactionType: typeFilter,
      });
      setItems(res.vouchers);
      setTotalRecords(res.totalRecords);
      setTotalPages(res.totalPages);
    } catch (err) {
      setItems([]);
      setTotalRecords(0);
      setTotalPages(0);
      showNotification('error', getApiErrorMessage(err, 'Failed to load journal vouchers.'));
    } finally {
      setLoading(false);
    }
  }, [hasBranchSelection, branchId, currentPage, pageSize, fromDate, toDate, typeFilter, showNotification]);

  useEffect(() => {
    void fetchVouchers();
  }, [fetchVouchers]);

  useEffect(() => {
    if (!isOpen) void fetchVouchers();
  }, [isOpen, fetchVouchers]);

  useEffect(() => {
    setCurrentPage(1);
  }, [fromDate, toDate, typeFilter, pageSize, branchId]);

  const columns = useMemo<Column<JournalVoucherDto>[]>(
    () => [
      {
        key: 'voucherNo',
        header: 'Voucher No',
        sortable: false,
        render: (value: string) => (
          <span className="font-mono text-sm text-gray-800">{value?.trim() || '—'}</span>
        ),
      },
      {
        key: 'voucherDate',
        header: 'Date',
        sortable: false,
        render: (value: string) => <span className="whitespace-nowrap">{formatDate(value)}</span>,
      },
      {
        key: 'transactionType',
        header: 'Type',
        sortable: false,
        render: (value: string) => {
          const type = value as JournalVoucherType;
          return (
            <Badge variant={TYPE_VARIANT[type] ?? 'secondary'} size="sm" dot>
              {TYPE_LABELS[type] ?? value}
            </Badge>
          );
        },
      },
      {
        key: 'paymentMethod',
        header: 'Method',
        sortable: false,
        render: (value: string) => value || '—',
      },
      {
        key: 'amount',
        header: 'Amount',
        sortable: false,
        render: (value: number) => <span className="tabular-nums font-medium">{fmt(value)}</span>,
      },
      {
        key: 'description',
        header: 'Description',
        sortable: false,
        render: (value: string | null) => (
          <span className="max-w-[280px] truncate block text-gray-600" title={value ?? undefined}>
            {value?.trim() || '—'}
          </span>
        ),
      },
    ],
    [fmt],
  );

  if (!hasBranchSelection) {
    return (
      <div className="flex items-center justify-center h-64 text-gray-500 p-4 md:p-6">
        Please select a branch to view journal vouchers.
      </div>
    );
  }

  return (
    <div className="space-y-4 p-4 md:p-6">
      {notification && (
        <div
          className={`rounded-xl px-5 py-3 text-sm border ${
            notification.type === 'success'
              ? 'bg-emerald-50 border-emerald-200 text-emerald-800'
              : 'bg-red-50 border-red-200 text-red-700'
          }`}
        >
          {notification.message}
        </div>
      )}

      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-gray-800">Journal Vouchers</h1>
          <p className="text-sm text-gray-500 mt-0.5">Record manual cash in and cash out entries</p>
        </div>
        <PermissionGate module="Cash Flow" action="create">
          <button
            type="button"
            onClick={() => {
              if (!canAdd) {
                showNotification('error', getWriteBlockMessage('create'));
                return;
              }
              openForm('journalVoucher');
            }}
            disabled={!canAdd}
            className="px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-60"
          >
            + Add Journal Voucher
          </button>
        </PermissionGate>
      </div>

      <div className="bg-white rounded-xl border border-gray-100 p-4">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
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
            <label className="text-xs text-gray-500 font-medium mb-1 block">Type</label>
            <select
              value={typeFilter}
              onChange={(e) => setTypeFilter(e.target.value as JournalVoucherType | '')}
              className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-400"
            >
              <option value="">All types</option>
              <option value="CashIn">Cash In</option>
              <option value="CashOut">Cash Out</option>
            </select>
          </div>
        </div>
      </div>

      <DataTable
        data={items}
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
        emptyMessage="No journal vouchers found for the selected filters."
      />
    </div>
  );
}
