import React from 'react';
import { fmt, formatDate } from './reportFormatters';
import { formatPeriodLabel } from './reportPeriodUtils';
import type { ProfitLossExpenseLine, ProfitLossStatement } from './reportService';

interface ProfitLossStatementViewProps {
  statement: ProfitLossStatement | null;
  loading?: boolean;
  periodLabel?: string;
}

function LineRow({
  label,
  amount,
  bold = false,
  indent = false,
  negative = false,
  highlight = false,
}: {
  label: string;
  amount: number;
  bold?: boolean;
  indent?: boolean;
  negative?: boolean;
  highlight?: boolean;
}) {
  const display = negative && amount > 0 ? `(${fmt(amount)})` : fmt(amount);
  const amountClass = highlight
    ? amount >= 0 ? 'text-emerald-700' : 'text-red-600'
    : 'text-gray-900';

  return (
    <tr className={bold ? 'border-t border-gray-200 bg-gray-50' : ''}>
      <td className={`py-2.5 pr-4 text-sm ${indent ? 'pl-6' : 'pl-4'} ${bold ? 'font-semibold text-gray-900' : 'text-gray-700'}`}>
        {label}
      </td>
      <td className={`py-2.5 pr-4 text-right text-sm tabular-nums ${bold ? 'font-semibold' : ''} ${amountClass}`}>
        {display}
      </td>
    </tr>
  );
}

export default function ProfitLossStatementView({
  statement,
  loading = false,
  periodLabel,
}: ProfitLossStatementViewProps) {
  if (loading && !statement) {
    return (
      <div className="rounded-xl border border-gray-100 bg-white p-8 text-center text-sm text-gray-500 shadow-sm">
        Loading profit &amp; loss statement…
      </div>
    );
  }

  if (!statement) {
    return (
      <div className="rounded-xl border border-gray-100 bg-white p-8 text-center text-sm text-gray-500 shadow-sm">
        No profit &amp; loss data for this period.
      </div>
    );
  }

  const { summary, expenseLines, branchName } = statement;
  const label = periodLabel ?? formatPeriodLabel(statement.fromDate, statement.toDate);
  const netPositive = summary.totalNetProfit >= 0;
  const salesRevenue = summary.totalRevenue - (summary.stockAdjustmentGain ?? 0);

  return (
    <div className="profit-loss-statement rounded-xl border border-gray-200 bg-white shadow-sm">
      <div className="border-b border-gray-100 px-6 py-5 text-center print:py-4">
        <p className="text-xs font-semibold uppercase tracking-widest text-gray-500">Profit &amp; Loss Statement</p>
        <h2 className="mt-1 text-xl font-bold text-gray-900">{branchName}</h2>
        <p className="mt-1 text-sm text-gray-600">{label}</p>
        <p className="mt-0.5 text-xs text-gray-400">
          Generated {formatDate(new Date().toISOString())}
        </p>
      </div>

      <div className="overflow-x-auto px-2 py-2 sm:px-4">
        <table className="w-full min-w-[320px] border-collapse">
          <tbody>
            <tr>
              <td colSpan={2} className="px-4 pb-1 pt-3 text-xs font-bold uppercase tracking-wide text-gray-500">
                Income
              </td>
            </tr>
            <LineRow label="Sales Revenue" amount={salesRevenue} indent />
            {(summary.stockAdjustmentGain ?? 0) > 0 && (
              <LineRow label="Stock Adjustment (Gain)" amount={summary.stockAdjustmentGain ?? 0} indent />
            )}
            {summary.totalDiscounts > 0 && (
              <LineRow label="Less: Discounts" amount={summary.totalDiscounts} indent negative />
            )}
            {summary.totalTax > 0 && (
              <LineRow label="Tax Collected" amount={summary.totalTax} indent />
            )}
            <LineRow label="Net Revenue" amount={summary.totalRevenue} bold />

            <tr>
              <td colSpan={2} className="px-4 pb-1 pt-4 text-xs font-bold uppercase tracking-wide text-gray-500">
                Cost of Goods Sold
              </td>
            </tr>
            <LineRow label="Cost of Goods Sold" amount={summary.totalCostOfGoodsSold} indent negative />
            <LineRow label="Gross Profit" amount={summary.totalGrossProfit} bold highlight />

            <tr>
              <td colSpan={2} className="px-4 pb-1 pt-4 text-xs font-bold uppercase tracking-wide text-gray-500">
                Operating Expenses
              </td>
            </tr>
            {expenseLines.length === 0 ? (
              <LineRow label="No expenses recorded" amount={0} indent />
            ) : (
              expenseLines.map((line: ProfitLossExpenseLine) => (
                <LineRow
                  key={line.categoryId}
                  label={line.categoryName}
                  amount={line.amount}
                  indent
                  negative
                />
              ))
            )}
            <LineRow label="Total Expenses" amount={summary.totalExpenses} bold negative />

            <LineRow
              label={netPositive ? 'Net Profit' : 'Net Loss'}
              amount={Math.abs(summary.totalNetProfit)}
              bold
              highlight
            />
          </tbody>
        </table>
      </div>

      <div className="border-t border-gray-100 px-6 py-3 text-xs text-gray-500 print:py-2">
        {summary.totalSalesCount} completed sale{summary.totalSalesCount === 1 ? '' : 's'} in period
        {summary.totalRevenue > 0 && (
          <> · Net margin {((summary.totalNetProfit / summary.totalRevenue) * 100).toFixed(1)}%</>
        )}
      </div>
    </div>
  );
}
