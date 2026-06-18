import ReportAttractiveSummary, { type AttractiveSummaryCard } from './ReportAttractiveSummary';
import { fmt } from './reportFormatters';
import type { ProfitLossReportSummary } from './reportService';

interface ProfitLossAttractiveSummaryProps {
  summary: ProfitLossReportSummary | null;
  loading?: boolean;
}

export default function ProfitLossAttractiveSummary({
  summary,
  loading = false,
}: ProfitLossAttractiveSummaryProps) {
  if (!summary && !loading) return null;

  const netPositive = (summary?.totalNetProfit ?? 0) >= 0;
  const marginPct = summary && summary.totalRevenue > 0
    ? ((summary.totalNetProfit / summary.totalRevenue) * 100).toFixed(1)
    : '0.0';

  const cards: AttractiveSummaryCard[] = summary ? [
    {
      key: 'revenue',
      label: 'Total Revenue',
      value: fmt(summary.totalRevenue),
      sub: 'gross sales',
      iconKey: 'sales',
      cardClass: 'border-blue-100 bg-gradient-to-br from-blue-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-blue-100 to-blue-50 text-blue-600',
      valueClass: 'text-blue-700',
    },
    {
      key: 'cogs',
      label: 'Total COGS',
      value: fmt(summary.totalCostOfGoodsSold),
      sub: 'cost of goods',
      iconKey: 'purchase',
      cardClass: 'border-orange-100 bg-gradient-to-br from-orange-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-orange-100 to-orange-50 text-orange-600',
      valueClass: 'text-orange-700',
    },
    {
      key: 'gross',
      label: 'Gross Profit',
      value: fmt(summary.totalGrossProfit),
      sub: 'after COGS',
      iconKey: 'stock',
      cardClass: 'border-sky-100 bg-gradient-to-br from-sky-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-sky-100 to-sky-50 text-sky-600',
      valueClass: 'text-sky-700',
    },
    {
      key: 'expenses',
      label: 'Total Expenses',
      value: fmt(summary.totalExpenses),
      sub: 'operating costs',
      iconKey: 'expenses',
      cardClass: 'border-rose-100 bg-gradient-to-br from-rose-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-rose-100 to-rose-50 text-rose-600',
      valueClass: 'text-rose-700',
    },
    {
      key: 'net',
      label: 'Net Profit',
      value: fmt(summary.totalNetProfit),
      sub: netPositive ? 'profitable period' : 'loss period',
      iconKey: 'cashflow',
      cardClass: netPositive
        ? 'border-emerald-100 bg-gradient-to-br from-emerald-50 via-white to-white'
        : 'border-red-100 bg-gradient-to-br from-red-50 via-white to-white',
      iconWrapClass: netPositive
        ? 'bg-gradient-to-br from-emerald-100 to-emerald-50 text-emerald-600'
        : 'bg-gradient-to-br from-red-100 to-red-50 text-red-600',
      valueClass: netPositive ? 'text-emerald-700' : 'text-red-700',
    },
    {
      key: 'sales',
      label: 'Sales Count',
      value: String(summary.totalSalesCount),
      sub: 'completed invoices',
      iconKey: 'invoices',
      cardClass: 'border-violet-100 bg-gradient-to-br from-violet-50 via-white to-white',
      iconWrapClass: 'bg-gradient-to-br from-violet-100 to-violet-50 text-violet-600',
      valueClass: 'text-violet-700',
    },
  ] : [];

  return (
    <ReportAttractiveSummary
      loading={loading}
      columnsClassName="sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6"
      hero={{
        title: 'Period Profit & Loss Overview',
        value: summary ? fmt(summary.totalNetProfit) : '—',
        subtitle: summary
          ? `${fmt(summary.totalRevenue)} revenue · ${fmt(summary.totalExpenses)} expenses · ${summary.totalSalesCount} sales`
          : 'Loading period totals…',
        badgeLabel: 'Net Margin',
        badgeValue: `${marginPct}%`,
        badgeIconKey: 'reports',
        gradientClass: netPositive
          ? 'border-emerald-200 bg-gradient-to-r from-emerald-600 via-teal-600 to-cyan-600'
          : 'border-rose-200 bg-gradient-to-r from-rose-600 via-red-600 to-orange-600',
      }}
      cards={cards}
    />
  );
}
