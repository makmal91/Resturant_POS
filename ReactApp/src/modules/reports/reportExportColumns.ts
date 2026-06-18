import { fmt, formatDate, fmtQty } from './reportFormatters';
import type { ReportExportColumn } from './reportExport';
import type {
  CustomerOutstandingRow,
  PayableAgingRow,
  ProductWiseSalesReportRow,
  ProfitLossRow,
  PurchaseReportRow,
  ReceivableAgingRow,
  SalesReportRow,
  StockSummaryItem,
  SupplierPayableRow,
} from './reportService';

const money = (v: unknown) => fmt(Number(v ?? 0));
const qty = (v: unknown) => fmtQty(Number(v ?? 0));
const date = (v: unknown) => formatDate(String(v ?? ''));
const text = (v: unknown) => String(v ?? '');

export const salesExportColumns: ReportExportColumn<SalesReportRow>[] = [
  { key: 'invoiceNo', header: 'Invoice' },
  { key: 'saleDate', header: 'Date', format: date },
  { key: 'customerName', header: 'Customer' },
  { key: 'grandTotal', header: 'Total', format: money },
  { key: 'paidAmount', header: 'Paid', format: money },
  { key: 'balanceDue', header: 'Balance', format: money },
  { key: 'paymentMethod', header: 'Payment' },
  { key: 'isCreditSale', header: 'Credit Sale', format: (v) => (v ? 'Yes' : 'No') },
  { key: 'cashierName', header: 'Cashier', format: text },
];

export const purchaseExportColumns: ReportExportColumn<PurchaseReportRow>[] = [
  { key: 'invoiceNo', header: 'Invoice' },
  { key: 'purchaseDate', header: 'Date', format: date },
  { key: 'supplierName', header: 'Supplier' },
  { key: 'totalAmount', header: 'Total', format: money },
  { key: 'paidAmount', header: 'Paid', format: money },
  { key: 'balanceDue', header: 'Balance', format: money },
  { key: 'status', header: 'Status' },
  { key: 'isCreditPurchase', header: 'Credit Purchase', format: (v) => (v ? 'Yes' : 'No') },
];

export const supplierPayableExportColumns: ReportExportColumn<SupplierPayableRow>[] = [
  { key: 'supplierCode', header: 'Code' },
  { key: 'supplierName', header: 'Supplier' },
  { key: 'phone', header: 'Phone' },
  { key: 'invoicePayable', header: 'Invoice Due', format: money },
  { key: 'payableAmount', header: 'Payable', format: money },
  { key: 'outstandingInvoices', header: 'Invoices' },
  { key: 'lastPurchaseDate', header: 'Last Purchase', format: date },
];

export const customerOutstandingExportColumns: ReportExportColumn<CustomerOutstandingRow>[] = [
  { key: 'customerCode', header: 'Code' },
  { key: 'customerName', header: 'Customer' },
  { key: 'phone', header: 'Phone', format: text },
  { key: 'openingBalance', header: 'Opening', format: money },
  { key: 'invoiceOutstanding', header: 'Invoice Due', format: money },
  { key: 'outstandingAmount', header: 'Outstanding', format: money },
  { key: 'outstandingInvoices', header: 'Invoices' },
  { key: 'lastSaleDate', header: 'Last Sale', format: date },
];

export const receivableAgingExportColumns: ReportExportColumn<ReceivableAgingRow>[] = [
  { key: 'customerName', header: 'Customer' },
  { key: 'invoiceNo', header: 'Invoice No' },
  { key: 'invoiceDate', header: 'Invoice Date', format: date },
  { key: 'totalAmount', header: 'Total', format: money },
  { key: 'paidAmount', header: 'Paid', format: money },
  { key: 'outstanding', header: 'Outstanding', format: money },
  { key: 'daysOverdue', header: 'Days Overdue' },
  { key: 'agingBucket', header: 'Bucket' },
];

export const payableAgingExportColumns: ReportExportColumn<PayableAgingRow>[] = [
  { key: 'supplierName', header: 'Supplier' },
  { key: 'invoiceNo', header: 'Invoice No' },
  { key: 'invoiceDate', header: 'Invoice Date', format: date },
  { key: 'totalAmount', header: 'Total', format: money },
  { key: 'paidAmount', header: 'Paid', format: money },
  { key: 'outstanding', header: 'Outstanding', format: money },
  { key: 'daysOverdue', header: 'Days Overdue' },
  { key: 'agingBucket', header: 'Bucket' },
];

export const profitLossExportColumns: ReportExportColumn<ProfitLossRow>[] = [
  { key: 'date', header: 'Date', format: date },
  { key: 'revenue', header: 'Revenue', format: money },
  { key: 'costOfGoodsSold', header: 'COGS', format: money },
  { key: 'grossProfit', header: 'Gross Profit', format: money },
  { key: 'expenses', header: 'Expenses', format: money },
  { key: 'netProfit', header: 'Net Profit', format: money },
  { key: 'salesCount', header: 'Sales Count' },
];

export const productWiseSalesExportColumns: ReportExportColumn<ProductWiseSalesReportRow>[] = [
  { key: 'productCode', header: 'Code' },
  { key: 'productName', header: 'Product' },
  { key: 'categoryName', header: 'Category' },
  { key: 'subCategoryName', header: 'Sub Category', format: text },
  { key: 'brandName', header: 'Brand', format: text },
  { key: 'totalQuantity', header: 'Qty Sold', format: qty },
  { key: 'totalAmount', header: 'Sales', format: money },
  { key: 'totalDiscount', header: 'Discount', format: money },
  { key: 'totalTax', header: 'Tax', format: money },
  { key: 'grossProfit', header: 'Gross Profit', format: money },
  { key: 'invoiceCount', header: 'Invoices' },
];

export const stockExportColumns: ReportExportColumn<StockSummaryItem>[] = [
  { key: 'productId', header: 'Product ID' },
  { key: 'productName', header: 'Product' },
  { key: 'closingBalance', header: 'Closing Balance', format: qty },
];
