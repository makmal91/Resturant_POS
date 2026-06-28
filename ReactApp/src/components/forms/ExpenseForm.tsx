import React, { useEffect, useMemo, useState } from 'react';
import { FormButton, FormInput, FormSelect, FormTextarea, SearchableSelect } from './index';
import { useBusinessCurrency } from '../../hooks/useBusinessCurrency';
import { useFormBranchId } from '../../hooks/useFormBranchId';
import { masterDataService } from '../../services/masterDataService';
import { safeString } from '../../utils/safeValues';
import type { ExpensePaymentMethod } from '../../modules/expense/expenseService';

export interface ExpenseFormData {
  expenseCategoryId: number;
  categoryName: string;
  description: string;
  amount: string;
  paymentMethod: ExpensePaymentMethod;
  expenseDate: string;
  referenceNo: string;
  notes: string;
  branchId: number;
}

interface ExpenseFormProps {
  initialData?: Partial<ExpenseFormData & { id?: number }> | null;
  onSubmit: (data: ExpenseFormData) => void;
  isLoading?: boolean;
  submitLabel?: string;
}

const PAYMENT_METHODS: ExpensePaymentMethod[] = ['Cash', 'Bank', 'Wallet'];

const buildDefaultFormData = (source?: Partial<ExpenseFormData> | null): ExpenseFormData => ({
  expenseCategoryId: Number(source?.expenseCategoryId ?? 0),
  categoryName: safeString(source?.categoryName),
  description: safeString(source?.description),
  amount: source?.amount != null ? String(source.amount) : '',
  paymentMethod: (source?.paymentMethod ?? 'Cash') as ExpensePaymentMethod,
  expenseDate: safeString(source?.expenseDate) || new Date().toISOString().slice(0, 10),
  referenceNo: safeString(source?.referenceNo),
  notes: safeString(source?.notes),
  branchId: Number(source?.branchId ?? 0),
});

