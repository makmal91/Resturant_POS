import type { PartyLedgerEntry } from './partyLedgerService';

export type PartyLedgerVoucherKind = 'purchase' | 'sale' | 'payment' | 'none';

export interface PartyLedgerVoucherTarget {
  kind: PartyLedgerVoucherKind;
  id: number;
}

const PURCHASE_TYPES = new Set(['CreditPurchase', 'CashPurchase']);
const SALE_TYPES = new Set(['CreditSale', 'CashSale']);
const PAYMENT_TYPES = new Set([
  'PaymentMade',
  'PaymentReceived',
  'AgainstInvoice',
  'Advance',
  'Adjustment',
  'Reversal',
]);

export function resolvePartyLedgerVoucher(row: PartyLedgerEntry): PartyLedgerVoucherTarget {
  if (row.type === 'OpeningBalance') return { kind: 'none', id: 0 };

  if (PURCHASE_TYPES.has(row.type) && row.referenceId > 0) {
    return { kind: 'purchase', id: row.referenceId };
  }

  if (SALE_TYPES.has(row.type) && row.referenceId > 0) {
    return { kind: 'sale', id: row.referenceId };
  }

  const paymentId = row.paymentId ?? (PAYMENT_TYPES.has(row.type) ? row.referenceId : 0);
  if (paymentId > 0) {
    return { kind: 'payment', id: paymentId };
  }

  return { kind: 'none', id: 0 };
}

export function partyLedgerVoucherLabel(row: PartyLedgerEntry): string {
  const dashMatch = row.description.match(/—\s*([^|]+)/);
  if (dashMatch?.[1]) return dashMatch[1].trim();

  const invoiceMatch = row.description.match(/Invoice:\s*([^\s|]+)/i);
  if (invoiceMatch?.[1]) return invoiceMatch[1].trim();

  const target = resolvePartyLedgerVoucher(row);
  if (target.kind === 'payment') return `Payment #${target.id}`;
  if (target.kind === 'purchase') return `Purchase #${target.id}`;
  if (target.kind === 'sale') return `Invoice #${target.id}`;

  return row.description?.trim() || row.type;
}
