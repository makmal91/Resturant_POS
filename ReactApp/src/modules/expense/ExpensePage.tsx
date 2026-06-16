import React, { useCallback, useEffect, useMemo, useState } from 'react';
import DataTable, { type Action, type Column } from '../../components/DataTable';
import Badge from '../../components/Badge';
import SearchableSelect from '../../components/forms/SearchableSelect';
import PermissionGate from '../../components/PermissionGate';
import { useConfirmDialog } from '../../contexts/ConfirmDialogContext';
import { usePermission } from '../../hooks/usePermission';
import { useBranchWriteAccess } from '../../hooks/useBranchWriteAccess';
import { useBusinessCurrency } from '../../hooks/useBusinessCurrency';
import { hasBranchContext } from '../../types/permissions';
import { getApiErrorMessage } from '../../services/api';
import { masterDataService } from '../../services/masterDataService';
import { safeString } from '../../utils/safeValues';
import { expenseService, type ExpenseDto, type ExpensePaymentMethod } from './expenseService';

// ─── Helpers ──────────────────────────────────────────────────────────────────

const formatDate = (value: string) => {
  if (!value) return '—';
  const d = new Date(value);
  return isNaN(d.getTime())
    ? '—'
    : d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
};

const PAYMENT_METHODS: ExpensePaymentMethod[] = ['Cash', 'Bank', 'Wallet'];

const METHOD_VARIANT: Record<ExpensePaymentMethod, 'success' | 'info' | 'warning'> = {
  Cash:   'success',
  Bank:   'info',
  Wallet: 'warning',
};

// ─── Form state type ──────────────────────────────────────────────────────────

interface ExpenseFormData {
  id?: number;
  expenseCategoryId: number;
  categoryName: string;
  description: string;
  amount: string;
  paymentMethod: ExpensePaymentMethod;
  expenseDate: string;
  referenceNo: string;
  notes: string;
}

const emptyForm = (): ExpenseFormData => ({
  expenseCategoryId: 0,
  categoryName: '',
  description: '',
  amount: '',
  paymentMethod: 'Cash',
  expenseDate: new Date().toISOString().slice(0, 10),
  referenceNo: '',
  notes: '',
});

// ─── Slide-in Form Panel ──────────────────────────────────────────────────────

interface ExpenseFormPanelProps {
  open: boolean;
  formData: ExpenseFormData;
  isEdit: boolean;
  submitting: boolean;
  error: string | null;
  branchId: number;
  currencySymbol: string;
  onChange: (data: ExpenseFormData) => void;
  onSubmit: (e: React.FormEvent) => void;
  onClose: () => void;
}

