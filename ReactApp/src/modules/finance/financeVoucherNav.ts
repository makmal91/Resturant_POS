import type { AccountLedgerEntry } from '../accounting/accountLedgerService';
import type { PartyLedgerEntry } from '../ledger/partyLedgerService';
import { resolvePartyLedgerVoucher } from '../ledger/partyLedgerVoucher';

export type FinanceSourceTarget =
  | { path: '/finance/payables'; paymentId?: number }
  | { path: '/finance/receivables'; paymentId?: number }
  | { path: '/finance/expenses'; expenseId?: number }
  | { path: '/purchase'; purchaseId?: number }
  | { path: '/sales-invoices'; invoiceId?: number }
  | null;

export function resolvePartyLedgerSource(row: PartyLedgerEntry): FinanceSourceTarget {
  const voucher = resolvePartyLedgerVoucher(row);
  if (voucher.kind === 'none') return null;
  if (voucher.kind === 'purchase') return { path: '/purchase', purchaseId: voucher.id };
  if (voucher.kind === 'sale') return { path: '/sales-invoices', invoiceId: voucher.id };
  if (voucher.kind === 'payment') {
    if (row.type === 'PaymentReceived') {
      return { path: '/finance/receivables', paymentId: voucher.id };
    }
    return { path: '/finance/payables', paymentId: voucher.id };
  }
  return null;
}

export function resolveAccountLedgerSource(row: AccountLedgerEntry): FinanceSourceTarget {
  if (row.isOpeningBalance || !row.referenceId) return null;
  const type = row.referenceType;
  if (type === 'Payment') return { path: '/finance/payables', paymentId: row.referenceId };
  if (type === 'Receipt') return { path: '/finance/receivables', paymentId: row.referenceId };
  if (type === 'Expense') return { path: '/finance/expenses', expenseId: row.referenceId };
  if (type === 'Purchase') return { path: '/purchase', purchaseId: row.referenceId };
  if (type === 'Sale') return { path: '/sales-invoices', invoiceId: row.referenceId };
  return null;
}

export function financeSourceLabel(target: FinanceSourceTarget): string {
  if (!target) return '';
  if (target.path === '/finance/payables') return 'Open Payable';
  if (target.path === '/finance/receivables') return 'Open Receivable';
  if (target.path === '/finance/expenses') return 'Open Expense';
  if (target.path === '/purchase') return 'Open Purchase';
  if (target.path === '/sales-invoices') return 'Open Invoice';
  return 'Open Source';
}
