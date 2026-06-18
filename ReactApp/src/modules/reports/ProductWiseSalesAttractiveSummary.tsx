import ReportAttractiveSummary, { type AttractiveSummaryCard } from './ReportAttractiveSummary';
import { fmt, fmtQty } from './reportFormatters';
import type { ProductWiseSalesReportSummary } from './reportService';

interface ProductWiseSalesAttractiveSummaryProps {
  summary: ProductWiseSalesReportSummary | null;
  loading?: boolean;
}

export default function ProductWiseSalesAttractiveSummary({
  summary,
  loading = false,
}: ProductWiseSalesAttractiveSummaryProps) {
  if (!summary && !loading) return null;

  const marginPct = summary && summary.totalAmount > 0
    ? ((summary.grossProfit / summary.totalAmount) * 100).toFixed(1)
    : '0.0';

  const cards: AttractiveSummaryCard[] = summary ? [
    {
      key: 'products',
      label: 'Products Sold',
      value: String(summary.totalProducts),
      sub: 'unique items',
      iconKey: 'products',
      cardClass: 'border-violet-100 bg-gradient-to-br from-violet-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-violet-100 to-violet-50 text-violet-600',
      valueClass: 'text-violet-700',
    },
    {
      key: 'quantity',
      label: 'Qty Sold',
      value: fmtQty(summary.totalQuantity),
      sub: 'units moved',
      iconKey: 'stock',
      cardClass: 'border-sky-100 bg-gradient-to-br from-sky-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-sky-100 to-sky-50 text-sky-600',
      valueClass: 'text-sky-700',
    },
    {
      key: 'sales',
      label: 'Total Sales',
      value: fmt(summary.totalAmount),
      sub: 'revenue',
      iconKey: 'sales',
      cardClass: 'border-blue-100 bg-gradient-to-br from-blue-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-blue-100 to-blue-50 text-blue-600',
      valueClass: 'text-blue-700',
    },
    {
      key: 'profit',
      label: 'Gross Profit',
      value: fmt(summary.grossProfit),
      sub: summary.grossProfit >= 0 ? 'positive margin' : 'below cost',
      iconKey: 'cashflow',
      cardClass: summary.grossProfit >= 0
        ? 'border-emerald-100 bg-gradient-to-br from-emerald-50 via-white to-white'
        : 'border-rose-100 bg-gradient-to-br from-rose-50 via-white to-white',
      iconWrapClass: summary.grossProfit >= 0
        ? 'bg-gradient-to-br from-emerald-100 to-emerald-50 text-emerald-600'
        : 'bg-gradient-to-br from-rose-100 to-rose-50 text-rose-600',
      valueClass: summary.grossProfit >= 0 ? 'text-emerald-700' : 'text-rose-700',
    },
    {
      key: 'invoices',
      label: 'Invoices',
      value: String(summary.totalInvoices),
      sub: 'completed sales',
      iconKey: 'invoices',
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
        value: summary ? fmt(summary.totalAmount) : '—',
        subtitle: summary
          ? `${summary.totalProducts} products · ${fmtQty(summary.totalQuantity)} units · ${summary.totalInvoices} invoices`
          : 'Loading period totals…',
        badgeLabel: 'Gross Margin',
        badgeValue: `${marginPct}%`,
        badgeIconKey: 'reports',
      }}
      cards={cards}
    />
  );
}