const ExpenseFormPanel: React.FC<ExpenseFormPanelProps> = ({
  open, formData, isEdit, submitting, error, branchId, currencySymbol, onChange, onSubmit, onClose,
}) => {
  const [categories, setCategories] = useState<{ label: string; value: number }[]>([]);
  const [categoriesLoading, setCategoriesLoading] = useState(false);

  useEffect(() => {
    if (!open || branchId <= 0) return;
    let cancelled = false;
    const load = async () => {
      setCategoriesLoading(true);
      try {
        const rows = await masterDataService.getExpenseCategories(branchId);
        if (!cancelled) {
          setCategories(rows.map((c) => ({ label: c.name, value: c.id })));
        }
      } catch {
        if (!cancelled) setCategories([]);
      } finally {
        if (!cancelled) setCategoriesLoading(false);
      }
    };
    void load();
    return () => { cancelled = true; };
  }, [open, branchId]);

  const set = (field: keyof ExpenseFormData) =>
    (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) =>
      onChange({ ...formData, [field]: e.target.value });

  const handleCategoryChange = (_name: string, value: string | number) => {
    const expenseCategoryId = Number(value);
    const selected = categories.find((c) => c.value === expenseCategoryId);
    onChange({
      ...formData,
      expenseCategoryId,
      categoryName: selected?.label ?? '',
    });
  };

  return (
    <>
      {/* Backdrop */}
      {open && (
        <div
          className="fixed inset-0 z-40 bg-black/30 backdrop-blur-[1px]"
          onClick={onClose}
        />
      )}

      {/* Slide panel */}
      <div
        className={`fixed right-0 top-0 z-50 h-full w-full max-w-md transform bg-white shadow-2xl transition-transform duration-300 ease-in-out flex flex-col ${
          open ? 'translate-x-0' : 'translate-x-full'
        }`}
      >
        {/* Panel header */}
        <div className="flex items-center justify-between border-b border-gray-200 bg-gray-50 px-6 py-4">
          <div>
            <h2 className="text-lg font-semibold text-gray-900">
              {isEdit ? 'Edit Expense' : 'Add Expense'}
            </h2>
            <p className="text-xs text-gray-500 mt-0.5">
              {isEdit ? 'Update expense details' : 'Record a new expense for this branch'}
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="flex h-8 w-8 items-center justify-center rounded-full text-gray-400 transition-colors hover:bg-gray-200 hover:text-gray-600"
          >
            <svg className="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Form body */}
        <form onSubmit={onSubmit} className="flex flex-1 flex-col overflow-hidden">
          <div className="flex-1 overflow-y-auto px-6 py-5 space-y-5">

            {/* Category */}
            <SearchableSelect
              label="Category"
              name="expenseCategoryId"
              value={formData.expenseCategoryId || ''}
              onChange={handleCategoryChange}
              placeholder={categoriesLoading ? 'Loading categories…' : 'Select category'}
              options={categories}
              required
              loading={categoriesLoading}
              disabled={categoriesLoading}
            />

            {/* Description */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5">
                Description <span className="text-red-500">*</span>
              </label>
              <input
                type="text"
                value={formData.description}
                onChange={set('description')}
                placeholder="Brief description of the expense"
                required
                className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm shadow-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
              />
            </div>

            {/* Amount + Date */}
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1.5">
                  Amount <span className="text-red-500">*</span>
                </label>
                <div className="relative">
                  <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 text-sm font-medium">{currencySymbol}</span>
                  <input
                    type="number"
                    min="0.01"
                    step="0.01"
                    value={formData.amount}
                    onChange={set('amount')}
                    placeholder="0.00"
                    required
                    className="w-full rounded-lg border border-gray-300 pl-7 pr-3 py-2.5 text-sm shadow-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
                  />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1.5">
                  Date <span className="text-red-500">*</span>
                </label>
                <input
                  type="date"
                  value={formData.expenseDate}
                  onChange={set('expenseDate')}
                  required
                  className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm shadow-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
                />
              </div>
            </div>

            {/* Payment Method */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5">Payment Method</label>
              <select
                value={formData.paymentMethod}
                onChange={set('paymentMethod')}
                className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm shadow-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
              >
                {PAYMENT_METHODS.map((m) => (
                  <option key={m} value={m}>{m}</option>
                ))}
              </select>
            </div>

            {/* Reference No */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5">Reference No.</label>
              <input
                type="text"
                value={formData.referenceNo}
                onChange={set('referenceNo')}
                placeholder="Optional reference number"
                className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm shadow-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
              />
            </div>

            {/* Notes */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5">Notes</label>
              <textarea
                value={formData.notes}
                onChange={set('notes')}
                rows={3}
                placeholder="Any additional notes…"
                className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm shadow-sm resize-none focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
              />
            </div>

            {error && (
              <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                {error}
              </div>
            )}
          </div>

          {/* Footer actions */}
          <div className="border-t border-gray-200 bg-gray-50 px-6 py-4 flex gap-3">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm font-medium text-gray-700 shadow-sm hover:bg-gray-50 transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={submitting}
              className="flex-1 rounded-lg border border-transparent bg-blue-600 px-4 py-2.5 text-sm font-medium text-white shadow-sm hover:bg-blue-700 disabled:opacity-60 transition-colors"
            >
              {submitting ? 'Saving…' : isEdit ? 'Update Expense' : 'Add Expense'}
            </button>
          </div>
        </form>
      </div>
    </>
  );
};

// ─── Main Page ────────────────────────────────────────────────────────────────

const ExpensePage: React.FC = () => {
  const { showConfirm } = useConfirmDialog();
  const { fmt, symbol: currencySymbol } = useBusinessCurrency();
  const { canCreate, canEdit, canDelete } = usePermission('Expenses');
  const {
    selectedBranchId,
    isMasterUser,
    isGlobalMode,
    canWriteInView,
    resolveEntityBranchId,
    getWriteBlockMessage,
  } = useBranchWriteAccess();

  const hasBranchSelection = hasBranchContext(selectedBranchId);
  const canAdd    = canWriteInView && (isMasterUser || canCreate);
  const canModify = canWriteInView && (isMasterUser || canEdit);
  const canRemove = canWriteInView && (isMasterUser || canDelete);

  // List state
  const [items, setItems]             = useState<ExpenseDto[]>([]);
  const [loading, setLoading]         = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize]       = useState(10);
  const [searchTerm, setSearchTerm]   = useState('');
  const [sortColumn, setSortColumn]   = useState<string>('expenseDate');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('desc');
  const [totalRecords, setTotalRecords]   = useState(0);
  const [totalPages, setTotalPages]       = useState(0);
  const [methodFilter, setMethodFilter]   = useState<ExpensePaymentMethod | ''>('');

  // Today's totals (summary row)
  const [todayTotal, setTodayTotal] = useState(0);
  const [todayCash, setTodayCash]   = useState(0);
  const [todayBank, setTodayBank]   = useState(0);

  const [notification, setNotification] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  // Panel / form state
  const [panelOpen, setPanelOpen]     = useState(false);
  const [formData, setFormData]       = useState<ExpenseFormData>(emptyForm());
  const [submitting, setSubmitting]   = useState(false);
  const [formError, setFormError]     = useState<string | null>(null);

  const isEdit = Boolean(formData.id);

  const showNotification = useCallback((type: 'success' | 'error', message: string) => {
    setNotification({ type, message });
    setTimeout(() => setNotification(null), 4000);
  }, []);

  // ─── Data fetching ─────────────────────────────────────────────────────────

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
      setItems(rows.map((r) => ({
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
      })));
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
    setCurrentPage(1);
  }, [selectedBranchId, methodFilter, pageSize]);

  // ─── Panel helpers ─────────────────────────────────────────────────────────

  const openAdd = () => {
    const blockMessage = getWriteBlockMessage();
    if (!canAdd || blockMessage) {
      showNotification('error', blockMessage ?? 'You do not have permission to add expenses.');
      return;
    }
    setFormData(emptyForm());
    setFormError(null);
    setPanelOpen(true);
  };

  const openEdit = (item: ExpenseDto) => {
    const blockMessage = getWriteBlockMessage();
    if (!canModify || blockMessage) {
      showNotification('error', blockMessage ?? 'You do not have permission to edit expenses.');
      return;
    }
    setFormData({
      id: item.id,
      expenseCategoryId: item.expenseCategoryId,
      categoryName: item.categoryName,
      description: item.description,
      amount: String(item.amount),
      paymentMethod: item.paymentMethod,
      expenseDate: item.expenseDate ? item.expenseDate.slice(0, 10) : new Date().toISOString().slice(0, 10),
      referenceNo: item.referenceNo ?? '',
      notes: item.notes ?? '',
    });
    setFormError(null);
    setPanelOpen(true);
  };

  const closePanel = () => {
    if (submitting) return;
    setPanelOpen(false);
  };

  // ─── Submit ────────────────────────────────────────────────────────────────

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const parsed = parseFloat(formData.amount);
    if (isNaN(parsed) || parsed <= 0) {
      setFormError('Amount must be greater than zero.');
      return;
    }
    if (formData.expenseCategoryId <= 0) {
      setFormError('Category is required.');
      return;
    }

    const branchId = selectedBranchId ?? 0;
    if (branchId <= 0) {
      setFormError('No branch selected.');
      return;
    }

    setSubmitting(true);
    setFormError(null);

    try {
      const payload = {
        branchId,
        expenseCategoryId: formData.expenseCategoryId,
        description: formData.description.trim(),
        amount: parsed,
        paymentMethod: formData.paymentMethod,
        expenseDate: formData.expenseDate,
        referenceNo: formData.referenceNo.trim() || undefined,
        notes: formData.notes.trim() || undefined,
      };

      if (isEdit && formData.id) {
        await expenseService.update(formData.id, payload);
        showNotification('success', 'Expense updated successfully.');
      } else {
        await expenseService.create(payload);
        showNotification('success', 'Expense recorded successfully.');
      }
      setPanelOpen(false);
      void fetchExpenses();
    } catch (err) {
      setFormError(getApiErrorMessage(err, 'Failed to save expense.'));
    } finally {
      setSubmitting(false);
    }
  };

  // ─── Delete ────────────────────────────────────────────────────────────────

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

  // ─── Table columns ─────────────────────────────────────────────────────────

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
      }
    );

    return base;
  }, [isGlobalMode]);

  // ─── Table actions ─────────────────────────────────────────────────────────

  const actions: Action<ExpenseDto>[] = [];
  if (canModify) {
    actions.push({
      label: '',
      onClick: openEdit,
      icon: (
        <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" title="Edit">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
            d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
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
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
            d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
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

  // ─── Render ────────────────────────────────────────────────────────────────

  return (
    <div>
      {/* Slide-in form panel */}
      <ExpenseFormPanel
        open={panelOpen}
        formData={formData}
        isEdit={isEdit}
        submitting={submitting}
        error={formError}
        branchId={selectedBranchId ?? 0}
        currencySymbol={currencySymbol}
        onChange={setFormData}
        onSubmit={handleSubmit}
        onClose={closePanel}
      />

      {/* Notification */}
      {notification && (
        <div
          className={`mb-6 flex items-center rounded-md p-4 ${
            notification.type === 'success' ? 'bg-green-50 text-green-800' : 'bg-red-50 text-red-800'
          }`}
        >
          {notification.type === 'success' ? (
            <svg className="mr-3 h-5 w-5" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
            </svg>
          ) : (
            <svg className="mr-3 h-5 w-5" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
            </svg>
          )}
          <span className="font-medium">{notification.message}</span>
        </div>
      )}

      {/* Page header */}
      <div className="mb-8">
        <h1 className="mb-2 text-3xl font-bold text-gray-900">Expenses</h1>
        <p className="text-gray-600">Track and manage all branch expenditures</p>
      </div>

      {/* Global mode banner */}
      {isGlobalMode && isMasterUser && (
        <div className="mb-6 rounded-md border border-blue-200 bg-blue-50 px-4 py-3 text-sm text-blue-800">
          Global view is active. Select a target branch when recording expenses.
        </div>
      )}

      {/* No branch selected */}
      {!hasBranchSelection && (
        <div className="mb-6 rounded-md border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-700">
          Select a branch from the header to load expenses.
        </div>
      )}

      {/* Summary strip */}
      {hasBranchSelection && !loading && (
        <div className="mb-6 grid grid-cols-3 gap-4">
          {[
            { label: 'Total Expenses', value: todayTotal, color: 'text-red-600 bg-red-50 border-red-100' },
            { label: 'Cash Paid',      value: todayCash,  color: 'text-emerald-700 bg-emerald-50 border-emerald-100' },
            { label: 'Bank Paid',      value: todayBank,  color: 'text-blue-700 bg-blue-50 border-blue-100' },
          ].map(({ label, value, color }) => (
            <div key={label} className={`rounded-lg border p-4 text-center ${color}`}>
              <p className="text-xs font-medium uppercase tracking-wide opacity-70">{label}</p>
              <p className="mt-1 text-xl font-bold">{fmt(value)}</p>
            </div>
          ))}
        </div>
      )}

      {/* Toolbar */}
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
            {PAYMENT_METHODS.map((m) => <option key={m} value={m}>{m}</option>)}
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

      {/* DataTable */}
      <DataTable
        data={items}
        columns={columns}
        actions={actions}
        loading={loading}
        searchable={false}
        pagination
        pageSize={pageSize}
        pageSizeOptions={[5, 10, 25, 50]}
        onPageSizeChange={(s) => { setPageSize(s); setCurrentPage(1); }}
        emptyMessage={emptyMessage}
        serverSide
        totalRecords={totalRecords}
        totalPages={totalPages}
        currentPage={currentPage}
        onPageChange={setCurrentPage}
        searchTerm={searchTerm}
        onSearchChange={(v) => { setSearchTerm(v); setCurrentPage(1); }}
        sortColumn={sortColumn}
        sortDirection={sortDirection}
        onSortChange={(col, dir) => { setSortColumn(col); setSortDirection(dir); setCurrentPage(1); }}
      />
    </div>
  );
};

export default ExpensePage;
