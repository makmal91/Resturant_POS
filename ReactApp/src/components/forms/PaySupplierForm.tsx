import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { FormButton, FormInput, FormSelect, FormTextarea } from './index';
import CodeFieldWithGenerate from './CodeFieldWithGenerate';
import { CODE_MODULES } from '../../services/codeGeneratorService';
import PaymentAllocationGrid, {
  buildEditAllocationRows,
  buildInitialAllocationRows,
  type AllocationMode,
  type InvoiceAllocationRow,
} from './PaymentAllocationGrid';
import { useBusinessCurrency } from '../../hooks/useBusinessCurrency';
import { useFormBranchId } from '../../hooks/useFormBranchId';
import {
  partyLedgerService,
  type OutstandingInvoiceOption,
} from '../../modules/ledger/partyLedgerService';
import type { PartyPaymentType, PaymentAllocationInput, InvoicePaymentCategory } from './ReceivePaymentForm';

export interface PaySupplierFormData {
  supplierId: string;
  paymentMethod: PartyPaymentType;
  paymentCategory: InvoicePaymentCategory;
  amount: string;
  paymentDate: string;
  referenceNo: string;
  notes: string;
  allocationMode: AllocationMode;
  autoAllocate: boolean;
  allocations: PaymentAllocationInput[];
}

interface LookupOption {
  id: number;
  name: string;
}

interface PaySupplierFormInitialData {
  id?: number;
  supplierId?: number;
  paymentMethod?: PartyPaymentType;
  paymentCategory?: InvoicePaymentCategory;
  amount?: string | number;
  paymentDate?: string;
  referenceNo?: string;
  notes?: string;
  allocationMode?: AllocationMode;
  allocations?: PaymentAllocationInput[];
}

interface PaySupplierFormProps {
  suppliers?: LookupOption[];
  initialData?: PaySupplierFormInitialData | null;
  branchId: number;
  onSubmit: (data: PaySupplierFormData) => void;
  isLoading?: boolean;
  submitLabel?: string;
}

const PAYMENT_METHOD_OPTIONS = [
  { label: 'Cash', value: 'Cash' },
  { label: 'Bank', value: 'Bank' },
  { label: 'Online', value: 'Online' },
];

const PAYMENT_CATEGORY_OPTIONS = [
  { label: 'Against Invoice', value: 'AgainstInvoice' },
  { label: 'Advance', value: 'Advance' },
  { label: 'Adjustment', value: 'Adjustment' },
];

const buildFormDataFromInitial = (initial?: PaySupplierFormInitialData | null): PaySupplierFormData => ({
  supplierId: initial?.supplierId ? String(initial.supplierId) : '',
  paymentMethod: initial?.paymentMethod ?? 'Cash',
  paymentCategory: initial?.paymentCategory ?? 'AgainstInvoice',
  amount: initial?.amount != null && initial.amount !== '' ? String(initial.amount) : '',
  paymentDate: initial?.paymentDate
    ? initial.paymentDate.slice(0, 10)
    : new Date().toISOString().slice(0, 10),
  referenceNo: initial?.referenceNo ?? '',
  notes: initial?.notes ?? '',
  allocationMode: initial?.allocationMode ?? (initial?.id ? 'manual' : 'auto'),
  autoAllocate: !initial?.id,
  allocations: initial?.allocations ?? [],
});

const buildDefaultFormData = (supplierId = ''): PaySupplierFormData =>
  buildFormDataFromInitial(supplierId ? { supplierId: Number(supplierId) } : null);

