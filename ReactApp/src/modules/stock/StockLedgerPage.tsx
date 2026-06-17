import React, { useCallback, useEffect, useMemo, useState } from 'react';
import DataTable, { type Column } from '../../components/DataTable';
import Badge from '../../components/Badge';
import { useBranchWriteAccess } from '../../hooks/useBranchWriteAccess';
import { hasBranchContext } from '../../types/permissions';
import { getApiErrorMessage } from '../../services/api';
import { safeString } from '../../utils/safeValues';
import { stockService, type StockBalance, type StockLedgerEntry, type StockLedgerType } from './stockService';
import { warehouseService, type WarehouseItem } from '../warehouse/warehouseService';

// ── Helpers ──────────────────────────────────────────────────────────────────

const formatDate = (value: string) => {
  if (!value) return '—';
  const d = new Date(value);
  return Number.isNaN(d.getTime())
    ? '—'
    : d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
};

const formatCurrency = (v: number) =>
  new Intl.NumberFormat(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(v);

const formatQty = (v: number) => {
  const abs = Math.abs(v);
  const s = abs % 1 === 0 ? abs.toFixed(0) : abs.toFixed(4).replace(/\.?0+$/, '');
  return v < 0 ? `−${s}` : s;
};

/** Human-readable label for each ledger type */
const TYPE_LABEL: Record<StockLedgerType, string> = {
  PurchaseEntry:  'Purchase',
  SaleEntry:      'Sale',
  PurchaseReturn: 'Return In',
  SaleReturn:     'Return In',
  Adjustment:     'Adjustment',
  TransferIn:     'Return In',
  TransferOut:    'Return Out',
  Opening:        'Opening',
  SaleReversal:   'Sale Reversal',
  PurchaseReversal: 'Purchase Reversal',
};

const TYPE_BADGE: Record<StockLedgerType, 'success' | 'danger' | 'info' | 'warning' | 'secondary' | 'primary'> = {
  PurchaseEntry:  'success',
  SaleEntry:      'danger',
  PurchaseReturn: 'warning',
  SaleReturn:     'info',
  Adjustment:     'primary',
  TransferIn:     'info',
  TransferOut:    'warning',
  Opening:        'secondary',
  SaleReversal:   'warning',
  PurchaseReversal: 'warning',
};

/** Reference prefix based on type */
const refPrefix = (type: StockLedgerType) => {
  switch (type) {
    case 'PurchaseEntry':  return 'PO';
    case 'SaleEntry':      return 'SO';
    case 'Opening':        return 'OP';
    case 'PurchaseReturn': return 'PR';
    case 'SaleReturn':     return 'SR';
    case 'TransferIn':
    case 'TransferOut':    return 'TRF';
    default:               return 'REF';
  }
};

// ── Types ─────────────────────────────────────────────────────────────────────

type ViewMode = 'balances' | 'ledger';

// ── Component ─────────────────────────────────────────────────────────────────

const StockLedgerPage: React.FC = () => {
  const { selectedBranchId } = useBranchWriteAccess();
  const hasBranchSelection = hasBranchContext(selectedBranchId);

  const [viewMode, setViewMode] = useState<ViewMode>('balances');
  const [warehouses, setWarehouses] = useState<WarehouseItem[]>([]);
  const [selectedWarehouse, setSelectedWarehouse] = useState<number | null>(null);

  // Ledger state
  const [ledgerEntries, setLedgerEntries] = useState<StockLedgerEntry[]>([]);
  const [ledgerLoading, setLedgerLoading] = useState(false);
  const [ledgerPage, setLedgerPage] = useState(1);
  const [ledgerPageSize, setLedgerPageSize] = useState(50);
  const [ledgerTotal, setLedgerTotal] = useState(0);
  const [ledgerTotalPages, setLedgerTotalPages] = useState(0);
  const [typeFilter, setTypeFilter] = useState<StockLedgerType | null>(null);
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');

  // Balance state
  const [balances, setBalances] = useState<StockBalance[]>([]);
  const [balanceLoading, setBalanceLoading] = useState(false);
  const [variantWise, setVariantWise] = useState(false);

  const [notification, setNotification] = useState<{ kind: 'error'; msg: string } | null>(null);

  const showError = (msg: string) => {
    setNotification({ kind: 'error', msg });
    setTimeout(() => setNotification(null), 5000);
  };

  // ── Load warehouses ──────────────────────────────────────────────────────
  const loadWarehouses = useCallback(async () => {
    if (!hasBranchSelection || selectedBranchId === null) return;
    try {
      const res = await warehouseService.getAllActive(selectedBranchId);
      setWarehouses(Array.isArray(res.data) ? res.data : []);
    } catch { /* silent */ }
  }, [hasBranchSelection, selectedBranchId]);

  useEffect(() => { void loadWarehouses(); }, [loadWarehouses]);
  useEffect(() => { setSelectedWarehouse(null); setWarehouses([]); }, [selectedBranchId]);

  // ── Fetch ledger ─────────────────────────────────────────────────────────
  const fetchLedger = useCallback(async () => {
    if (!hasBranchSelection || selectedBranchId === null) return;
    setLedgerLoading(true);
    try {
      const res = await stockService.getLedger({
        branchId: selectedBranchId,
        warehouseId: selectedWarehouse ?? undefined,
        type: typeFilter ?? undefined,
        dateFrom: dateFrom || undefined,
        dateTo: dateTo || undefined,
        page: ledgerPage,
        pageSize: ledgerPageSize,
      });
      const rows = Array.isArray(res.data?.entries) ? res.data.entries : [];
      setLedgerEntries(
        rows.map((r: unknown) => {
          const row = r as Record<string, unknown>;
          return {
            id: Number(row.id ?? row.Id ?? 0),
            productId: Number(row.productId ?? row.ProductId ?? 0),
            productName: safeString(row.productName ?? row.ProductName),
            variantId: row.variantId ?? row.VariantId ?? null,
            variantName: safeString(row.variantName ?? row.VariantName) || null,
            warehouseId: Number(row.warehouseId ?? row.WarehouseId ?? 0),
            warehouseName: safeString(row.warehouseName ?? row.WarehouseName),
            type: safeString(row.type ?? row.Type) as StockLedgerType,
            referenceId: row.referenceId != null ? Number(row.referenceId ?? row.ReferenceId) : null,
            quantityInBaseUnit: Number(row.quantityInBaseUnit ?? row.QuantityInBaseUnit ?? 0),
            unitPrice: Number(row.unitPrice ?? row.UnitPrice ?? 0),
            totalAmount: Number(row.totalAmount ?? row.TotalAmount ?? 0),
            date: safeString(row.date ?? row.Date),
            remarks: safeString(row.remarks ?? row.Remarks),
            branchId: Number(row.branchId ?? row.BranchId ?? selectedBranchId),
            branchName: safeString(row.branchName ?? row.BranchName),
          } as StockLedgerEntry;
        })
      );
      setLedgerTotal(Number(res.data?.totalRecords ?? 0));
      setLedgerTotalPages(Number(res.data?.totalPages ?? 0));
    } catch (err) {
      showError(getApiErrorMessage(err, 'Failed to load ledger.'));
    } finally {
      setLedgerLoading(false);
    }
  }, [hasBranchSelection, selectedBranchId, selectedWarehouse, typeFilter, dateFrom, dateTo, ledgerPage, ledgerPageSize]);

  // ── Fetch balances ───────────────────────────────────────────────────────
  const fetchBalances = useCallback(async () => {
    if (!hasBranchSelection || selectedBranchId === null) return;
    setBalanceLoading(true);
    try {
      const res = await stockService.getBalances(
        selectedBranchId,
        selectedWarehouse ?? undefined,
        undefined,
        undefined,
        variantWise,
      );
      const raw = Array.isArray(res.data) ? res.data : [];
      setBalances(
        raw.map((row: unknown) => {
          const r = row as Record<string, unknown>;
          return {
            productId: Number(r.productId ?? r.ProductId ?? 0),
            productName: safeString(r.productName ?? r.ProductName),
            productCode: safeString(r.productCode ?? r.ProductCode),
            variantId: r.variantId ?? r.VariantId ?? null,
            variantName: safeString(r.variantName ?? r.VariantName) || null,
            warehouseId: Number(r.warehouseId ?? r.WarehouseId ?? 0),
            warehouseName: safeString(r.warehouseName ?? r.WarehouseName),
            quantity: Number(
              r.quantity ?? r.Quantity ?? r.closingBalance ?? r.ClosingBalance ?? 0,
            ),
          } as StockBalance;
        }),
      );
    } catch (err) {
      showError(getApiErrorMessage(err, 'Failed to load stock balances.'));
    } finally {
      setBalanceLoading(false);
    }
  }, [hasBranchSelection, selectedBranchId, selectedWarehouse, variantWise]);

  useEffect(() => {
    if (!hasBranchSelection) return;
    if (viewMode === 'ledger') void fetchLedger();
    else void fetchBalances();
  }, [viewMode, fetchLedger, fetchBalances, hasBranchSelection]);

  useEffect(() => { setLedgerPage(1); }, [selectedWarehouse, typeFilter, dateFrom, dateTo, ledgerPageSize]);

  // ── Summary stats for balances ───────────────────────────────────────────
  const balanceStats = useMemo(() => ({
    total: balances.length,
    inStock: balances.filter((b) => b.quantity > 0).length,
    lowStock: balances.filter((b) => b.quantity > 0 && b.quantity < 10).length,
    outOfStock: balances.filter((b) => b.quantity <= 0).length,
  }), [balances]);

  // ── Ledger total amount ──────────────────────────────────────────────────
  const ledgerPageTotal = useMemo(
    () => ledgerEntries.reduce((s, e) => s + e.totalAmount, 0),
    [ledgerEntries]
  );

  // ── Column definitions ───────────────────────────────────────────────────

  const ledgerColumns: Column<StockLedgerEntry>[] = useMemo(() => [
    {
      key: 'date',
      header: 'Date',
      sortable: true,
      render: (v) => (
        <span className="whitespace-nowrap text-sm text-gray-700">{formatDate(safeString(v))}</span>
      ),
    },
    {
      key: 'type',
      header: 'Type',
      render: (v) => {
        const t = v as StockLedgerType;
        return (
          <Badge variant={TYPE_BADGE[t] ?? 'secondary'} size="sm" dot>
            {TYPE_LABEL[t] ?? safeString(v)}
          </Badge>
        );
      },
    },
    {
      key: 'referenceId',
      header: 'Reference',
      render: (v, row) => {
        const id = v as number | null | undefined;
        if (!id) return <span className="text-gray-300">—</span>;
        const prefix = refPrefix(row.type);
        return (
          <span className="inline-flex items-center gap-1 rounded-md border border-gray-200 bg-gray-50 px-2 py-0.5 font-mono text-xs font-medium text-gray-700">
            {prefix}&nbsp;#{id}
          </span>
        );
      },
    },
    {
      key: 'productName',
      header: 'Product',
      sortable: true,
      render: (v, row) => (
        <div>
          <p className="text-sm font-medium text-gray-900">{safeString(v)}</p>
          {row.variantName && (
            <p className="text-xs text-purple-600">{row.variantName}</p>
          )}
        </div>
      ),
    },
    {
      key: 'warehouseName',
      header: 'Warehouse',
      sortable: true,
      render: (v) => (
        <span className="inline-flex items-center gap-1 text-sm text-gray-700">
          <svg className="h-3.5 w-3.5 shrink-0 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
          </svg>
          {safeString(v)}
        </span>
      ),
    },
    {
      key: 'quantityInBaseUnit',
      header: 'Qty (Base Unit)',
      sortable: true,
      render: (v, row) => {
        const qty = Number(v);
        const isOut = row.type === 'SaleEntry' || row.type === 'TransferOut';
        const color = isOut ? 'text-red-600' : 'text-green-700';
        const sign = isOut ? '−' : '+';
        return (
          <span className={`font-semibold tabular-nums ${color}`}>
            {sign}{formatQty(Math.abs(qty))}
          </span>
        );
      },
    },
    {
      key: 'unitPrice',
      header: 'Unit Price',
      render: (v) => {
        const n = Number(v);
        return n > 0
          ? <span className="tabular-nums text-gray-700">{formatCurrency(n)}</span>
          : <span className="text-gray-300">—</span>;
      },
    },
    {
      key: 'totalAmount',
      header: 'Total Amount',
      render: (v) => {
        const n = Number(v);
        return n > 0
          ? <span className="tabular-nums font-medium text-gray-900">{formatCurrency(n)}</span>
          : <span className="text-gray-300">—</span>;
      },
    },
    {
      key: 'remarks',
      header: 'Remarks',
      render: (v) => {
        const s = safeString(v);
        return s
          ? <span className="max-w-[180px] truncate text-xs text-gray-500" title={s}>{s}</span>
          : <span className="text-gray-300">—</span>;
      },
    },
  ], []);

  const balanceColumns: Column<StockBalance>[] = useMemo(() => [
    {
      key: 'productName',
      header: 'Product',
      sortable: true,
      render: (v, row) => (
        <div className="flex items-center gap-3">
          <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md border border-gray-200 bg-gray-100 text-xs font-bold text-gray-500">
            {safeString(v).slice(0, 2).toUpperCase()}
          </div>
          <div>
            <p className="text-sm font-medium text-gray-900">{safeString(v)}</p>
            {row.productCode && <p className="text-xs text-gray-400">{row.productCode}</p>}
          </div>
        </div>
      ),
    },
    ...(variantWise
      ? [{
          key: 'variantName' as const,
          header: 'Variant',
          sortable: true,
          render: (v: unknown) =>
            safeString(v) ? (
              <span className="rounded-full bg-purple-50 px-2 py-0.5 text-xs font-medium text-purple-700">
                {safeString(v)}
              </span>
            ) : (
              <span className="text-gray-400 text-xs">No variant</span>
            ),
        }]
      : []),
    {
      key: 'quantity',
      header: 'Remaining Balance',
      sortable: true,
      render: (v, row) => {
        const qty = Number(v ?? row.quantity ?? 0);
        const color = qty <= 0 ? 'text-red-700' : qty < 10 ? 'text-yellow-700' : 'text-green-700';
        return (
          <span className={`text-base font-bold tabular-nums ${color}`}>
            {formatQty(qty)}
          </span>
        );
      },
    },
    {
      key: 'quantity',
      header: 'Status',
      render: (v, row) => {
        const qty = Number(v ?? row.quantity ?? 0);
        if (qty <= 0)
          return <Badge variant="danger" size="sm" dot>Out of Stock</Badge>;
        if (qty < 10)
          return <Badge variant="warning" size="sm" dot>Low Stock</Badge>;
        return <Badge variant="success" size="sm" dot>In Stock</Badge>;
      },
    },
  ], [variantWise]);

  // ── Render ───────────────────────────────────────────────────────────────
  return (
    <div>
      {/* Notification */}
      {notification && (
        <div className="mb-6 flex items-center gap-2 rounded-md bg-red-50 px-4 py-3 text-sm font-medium text-red-800">
          <svg className="h-4 w-4 shrink-0" fill="currentColor" viewBox="0 0 20 20">
            <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
          </svg>
          {notification.msg}
        </div>
      )}

      {/* Page header */}
      <div className="mb-6">
        <h1 className="mb-1 text-3xl font-bold text-gray-900">Stock Management</h1>
        <p className="text-gray-500">Real-time stock balances and full ledger audit trail per warehouse</p>
      </div>

      {!hasBranchSelection ? (
        <div className="rounded-md border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-600">
          Select a branch from the header to view stock data.
        </div>
      ) : (
        <>
          {/* ── Toolbar ──────────────────────────────────────────────── */}
          <div className="mb-6 flex flex-wrap items-end gap-3">
            {/* View toggle */}
            <div className="flex overflow-hidden rounded-lg border border-gray-300 bg-white">
              {(['balances', 'ledger'] as ViewMode[]).map((mode) => (
                <button
                  key={mode}
                  onClick={() => setViewMode(mode)}
                  className={`px-4 py-2 text-sm font-medium transition-colors ${
                    viewMode === mode
                      ? 'bg-blue-600 text-white'
                      : 'text-gray-600 hover:bg-gray-50'
                  }`}
                >
                  {mode === 'balances' ? 'Stock Balances' : 'Ledger History'}
                </button>
              ))}
            </div>

            {/* Warehouse filter */}
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-500">Warehouse</label>
              <select
                value={selectedWarehouse ?? ''}
                onChange={(e) => setSelectedWarehouse(e.target.value ? Number(e.target.value) : null)}
                className="rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
              >
                <option value="">All Warehouses</option>
                {warehouses.map((w) => (
                  <option key={w.id} value={w.id}>{w.name}</option>
                ))}
              </select>
            </div>

            {viewMode === 'balances' && (
              <label className="flex cursor-pointer items-center gap-2 rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm text-gray-700">
                <input
                  type="checkbox"
                  checked={variantWise}
                  onChange={(e) => setVariantWise(e.target.checked)}
                  className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                Variant-wise stock
              </label>
            )}

            {/* Ledger-only filters */}
            {viewMode === 'ledger' && (
              <>
                <div>
                  <label className="mb-1 block text-xs font-medium text-gray-500">Transaction Type</label>
                  <select
                    value={typeFilter ?? ''}
                    onChange={(e) =>
                      setTypeFilter(e.target.value ? (e.target.value as StockLedgerType) : null)
                    }
                    className="rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
                  >
                    <option value="">All Types</option>
                    <optgroup label="Purchase">
                      <option value="PurchaseEntry">Purchase Entry</option>
                      <option value="PurchaseReturn">Purchase Return</option>
                    </optgroup>
                    <optgroup label="Sales">
                      <option value="SaleEntry">Sale Entry</option>
                      <option value="SaleReturn">Sale Return</option>
                    </optgroup>
                    <optgroup label="Transfer">
                      <option value="TransferIn">Transfer In</option>
                      <option value="TransferOut">Transfer Out</option>
                    </optgroup>
                    <optgroup label="Other">
                      <option value="Opening">Opening</option>
                      <option value="Adjustment">Adjustment</option>
                    </optgroup>
                  </select>
                </div>

                <div>
                  <label className="mb-1 block text-xs font-medium text-gray-500">From Date</label>
                  <input
                    type="date"
                    value={dateFrom}
                    onChange={(e) => setDateFrom(e.target.value)}
                    className="rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
                  />
                </div>

                <div>
                  <label className="mb-1 block text-xs font-medium text-gray-500">To Date</label>
                  <input
                    type="date"
                    value={dateTo}
                    onChange={(e) => setDateTo(e.target.value)}
                    className="rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
                  />
                </div>
              </>
            )}

            <button
              onClick={() => {
                if (viewMode === 'ledger') void fetchLedger();
                else void fetchBalances();
              }}
              className="flex items-center gap-1.5 rounded-lg border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 transition-colors hover:bg-gray-50"
            >
              <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
              </svg>
              Refresh
            </button>
          </div>

          {/* ── Balance summary cards ─────────────────────────────────── */}
          {viewMode === 'balances' && !balanceLoading && balances.length > 0 && (
            <div className="mb-6 grid grid-cols-2 gap-4 sm:grid-cols-4">
              {[
                { label: 'Total SKUs', value: balanceStats.total, color: 'text-gray-900', bg: 'bg-gray-50 border-gray-200' },
                { label: 'In Stock', value: balanceStats.inStock, color: 'text-green-700', bg: 'bg-green-50 border-green-200' },
                { label: 'Low Stock', value: balanceStats.lowStock, color: 'text-yellow-700', bg: 'bg-yellow-50 border-yellow-200' },
                { label: 'Out of Stock', value: balanceStats.outOfStock, color: 'text-red-700', bg: 'bg-red-50 border-red-200' },
              ].map((stat) => (
                <div key={stat.label} className={`rounded-lg border ${stat.bg} px-4 py-3`}>
                  <p className="text-xs font-medium text-gray-500">{stat.label}</p>
                  <p className={`mt-1 text-2xl font-bold tabular-nums ${stat.color}`}>{stat.value}</p>
                </div>
              ))}
            </div>
          )}

          {/* ── Ledger page-total strip ───────────────────────────────── */}
          {viewMode === 'ledger' && !ledgerLoading && ledgerEntries.length > 0 && (
            <div className="mb-4 flex items-center justify-between rounded-lg border border-blue-100 bg-blue-50 px-4 py-2.5">
              <span className="text-sm text-blue-700">
                Showing <strong>{ledgerEntries.length}</strong> of <strong>{ledgerTotal}</strong> entries
                {typeFilter && (
                  <span> · filtered by <strong>{TYPE_LABEL[typeFilter]}</strong></span>
                )}
              </span>
              <span className="text-sm font-semibold text-blue-900">
                Page total: {formatCurrency(ledgerPageTotal)}
              </span>
            </div>
          )}

          {/* ── Tables ───────────────────────────────────────────────── */}
          {viewMode === 'balances' ? (
            <DataTable
              data={balances}
              columns={balanceColumns}
              loading={balanceLoading}
              searchable
              searchPlaceholder="Search by product or warehouse…"
              emptyMessage={
                selectedWarehouse
                  ? 'No stock found for the selected warehouse.'
                  : 'No stock entries found. Post a purchase to update stock.'
              }
            />
          ) : (
            <DataTable
              data={ledgerEntries}
              columns={ledgerColumns}
              loading={ledgerLoading}
              pagination
              pageSize={ledgerPageSize}
              pageSizeOptions={[25, 50, 100]}
              onPageSizeChange={(n) => { setLedgerPageSize(n); setLedgerPage(1); }}
              emptyMessage={
                typeFilter || dateFrom || dateTo
                  ? 'No ledger entries match the current filters.'
                  : 'No ledger entries found. Post a purchase to create ledger entries.'
              }
              serverSide
              totalRecords={ledgerTotal}
              totalPages={ledgerTotalPages}
              currentPage={ledgerPage}
              onPageChange={setLedgerPage}
            />
          )}
        </>
      )}
    </div>
  );
};

export default StockLedgerPage;
