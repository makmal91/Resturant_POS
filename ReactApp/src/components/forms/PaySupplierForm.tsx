import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { FormButton, FormInput, FormSelect, FormTextarea } from './index';
import { useBusinessCurrency } from '../../hooks/useBusinessCurrency';
import { useFormBranchId } from '../../hooks/useFormBranchId';
import {
  partyLedgerService,
  type OutstandingInvoiceOption,
} from '../../modules/ledger/partyLedgerService';
import type { PartyPaymentType } from './ReceivePaymentForm';

export interface PaySupplierFormData {
  supplierId: string;
  purchaseId: string;
  paymentType: PartyPaymentType;
  amount: string;
  paymentDate: string;
  referenceNo: string;
  notes: string;
}

interface LookupOption {
  id: number;
  name: string;
}

interface PaySupplierFormProps {
  suppliers?: LookupOption[];
  initialData?: { supplierId?: number; purchaseId?: number } | null;
  branchId: number;
  onSubmit: (data: PaySupplierFormData) => void;
  isLoading?: boolean;
  submitLabel?: string;
}

const PAYMENT_TYPE_OPTIONS = [
  { label: 'Cash', value: 'Cash' },
  { label: 'Bank', value: 'Bank' },
  { label: 'Online', value: 'Online' },
];

const buildDefaultFormData = (
  supplierId = '',
  purchaseId = '',
): PaySupplierFormData => ({
  supplierId: supplierId ? String(supplierId) : '',
  purchaseId: purchaseId ? String(purchaseId) : '',
  paymentType: 'Cash',
  amount: '',
  paymentDate: new Date().toISOString().slice(0, 10),
  referenceNo: '',
  notes: '',
});

