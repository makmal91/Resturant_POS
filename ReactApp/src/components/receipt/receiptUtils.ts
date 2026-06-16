import type { SaleInvoiceDto, SaleInvoiceItemResult } from '../../modules/pos/posService';

import { formatCurrency as formatCurrencyValue, getCurrencySymbol } from '../../utils/currencyHelper';

export interface ReceiptBusinessInfo {
  id: number;
  name: string;
  legalName: string;
  address: string;
  phone: string;
  email: string;
  currency: string;
  taxNumber: string;
  hasLogo: boolean;
  slogan?: string | null;
  website?: string | null;
}

export type ReceiptLayout = 'thermal' | 'a4';

export const formatReceiptCurrency = (value: number, currency = 'PKR'): string =>
  formatCurrencyValue(value, currency);

export { getCurrencySymbol };

export const formatReceiptNumber = (value: number): string =>
  new Intl.NumberFormat(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);

export const formatReceiptDateTime = (value: string | null | undefined): string => {
  if (!value) return '—';
  const d = new Date(value);
  return Number.isNaN(d.getTime())
    ? '—'
    : d.toLocaleString(undefined, {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      });
};

export const formatReceiptDateCompact = (value: string | null | undefined): string => {
  if (!value) return '—';
  const d = new Date(value);
  return Number.isNaN(d.getTime())
    ? '—'
    : d.toLocaleString(undefined, {
        day: '2-digit',
        month: 'short',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      });
};

export const resolveCashierName = (
  invoice: SaleInvoiceDto,
  sessionName?: string | null,
): string | null => {
  const fromInvoice = invoice.cashierName?.trim();
  if (fromInvoice) return fromInvoice;
  const fromSession = sessionName?.trim();
  return fromSession || null;
};

export const computeRoundOff = (invoice: SaleInvoiceDto): number | null => {
  const expected = invoice.subTotal - invoice.discountAmount + invoice.taxAmount;
  const diff = Math.round((invoice.grandTotal - expected) * 100) / 100;
  return Math.abs(diff) >= 0.01 ? diff : null;
};

export const getItemSkuLabel = (item: SaleInvoiceItemResult): string | null => {
  const code = item.productCode?.trim();
  if (code) return code;
  return null;
};

export const getItemDiscount = (item: SaleInvoiceItemResult): number => {
  if (item.discountAmount > 0) return item.discountAmount;
  const gross = item.quantity * item.unitPrice;
  if (item.discountPercent > 0) return (gross * item.discountPercent) / 100;
  return 0;
};

export const getBalanceDue = (invoice: SaleInvoiceDto): number => {
  const due = invoice.grandTotal - invoice.paidAmount;
  return due > 0.009 ? Math.round(due * 100) / 100 : 0;
};

export const barcode39Text = (value: string): string => {
  const sanitized = value.replace(/[^0-9A-Z\-.\s$/%+]/gi, '').toUpperCase().replace(/\s+/g, '');
  if (!sanitized) return '';
  return `*${sanitized}*`;
};

export const barcodeFontSize = (value: string, layout: ReceiptLayout): string => {
  const len = value.length;
  if (layout === 'a4') {
    if (len > 18) return '36px';
    if (len > 14) return '44px';
    return '52px';
  }
  if (len > 18) return '24px';
  if (len > 14) return '30px';
  if (len > 10) return '36px';
  return '42px';
};

export const triggerReceiptPrint = (): void => {
  window.print();
};