const ExpenseForm: React.FC<ExpenseFormProps> = ({
  initialData,
  onSubmit,
  isLoading = false,
  submitLabel = 'Add Expense',
}) => {
  const { branchId: resolvedBranchId, branchError } = useFormBranchId(initialData?.branchId);
  const { symbol, currencyCode, loading: currencyLoading } = useBusinessCurrency();

  const safeInitialData = useMemo(() => {
    const base = initialData ?? {};
    if (resolvedBranchId > 0) {
      return { ...base, branchId: resolvedBranchId };
    }
    return base;
  }, [initialData, resolvedBranchId]);

  const [formData, setFormData] = useState<ExpenseFormData>(() => buildDefaultFormData(safeInitialData));
  const [errors, setErrors] = useState<Partial<Record<keyof ExpenseFormData, string>>>({});
  const [categories, setCategories] = useState<{ label: string; value: number }[]>([]);
  const [categoriesLoading, setCategoriesLoading] = useState(false);

  useEffect(() => {
    setFormData(buildDefaultFormData(safeInitialData));
    setErrors({});
  }, [safeInitialData]);

  useEffect(() => {
    if (resolvedBranchId > 0) {
      setFormData((prev) =>
        prev.branchId === resolvedBranchId ? prev : { ...prev, branchId: resolvedBranchId },
      );
    }
  }, [resolvedBranchId]);

  useEffect(() => {
    if (resolvedBranchId <= 0) {
      setCategories([]);
      return;
    }

    let cancelled = false;
    const load = async () => {
      setCategoriesLoading(true);
      try {
        const rows = await masterDataService.getExpenseCategories(resolvedBranchId);
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
    return () => {
      cancelled = true;
    };
  }, [resolvedBranchId]);

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>,
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    setErrors((prev) => ({ ...prev, [name]: '' }));
  };

  const handleCategoryChange = (_name: string, value: string | number) => {
    const expenseCategoryId = Number(value);
    const selected = categories.find((c) => c.value === expenseCategoryId);
    setFormData((prev) => ({
      ...prev,
      expenseCategoryId,
      categoryName: selected?.label ?? '',
    }));
    setErrors((prev) => ({ ...prev, expenseCategoryId: '' }));
  };

  const validateForm = () => {
    const nextErrors: Partial<Record<keyof ExpenseFormData, string>> = {};
    const parsed = parseFloat(formData.amount);

    if (formData.expenseCategoryId <= 0) {
      nextErrors.expenseCategoryId = 'Category is required';
    }

    if (!formData.description.trim()) {
      nextErrors.description = 'Description is required';
    }

    if (!formData.amount.trim()) {
      nextErrors.amount = 'Amount is required';
    } else if (isNaN(parsed) || parsed <= 0) {
      nextErrors.amount = 'Amount must be greater than zero';
    }

    if (resolvedBranchId <= 0) {
      nextErrors.branchId = branchError ?? 'Branch is required';
    }

    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const handleReset = () => {
    setFormData(buildDefaultFormData(safeInitialData));
    setErrors({});
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validateForm()) {
      onSubmit({ ...formData, branchId: resolvedBranchId });
    }
  };

  return (
    <form onSubmit={handleSubmit} className="flex h-full min-h-0 flex-col">
      <div className="flex-1 overflow-y-auto min-h-0 px-6 py-4">
        <p className="text-sm text-gray-600 mb-6">
          Record a branch expense. Currency is taken from your business settings.
        </p>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {branchError && (
            <p className="md:col-span-2 text-sm text-red-600">{branchError}</p>
          )}

          <div className="md:col-span-2">
            <SearchableSelect
              label="Category"
              name="expenseCategoryId"
              value={formData.expenseCategoryId || ''}
              onChange={handleCategoryChange}
              placeholder={categoriesLoading ? 'Loading categories…' : 'Select category'}
              options={categories}
              required
              loading={categoriesLoading}
              disabled={categoriesLoading || isLoading}
              error={errors.expenseCategoryId}
            />
          </div>

          <div className="md:col-span-2">
            <FormInput
              label="Description"
              name="description"
              value={formData.description}
              onChange={handleChange}
              placeholder="Brief description of the expense"
              required
              error={errors.description}
            />
          </div>

          <FormInput
            label="Currency"
            name="currencyCode"
            value={currencyLoading ? 'Loading…' : currencyCode}
            onChange={() => undefined}
            disabled
          />

          <FormInput
            label="Date"
            name="expenseDate"
            type="date"
            value={formData.expenseDate}
            onChange={handleChange}
            required
          />

          <div className="md:col-span-2">
            <label htmlFor="amount" className="block text-sm font-medium text-gray-800 mb-2">
              Amount ({currencyCode}) <span className="text-red-500 ml-1">*</span>
            </label>
            <div className="relative">
              <span className="absolute left-4 top-1/2 -translate-y-1/2 text-gray-500 font-medium">
                {symbol}
              </span>
              <input
                id="amount"
                name="amount"
                type="number"
                min="0.01"
                step="0.01"
                value={formData.amount}
                onChange={handleChange}
                placeholder="0.00"
                disabled={currencyLoading || isLoading}
                className={`w-full pl-10 pr-4 py-3 border rounded-lg shadow-sm placeholder-gray-400 focus:outline-none focus:ring-2 transition-colors duration-200 ${
                  errors.amount
                    ? 'border-red-300 focus:ring-red-500 focus:border-red-500'
                    : 'border-gray-300 focus:ring-blue-500 focus:border-blue-500'
                } ${currencyLoading || isLoading ? 'bg-gray-50 cursor-not-allowed text-gray-500' : 'bg-white'}`}
              />
            </div>
            {errors.amount && (
              <p className="mt-1 text-sm text-red-600 flex items-center">
                <svg className="w-4 h-4 mr-1" fill="currentColor" viewBox="0 0 20 20">
                  <path
                    fillRule="evenodd"
                    d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z"
                    clipRule="evenodd"
                  />
                </svg>
                {errors.amount}
              </p>
            )}
          </div>

          <FormSelect
            label="Payment Method"
            name="paymentMethod"
            value={formData.paymentMethod}
            onChange={handleChange}
            options={PAYMENT_METHODS.map((m) => ({ label: m, value: m }))}
            required
          />

          <FormInput
            label="Reference No."
            name="referenceNo"
            value={formData.referenceNo}
            onChange={handleChange}
            placeholder="Optional reference number"
          />

          <div className="md:col-span-2">
            <FormTextarea
              label="Notes"
              name="notes"
              value={formData.notes}
              onChange={handleChange}
              placeholder="Any additional notes…"
              rows={3}
            />
          </div>
        </div>
      </div>

      <div className="shrink-0 border-t border-gray-200 bg-white px-6 py-4 flex justify-end gap-3">
        <FormButton type="button" label="Reset" variant="secondary" onClick={handleReset} disabled={isLoading} />
        <FormButton type="submit" label={submitLabel} loading={isLoading} variant="primary" />
      </div>
    </form>
  );
};

export default ExpenseForm;