const formatInvoiceLabel = (
  invoice: OutstandingInvoiceOption,
  fmt: (value: number) => string,
) => `${invoice.invoiceNo} — Due ${fmt(invoice.balanceDue)}`;

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
    buildDefaultFormData(initialData?.supplierId, initialData?.purchaseId ? String(initialData.purchaseId) : '')
  );
  const [errors, setErrors] = useState<Partial<Record<keyof PaySupplierFormData, string>>>({});
  const [balance, setBalance] = useState<number | null>(null);
  const [outstandingInvoices, setOutstandingInvoices] = useState<OutstandingInvoiceOption[]>([]);
  const [loadingBalance, setLoadingBalance] = useState(false);
  const [loadingInvoices, setLoadingInvoices] = useState(false);

  useEffect(() => {
    setFormData(buildDefaultFormData(initialData?.supplierId, initialData?.purchaseId ? String(initialData.purchaseId) : ''));
    setErrors({});
  }, [initialData?.supplierId, initialData?.purchaseId]);

  const selectedInvoice = useMemo(
    () => outstandingInvoices.find((inv) => String(inv.invoiceId) === formData.purchaseId),
    [outstandingInvoices, formData.purchaseId],
  );

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

  const loadOutstandingInvoices = useCallback(async (supplierId: number) => {
    if (supplierId <= 0 || branchId <= 0) {
      setOutstandingInvoices([]);
      return;
    }
    setLoadingInvoices(true);
    try {
      const res = await partyLedgerService.getSupplierOutstandingInvoices(branchId, supplierId);
      setOutstandingInvoices(res.data);
    } catch {
      setOutstandingInvoices([]);
    } finally {
      setLoadingInvoices(false);
    }
  }, [branchId]);

  useEffect(() => {
    const id = Number(formData.supplierId);
    void loadBalance(id);
    void loadOutstandingInvoices(id);
  }, [formData.supplierId, loadBalance, loadOutstandingInvoices]);

  const invoiceOptions = useMemo(() => {
    const base = [{ label: 'Advance payment (no invoice)', value: '' }];
    return [
      ...base,
      ...outstandingInvoices.map((inv) => ({
        label: formatInvoiceLabel(inv, fmt),
        value: String(inv.invoiceId),
      })),
    ];
  }, [outstandingInvoices, fmt]);

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>,
  ) => {
    const { name, value } = e.target;

    if (name === 'supplierId') {
      setFormData((prev) => ({
        ...prev,
        supplierId: value,
        purchaseId: '',
        amount: '',
      }));
      setErrors((prev) => ({ ...prev, supplierId: '', purchaseId: '', amount: '' }));
      return;
    }

    if (name === 'purchaseId') {
      const invoice = outstandingInvoices.find((inv) => String(inv.invoiceId) === value);
      setFormData((prev) => ({
        ...prev,
        purchaseId: value,
        amount: invoice ? String(invoice.balanceDue) : prev.amount,
      }));
      setErrors((prev) => ({ ...prev, purchaseId: '', amount: '' }));
      return;
    }

    setFormData((prev) => ({ ...prev, [name]: value }));
    setErrors((prev) => ({ ...prev, [name]: '' }));
  };

  const validateForm = () => {
    const nextErrors: Partial<Record<keyof PaySupplierFormData, string>> = {};
    const supplierId = Number(formData.supplierId);
    const purchaseId = formData.purchaseId.trim() ? Number(formData.purchaseId) : 0;
    const parsed = parseFloat(formData.amount);

    if (!supplierId) nextErrors.supplierId = 'Supplier is required';
    if (formData.purchaseId.trim() && (!purchaseId || purchaseId <= 0)) {
      nextErrors.purchaseId = 'Select a valid invoice';
    }
    if (!formData.amount.trim()) nextErrors.amount = 'Amount is required';
    else if (isNaN(parsed) || parsed <= 0) nextErrors.amount = 'Amount must be greater than zero';
    else if (selectedInvoice && parsed > selectedInvoice.balanceDue) {
      nextErrors.amount = `Amount exceeds invoice balance due of ${fmt(selectedInvoice.balanceDue)}`;
    }

    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0 && !branchError;
  };

  const handleReset = () => {
    setFormData(buildDefaultFormData(initialData?.supplierId, initialData?.purchaseId ? String(initialData.purchaseId) : ''));
    setErrors({});
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validateForm()) onSubmit(formData);
  };

  const isAdvance = !formData.purchaseId.trim();

  return (
    <form onSubmit={handleSubmit} className="flex h-full min-h-0 flex-col">
      <div className="flex-1 overflow-y-auto min-h-0 px-6 py-4">
        <p className="text-sm text-gray-600 mb-6">
          Record a supplier payment against a purchase invoice or as an advance payment.
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
          />

          <FormSelect
            label="Payment Type"
            name="paymentType"
            value={formData.paymentType}
            onChange={handleChange}
            options={PAYMENT_TYPE_OPTIONS}
            required
          />

          <div className="md:col-span-2">
            <FormSelect
              label="Purchase Invoice"
              name="purchaseId"
              value={formData.purchaseId}
              onChange={handleChange}
              disabled={!formData.supplierId || loadingInvoices}
              options={
                !formData.supplierId
                  ? [{ label: 'Select a supplier first', value: '' }]
                  : loadingInvoices
                    ? [{ label: 'Loading invoices…', value: '' }]
                    : invoiceOptions
              }
              error={errors.purchaseId}
            />
          </div>

          <FormInput
            label="Reference No"
            name="referenceNo"
            value={formData.referenceNo}
            onChange={handleChange}
            placeholder="Cheque / transaction reference"
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
              <p className="text-xs font-medium text-orange-600 uppercase tracking-wide">
                {isAdvance ? 'Payable Balance' : 'Supplier Balance'}
              </p>
              <p className="text-xl font-bold text-orange-900 mt-1">
                {loadingBalance ? 'Loading…' : fmt(balance ?? 0)}
              </p>
              {selectedInvoice && (
                <p className="text-sm text-orange-700 mt-1">
                  Invoice {selectedInvoice.invoiceNo} — balance due: {fmt(selectedInvoice.balanceDue)}
                </p>
              )}
            </div>
          )}

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
              <p className="mt-1 text-sm text-red-600">{errors.amount}</p>
            )}
          </div>

          <FormInput
            label="Payment Date"
            name="paymentDate"
            type="date"
            value={formData.paymentDate}
            onChange={handleChange}
            required
          />

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
