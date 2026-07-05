import type { CashFlowTransactionDto, CashFlowTransactionType } from '../modules/cashflow/cashFlowService';
import { LEDGER_TYPE_LABELS, type PartyLedgerEntry } from '../modules/ledger/partyLedgerService';
import type { StockBalance, StockLedgerEntry, StockLedgerType } from '../modules/stock/stockService';
import { getStockStatus, stockStatusLabel } from '../modules/stock/stockService';
import { fmt, fmtQty, formatDate } from '../modules/reports/reportFormatters';
import type { GridExportColumn } from './gridExport';

const money = (v: unknown) => fmt(Number(v ?? 0));
const qty = (v: unknown) => fmtQty(Number(v ?? 0));
const date = (v: unknown) => formatDate(String(v ?? ''));
const text = (v: unknown) => String(v ?? '');

const CASH_TYPE_LABELS: Record<CashFlowTransactionType, string> = {
  Sale: 'Sale',
  Expense: 'Expense',
  CashIn: 'Cash In',
  CashOut: 'Cash Out',
  BankTransfer: 'Bank Transfer',
  OpeningBalance: 'Opening',
  OpeningStockVoucher: 'Opening Stock',
  ClosingBalance: 'Closing',
  Reversal: 'Reversal',
};

const STOCK_TYPE_LABELS: Record<StockLedgerType, string> = {
  PurchaseEntry: 'Purchase',
  SaleEntry: 'Sale',
  PurchaseReturn: 'Return In',
  SaleReturn: 'Return In',
  Adjustment: 'Adjustment',
  TransferIn: 'Return In',
  TransferOut: 'Return Out',
  Opening: 'Opening',
  SaleReversal: 'Sale Reversal',
  PurchaseReversal: 'Purchase Reversal',
};

export const partyLedgerExportColumns: GridExportColumn<PartyLedgerEntry>[] = [
  { key: 'date', header: 'Date', format: date },
  { key: 'type', header: 'Type', format: (v) => LEDGER_TYPE_LABELS[String(v)] ?? String(v ?? '') },
  { key: 'description', header: 'Description' },
  { key: 'debit', header: 'Debit', format: (v) => (Number(v) > 0 ? money(v) : '') },
  { key: 'credit', header: 'Credit', format: (v) => (Number(v) > 0 ? money(v) : '') },
  { key: 'runningBalance', header: 'Running Balance', format: money },
];

export const supplierLedgerExportColumns: GridExportColumn<PartyLedgerEntry>[] = [
  { key: 'date', header: 'Date', format: date },
  { key: 'type', header: 'Type', format: (v) => LEDGER_TYPE_LABELS[String(v)] ?? String(v ?? '') },
  { key: 'description', header: 'Description' },
  { key: 'debit', header: 'Debit', format: (v) => (Number(v) > 0 ? money(v) : '') },
  { key: 'credit', header: 'Credit', format: (v) => (Number(v) > 0 ? money(v) : '') },
  { key: 'runningBalance', header: 'Running Balance', format: money },
];

export const cashLedgerExportColumns: GridExportColumn<CashFlowTransactionDto>[] = [
  { key: 'transactionDate', header: 'Date', format: date },
  { key: 'transactionType', header: 'Type', format: (v) => CASH_TYPE_LABELS[v as CashFlowTransactionType] ?? text(v) },
  { key: 'accountName', header: 'Account', format: text },
  { key: 'description', header: 'Description', format: text },
  { key: 'referenceNo', header: 'Reference', format: text },
  { key: 'debit', header: 'In', format: (v) => (Number(v) > 0 ? money(v) : '') },
  { key: 'credit', header: 'Out', format: (v) => (Number(v) > 0 ? money(v) : '') },
  { key: 'runningBalance', header: 'Balance', format: money },
];

export interface CashDailyTrendRow extends Record<string, unknown> {
  date: string;
  cashIn: number;
  cashOut: number;
  net: number;
}

export const cashDailyTrendExportColumns: GridExportColumn<CashDailyTrendRow>[] = [
  { key: 'date', header: 'Date', format: date },
  { key: 'cashIn', header: 'Cash In', format: money },
  { key: 'cashOut', header: 'Cash Out', format: money },
  { key: 'net', header: 'Net', format: money },
];

export const stockLedgerExportColumns: GridExportColumn<StockLedgerEntry>[] = [
  { key: 'date', header: 'Date', format: date },
  { key: 'type', header: 'Type', format: (v) => STOCK_TYPE_LABELS[v as StockLedgerType] ?? text(v) },
  { key: 'referenceId', header: 'Reference', format: text },
  { key: 'productName', header: 'Product' },
  { key: 'variantName', header: 'Variant', format: text },
  { key: 'warehouseName', header: 'Warehouse' },
  { key: 'quantityInBaseUnit', header: 'Quantity', format: qty },
  { key: 'unitPrice', header: 'Unit Price', format: money },
  { key: 'totalAmount', header: 'Total Amount', format: money },
  { key: 'remarks', header: 'Remarks', format: text },
];

export const stockBalanceExportColumns: GridExportColumn<StockBalance>[] = [
  { key: 'productCode', header: 'Code', format: text },
  { key: 'productName', header: 'Product' },
  { key: 'variantName', header: 'Variant', format: text },
  { key: 'warehouseName', header: 'Warehouse' },
  { key: 'quantity', header: 'Quantity', format: qty },
  {
    key: '_status',
    header: 'Status',
    format: (_v, row) => stockStatusLabel(getStockStatus(row.quantity, row)),
  },
];
