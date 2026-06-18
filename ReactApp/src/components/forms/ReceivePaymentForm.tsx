import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { FormButton, FormInput, FormSelect, FormTextarea } from './index';
import { useBusinessCurrency } from '../../hooks/useBusinessCurrency';
import { useFormBranchId } from '../../hooks/useFormBranchId';
import {
  partyLedgerService,
  type OutstandingInvoiceOption,
} from '../../modules/ledger/partyLedgerService';

export type PartyPaymentType = 'Cash' | 'Bank' | 'Online';

export interface ReceivePaymentFormData {
  customerId: string;
  saleInvoiceId: string;
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

interface ReceivePaymentFormProps {
  customers?: LookupOption[];
  initialData?: { customerId?: number; saleInvoiceId?: number } | null;
  branchId: number;
  onSubmit: (data: ReceivePaymentFormData) => void;
  isLoading?: boolean;
  submitLabel?: string;
}

const PAYMENT_TYPE_OPTIONS = [
  { label: 'Cash', value: 'Cash' },
  { label: 'Bank', value: 'Bank' },
  { label: 'Online', value: 'Online' },
];

const buildDefaultFormData = (
  customerId = '',
  saleInvoiceId = '',
): ReceivePaymentFormData => ({
  customerId: customerId ? String(customerId) : '',
  saleInvoiceId: saleInvoiceId ? String(saleInvoiceId) : '',
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

const ReceivePaymentForm: React.FC<ReceivePaymentFormProps> = ({
  customers = [],
  initialData,
  branchId,
  onSubmit,
  isLoading = false,
  submitLabel = 'Receive Payment',
}) => {
  const { branchError } = useFormBranchId();
  const { symbol, currencyCode, fmt, loading: currencyLoading } = useBusinessCurrency();

  const [formData, setFormData] = useState<ReceivePaymentFormData>(() =>
    buildDefaultFormData(initialData?.customerId, initialData?.saleInvoiceId ? String(initialData.saleInvoiceId) : '')
  );
  const [errors, setErrors] = useState<Partial<Record<keyof ReceivePaymentFormData, string>>>({});
  const [balance, setBalance] = useState<number | null>(null);
  const [outstandingInvoices, setOutstandingInvoices] = useState<OutstandingInvoiceOption[]>([]);
  const [loadingBalance, setLoadingBalance] = useState(false);
  const [loadingInvoices, setLoadingInvoices] = useState(false);

  useEffect(() => {
    setFormData(buildDefaultFormData(initialData?.customerId, initialData?.saleInvoiceId ? String(initialData.saleInvoiceId) : ''));
    setErrors({});
  }, [initialData?.customerId, initialData?.saleInvoiceId]);

  const selectedInvoice = useMemo(
    () => outstandingInvoices.find((inv) => String(inv.invoiceId) === formData.saleInvoiceId),
    [outstandingInvoices, formData.saleInvoiceId],
  );

  const loadBalance = useCallback(async (customerId: number) => {
    if (customerId <= 0 || branchId <= 0) {
      setBalance(null);
      return;
    }
    setLoadingBalance(true);
    try {
      const res = await partyLedgerService.getCustomerBalance(branchId, customerId);
      setBalance(res.data.balance);
    } catch {
      setBalance(null);
    } finally {
      setLoadingBalance(false);
    }
  }, [branchId]);

  const loadOutstandingInvoices = useCallback(async (customerId: number) => {
    if (customerId <= 0 || branchId <= 0) {
      setOutstandingInvoices([]);
      return;
    }
    setLoadingInvoices(true);
    try {
      const res = await partyLedgerService.getCustomerOutstandingInvoices(branchId, customerId);
      setOutstandingInvoices(res.data);
    } catch {
      setOutstandingInvoices([]);
    } finally {
      setLoadingInvoices(false);
    }
  }, [branchId]);

  useEffect(() => {
    const id = Number(formData.customerId);
    void loadBalance(id);
    void loadOutstandingInvoices(id);
  }, [formData.customerId, loadBalance, loadOutstandingInvoices]);

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

    if (name === 'customerId') {
      setFormData((prev) => ({
        ...prev,
        customerId: value,
        saleInvoiceId: '',
        amount: '',
      }));
      setErrors((prev) => ({ ...prev, customerId: '', saleInvoiceId: '', amount: '' }));
      return;
    }

    if (name === 'saleInvoiceId') {
      const invoice = outstandingInvoices.find((inv) => String(inv.invoiceId) === value);
      setFormData((prev) => ({
        ...prev,
        saleInvoiceId: value,
        amount: invoice ? String(invoice.balanceDue) : prev.amount,
      }));
      setErrors((prev) => ({ ...prev, saleInvoiceId: '', amount: '' }));
      return;
    }

    setFormData((prev) => ({ ...prev, [name]: value }));
    setErrors((prev) => ({ ...prev, [name]: '' }));
  };

  const validateForm = () => {
    const nextErrors: Partial<Record<keyof ReceivePaymentFormData, string>> = {};
    const customerId = Number(formData.customerId);
    const saleInvoiceId = formData.saleInvoiceId.trim() ? Number(formData.saleInvoiceId) : 0;
    const parsed = parseFloat(formData.amount);

    if (!customerId) nextErrors.customerId = 'Customer is required';
    if (formData.saleInvoiceId.trim() && (!saleInvoiceId || saleInvoiceId <= 0)) {
      nextErrors.saleInvoiceId = 'Select a valid invoice';
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
    setFormData(buildDefaultFormData(initialData?.customerId, initialData?.saleInvoiceId ? String(initialData.saleInvoiceId) : ''));
    setErrors({});
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validateForm()) onSubmit(formData);
  };

  const isAdvance = !formData.saleInvoiceId.trim();

  return (
    <form onSubmit={handleSubmit} className="flex h-full min-h-0 flex-col">
      <div className="flex-1 overflow-y-auto min-h-0 px-6 py-4">
        <p className="text-sm text-gray-600 mb-6">
          Record a customer payment against a sale invoice or as an advance payment.
        </p>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {branchError && (
            <p className="md:col-span-2 text-sm text-red-600">{branchError}</p>
          )}

          <FormSelect
            label="Customer"
            name="customerId"
            value={formData.customerId}
            onChange={handleChange}
            options={[
              { label: 'Select customer', value: '' },
              ...customers.map((c) => ({ label: c.name, value: String(c.id) })),
            ]}
            required
            error={errors.customerId}
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
              label="Sale Invoice"
              name="saleInvoiceId"
              value={formData.saleInvoiceId}
              onChange={handleChange}
              disabled={!formData.customerId || loadingInvoices}
              options={
                !formData.customerId
                  ? [{ label: 'Select a customer first', value: '' }]
                  : loadingInvoices
                    ? [{ label: 'Loading invoices…', value: '' }]
                    : invoiceOptions
              }
              error={errors.saleInvoiceId}
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

          {formData.customerId && (
            <div className="md:col-span-2 rounded-lg bg-blue-50 border border-blue-100 px-4 py-3">
              <p className="text-xs font-medium text-blue-600 uppercase tracking-wide">
                {isAdvance ? 'Outstanding Balance' : 'Customer Balance'}
              </p>
              <p className="text-xl font-bold text-blue-900 mt-1">
                {loadingBalance ? 'Loading…' : fmt(balance ?? 0)}
              </p>
              {selectedInvoice && (
                <p className="text-sm text-blue-700 mt-1">
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

export default ReceivePaymentForm;
