import React, { useCallback, useEffect, useMemo, useState } from 'react';
import DataTable, { type Action, type Column } from '../../components/DataTable';
import Badge from '../../components/Badge';
import PermissionGate from '../../components/PermissionGate';
import { useFormModal } from '../../contexts/FormModalContext';
import { useConfirmDialog } from '../../contexts/ConfirmDialogContext';
import { useBusinessCurrency } from '../../hooks/useBusinessCurrency';
import { useModuleCrudAccess } from '../../hooks/useModuleCrudAccess';
import { hasBranchContext } from '../../types/permissions';
import { getApiErrorMessage } from '../../services/api';
import { safeString } from '../../utils/safeValues';
import { expenseService, type ExpenseDto, type ExpensePaymentMethod } from './expenseService';

const formatDate = (value: string) => {
  if (!value) return '—';
  const d = new Date(value);
  return isNaN(d.getTime())
    ? '—'
    : d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
};

const PAYMENT_METHODS: ExpensePaymentMethod[] = ['Cash', 'Bank', 'Wallet'];

const METHOD_VARIANT: Record<ExpensePaymentMethod, 'success' | 'info' | 'warning'> = {
  Cash: 'success',
  Bank: 'info',
  Wallet: 'warning',
};

const ExpensePage: React.FC = () => {
  const { openForm, isOpen } = useFormModal();
  const { showConfirm } = useConfirmDialog();
  const { fmt } = useBusinessCurrency();
  const {
    canAdd,
    canModify,
    canRemove,
    selectedBranchId,
    isGlobalAdmin,
    isGlobalMode,
    resolveEntityBranchId,
    getWriteBlockMessage,
  } = useModuleCrudAccess('Expenses');

  const hasBranchSelection = hasBranchContext(selectedBranchId);

  const [items, setItems] = useState<ExpenseDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [searchTerm, setSearchTerm] = useState('');
  const [sortColumn, setSortColumn] = useState<string>('expenseDate');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('desc');
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [methodFilter, setMethodFilter] = useState<ExpensePaymentMethod | ''>('');

  const [todayTotal, setTodayTotal] = useState(0);
  const [todayCash, setTodayCash] = useState(0);
  const [todayBank, setTodayBank] = useState(0);

  const [notification, setNotification] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  const showNotification = useCallback((type: 'success' | 'error', message: string) => {
    setNotification({ type, message });
    setTimeout(() => setNotification(null), 4000);
  }, []);

  const fetchExpenses = useCallback(async () => {
    if (!hasBranchSelection || selectedBranchId === null) {
      setItems([]);
      setTotalRecords(0);
      setTotalPages(0);
      return;
    }

    setLoading(true);
    try {
      const res = await expenseService.getAll(selectedBranchId, {
        paymentMethod: methodFilter || null,
        page: currentPage,
        pageSize,
      });

      const rows = Array.isArray(res.data?.expenses) ? res.data.expenses : [];
      setItems(
        rows.map((r) => ({
          id: Number(r.id ?? 0),
          branchId: Number(r.branchId ?? 0),
          branchName: safeString(r.branchName),
          expenseCategoryId: Number(r.expenseCategoryId ?? 0),
          categoryName: safeString(r.categoryName),
          description: safeString(r.description),
          amount: Number(r.amount ?? 0),
          paymentMethod: (r.paymentMethod ?? 'Cash') as ExpensePaymentMethod,
          expenseDate: safeString(r.expenseDate),
          referenceNo: r.referenceNo ? safeString(r.referenceNo) : null,
          notes: r.notes ? safeString(r.notes) : null,
          createdBy: r.createdBy ? Number(r.createdBy) : null,
          createdAt: safeString(r.createdAt),
          status: 'Pending' as const,
        })),
      );
      setTotalRecords(Number(res.data?.totalRecords ?? 0));
      setTotalPages(Number(res.data?.totalPages ?? 0));
      setTodayTotal(Number(res.data?.summary?.totalExpenses ?? 0));
      setTodayCash(Number(res.data?.summary?.totalCash ?? 0));
      setTodayBank(Number(res.data?.summary?.totalBank ?? 0));
    } catch (err) {
      setItems([]);
      setTotalRecords(0);
      setTotalPages(0);
      showNotification('error', getApiErrorMessage(err, 'Failed to load expenses.'));
    } finally {
      setLoading(false);
    }
  }, [hasBranchSelection, selectedBranchId, methodFilter, currentPage, pageSize, showNotification]);

  useEffect(() => {
    void fetchExpenses();
  }, [fetchExpenses]);

  useEffect(() => {
    if (!isOpen) return undefined;
    return () => {
      void fetchExpenses();
    };
  }, [isOpen, fetchExpenses]);

  useEffect(() => {
    setCurrentPage(1);
  }, [selectedBranchId, methodFilter, pageSize]);

  const openAdd = () => {
    const blockMessage = getWriteBlockMessage();
    if (!canAdd || blockMessage) {
      showNotification('error', blockMessage ?? 'You do not have permission to add expenses.');
      return;
    }
    openForm('expense', isGlobalMode ? {} : { branchId: selectedBranchId });
  };

  const openEdit = (item: ExpenseDto) => {
    const blockMessage = getWriteBlockMessage();
    if (!canModify || blockMessage) {
      showNotification('error', blockMessage ?? 'You do not have permission to edit expenses.');
      return;
    }
    openForm('expense', {
      id: item.id,
      expenseCategoryId: item.expenseCategoryId,
      categoryName: item.categoryName,
      description: item.description,
      amount: String(item.amount),
      paymentMethod: item.paymentMethod,
      expenseDate: item.expenseDate ? item.expenseDate.slice(0, 10) : new Date().toISOString().slice(0, 10),
      referenceNo: item.referenceNo ?? '',
      notes: item.notes ?? '',
      branchId: item.branchId,
    });
  };

  const handleDelete = (item: ExpenseDto) => {
    const blockMessage = getWriteBlockMessage();
    if (!canRemove || blockMessage) {
      showNotification('error', blockMessage ?? 'You do not have permission to delete expenses.');
      return;
    }

    const branchId = resolveEntityBranchId(item.branchId);
    if (branchId <= 0) {
      showNotification('error', 'Unable to determine the branch for this expense.');
      return;
    }

    showConfirm({
      title: 'Delete Expense?',
      message: 'This expense record will be permanently removed.',
      highlightText: `${item.categoryName} — ${fmt(item.amount)}`,
      variant: 'danger',
      confirmLabel: 'Yes, Delete',
      cancelLabel: 'Keep Expense',
      onConfirm: async () => {
        try {
          await expenseService.delete(item.id, branchId);
          void fetchExpenses();
          showNotification('success', 'Expense deleted successfully.');
        } catch (err) {
          showNotification('error', getApiErrorMessage(err, 'Failed to delete expense.'));
        }
      },
    });
  };

  const columns: Column<ExpenseDto>[] = useMemo(() => {
    const base: Column<ExpenseDto>[] = [
      {
        key: 'expenseDate',
        header: 'Date',
        sortable: true,
        render: (value) => formatDate(safeString(value)),
      },
      {
        key: 'categoryName',
        header: 'Category',
        sortable: true,
      },
      {
        key: 'description',
        header: 'Description',
        sortable: false,
      },
    ];

    if (isGlobalMode) {
      base.push({
        key: 'branchName',
        header: 'Branch',
        sortable: true,
        render: (value) => safeString(value) || '—',
      });
    }

    base.push(
      {
        key: 'paymentMethod',
        header: 'Method',
        sortable: true,
        render: (value) => (
          <Badge variant={METHOD_VARIANT[value as ExpensePaymentMethod] ?? 'secondary'} size="sm" dot>
            {safeString(value)}
          </Badge>
        ),
      },
      {
        key: 'referenceNo',
        header: 'Reference',
        sortable: false,
        render: (value) => (
          <span className="font-mono text-xs text-gray-500">{safeString(value) || '—'}</span>
        ),
      },
      {
        key: 'amount',
        header: 'Amount',
        sortable: true,
        render: (value) => (
          <span className="font-semibold text-red-600">{fmt(Number(value ?? 0))}</span>
        ),
      },
    );

    return base;
  }, [isGlobalMode, fmt]);

  const actions: Action<ExpenseDto>[] = [];
  if (canModify) {
    actions.push({
      label: '',
      onClick: openEdit,
      icon: (
        <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" title="Edit">
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
          />
        </svg>
      ),
      variant: 'secondary',
    });
  }
  if (canRemove) {
    actions.push({
      label: '',
      onClick: handleDelete,
      icon: (
        <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" title="Delete">
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
          />
        </svg>
      ),
      variant: 'danger',
    });
  }

  const emptyMessage = !hasBranchSelection
    ? 'Select a branch to load expenses.'
    : searchTerm
      ? 'No expenses match your search.'
      : 'No expenses found for this period.';

  return (
    <div>
      {notification && (
        <div
          className={`mb-6 flex items-center rounded-md p-4 ${
            notification.type === 'success' ? 'bg-green-50 text-green-800' : 'bg-red-50 text-red-800'
          }`}
        >
          {notification.type === 'success' ? (
            <svg className="mr-3 h-5 w-5" fill="currentColor" viewBox="0 0 20 20">
              <path
                fillRule="evenodd"
                d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                clipRule="evenodd"
              />
            </svg>
          ) : (
            <svg className="mr-3 h-5 w-5" fill="currentColor" viewBox="0 0 20 20">
              <path
                fillRule="evenodd"
                d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
                clipRule="evenodd"
              />
            </svg>
          )}
          <span className="font-medium">{notification.message}</span>
        </div>
      )}

      <div className="mb-8">
        <h1 className="mb-2 text-3xl font-bold text-gray-900">Expenses</h1>
        <p className="text-gray-600">Track and manage all branch expenditures</p>
      </div>

      {isGlobalMode && isGlobalAdmin && (
        <div className="mb-6 rounded-md border border-blue-200 bg-blue-50 px-4 py-3 text-sm text-blue-800">
          Global view is active. Select a target branch when recording expenses.
        </div>
      )}

      {!hasBranchSelection && (
        <div className="mb-6 rounded-md border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-700">
          Select a branch from the header to load expenses.
        </div>
      )}

      {hasBranchSelection && !loading && (
        <div className="mb-6 grid grid-cols-3 gap-4">
          {[
            { label: 'Total Expenses', value: todayTotal, color: 'text-red-600 bg-red-50 border-red-100' },
            { label: 'Cash Paid', value: todayCash, color: 'text-emerald-700 bg-emerald-50 border-emerald-100' },
            { label: 'Bank Paid', value: todayBank, color: 'text-blue-700 bg-blue-50 border-blue-100' },
          ].map(({ label, value, color }) => (
            <div key={label} className={`rounded-lg border p-4 text-center ${color}`}>
              <p className="text-xs font-medium uppercase tracking-wide opacity-70">{label}</p>
              <p className="mt-1 text-xl font-bold">{fmt(value)}</p>
            </div>
          ))}
        </div>
      )}

      <div className="mb-6 flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">Payment Method</label>
          <select
            value={methodFilter}
            onChange={(e) => setMethodFilter(e.target.value as ExpensePaymentMethod | '')}
            disabled={!hasBranchSelection}
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none disabled:cursor-not-allowed disabled:bg-gray-100 sm:w-48"
          >
            <option value="">All Methods</option>
            {PAYMENT_METHODS.map((m) => (
              <option key={m} value={m}>
                {m}
              </option>
            ))}
          </select>
        </div>

        <PermissionGate module="Expenses" action="create">
          <button
            onClick={openAdd}
            disabled={!canAdd || !hasBranchSelection}
            className="inline-flex items-center rounded-md border border-transparent bg-blue-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-60"
          >
            <svg className="-ml-1 mr-2 h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
            </svg>
            Add Expense
          </button>
        </PermissionGate>
      </div>

      <DataTable
        data={items}
        columns={columns}
        actions={actions}
        loading={loading}
        searchable={false}
        pagination
        pageSize={pageSize}
        pageSizeOptions={[5, 10, 25, 50]}
        onPageSizeChange={(s) => {
          setPageSize(s);
          setCurrentPage(1);
        }}
        emptyMessage={emptyMessage}
        serverSide
        totalRecords={totalRecords}
        totalPages={totalPages}
        currentPage={currentPage}
        onPageChange={setCurrentPage}
        searchTerm={searchTerm}
        onSearchChange={(v) => {
          setSearchTerm(v);
          setCurrentPage(1);
        }}
        sortColumn={sortColumn}
        sortDirection={sortDirection}
        onSortChange={(col, dir) => {
          setSortColumn(col);
          setSortDirection(dir);
          setCurrentPage(1);
        }}
      />
    </div>
  );
};

export default ExpensePage;
