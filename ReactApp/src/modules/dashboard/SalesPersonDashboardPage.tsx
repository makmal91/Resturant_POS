import React, { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import Badge from '../../components/Badge';
import { useBranchWriteAccess } from '../../hooks/useBranchWriteAccess';
import { useHasPermission } from '../../hooks/usePermission';
import { getApiErrorMessage } from '../../services/api';
import { hasBranchContext } from '../../types/permissions';
import {
  dashboardService,
  type SalesPersonSummaryDto,
} from './dashboardService';

const fmt = (n: number) =>
  new Intl.NumberFormat(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(n);

const formatDate = (value: string) => {
  if (!value) return '—';
  const d = new Date(value);
  return Number.isNaN(d.getTime())
    ? '—'
    : d.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
};

const formatDateTime = (value: string) => {
  if (!value) return '—';
  const d = new Date(value);
  return Number.isNaN(d.getTime())
    ? '—'
    : d.toLocaleString(undefined, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
};

const REFRESH_MS = 60_000;

function KpiCard({
  label,
  value,
  sub,
  icon,
  accent = 'text-gray-800',
}: {
  label: string;
  value: string;
  sub?: string;
  icon: string;
  accent?: string;
}) {
  return (
    <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5 flex items-start gap-4">
      <div className="flex-shrink-0 w-11 h-11 rounded-xl bg-gradient-to-br from-emerald-50 to-teal-50 flex items-center justify-center text-xl">
        {icon}
      </div>
      <div className="min-w-0 flex-1">
        <p className="text-xs text-gray-500 font-medium uppercase tracking-wide">{label}</p>
        <p className={`text-xl font-bold mt-1 ${accent}`}>{value}</p>
        {sub && <p className="text-xs text-gray-400 mt-1">{sub}</p>}
      </div>
    </div>
  );
}

function BarChart({
  data,
  valueKey,
  labelKey,
}: {
  data: Array<Record<string, unknown>>;
  valueKey: string;
  labelKey: string;
}) {
  if (data.length === 0) {
    return <p className="text-sm text-gray-400 text-center py-6">No sales data yet</p>;
  }

  const max = Math.max(...data.map((d) => Number(d[valueKey] ?? 0)), 1);

  return (
    <div className="flex items-end gap-1 h-36">
      {data.map((point, i) => {
        const val = Number(point[valueKey] ?? 0);
        const pct = (val / max) * 100;
        return (
          <div key={i} className="flex-1 flex flex-col items-center gap-1 min-w-0 group">
            <span className="text-[10px] text-gray-400 opacity-0 group-hover:opacity-100 transition-opacity truncate w-full text-center">
              {fmt(val)}
            </span>
            <div className="w-full flex items-end justify-center" style={{ height: '100px' }}>
              <div
                className="w-full max-w-[24px] rounded-t bg-emerald-500 opacity-80 hover:opacity-100"
                style={{ height: `${Math.max(pct, 2)}%` }}
              />
            </div>
            <span className="text-[9px] text-gray-400 truncate w-full text-center">
              {String(point[labelKey] ?? '')}
            </span>
          </div>
        );
      })}
    </div>
  );
}

function HorizontalBarList({
  items,
  labelKey,
  valueKey,
}: {
  items: Array<Record<string, unknown>>;
  labelKey: string;
  valueKey: string;
}) {
  if (items.length === 0) {
    return <p className="text-sm text-gray-400 text-center py-4">No products sold this month</p>;
  }

  const max = Math.max(...items.map((d) => Number(d[valueKey] ?? 0)), 1);

  return (
    <div className="space-y-3">
      {items.map((item, i) => {
        const val = Number(item[valueKey] ?? 0);
        const pct = (val / max) * 100;
        return (
          <div key={i}>
            <div className="flex justify-between text-xs mb-1">
              <span className="text-gray-700 font-medium truncate mr-2">{String(item[labelKey] ?? '')}</span>
              <span className="text-gray-500 flex-shrink-0">{fmt(val)}</span>
            </div>
            <div className="h-2 bg-gray-100 rounded-full overflow-hidden">
              <div className="h-full rounded-full bg-emerald-500" style={{ width: `${pct}%` }} />
            </div>
          </div>
        );
      })}
    </div>
  );
}

export default function SalesPersonDashboardPage() {
  const navigate = useNavigate();
  const { selectedBranchId } = useBranchWriteAccess();
  const hasBranch = hasBranchContext(selectedBranchId);
  const branchId = hasBranch && selectedBranchId !== null && selectedBranchId > 0 ? selectedBranchId : 0;

  const canPos    = useHasPermission('POS Billing', 'view');
  const canSales  = useHasPermission('Sales', 'view');

  const [data, setData] = useState<SalesPersonSummaryDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);

  const load = useCallback(async (silent = false) => {
    if (branchId <= 0) return;
    if (!silent) setLoading(true);
    setError(null);
    try {
      const res = await dashboardService.getMySalesSummary(branchId);
      setData(res.data);
      setLastUpdated(new Date());
    } catch (e) {
      setError(getApiErrorMessage(e, 'Failed to load your sales summary.'));
    } finally {
      if (!silent) setLoading(false);
    }
  }, [branchId]);

  useEffect(() => { void load(); }, [load]);

  useEffect(() => {
    if (branchId <= 0) return;
    const timer = setInterval(() => void load(true), REFRESH_MS);
    return () => clearInterval(timer);
  }, [branchId, load]);

  if (branchId <= 0) {
    return (
      <div className="flex items-center justify-center h-64 text-gray-500">
        Please select your branch to view your sales summary.
      </div>
    );
  }

  const kpis = data?.kpis;
  const trendChart = (data?.salesTrend ?? []).slice(-14).map((p) => ({
    date: formatDate(p.date),
    totalSales: p.totalSales,
  }));

  return (
    <div className="space-y-6 p-4 md:p-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">My Sales Summary</h1>
          <p className="text-sm text-gray-500 mt-0.5">
            {data?.fullName ?? 'Your performance'}
            {data?.branchName && <span className="text-gray-400"> · {data.branchName}</span>}
            {lastUpdated && (
              <span className="text-gray-400 ml-1">
                · Updated {lastUpdated.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })}
              </span>
            )}
          </p>
        </div>
        <div className="flex gap-2">
          {canPos && (
            <button
              type="button"
              onClick={() => navigate('/pos')}
              className="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium text-white bg-emerald-600 rounded-lg hover:bg-emerald-700 transition-colors"
            >
              Open POS
            </button>
          )}
          <button
            type="button"
            onClick={() => void load()}
            disabled={loading}
            className="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium text-emerald-700 bg-emerald-50 rounded-lg hover:bg-emerald-100 disabled:opacity-50"
          >
            {loading ? 'Refreshing…' : '↻ Refresh'}
          </button>
        </div>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">{error}</div>
      )}

      {loading && !data ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
          {[...Array(4)].map((_, i) => (
            <div key={i} className="bg-white rounded-xl border border-gray-100 p-5 h-24 animate-pulse" />
          ))}
        </div>
      ) : kpis ? (
        <>
          <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
            <KpiCard
              label="Today's Sales"
              value={fmt(kpis.todaySales)}
              sub={`${kpis.todayInvoices} invoice${kpis.todayInvoices === 1 ? '' : 's'}`}
              icon="📈"
              accent="text-emerald-700"
            />
            <KpiCard
              label="This Month"
              value={fmt(kpis.monthlySales)}
              sub={`${kpis.monthlyInvoices} invoice${kpis.monthlyInvoices === 1 ? '' : 's'}`}
              icon="📊"
              accent="text-blue-700"
            />
            <KpiCard
              label="Average Sale"
              value={fmt(kpis.averageSale)}
              sub="This month"
              icon="🧾"
            />
            <KpiCard
              label="Today's Collection"
              value={fmt(kpis.todayCash + kpis.todayCard)}
              sub={`Cash ${fmt(kpis.todayCash)} · Card ${fmt(kpis.todayCard)}`}
              icon="💳"
            />
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            <div className="lg:col-span-2 bg-white rounded-xl shadow-sm border border-gray-100 p-5">
              <h2 className="text-sm font-semibold text-gray-800 mb-4">My Sales Trend (14 days)</h2>
              <BarChart data={trendChart} valueKey="totalSales" labelKey="date" />
            </div>

            <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5">
              <h2 className="text-sm font-semibold text-gray-800 mb-4">Payment Status (This Month)</h2>
              <div className="flex gap-2 mb-4">
                <Badge variant="success">{kpis.paidCount} Paid</Badge>
                <Badge variant="warning">{kpis.pendingPaymentCount} Pending</Badge>
              </div>
              {data?.payment && (
                <div className="space-y-2 text-sm">
                  <div className="flex justify-between py-2 border-b border-gray-50">
                    <span className="text-gray-600">Cash sales</span>
                    <span className="font-medium">{fmt(data.payment.totalCash)} ({data.payment.cashInvoices})</span>
                  </div>
                  <div className="flex justify-between py-2 border-b border-gray-50">
                    <span className="text-gray-600">Card sales</span>
                    <span className="font-medium">{fmt(data.payment.totalCard)} ({data.payment.cardInvoices})</span>
                  </div>
                  <div className="flex justify-between py-2">
                    <span className="text-gray-600">Mixed sales</span>
                    <span className="font-medium">{fmt(data.payment.totalMixed)} ({data.payment.mixedInvoices})</span>
                  </div>
                </div>
              )}
            </div>
          </div>

          <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
            <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-sm font-semibold text-gray-800">My Recent Sales</h2>
                {canSales && (
                  <button
                    type="button"
                    onClick={() => navigate('/sales-invoices')}
                    className="text-xs text-emerald-600 hover:text-emerald-800 font-medium"
                  >
                    View all →
                  </button>
                )}
              </div>
              <div className="space-y-2">
                {(data?.recentSales ?? []).map((s) => (
                  <div key={s.id} className="flex items-center justify-between text-sm border-b border-gray-50 pb-2 last:border-0">
                    <div>
                      <span className="font-medium text-gray-800">{s.invoiceNo}</span>
                      <span className="text-gray-400 text-xs ml-2">{formatDateTime(s.saleDate)}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <span className="font-medium">{fmt(s.grandTotal)}</span>
                      <Badge variant={s.paymentStatus === 'Paid' ? 'success' : 'warning'}>
                        {s.paymentStatus}
                      </Badge>
                    </div>
                  </div>
                ))}
                {(data?.recentSales ?? []).length === 0 && (
                  <p className="text-sm text-gray-400 text-center py-4">No sales recorded yet</p>
                )}
              </div>
            </div>

            <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5">
              <h2 className="text-sm font-semibold text-gray-800 mb-4">My Top Products (This Month)</h2>
              <HorizontalBarList
                items={(data?.topProducts ?? []).map((p) => ({
                  productName: p.productName,
                  totalAmount: p.totalAmount,
                }))}
                labelKey="productName"
                valueKey="totalAmount"
              />
            </div>
          </div>
        </>
      ) : null}
    </div>
  );
}
