import React, { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import Badge from '../../components/Badge';
import MenuIcon, { type MenuIconKey } from '../../components/MenuIcon';
import { useBranchWriteAccess } from '../../hooks/useBranchWriteAccess';
import { usePermission } from '../../hooks/usePermission';
import { useHasFeature } from '../../hooks/useFeature';
import { FEATURE_KEYS } from '../../types/featurePermissions';
import { getApiErrorMessage } from '../../services/api';
import { hasBranchContext } from '../../types/permissions';
import {
  dashboardService,
  type DashboardOverviewDto,
  type SalesTrendPointDto,
  type ProfitTrendPointDto,
} from './dashboardService';

// ─── Helpers ──────────────────────────────────────────────────────────────────

const fmt = (n: number) =>
  new Intl.NumberFormat(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(n);

const fmtInt = (n: number) => new Intl.NumberFormat(undefined).format(n);

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

// ─── Sub-components ─────────────────────────────────────────────────────────────

function KpiCard({
  label,
  value,
  sub,
  iconKey,
  accent = 'text-gray-800',
}: {
  label: string;
  value: string;
  sub?: string;
  iconKey: MenuIconKey;
  accent?: string;
}) {
  return (
    <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5 flex items-start gap-4 hover:shadow-md transition-shadow">
      <div className="flex-shrink-0 w-11 h-11 rounded-xl bg-gradient-to-br from-blue-50 to-indigo-50 flex items-center justify-center text-indigo-600">
        <MenuIcon iconKey={iconKey} className="w-5 h-5" />
      </div>
      <div className="min-w-0 flex-1">
        <p className="text-xs text-gray-500 font-medium uppercase tracking-wide truncate">{label}</p>
        <p className={`text-xl font-bold mt-1 truncate ${accent}`}>{value}</p>
        {sub && <p className="text-xs text-gray-400 mt-1">{sub}</p>}
      </div>
    </div>
  );
}

function SectionCard({
  title,
  children,
  action,
}: {
  title: string;
  children: React.ReactNode;
  action?: React.ReactNode;
}) {
  return (
    <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
      <div className="px-5 py-4 border-b border-gray-50 flex items-center justify-between">
        <h2 className="text-sm font-semibold text-gray-800">{title}</h2>
        {action}
      </div>
      <div className="p-5">{children}</div>
    </div>
  );
}

function BarChart({
  data,
  valueKey,
  labelKey,
  color = 'bg-blue-500',
  formatValue = fmt,
}: {
  data: Array<Record<string, unknown>>;
  valueKey: string;
  labelKey: string;
  color?: string;
  formatValue?: (n: number) => string;
}) {
  if (data.length === 0) {
    return <p className="text-sm text-gray-400 text-center py-6">No data available</p>;
  }

  const max = Math.max(...data.map((d) => Number(d[valueKey] ?? 0)), 1);

  return (
    <div className="flex items-end gap-1 h-40">
      {data.map((point, i) => {
        const val = Number(point[valueKey] ?? 0);
        const pct = (val / max) * 100;
        const label = String(point[labelKey] ?? '');
        return (
          <div key={i} className="flex-1 flex flex-col items-center gap-1 min-w-0 group">
            <span className="text-[10px] text-gray-400 opacity-0 group-hover:opacity-100 transition-opacity truncate w-full text-center">
              {formatValue(val)}
            </span>
            <div className="w-full flex items-end justify-center" style={{ height: '120px' }}>
              <div
                className={`w-full max-w-[28px] rounded-t ${color} opacity-80 hover:opacity-100 transition-opacity`}
                style={{ height: `${Math.max(pct, 2)}%` }}
                title={`${label}: ${formatValue(val)}`}
              />
            </div>
            <span className="text-[9px] text-gray-400 truncate w-full text-center">{label}</span>
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
  color = 'bg-indigo-500',
}: {
  items: Array<Record<string, unknown>>;
  labelKey: string;
  valueKey: string;
  color?: string;
}) {
  if (items.length === 0) {
    return <p className="text-sm text-gray-400 text-center py-4">No data available</p>;
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
              <div className={`h-full rounded-full ${color}`} style={{ width: `${pct}%` }} />
            </div>
          </div>
        );
      })}
    </div>
  );
}

function QuickAction({
  label,
  iconKey,
  onClick,
  disabled,
}: {
  label: string;
  iconKey: MenuIconKey;
  onClick: () => void;
  disabled?: boolean;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      className="flex flex-col items-center gap-2 p-4 rounded-xl border border-gray-100 bg-gray-50 hover:bg-white hover:border-blue-200 hover:shadow-sm transition-all disabled:opacity-40 disabled:cursor-not-allowed"
    >
      <MenuIcon iconKey={iconKey} className="w-6 h-6 text-gray-600" />
      <span className="text-xs font-medium text-gray-700 text-center">{label}</span>
    </button>
  );
}

// ─── Main Page ──────────────────────────────────────────────────────────────────

export default function AdminDashboardPage() {
  const navigate = useNavigate();
  const { selectedBranchId, isGlobalMode } = useBranchWriteAccess();
  const hasBranch = hasBranchContext(selectedBranchId);
  const branchId = hasBranch && selectedBranchId !== null ? selectedBranchId : 0;

  const permBranches  = usePermission('Branches');
  const permUsers     = usePermission('Users');
  const permProducts  = usePermission('Products');
  const permRoles     = usePermission('Roles');
  const permPurchase  = usePermission('Purchase');
  const permExpenses  = usePermission('Expenses');
  const stockFeatureEnabled = useHasFeature(FEATURE_KEYS.STOCK);

  const [data, setData] = useState<DashboardOverviewDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);

  const load = useCallback(async (silent = false) => {
    if (!hasBranch) return;
    if (!silent) setLoading(true);
    setError(null);
    try {
      const res = await dashboardService.getOverview(branchId);
      setData(res.data);
      setLastUpdated(new Date());
    } catch (e) {
      setError(getApiErrorMessage(e, 'Failed to load dashboard.'));
    } finally {
      if (!silent) setLoading(false);
    }
  }, [branchId, hasBranch]);

  useEffect(() => { void load(); }, [load]);

  useEffect(() => {
    if (!hasBranch) return;
    const timer = setInterval(() => void load(true), REFRESH_MS);
    return () => clearInterval(timer);
  }, [hasBranch, load]);

  if (!hasBranch) {
    return (
      <div className="flex items-center justify-center h-64 text-gray-500">
        Please select a branch from the header to view the control panel.
      </div>
    );
  }

  const kpis = data?.kpis;
  const salesTrend: SalesTrendPointDto[] = data?.charts.salesTrend ?? [];
  const profitTrend: ProfitTrendPointDto[] = data?.charts.profitTrend ?? [];

  const salesTrendChart = salesTrend.slice(-14).map((p) => ({
    date: formatDate(p.date),
    totalSales: p.totalSales,
  }));

  const profitTrendChart = profitTrend.slice(-14).map((p) => ({
    date: formatDate(p.date),
    grossProfit: p.grossProfit,
  }));

  return (
    <div className="space-y-6 p-4 md:p-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Control Panel</h1>
          <p className="text-sm text-gray-500 mt-0.5">
            {data?.branchName ?? (isGlobalMode ? 'All Branches' : 'Loading…')}
            {lastUpdated && (
              <span className="ml-2 text-gray-400">
                · Updated {lastUpdated.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })}
              </span>
            )}
          </p>
        </div>
        <button
          type="button"
          onClick={() => void load()}
          disabled={loading}
          className="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium text-blue-700 bg-blue-50 rounded-lg hover:bg-blue-100 disabled:opacity-50 transition-colors"
        >
          {loading ? 'Refreshing…' : '↻ Refresh'}
        </button>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">
          {error}
        </div>
      )}

      {/* 1. KPI Overview */}
      <div>
        <h2 className="text-xs font-semibold text-gray-400 uppercase tracking-wider mb-3">KPI Overview</h2>
        {loading && !data ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
            {[...Array(8)].map((_, i) => (
              <div key={i} className="bg-white rounded-xl border border-gray-100 p-5 h-24 animate-pulse" />
            ))}
          </div>
        ) : kpis ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
            <KpiCard label="Total Branches" value={fmtInt(kpis.totalBranches)} iconKey="branches" />
            <KpiCard label="Total Users" value={fmtInt(kpis.totalUsers)} iconKey="users" />
            <KpiCard
              label="Today's Sales"
              value={fmt(kpis.todaySales)}
              sub={`${kpis.todayInvoices} invoices`}
              iconKey="sales"
              accent="text-emerald-700"
            />
            <KpiCard
              label="Monthly Sales"
              value={fmt(kpis.monthlySales)}
              sub={`${kpis.monthlyInvoices} invoices`}
              iconKey="reports"
              accent="text-blue-700"
            />
            <KpiCard label="Gross Profit" value={fmt(kpis.grossProfit)} iconKey="cashflow" accent="text-emerald-600" />
            <KpiCard label="Net Profit" value={fmt(kpis.netProfit)} iconKey="expenses" accent="text-indigo-700" />
            {stockFeatureEnabled && (
              <>
                <KpiCard label="Stock Value" value={fmt(kpis.stockValue)} iconKey="stock" />
                <KpiCard
                  label="Stock Alerts"
                  value={`${kpis.lowStockCount} low / ${kpis.outOfStockCount} out`}
                  iconKey="alert"
                  accent={kpis.outOfStockCount > 0 ? 'text-red-600' : 'text-amber-600'}
                />
              </>
            )}
          </div>
        ) : null}
      </div>

      {/* Charts row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <SectionCard title="Sales Trend (14 days)">
          <BarChart data={salesTrendChart} valueKey="totalSales" labelKey="date" color="bg-emerald-500" />
        </SectionCard>
        <SectionCard title="Profit Trend (14 days)">
          <BarChart data={profitTrendChart} valueKey="grossProfit" labelKey="date" color="bg-indigo-500" />
        </SectionCard>
      </div>

      {/* Branch Analytics + Financial */}
      <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
        <SectionCard title="Branch Analytics (This Month)">
          {data?.branchAnalytics.length ? (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-left text-xs text-gray-400 border-b border-gray-50">
                    <th className="pb-2 font-medium">Branch</th>
                    <th className="pb-2 font-medium text-right">Sales</th>
                    <th className="pb-2 font-medium text-right">Gross</th>
                    <th className="pb-2 font-medium text-right">Net</th>
                  </tr>
                </thead>
                <tbody>
                  {data.branchAnalytics.map((b) => (
                    <tr key={b.branchId} className="border-b border-gray-50 last:border-0">
                      <td className="py-2.5 font-medium text-gray-800">{b.branchName}</td>
                      <td className="py-2.5 text-right text-gray-600">{fmt(b.totalSales)}</td>
                      <td className="py-2.5 text-right text-emerald-600">{fmt(b.grossProfit)}</td>
                      <td className="py-2.5 text-right text-indigo-600">{fmt(b.netProfit)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <p className="text-sm text-gray-400 text-center py-4">No branch data for this period</p>
          )}
        </SectionCard>

        <SectionCard title="Financial Summary (This Month)">
          {data?.financial ? (
            <div className="space-y-3">
              {[
                { label: 'Total Sales', value: data.financial.totalSales, color: 'text-emerald-700' },
                { label: 'Total Purchases', value: data.financial.totalPurchases, color: 'text-orange-600' },
                { label: 'Gross Profit', value: data.financial.grossProfit, color: 'text-blue-700' },
                { label: 'Expenses', value: data.financial.totalExpenses, color: 'text-red-600' },
                { label: 'Net Profit', value: data.financial.netProfit, color: 'text-indigo-700' },
                { label: 'Receivables', value: data.financial.totalReceivables, color: 'text-sky-700' },
                { label: 'Payables', value: data.financial.totalPayables, color: 'text-amber-700' },
              ].map((row) => (
                <div key={row.label} className="flex justify-between items-center py-2 border-b border-gray-50 last:border-0">
                  <span className="text-sm text-gray-600">{row.label}</span>
                  <span className={`text-sm font-semibold ${row.color}`}>{fmt(row.value)}</span>
                </div>
              ))}
              {data.financial.dailyCashFlow.length > 0 && (
                <div className="pt-2">
                  <p className="text-xs text-gray-400 mb-2">Daily Cash Flow (last 30 days)</p>
                  <BarChart
                    data={data.financial.dailyCashFlow.slice(-7).map((d) => ({
                      date: formatDate(d.date),
                      netFlow: d.netFlow,
                    }))}
                    valueKey="netFlow"
                    labelKey="date"
                    color="bg-teal-500"
                  />
                </div>
              )}
            </div>
          ) : null}
        </SectionCard>
      </div>

      {/* Stock + Charts products/categories */}
      <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
        {stockFeatureEnabled && (
        <SectionCard title="Stock Overview">
          {data?.stock ? (
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-3">
                <div className="bg-gray-50 rounded-lg p-3 text-center">
                  <p className="text-xs text-gray-400">Products</p>
                  <p className="text-lg font-bold text-gray-800">{fmtInt(data.stock.totalProducts)}</p>
                </div>
                <div className="bg-gray-50 rounded-lg p-3 text-center">
                  <p className="text-xs text-gray-400">Variants</p>
                  <p className="text-lg font-bold text-gray-800">{fmtInt(data.stock.totalVariants)}</p>
                </div>
                <div className="bg-gray-50 rounded-lg p-3 text-center">
                  <p className="text-xs text-gray-400">Total Qty</p>
                  <p className="text-lg font-bold text-gray-800">{fmt(data.stock.totalQuantity)}</p>
                </div>
                <div className="bg-gray-50 rounded-lg p-3 text-center">
                  <p className="text-xs text-gray-400">Stock Value</p>
                  <p className="text-lg font-bold text-emerald-700">{fmt(data.stock.totalStockValue)}</p>
                </div>
              </div>

              {data.stock.lowStockItems.length > 0 && (
                <div>
                  <p className="text-xs font-medium text-amber-600 mb-2">Low Stock ({data.stock.lowStockCount})</p>
                  <div className="space-y-1.5">
                    {data.stock.lowStockItems.slice(0, 5).map((item, i) => (
                      <div key={i} className="flex justify-between text-xs">
                        <span className="text-gray-700 truncate mr-2">
                          {item.productName}{item.variantName ? ` (${item.variantName})` : ''}
                        </span>
                        <span className="text-amber-600 font-medium flex-shrink-0">{fmt(item.quantity)}</span>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {data.stock.warehouseDistribution.length > 0 && (
                <div>
                  <p className="text-xs font-medium text-gray-500 mb-2">By Warehouse</p>
                  <HorizontalBarList
                    items={data.stock.warehouseDistribution.map((w) => ({
                      warehouseName: w.warehouseName,
                      totalValue: w.totalValue,
                    }))}
                    labelKey="warehouseName"
                    valueKey="totalValue"
                    color="bg-blue-400"
                  />
                </div>
              )}
            </div>
          ) : null}
        </SectionCard>
        )}

        <SectionCard title="Top Products">
          <HorizontalBarList
            items={(data?.charts.topProducts ?? []).map((p) => ({
              productName: p.productName,
              totalAmount: p.totalAmount,
            }))}
            labelKey="productName"
            valueKey="totalAmount"
            color="bg-emerald-500"
          />
        </SectionCard>

        <SectionCard title="Category Performance">
          <HorizontalBarList
            items={(data?.charts.categoryPerformance ?? []).map((c) => ({
              categoryName: c.categoryName,
              totalSales: c.totalSales,
            }))}
            labelKey="categoryName"
            valueKey="totalSales"
            color="bg-violet-500"
          />
        </SectionCard>
      </div>

      {/* User Activity + Recent Transactions */}
      <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
        <SectionCard title="User Activity">
          {data?.userActivity ? (
            <div className="space-y-5">
              <div>
                <p className="text-xs font-medium text-gray-400 mb-2">Recent Users</p>
                <div className="space-y-2">
                  {data.userActivity.recentUsers.map((u) => (
                    <div key={u.userId} className="flex items-center justify-between text-sm">
                      <div>
                        <span className="font-medium text-gray-800">{u.fullName}</span>
                        <span className="text-gray-400 ml-2 text-xs">{u.roleName}</span>
                      </div>
                      <Badge variant={u.isActive ? 'success' : 'danger'}>{u.isActive ? 'Active' : 'Inactive'}</Badge>
                    </div>
                  ))}
                  {data.userActivity.recentUsers.length === 0 && (
                    <p className="text-sm text-gray-400">No users found</p>
                  )}
                </div>
              </div>

              <div>
                <p className="text-xs font-medium text-gray-400 mb-2">Sales by User (This Month)</p>
                <HorizontalBarList
                  items={data.userActivity.salesByUsers.map((s) => ({
                    cashierName: s.cashierName,
                    totalSales: s.totalSales,
                  }))}
                  labelKey="cashierName"
                  valueKey="totalSales"
                  color="bg-sky-500"
                />
              </div>

              <div>
                <p className="text-xs font-medium text-gray-400 mb-2">Activity Log</p>
                <div className="space-y-2 max-h-40 overflow-y-auto">
                  {data.userActivity.activityLogs.map((log, i) => (
                    <div key={i} className="flex justify-between text-xs border-b border-gray-50 pb-1.5">
                      <div>
                        <span className="font-medium text-gray-700">{log.type}</span>
                        <span className="text-gray-400 ml-1">{log.reference}</span>
                        <span className="text-gray-300 ml-1">· {log.branchName}</span>
                      </div>
                      <span className="text-gray-400 flex-shrink-0">{formatDateTime(log.timestamp)}</span>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          ) : null}
        </SectionCard>

        <SectionCard
          title="Recent Transactions"
          action={
            data?.recentTransactions ? (
              <div className="flex gap-2 text-xs">
                <Badge variant="success">{data.recentTransactions.paidCount} Paid</Badge>
                <Badge variant="warning">{data.recentTransactions.pendingPaymentCount} Pending</Badge>
                {data.recentTransactions.returnCount > 0 && (
                  <Badge variant="danger">{data.recentTransactions.returnCount} Returns</Badge>
                )}
              </div>
            ) : undefined
          }
        >
          {data?.recentTransactions ? (
            <div className="space-y-4">
              <div>
                <p className="text-xs font-medium text-gray-400 mb-2">Latest Sales</p>
                <div className="space-y-2">
                  {data.recentTransactions.recentSales.map((s) => (
                    <div key={s.id} className="flex items-center justify-between text-sm border-b border-gray-50 pb-2">
                      <div>
                        <span className="font-medium text-gray-800">{s.invoiceNo}</span>
                        <span className="text-gray-400 text-xs ml-2">{s.branchName}</span>
                      </div>
                      <div className="flex items-center gap-2">
                        <span className="font-medium">{fmt(s.grandTotal)}</span>
                        <Badge variant={s.paymentStatus === 'Paid' ? 'success' : 'warning'}>
                          {s.paymentStatus}
                        </Badge>
                      </div>
                    </div>
                  ))}
                  {data.recentTransactions.recentSales.length === 0 && (
                    <p className="text-sm text-gray-400">No recent sales</p>
                  )}
                </div>
              </div>

              <div>
                <p className="text-xs font-medium text-gray-400 mb-2">Latest Purchases</p>
                <div className="space-y-2">
                  {data.recentTransactions.recentPurchases.map((p) => (
                    <div key={p.id} className="flex items-center justify-between text-sm border-b border-gray-50 pb-2">
                      <div>
                        <span className="font-medium text-gray-800">{p.invoiceNo}</span>
                        <span className="text-gray-400 text-xs ml-2">{p.supplierName}</span>
                      </div>
                      <span className="font-medium">{fmt(p.totalAmount)}</span>
                    </div>
                  ))}
                  {data.recentTransactions.recentPurchases.length === 0 && (
                    <p className="text-sm text-gray-400">No recent purchases</p>
                  )}
                </div>
              </div>
            </div>
          ) : null}
        </SectionCard>
      </div>

      {/* Quick Actions */}
      <SectionCard title="Quick Actions">
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-6 gap-3">
          <QuickAction label="Add Branch" iconKey="branches" onClick={() => navigate('/branches')} disabled={!permBranches.canCreate} />
          <QuickAction label="Add User" iconKey="users" onClick={() => navigate('/users')} disabled={!permUsers.canCreate} />
          <QuickAction label="Add Product" iconKey="products" onClick={() => navigate('/products')} disabled={!permProducts.canCreate} />
          <QuickAction label="Manage Roles" iconKey="roles" onClick={() => navigate('/roles')} disabled={!permRoles.canView} />
          <QuickAction label="Add Purchase" iconKey="purchase" onClick={() => navigate('/purchase')} disabled={!permPurchase.canCreate} />
          <QuickAction label="Add Expense" iconKey="expenses" onClick={() => navigate('/expenses')} disabled={!permExpenses.canCreate} />
        </div>
      </SectionCard>
    </div>
  );
}