const PaySupplierForm: React.FC<PaySupplierFormProps> = ({
  suppliers = [],
  initialData,
  branchId,
  onSubmit,
  isLoading = false,
  submitLabel = 'Pay Supplier',
}) => {
  const { branchError } = useFormBranchId();
  const { symbol, currencyCode, fmt, loading: currencyLoading } = useBusinessCurrency();

  const [formData, setFormData] = useState<PaySupplierFormData>(() =>
    buildFormDataFromInitial(initialData),
  );
  const isEditMode = Boolean(initialData?.id);
  const [errors, setErrors] = useState<Partial<Record<string, string>>>({});
  const [balance, setBalance] = useState<number | null>(null);
  const [outstandingInvoices, setOutstandingInvoices] = useState<OutstandingInvoiceOption[]>([]);
  const [allocationRows, setAllocationRows] = useState<InvoiceAllocationRow[]>([]);
  const [loadingBalance, setLoadingBalance] = useState(false);
  const [loadingInvoices, setLoadingInvoices] = useState(false);

  useEffect(() => {
    setFormData(buildFormDataFromInitial(initialData));
    setErrors({});
    if (!initialData?.id) setAllocationRows([]);
  }, [initialData]);

  const loadOutstandingInvoices = useCallback(async (supplierId: number, excludePaymentId?: number) => {
    if (supplierId <= 0 || branchId <= 0) {
      setOutstandingInvoices([]);
      if (!excludePaymentId) setAllocationRows([]);
      return;
    }
    setLoadingInvoices(true);
    try {
      const res = await partyLedgerService.getSupplierOutstandingInvoices(
        branchId,
        supplierId,
        excludePaymentId,
      );
      setOutstandingInvoices(res.data);
    } catch {
      setOutstandingInvoices([]);
    } finally {
      setLoadingInvoices(false);
    }
  }, [branchId]);

  const loadBalance = useCallback(async (supplierId: number) => {
    if (supplierId <= 0 || branchId <= 0) {
      setBalance(null);
      return;
    }
    setLoadingBalance(true);
    try {
      const res = await partyLedgerService.getSupplierBalance(branchId, supplierId);
      setBalance(res.data.balance);
    } catch {
      setBalance(null);
    } finally {
      setLoadingBalance(false);
    }
  }, [branchId]);

  useEffect(() => {
    const id = Number(formData.supplierId);
    void loadBalance(id);
    void loadOutstandingInvoices(id, initialData?.id);
  }, [formData.supplierId, loadBalance, loadOutstandingInvoices, initialData?.id]);

  useEffect(() => {
    if (formData.paymentCategory === 'Advance') {
      setAllocationRows([]);
      return;
    }

    if (isEditMode && initialData?.allocations?.length) {
      setAllocationRows(buildEditAllocationRows(outstandingInvoices, initialData.allocations));
      return;
    }

    setAllocationRows(buildInitialAllocationRows(outstandingInvoices, formData.allocationMode, formData.amount));
  }, [
    formData.amount,
    formData.allocationMode,
    formData.paymentCategory,
    outstandingInvoices,
    isEditMode,
    initialData?.allocations,
  ]);

  const showInvoiceAllocation = formData.paymentCategory !== 'Advance';

  const totalApplied = useMemo(
    () =>
      allocationRows.reduce((sum, row) => {
        if (formData.allocationMode === 'manual' && !row.selected) return sum;
        const val = parseFloat(row.applyAmount);
        return sum + (isNaN(val) ? 0 : val);
      }, 0),
    [allocationRows, formData.allocationMode],
  );

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>,
  ) => {
    const { name, value } = e.target;

    if (name === 'supplierId') {
      if (isEditMode) return;
      setFormData((prev) => ({
        ...prev,
        supplierId: value,
        amount: '',
      }));
      setErrors((prev) => ({ ...prev, supplierId: '', amount: '' }));
      return;
    }

    setFormData((prev) => ({ ...prev, [name]: value }));
    setErrors((prev) => ({ ...prev, [name]: '' }));
  };

  const handleModeChange = (mode: AllocationMode) => {
    setFormData((prev) => ({
      ...prev,
      allocationMode: mode,
      autoAllocate: mode === 'auto',
    }));
  };

  const validateForm = () => {
    const nextErrors: Partial<Record<string, string>> = {};
    const supplierId = Number(formData.supplierId);
    const parsed = parseFloat(formData.amount);

    if (!supplierId) nextErrors.supplierId = 'Supplier is required';
    if (!formData.amount.trim()) nextErrors.amount = 'Amount is required';
    else if (isNaN(parsed) || parsed <= 0) nextErrors.amount = 'Amount must be greater than zero';
    else if (totalApplied > parsed + 0.005) {
      nextErrors.amount = 'Total applied amount exceeds payment amount';
    }

    if (formData.allocationMode === 'manual' && showInvoiceAllocation) {
      const invalidRow = allocationRows.find((row) => {
        if (!row.selected) return false;
        const val = parseFloat(row.applyAmount);
        return isNaN(val) || val <= 0 || val > row.balanceDue + 0.005;
      });
      if (invalidRow) {
        nextErrors.allocations = `Invalid apply amount for invoice ${invalidRow.invoiceNo}`;
      }
    }

    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0 && !branchError;
  };

  const buildAllocations = (): PaymentAllocationInput[] =>
    allocationRows
      .filter((row) => {
        if (formData.allocationMode === 'manual') return row.selected;
        const val = parseFloat(row.applyAmount);
        return !isNaN(val) && val > 0;
      })
      .map((row) => ({
        invoiceId: row.invoiceId,
        appliedAmount: parseFloat(row.applyAmount),
      }));

  const handleReset = () => {
    setFormData(buildDefaultFormData(initialData?.supplierId ? String(initialData.supplierId) : ''));
    setErrors({});
    setAllocationRows([]);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!validateForm()) return;

    onSubmit({
      ...formData,
      autoAllocate: formData.paymentCategory === 'AgainstInvoice' && formData.allocationMode === 'auto',
      allocations:
        showInvoiceAllocation && formData.allocationMode === 'manual' ? buildAllocations() : [],
    });
  };

  return (
    <form onSubmit={handleSubmit} className="flex h-full min-h-0 flex-col">
      <div className="flex-1 overflow-y-auto min-h-0 px-6 py-4">
        <p className="text-sm text-gray-600 mb-6">
          {isEditMode
            ? 'Update payment details. The original GL entry will be reversed and re-posted with the new amounts.'
            : 'Record a supplier payment with automatic FIFO allocation or manual invoice selection.'}
        </p>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {branchError && (
            <p className="md:col-span-2 text-sm text-red-600">{branchError}</p>
          )}

          <FormSelect
            label="Supplier"
            name="supplierId"
            value={formData.supplierId}
            onChange={handleChange}
            options={[
              { label: 'Select supplier', value: '' },
              ...suppliers.map((s) => ({ label: s.name, value: String(s.id) })),
            ]}
            required
            error={errors.supplierId}
            disabled={isEditMode || isLoading}
          />

          <FormSelect
            label="Payment Category"
            name="paymentCategory"
            value={formData.paymentCategory}
            onChange={handleChange}
            options={PAYMENT_CATEGORY_OPTIONS}
            required
          />

          <FormSelect
            label="Payment Method"
            name="paymentMethod"
            value={formData.paymentMethod}
            onChange={handleChange}
            options={PAYMENT_METHOD_OPTIONS}
            required
          />

          <CodeFieldWithGenerate
            label="Payment No"
            name="referenceNo"
            value={formData.referenceNo}
            onChange={(value) => setFormData((prev) => ({ ...prev, referenceNo: value }))}
            module={CODE_MODULES.SupplierPayment}
            branchId={branchId}
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

          {formData.supplierId && (
            <div className="md:col-span-2 rounded-lg bg-orange-50 border border-orange-100 px-4 py-3">
              <p className="text-xs font-medium text-orange-600 uppercase tracking-wide">Payable Balance</p>
              <p className="text-xl font-bold text-orange-900 mt-1">
                {loadingBalance ? 'Loading…' : fmt(balance ?? 0)}
              </p>
            </div>
          )}

          <div className="md:col-span-2">
            <label htmlFor="amount" className="block text-sm font-medium text-gray-800 mb-2">
              Payment Amount ({currencyCode}) <span className="text-red-500 ml-1">*</span>
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
                disabled={currencyLoading || isLoading || !formData.supplierId}
                className={`w-full pl-10 pr-4 py-3 border rounded-lg shadow-sm placeholder-gray-400 focus:outline-none focus:ring-2 transition-colors duration-200 ${
                  errors.amount
                    ? 'border-red-300 focus:ring-red-500 focus:border-red-500'
                    : 'border-gray-300 focus:ring-blue-500 focus:border-blue-500'
                } ${currencyLoading || isLoading ? 'bg-gray-50 cursor-not-allowed text-gray-500' : 'bg-white'}`}
              />
            </div>
            {errors.amount && <p className="mt-1 text-sm text-red-600">{errors.amount}</p>}
          </div>

          <FormInput
            label="Payment Date"
            name="paymentDate"
            type="date"
            value={formData.paymentDate}
            onChange={handleChange}
            required
          />

          {formData.supplierId && showInvoiceAllocation && (
            <PaymentAllocationGrid
              invoices={outstandingInvoices}
              mode={formData.allocationMode}
              paymentAmount={formData.amount}
              loading={loadingInvoices}
              fmt={fmt}
              rows={allocationRows}
              onRowsChange={setAllocationRows}
              onModeChange={handleModeChange}
              accent="orange"
            />
          )}

          {errors.allocations && (
            <p className="md:col-span-2 text-sm text-red-600">{errors.allocations}</p>
          )}

          <div className="md:col-span-2">
            <FormTextarea
              label="Notes"
              name="notes"
              value={formData.notes}
              onChange={handleChange}
              placeholder="Optional payment notes…"
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

export default PaySupplierForm;
