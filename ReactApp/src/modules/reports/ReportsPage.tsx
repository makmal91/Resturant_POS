import React, { useCallback, useEffect, useMemo, useState } from 'react';
import DataTable, { type Column } from '../../components/DataTable';
import Badge from '../../components/Badge';
import { useBranchWriteAccess } from '../../hooks/useBranchWriteAccess';
import { hasBranchContext } from '../../types/permissions';
import { getApiErrorMessage } from '../../services/api';
import { safeString } from '../../utils/safeValues';
import { warehouseService, type WarehouseItem } from '../warehouse/warehouseService';
import {
  reportService,
  type SalesByProductRow,
  type SalesSummaryDto,
  type StockMovementResponse,
  type StockSummaryItem,
  type StockSummaryResponse,
} from './reportService';

// ─── Helpers ──────────────────────────────────────────────────────────────────

type ReportTab = 'sales' | 'stock';

const fmt = (n: number) =>
  new Intl.NumberFormat(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(n);

const fmtQty = (n: number) => {
  const abs = Math.abs(n);
  const s = abs % 1 === 0 ? abs.toFixed(0) : abs.toFixed(4).replace(/\.?0+$/, '');
  return n < 0 ? `−${s}` : s;
};

const formatDate = (value: string) => {
  if (!value) return '—';
  const d = new Date(value);
  return Number.isNaN(d.getTime())
    ? '—'
    : d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
};

const monthStart = () => {
  const d = new Date();
  return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().slice(0, 10);
};

const todayStr = () => new Date().toISOString().slice(0, 10);

const TYPE_LABEL: Record<string, string> = {
  PurchaseEntry:  'Purchase',
  SaleEntry:      'Sale',
  PurchaseReturn: 'Purchase Return',
  SaleReturn:     'Sale Return',
  Adjustment:     'Adjustment',
  TransferIn:     'Transfer In',
  TransferOut:    'Transfer Out',
};

// ─── Component ────────────────────────────────────────────────────────────────

const ReportsPage: React.FC = () => {
  const { selectedBranchId, isGlobalMode } = useBranchWriteAccess();
  const hasBranch = hasBranchContext(selectedBranchId);
  const branchId = hasBranch && selectedBranchId !== null ? selectedBranchId : 0;

  const [tab, setTab] = useState<ReportTab>('sales');
  const [fromDate, setFromDate] = useState(monthStart());
  const [toDate, setToDate] = useState(todayStr());
  const [warehouseId, setWarehouseId] = useState<number | ''>('');
  const [warehouses, setWarehouses] = useState<WarehouseItem[]>([]);

  const [loading, setLoading] = useState(false);
  const [notification, setNotification] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  // Sales state
  const [salesSummary, setSalesSummary] = useState<SalesSummaryDto | null>(null);
  const [salesProducts, setSalesProducts] = useState<SalesByProductRow[]>([]);
  const [salesPage, setSalesPage] = useState(1);
  const [salesPageSize, setSalesPageSize] = useState(10);
  const [salesSearch, setSalesSearch] = useState('');
  const [salesSortColumn, setSalesSortColumn] = useState('totalAmount');
  const [salesSortDirection, setSalesSortDirection] = useState<'asc' | 'desc'>('desc');
  const [salesTotal, setSalesTotal] = useState(0);
  const [salesTotalPages, setSalesTotalPages] = useState(0);

  // Stock state
  const [stockSummary, setStockSummary] = useState<StockSummaryResponse | null>(null);
  const [stockMovement, setStockMovement] = useState<StockMovementResponse | null>(null);
  const [stockPage, setStockPage] = useState(1);
  const [stockPageSize, setStockPageSize] = useState(25);
  const [stockSearch, setStockSearch] = useState('');
  const [stockSortColumn, setStockSortColumn] = useState('productName');
  const [stockSortDirection, setStockSortDirection] = useState<'asc' | 'desc'>('asc');

  const showNotification = useCallback((type: 'success' | 'error', message: string) => {
    setNotification({ type, message });
    setTimeout(() => setNotification(null), 4000);
  }, []);

  // Load warehouses for stock filter
  useEffect(() => {
    if (!hasBranch) {
      setWarehouses([]);
      return;
    }
    void warehouseService.getAll(branchId, 1, 100).then((res) => {
      const rows = Array.isArray(res.data?.warehouses) ? res.data.warehouses : [];
      setWarehouses(rows);
    }).catch(() => setWarehouses([]));
  }, [hasBranch, branchId]);

  const loadSalesReport = useCallback(async () => {
    if (branchId <= 0) return;
    setLoading(true);
    try {
      const [summaryRes, productsRes] = await Promise.all([
        reportService.getSalesSummary(branchId, { fromDate, toDate }),
        reportService.getSalesByProduct(branchId, {
          fromDate,
          toDate,
          page: salesPage,
          pageSize: salesPageSize,
          search: salesSearch.trim() || undefined,
          sortBy: salesSortColumn,
          sortDirection: salesSortDirection,
        }),
      ]);
      setSalesSummary(summaryRes.data);
      setSalesProducts(Array.isArray(productsRes.data?.products) ? productsRes.data.products : []);
      setSalesTotal(Number(productsRes.data?.totalRecords ?? 0));
      setSalesTotalPages(Number(productsRes.data?.totalPages ?? 0));
    } catch (err) {
      setSalesSummary(null);
      setSalesProducts([]);
      showNotification('error', getApiErrorMessage(err, 'Failed to load sales report.'));
    } finally {
      setLoading(false);
    }
  }, [branchId, fromDate, toDate, salesPage, salesPageSize, salesSearch, salesSortColumn, salesSortDirection, showNotification]);

  const loadStockReport = useCallback(async () => {
    if (branchId <= 0) return;
    setLoading(true);
    const wh = warehouseId === '' ? undefined : Number(warehouseId);

    try {
      const summaryRes = await reportService.getStockSummary(branchId, {
        warehouseId: wh,
        page: stockPage,
        pageSize: stockPageSize,
        search: stockSearch.trim() || undefined,
        sortBy: stockSortColumn,
        sortDirection: stockSortDirection,
      });
      setStockSummary(summaryRes.data);
    } catch (err) {
      setStockSummary(null);
      showNotification('error', getApiErrorMessage(err, 'Failed to load stock balances.'));
    }

    try {
      const movementRes = await reportService.getStockMovement(branchId, {
        fromDate,
        toDate,
        warehouseId: wh,
      });
      setStockMovement(movementRes.data);
    } catch (err) {
      setStockMovement(null);
      showNotification('error', getApiErrorMessage(err, 'Failed to load stock movement summary.'));
    }

    setLoading(false);
  }, [branchId, fromDate, toDate, warehouseId, stockPage, stockPageSize, stockSearch, stockSortColumn, stockSortDirection, showNotification]);

  useEffect(() => {
    if (tab === 'sales') void loadSalesReport();
    else void loadStockReport();
  }, [tab, loadSalesReport, loadStockReport]);

  useEffect(() => {
    setSalesPage(1);
    setStockPage(1);
  }, [branchId, fromDate, toDate, warehouseId, tab, salesSearch, stockSearch, salesSortColumn, salesSortDirection, stockSortColumn, stockSortDirection]);

  const handlePrint = () => window.print();

  const salesProductColumns: Column<SalesByProductRow>[] = useMemo(() => [
    { key: 'productCode', header: 'Code', sortable: true, render: (v) => <span className="font-mono text-xs">{safeString(v) || '—'}</span> },
    { key: 'productName', header: 'Product', sortable: true },
    { key: 'totalQuantity', header: 'Qty Sold', sortable: true, render: (v) => fmtQty(Number(v ?? 0)) },
    { key: 'invoiceCount', header: 'Invoices', sortable: true },
    { key: 'totalAmount', header: 'Revenue', sortable: true, render: (v) => <span className="font-semibold text-emerald-700">{fmt(Number(v ?? 0))}</span> },
  ], []);

  const stockColumns: Column<StockSummaryItem>[] = useMemo(() => [
    { key: 'productCode', header: 'Code', sortable: true, render: (v) => <span className="font-mono text-xs">{safeString(v) || '—'}</span> },
    { key: 'productName', header: 'Product', sortable: true },
    { key: 'variantName', header: 'Variant', sortable: true, render: (v) => safeString(v) || '—' },
    { key: 'warehouseName', header: 'Warehouse', sortable: true },
    {
      key: 'quantity',
      header: 'Qty',
      sortable: true,
      render: (v) => {
        const q = Number(v ?? 0);
        return (
          <Badge variant={q <= 0 ? 'danger' : q <= 5 ? 'warning' : 'success'} size="sm">
            {fmtQty(q)}
          </Badge>
        );
      },
    },
    { key: 'costPrice', header: 'Cost', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    { key: 'stockValue', header: 'Value', sortable: true, render: (v) => <span className="font-semibold">{fmt(Number(v ?? 0))}</span> },
  ], []);

  if (!hasBranch) {
    return (
      <div>
        <div className="mb-8">
          <h1 className="mb-2 text-3xl font-bold text-gray-900">Reports</h1>
          <p className="text-gray-600">Sales and stock analysis</p>
        </div>
        <div className="rounded-md border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-700">
          Select a branch from the header to generate reports.
        </div>
      </div>
    );
  }

  return (
    <div className="print-area">
      {notification && (
        <div className={`mb-6 flex items-center rounded-md p-4 print:hidden ${notification.type === 'success' ? 'bg-green-50 text-green-800' : 'bg-red-50 text-red-800'}`}>
          <span className="font-medium">{notification.message}</span>
        </div>
      )}

      {/* Header */}
      <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between print:mb-4">
        <div>
          <h1 className="mb-2 text-3xl font-bold text-gray-900">Reports</h1>
          <p className="text-gray-600">
            {tab === 'sales' ? 'Sales performance and product breakdown' : 'Stock balances and movement summary'}
            {salesSummary?.branchName && tab === 'sales' ? ` — ${salesSummary.branchName}` : ''}
          </p>
        </div>
        <button
          type="button"
          onClick={handlePrint}
          className="inline-flex items-center self-start rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 shadow-sm hover:bg-gray-50 print:hidden"
        >
          <svg className="mr-2 h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z" />
          </svg>
          Print Report
        </button>
      </div>

      {/* Tabs */}
      <div className="mb-6 flex gap-2 print:hidden">
        {(['sales', 'stock'] as ReportTab[]).map((t) => (
          <button
            key={t}
            type="button"
            onClick={() => setTab(t)}
            className={`rounded-lg px-5 py-2.5 text-sm font-medium transition-colors ${
              tab === t
                ? 'bg-blue-600 text-white shadow-sm'
                : 'bg-white text-gray-600 border border-gray-200 hover:bg-gray-50'
            }`}
          >
            {t === 'sales' ? 'Sales Report' : 'Stock Report'}
          </button>
        ))}
      </div>

      {/* Filters */}
      <div className="mb-6 grid grid-cols-1 gap-4 rounded-xl border border-gray-100 bg-white p-5 shadow-sm print:border-0 print:shadow-none sm:grid-cols-2 lg:grid-cols-4 print:hidden">
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">From Date</label>
          <input type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none" />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">To Date</label>
          <input type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none" />
        </div>
        {tab === 'stock' && (
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Warehouse</label>
            <select
              value={warehouseId}
              onChange={(e) => setWarehouseId(e.target.value === '' ? '' : Number(e.target.value))}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
            >
              <option value="">All Warehouses</option>
              {warehouses.map((w) => (
                <option key={w.id} value={w.id}>{w.name}</option>
              ))}
            </select>
          </div>
        )}
        <div className="flex items-end">
          <button
            type="button"
            onClick={() => (tab === 'sales' ? void loadSalesReport() : void loadStockReport())}
            disabled={loading}
            className="w-full rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-60"
          >
            {loading ? 'Loading…' : 'Generate Report'}
          </button>
        </div>
      </div>

      {/* Print-only date range */}
      <p className="mb-4 hidden text-sm text-gray-600 print:block">
        Period: {formatDate(fromDate)} — {formatDate(toDate)}
        {tab === 'stock' && warehouseId !== '' ? ` | Warehouse: ${warehouses.find((w) => w.id === warehouseId)?.name ?? warehouseId}` : ''}
      </p>

      {/* ─── SALES TAB ─── */}
      {tab === 'sales' && (
        <div className="space-y-6">
          {salesSummary && (
            <>
              <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
                {[
                  { label: 'Total Sales', value: fmt(salesSummary.totalSales), color: 'text-emerald-700 bg-emerald-50 border-emerald-100' },
                  { label: 'Invoices', value: String(salesSummary.totalInvoices), color: 'text-blue-700 bg-blue-50 border-blue-100' },
                  { label: 'Cash Received', value: fmt(salesSummary.totalCash), color: 'text-gray-800 bg-gray-50 border-gray-200' },
                  { label: 'Card Received', value: fmt(salesSummary.totalCard), color: 'text-indigo-700 bg-indigo-50 border-indigo-100' },
                ].map(({ label, value, color }) => (
                  <div key={label} className={`rounded-lg border p-4 ${color}`}>
                    <p className="text-xs font-medium uppercase tracking-wide opacity-70">{label}</p>
                    <p className="mt-1 text-xl font-bold">{value}</p>
                  </div>
                ))}
              </div>

              <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
                {[
                  { label: 'Discount Given', value: fmt(salesSummary.totalDiscount) },
                  { label: 'Tax Collected', value: fmt(salesSummary.totalTax) },
                  { label: 'Average Sale', value: fmt(salesSummary.averageSale) },
                  { label: 'Total Paid', value: fmt(salesSummary.totalPaid) },
                ].map(({ label, value }) => (
                  <div key={label} className="rounded-lg border border-gray-100 bg-white p-4">
                    <p className="text-xs font-medium text-gray-500">{label}</p>
                    <p className="mt-1 text-lg font-semibold text-gray-800">{value}</p>
                  </div>
                ))}
              </div>

              {salesSummary.dailyTrend.length > 0 && (
                <div className="overflow-hidden rounded-xl border border-gray-100 bg-white">
                  <div className="border-b border-gray-50 px-5 py-4">
                    <h2 className="text-base font-semibold text-gray-700">Daily Sales Trend</h2>
                  </div>
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="bg-gray-50 text-xs uppercase tracking-wide text-gray-600">
                          <th className="px-5 py-3 text-left">Date</th>
                          <th className="px-4 py-3 text-right">Invoices</th>
                          <th className="px-4 py-3 text-right">Total Sales</th>
                          <th className="px-4 py-3 text-right">Cash</th>
                          <th className="px-5 py-3 text-right">Card</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-gray-50">
                        {salesSummary.dailyTrend.map((d) => (
                          <tr key={d.date}>
                            <td className="px-5 py-2.5">{formatDate(d.date)}</td>
                            <td className="px-4 py-2.5 text-right">{d.invoiceCount}</td>
                            <td className="px-4 py-2.5 text-right font-medium text-emerald-700">{fmt(d.totalSales)}</td>
                            <td className="px-4 py-2.5 text-right">{fmt(d.cashSales)}</td>
                            <td className="px-5 py-2.5 text-right">{fmt(d.cardSales)}</td>
                          </tr>
                        ))}
                      </tbody>
                      <tfoot>
                        <tr className="border-t border-gray-200 bg-gray-50 font-bold text-gray-700">
                          <td className="px-5 py-3">Total</td>
                          <td className="px-4 py-3 text-right">{salesSummary.totalInvoices}</td>
                          <td className="px-4 py-3 text-right text-emerald-700">{fmt(salesSummary.totalSales)}</td>
                          <td className="px-4 py-3 text-right">{fmt(salesSummary.totalCash)}</td>
                          <td className="px-5 py-3 text-right">{fmt(salesSummary.totalCard)}</td>
                        </tr>
                      </tfoot>
                    </table>
                  </div>
                </div>
              )}
            </>
          )}

          <div>
            <h2 className="mb-4 text-base font-semibold text-gray-700">Sales by Product</h2>
            <DataTable
              data={salesProducts}
              columns={salesProductColumns}
              loading={loading}
              searchable
              searchPlaceholder="Search products..."
              pagination
              pageSize={salesPageSize}
              pageSizeOptions={[10, 25, 50]}
              onPageSizeChange={(s) => { setSalesPageSize(s); setSalesPage(1); }}
              emptyMessage="No sales found for this period."
              serverSide
              totalRecords={salesTotal}
              totalPages={salesTotalPages}
              currentPage={salesPage}
              onPageChange={setSalesPage}
              searchTerm={salesSearch}
              onSearchChange={(value) => { setSalesSearch(value); setSalesPage(1); }}
              sortColumn={salesSortColumn}
              sortDirection={salesSortDirection}
              onSortChange={(column, direction) => { setSalesSortColumn(column); setSalesSortDirection(direction); setSalesPage(1); }}
            />
          </div>
        </div>
      )}

      {/* ─── STOCK TAB ─── */}
      {tab === 'stock' && (
        <div className="space-y-6">
          {stockSummary && (
            <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
              {[
                { label: 'Stock Items', value: String(stockSummary.totalRecords), color: 'text-blue-700 bg-blue-50 border-blue-100' },
                { label: 'Total Quantity', value: fmtQty(stockSummary.totalQuantity), color: 'text-gray-800 bg-gray-50 border-gray-200' },
                { label: 'Stock Value', value: fmt(stockSummary.totalStockValue), color: 'text-emerald-700 bg-emerald-50 border-emerald-100' },
                { label: 'Low Stock (≤5)', value: String(stockSummary.lowStockCount), color: 'text-orange-700 bg-orange-50 border-orange-100' },
              ].map(({ label, value, color }) => (
                <div key={label} className={`rounded-lg border p-4 ${color}`}>
                  <p className="text-xs font-medium uppercase tracking-wide opacity-70">{label}</p>
                  <p className="mt-1 text-xl font-bold">{value}</p>
                </div>
              ))}
            </div>
          )}

          {stockMovement && stockMovement.byType.length > 0 && (
            <div className="overflow-hidden rounded-xl border border-gray-100 bg-white">
              <div className="border-b border-gray-50 px-5 py-4">
                <h2 className="text-base font-semibold text-gray-700">Stock Movement Summary</h2>
              </div>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="bg-gray-50 text-xs uppercase tracking-wide text-gray-600">
                      <th className="px-5 py-3 text-left">Type</th>
                      <th className="px-4 py-3 text-right">Entries</th>
                      <th className="px-4 py-3 text-right">Stock In</th>
                      <th className="px-4 py-3 text-right">Stock Out</th>
                      <th className="px-5 py-3 text-right">Amount</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-50">
                    {stockMovement.byType.map((row) => (
                      <tr key={row.type}>
                        <td className="px-5 py-2.5 font-medium">{TYPE_LABEL[row.type] ?? row.type}</td>
                        <td className="px-4 py-2.5 text-right">{row.entryCount}</td>
                        <td className="px-4 py-2.5 text-right text-emerald-600">{fmtQty(row.totalIn)}</td>
                        <td className="px-4 py-2.5 text-right text-red-500">{fmtQty(row.totalOut)}</td>
                        <td className="px-5 py-2.5 text-right">{fmt(row.totalAmount)}</td>
                      </tr>
                    ))}
                  </tbody>
                  <tfoot>
                    <tr className="border-t border-gray-200 bg-gray-50 font-bold text-gray-700">
                      <td className="px-5 py-3">Total</td>
                      <td className="px-4 py-3 text-right">{stockMovement.totalEntries}</td>
                      <td className="px-4 py-3 text-right text-emerald-700">{fmtQty(stockMovement.totalStockIn)}</td>
                      <td className="px-4 py-3 text-right text-red-600">{fmtQty(stockMovement.totalStockOut)}</td>
                      <td className="px-5 py-3 text-right">—</td>
                    </tr>
                  </tfoot>
                </table>
              </div>
            </div>
          )}

          <div>
            <h2 className="mb-4 text-base font-semibold text-gray-700">Current Stock Balances</h2>
            <DataTable
              data={stockSummary?.items ?? []}
              columns={stockColumns}
              loading={loading}
              searchable
              searchPlaceholder="Search stock items..."
              pagination
              pageSize={stockPageSize}
              pageSizeOptions={[25, 50, 100]}
              onPageSizeChange={(s) => { setStockPageSize(s); setStockPage(1); }}
              emptyMessage="No stock balances found."
              serverSide
              totalRecords={stockSummary?.totalRecords ?? 0}
              totalPages={stockSummary?.totalPages ?? 0}
              currentPage={stockPage}
              onPageChange={setStockPage}
              searchTerm={stockSearch}
              onSearchChange={(value) => { setStockSearch(value); setStockPage(1); }}
              sortColumn={stockSortColumn}
              sortDirection={stockSortDirection}
              onSortChange={(column, direction) => { setStockSortColumn(column); setStockSortDirection(direction); setStockPage(1); }}
            />
          </div>
        </div>
      )}

      {isGlobalMode && (
        <p className="mt-6 text-xs text-gray-400 print:hidden">
          Global view — reports reflect the selected branch only.
        </p>
      )}
    </div>
  );
};

export default ReportsPage;
