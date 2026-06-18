import ReportAttractiveSummary, { type AttractiveSummaryCard } from './ReportAttractiveSummary';
import { fmt, formatDate, fmtQty } from './reportFormatters';
import type { AgingReportSummary, SalesSummaryDto } from './reportService';

interface SummaryProps {
  loading?: boolean;
}

export function SalesAttractiveSummary({
  summary,
  loading = false,
}: SummaryProps & { summary: SalesSummaryDto | null }) {
  if (!summary && !loading) return null;

  const cards: AttractiveSummaryCard[] = summary ? [
    {
      key: 'sales',
      label: 'Total Sales',
      value: fmt(summary.totalSales),
      sub: 'gross revenue',
      iconKey: 'sales',
      cardClass: 'border-blue-100 bg-gradient-to-br from-blue-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-blue-100 to-blue-50 text-blue-600',
      valueClass: 'text-blue-700',
    },
    {
      key: 'invoices',
      label: 'Invoices',
      value: String(summary.totalInvoices),
      sub: 'completed sales',
      iconKey: 'invoices',
      cardClass: 'border-violet-100 bg-gradient-to-br from-violet-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-violet-100 to-violet-50 text-violet-600',
      valueClass: 'text-violet-700',
    },
    {
      key: 'cash',
      label: 'Cash Sales',
      value: fmt(summary.totalCash),
      sub: 'cash payments',
      iconKey: 'cashflow',
      cardClass: 'border-emerald-100 bg-gradient-to-br from-emerald-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-emerald-100 to-emerald-50 text-emerald-600',
      valueClass: 'text-emerald-700',
    },
    {
      key: 'card',
      label: 'Card Sales',
      value: fmt(summary.totalCard),
      sub: 'card payments',
      iconKey: 'purchase',
      cardClass: 'border-sky-100 bg-gradient-to-br from-sky-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-sky-100 to-sky-50 text-sky-600',
      valueClass: 'text-sky-700',
    },
    {
      key: 'paid',
      label: 'Total Paid',
      value: fmt(summary.totalPaid),
      sub: `avg ${fmt(summary.averageSale)} / sale`,
      iconKey: 'reports',
      cardClass: 'border-amber-100 bg-gradient-to-br from-amber-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-amber-100 to-amber-50 text-amber-600',
      valueClass: 'text-amber-700',
    },
  ] : [];

  return (
    <ReportAttractiveSummary
      loading={loading}
      columnsClassName="sm:grid-cols-2 xl:grid-cols-5"
      hero={{
        title: 'Period Sales Overview',
        value: summary ? fmt(summary.totalSales) : '—',
        subtitle: summary
          ? `${summary.totalInvoices} invoices · ${fmt(summary.totalPaid)} collected`
          : 'Loading period totals…',
        badgeLabel: 'Avg Sale',
        badgeValue: summary ? fmt(summary.averageSale) : '—',
        badgeIconKey: 'sales',
      }}
      cards={cards}
    />
  );
}

export function StockAttractiveSummary({
  productCount,
  totalClosingBalance,
  loading = false,
}: SummaryProps & { productCount: number; totalClosingBalance: number }) {
  if (!loading && productCount <= 0 && totalClosingBalance === 0) return null;

  const cards: AttractiveSummaryCard[] = [
    {
      key: 'products',
      label: 'Products',
      value: String(productCount),
      sub: 'with stock balance',
      iconKey: 'products',
      cardClass: 'border-violet-100 bg-gradient-to-br from-violet-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-violet-100 to-violet-50 text-violet-600',
      valueClass: 'text-violet-700',
    },
    {
      key: 'balance',
      label: 'Closing Balance',
      value: fmtQty(totalClosingBalance),
      sub: 'total units',
      iconKey: 'stock',
      cardClass: 'border-emerald-100 bg-gradient-to-br from-emerald-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-emerald-100 to-emerald-50 text-emerald-600',
      valueClass: 'text-emerald-700',
    },
  ];

  return (
    <ReportAttractiveSummary
      loading={loading}
      columnsClassName="sm:grid-cols-2"
      hero={{
        title: 'Stock Position Overview',
        value: fmtQty(totalClosingBalance),
        subtitle: `${productCount} products tracked in stock ledger`,
        badgeLabel: 'Products',
        badgeValue: String(productCount),
        badgeIconKey: 'stock',
        gradientClass: 'border-teal-200 bg-gradient-to-r from-teal-600 via-emerald-600 to-green-600',
      }}
      cards={cards}
    />
  );
}

