import React, { useEffect, useState } from 'react';
import CodeFieldWithGenerate from './CodeFieldWithGenerate';
import { CODE_MODULES } from '../../services/codeGeneratorService';
import { FormButton, FormInput, FormSelect, FormTextarea } from './index';
import { useBusinessCurrency } from '../../hooks/useBusinessCurrency';
import { useFormBranchId } from '../../hooks/useFormBranchId';
import type { CashFlowPaymentMethod, CashFlowTransactionType } from '../../modules/cashflow/cashFlowService';

export interface CashTransactionFormData {
  transactionType: CashFlowTransactionType;
  paymentMethod: CashFlowPaymentMethod;
  amount: string;
  voucherNo: string;
  description: string;
}

interface CashTransactionFormProps {
  initialData?: { transactionType?: CashFlowTransactionType; voucherNo?: string; id?: number } | null;
  onSubmit: (data: CashTransactionFormData) => void;
  isLoading?: boolean;
  submitLabel?: string;
}

const buildDefaultFormData = (initialType: CashFlowTransactionType, voucherNo = ''): CashTransactionFormData => ({
  transactionType: initialType,
  paymentMethod: 'Cash',
  amount: '',
  voucherNo,
  description: '',
});

const CashTransactionForm: React.FC<CashTransactionFormProps> = ({
  initialData,
  onSubmit,
  isLoading = false,
  submitLabel = 'Add Journal Voucher',
}) => {
  const initialType = initialData?.transactionType ?? 'CashIn';
  const isEditMode = Boolean(initialData?.id);
  const { branchId: resolvedBranchId, branchError } = useFormBranchId();
  const { symbol, currencyCode, loading: currencyLoading } = useBusinessCurrency();

  const [formData, setFormData] = useState<CashTransactionFormData>(() =>
    buildDefaultFormData(initialType, initialData?.voucherNo ?? ''),
  );
  const [errors, setErrors] = useState<Partial<Record<keyof CashTransactionFormData, string>>>({});

  useEffect(() => {
    setFormData(buildDefaultFormData(initialType, initialData?.voucherNo ?? ''));
    setErrors({});
  }, [initialType, initialData?.voucherNo]);

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>,
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    setErrors((prev) => ({ ...prev, [name]: '' }));
  };

  const validateForm = () => {
    const nextErrors: Partial<Record<keyof CashTransactionFormData, string>> = {};
    const parsed = parseFloat(formData.amount);

    if (!formData.amount.trim()) {
      nextErrors.amount = 'Amount is required';
    } else if (isNaN(parsed) || parsed <= 0) {
      nextErrors.amount = 'Amount must be greater than zero';
    }

    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0 && !branchError;
  };

  const handleReset = () => {
    setFormData(buildDefaultFormData(initialType));
    setErrors({});
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validateForm()) {
      onSubmit(formData);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="flex h-full min-h-0 flex-col">
      <div className="flex-1 overflow-y-auto min-h-0 px-6 py-4">
        <p className="text-sm text-gray-600 mb-6">
          Record a manual journal voucher (cash in or cash out) for the selected branch.
        </p>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {branchError && (
            <p className="md:col-span-2 text-sm text-red-600">{branchError}</p>
          )}

          <CodeFieldWithGenerate
            label="Voucher No"
            name="voucherNo"
            value={formData.voucherNo}
            onChange={(value) => setFormData((prev) => ({ ...prev, voucherNo: value }))}
            module={CODE_MODULES.JournalVoucher}
            branchId={resolvedBranchId}
            isEditMode={isEditMode}
            required
          />

          <FormInput
            label="Currency"
            name="currencyCode"
            value={currencyLoading ? 'Loading…' : currencyCode}
            onChange={() => undefined}
            disabled
          />

          <FormSelect
            label="Voucher Type"
            name="transactionType"
            value={formData.transactionType}
            onChange={handleChange}
            options={[
              { label: 'Cash In', value: 'CashIn' },
              { label: 'Cash Out', value: 'CashOut' },
            ]}
            required
          />

          <FormSelect
            label="Payment Method"
            name="paymentMethod"
            value={formData.paymentMethod}
            onChange={handleChange}
            options={[
              { label: 'Cash', value: 'Cash' },
              { label: 'Bank', value: 'Bank' },
              { label: 'Wallet', value: 'Wallet' },
            ]}
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

          <div className="md:col-span-2">
            <FormTextarea
              label="Description"
              name="description"
              value={formData.description}
              onChange={handleChange}
              placeholder="Reason for this journal voucher…"
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

export default CashTransactionForm;