export function PurchaseAttractiveSummary({
  invoiceCount,
  totalAmount,
  paidAmount,
  balanceDue,
  loading = false,
}: SummaryProps & {
  invoiceCount: number;
  totalAmount: number;
  paidAmount: number;
  balanceDue: number;
}) {
  if (!loading && invoiceCount <= 0) return null;

  const cards: AttractiveSummaryCard[] = [
    {
      key: 'invoices',
      label: 'Purchases',
      value: String(invoiceCount),
      sub: 'posted invoices',
      iconKey: 'invoices',
      cardClass: 'border-violet-100 bg-gradient-to-br from-violet-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-violet-100 to-violet-50 text-violet-600',
      valueClass: 'text-violet-700',
    },
    {
      key: 'total',
      label: 'Total Amount',
      value: fmt(totalAmount),
      sub: 'purchase value',
      iconKey: 'purchase',
      cardClass: 'border-blue-100 bg-gradient-to-br from-blue-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-blue-100 to-blue-50 text-blue-600',
      valueClass: 'text-blue-700',
    },
    {
      key: 'paid',
      label: 'Paid',
      value: fmt(paidAmount),
      sub: 'settled amount',
      iconKey: 'cashflow',
      cardClass: 'border-emerald-100 bg-gradient-to-br from-emerald-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-emerald-100 to-emerald-50 text-emerald-600',
      valueClass: 'text-emerald-700',
    },
    {
      key: 'balance',
      label: 'Balance Due',
      value: fmt(balanceDue),
      sub: 'outstanding payable',
      iconKey: 'expenses',
      cardClass: 'border-orange-100 bg-gradient-to-br from-orange-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-orange-100 to-orange-50 text-orange-600',
      valueClass: 'text-orange-700',
    },
  ];

  return (
    <ReportAttractiveSummary
      loading={loading}
      columnsClassName="sm:grid-cols-2 xl:grid-cols-4"
      hero={{
        title: 'Period Purchase Overview',
        value: fmt(totalAmount),
        subtitle: `${invoiceCount} invoices · ${fmt(paidAmount)} paid · ${fmt(balanceDue)} due`,
        badgeLabel: 'Paid %',
        badgeValue: totalAmount > 0 ? `${((paidAmount / totalAmount) * 100).toFixed(1)}%` : '0.0%',
        badgeIconKey: 'purchase',
        gradientClass: 'border-orange-200 bg-gradient-to-r from-orange-600 via-amber-600 to-yellow-600',
      }}
      cards={cards}
    />
  );
}

export function SupplierPayableAttractiveSummary({
  supplierCount,
  totalPayable,
  totalInvoices,
  loading = false,
}: SummaryProps & {
  supplierCount: number;
  totalPayable: number;
  totalInvoices: number;
}) {
  if (!loading && supplierCount <= 0) return null;

  const cards: AttractiveSummaryCard[] = [
    {
      key: 'suppliers',
      label: 'Suppliers',
      value: String(supplierCount),
      sub: 'with payable balance',
      iconKey: 'suppliers',
      cardClass: 'border-violet-100 bg-gradient-to-br from-violet-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-violet-100 to-violet-50 text-violet-600',
      valueClass: 'text-violet-700',
    },
    {
      key: 'payable',
      label: 'Total Payable',
      value: fmt(totalPayable),
      sub: 'amount due',
      iconKey: 'expenses',
      cardClass: 'border-orange-100 bg-gradient-to-br from-orange-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-orange-100 to-orange-50 text-orange-600',
      valueClass: 'text-orange-700',
    },
    {
      key: 'invoices',
      label: 'Invoices',
      value: String(totalInvoices),
      sub: 'unpaid purchases',
      iconKey: 'invoices',
      cardClass: 'border-amber-100 bg-gradient-to-br from-amber-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-amber-100 to-amber-50 text-amber-600',
      valueClass: 'text-amber-700',
    },
  ];

  return (
    <ReportAttractiveSummary
      loading={loading}
      columnsClassName="sm:grid-cols-3"
      hero={{
        title: 'Supplier Payable Overview',
        value: fmt(totalPayable),
        subtitle: `${supplierCount} suppliers · ${totalInvoices} outstanding invoices`,
        badgeLabel: 'Avg / Supplier',
        badgeValue: supplierCount > 0 ? fmt(totalPayable / supplierCount) : '—',
        badgeIconKey: 'suppliers',
        gradientClass: 'border-orange-200 bg-gradient-to-r from-orange-600 via-red-600 to-rose-600',
      }}
      cards={cards}
    />
  );
}

export function CustomerOutstandingAttractiveSummary({
  customerCount,
  totalOutstanding,
  totalOpening,
  totalInvoiceDue,
  loading = false,
}: SummaryProps & {
  customerCount: number;
  totalOutstanding: number;
  totalOpening: number;
  totalInvoiceDue: number;
}) {
  if (!loading && customerCount <= 0) return null;

  const cards: AttractiveSummaryCard[] = [
    {
      key: 'customers',
      label: 'Customers',
      value: String(customerCount),
      sub: 'with balance',
      iconKey: 'customers',
      cardClass: 'border-violet-100 bg-gradient-to-br from-violet-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-violet-100 to-violet-50 text-violet-600',
      valueClass: 'text-violet-700',
    },
    {
      key: 'outstanding',
      label: 'Total Outstanding',
      value: fmt(totalOutstanding),
      sub: 'receivable amount',
      iconKey: 'cashflow',
      cardClass: 'border-blue-100 bg-gradient-to-br from-blue-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-blue-100 to-blue-50 text-blue-600',
      valueClass: 'text-blue-700',
    },
    {
      key: 'opening',
      label: 'Opening Balance',
      value: fmt(totalOpening),
      sub: 'carried forward',
      iconKey: 'reports',
      cardClass: 'border-sky-100 bg-gradient-to-br from-sky-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-sky-100 to-sky-50 text-sky-600',
      valueClass: 'text-sky-700',
    },
    {
      key: 'invoice',
      label: 'Invoice Due',
      value: fmt(totalInvoiceDue),
      sub: 'unpaid invoices',
      iconKey: 'invoices',
      cardClass: 'border-amber-100 bg-gradient-to-br from-amber-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-amber-100 to-amber-50 text-amber-600',
      valueClass: 'text-amber-700',
    },
  ];

  return (
    <ReportAttractiveSummary
      loading={loading}
      columnsClassName="sm:grid-cols-2 xl:grid-cols-4"
      hero={{
        title: 'Customer Outstanding Overview',
        value: fmt(totalOutstanding),
        subtitle: `${customerCount} customers · ${fmt(totalInvoiceDue)} invoice due`,
        badgeLabel: 'Opening',
        badgeValue: fmt(totalOpening),
        badgeIconKey: 'customers',
        gradientClass: 'border-blue-200 bg-gradient-to-r from-blue-600 via-indigo-600 to-violet-600',
      }}
      cards={cards}
    />
  );
}

export function AgingAttractiveSummary({
  summary,
  variant,
  loading = false,
}: SummaryProps & { summary: AgingReportSummary | null; variant: 'receivable' | 'payable' }) {
  if (!summary && !loading) return null;

  const isReceivable = variant === 'receivable';
  const cards: AttractiveSummaryCard[] = summary ? [
    {
      key: 'b030',
      label: '0–30 Days',
      value: fmt(summary.bucket0To30),
      sub: 'current bucket',
      iconKey: 'cashflow',
      cardClass: 'border-emerald-100 bg-gradient-to-br from-emerald-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-emerald-100 to-emerald-50 text-emerald-600',
      valueClass: 'text-emerald-700',
    },
    {
      key: 'b3160',
      label: '31–60 Days',
      value: fmt(summary.bucket31To60),
      sub: 'aging bucket',
      iconKey: 'reports',
      cardClass: 'border-sky-100 bg-gradient-to-br from-sky-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-sky-100 to-sky-50 text-sky-600',
      valueClass: 'text-sky-700',
    },
    {
      key: 'b6190',
      label: '61–90 Days',
      value: fmt(summary.bucket61To90),
      sub: 'aging bucket',
      iconKey: 'stock',
      cardClass: 'border-amber-100 bg-gradient-to-br from-amber-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-amber-100 to-amber-50 text-amber-600',
      valueClass: 'text-amber-700',
    },
    {
      key: 'b90',
      label: '90+ Days',
      value: fmt(summary.bucket90Plus),
      sub: 'overdue bucket',
      iconKey: 'alert',
      cardClass: 'border-rose-100 bg-gradient-to-br from-rose-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-rose-100 to-rose-50 text-rose-600',
      valueClass: 'text-rose-700',
    },
  ] : [];

  return (
    <ReportAttractiveSummary
      loading={loading}
      columnsClassName="sm:grid-cols-2 xl:grid-cols-4"
      hero={{
        title: isReceivable ? 'Receivable Aging Overview' : 'Payable Aging Overview',
        value: summary ? fmt(summary.totalOutstanding) : '—',
        subtitle: summary?.asOfDate
          ? `As of ${formatDate(summary.asOfDate)} · outstanding ${isReceivable ? 'receivables' : 'payables'}`
          : 'Loading aging totals…',
        badgeLabel: '90+ Risk',
        badgeValue: summary ? fmt(summary.bucket90Plus) : '—',
        badgeIconKey: isReceivable ? 'customers' : 'suppliers',
        gradientClass: isReceivable
          ? 'border-blue-200 bg-gradient-to-r from-blue-600 via-indigo-600 to-violet-600'
          : 'border-orange-200 bg-gradient-to-r from-orange-600 via-red-600 to-rose-600',
      }}
      cards={cards}
    />
  );
}
