import React, { useCallback, useEffect, useMemo, useState } from 'react';
import type { Column } from '../../components/DataTable';
import Badge from '../../components/Badge';
import { useBranchWriteAccess } from '../../hooks/useBranchWriteAccess';
import { hasBranchContext } from '../../types/permissions';
import { getApiErrorMessage } from '../../services/api';
import { warehouseService, type WarehouseItem } from '../warehouse/warehouseService';
import {
  getStockStatus,
  stockStatusBadgeVariant,
  stockStatusLabel,
  stockStatusQtyColor,
} from '../stock/stockService';
import ReportPageShell from './ReportPageShell';
import { fmtQty, monthStart, todayStr } from './reportFormatters';
import { reportService, type StockSummaryItem } from './reportService';

const StockReportPage: React.FC = () => {
  const { selectedBranchId } = useBranchWriteAccess();
  const branchId = hasBranchContext(selectedBranchId) && selectedBranchId !== null ? selectedBranchId : 0;
  const [fromDate, setFromDate] = useState(monthStart);
  const [toDate, setToDate] = useState(todayStr);
  const [warehouseId, setWarehouseId] = useState<number | ''>('');
  const [warehouses, setWarehouses] = useState<WarehouseItem[]>([]);
  const [rows, setRows] = useState<StockSummaryItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [search, setSearch] = useState('');
  const [sortColumn, setSortColumn] = useState('productName');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [totalClosingBalance, setTotalClosingBalance] = useState(0);

  useEffect(() => {
    if (branchId <= 0) {
      setWarehouses([]);
      return;
    }
    void warehouseService.getAll(branchId, 1, 100).then((res) => {
      setWarehouses(Array.isArray(res.data?.warehouses) ? res.data.warehouses : []);
    }).catch(() => setWarehouses([]));
  }, [branchId]);

  const load = useCallback(async () => {
    if (branchId <= 0) return;
    setLoading(true);
    setError(null);
    try {
      const res = await reportService.getStockSummary(branchId, {
        fromDate,
        toDate,
        warehouseId: warehouseId === '' ? undefined : Number(warehouseId),
        page: pageNumber,
        pageSize,
        search: search.trim() || undefined,
        sortBy: sortColumn,
        sortDirection,
      });
      const raw = res.data;
      setRows(Array.isArray(raw?.items) ? raw.items : []);
      setTotalRecords(raw?.totalRecords ?? 0);
      setTotalPages(raw?.totalPages ?? 0);
      setTotalClosingBalance(raw?.totalClosingBalance ?? 0);
    } catch (err) {
      setRows([]);
      setError(getApiErrorMessage(err, 'Failed to load stock report.'));
    } finally {
      setLoading(false);
    }
  }, [branchId, fromDate, toDate, warehouseId, pageNumber, pageSize, search, sortColumn, sortDirection]);

  useEffect(() => {
    const timer = setTimeout(() => { void load(); }, search ? 300 : 0);
    return () => clearTimeout(timer);
  }, [load, search]);

  useEffect(() => {
    setPageNumber(1);
  }, [branchId, fromDate, toDate, warehouseId, pageSize]);

  const columns: Column<StockSummaryItem>[] = useMemo(() => [
    { key: 'productId', header: 'Product ID', sortable: true },
    { key: 'productName', header: 'Product', sortable: true },
    {
      key: 'closingBalance',
      header: 'Closing Balance',
      sortable: true,
      render: (v, row) => {
        const q = Number(v ?? row.closingBalance ?? 0);
        const status = getStockStatus(q, row);
        return <span className={`font-semibold tabular-nums ${stockStatusQtyColor(status)}`}>{fmtQty(q)}</span>;
      },
    },
    {
      key: 'closingBalance',
      header: 'Status',
      render: (v, row) => {
        const q = Number(v ?? row.closingBalance ?? 0);
        const status = getStockStatus(q, row);
        return <Badge variant={stockStatusBadgeVariant(status)} size="sm" dot>{stockStatusLabel(status)}</Badge>;
      },
    },
  ], []);

  if (branchId <= 0) {
    return (
      <div className="rounded-md border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-700">
        Select a branch from the header to view this report.
      </div>
    );
  }

  return (
    <ReportPageShell
      title="Stock Report"
      description="Closing stock balance by product from the stock ledger."
      fromDate={fromDate}
      toDate={toDate}
      onFromDateChange={setFromDate}
      onToDateChange={setToDate}
      extraFilters={(
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
      error={error}
      loading={loading}
      onRefresh={load}
      columns={columns}
      rows={rows}
      searchPlaceholder="Search products..."
      emptyMessage="No stock balances found."
      pageNumber={pageNumber}
      pageSize={pageSize}
      totalRecords={totalRecords}
      totalPages={totalPages}
      search={search}
      sortColumn={sortColumn}
      sortDirection={sortDirection}
      onPageChange={setPageNumber}
      onPageSizeChange={(size) => { setPageSize(size); setPageNumber(1); }}
      onSearchChange={(value) => { setSearch(value); setPageNumber(1); }}
      onSortChange={(column, direction) => { setSortColumn(column); setSortDirection(direction); setPageNumber(1); }}
      summary={(
        <div className="mb-6 grid grid-cols-2 gap-4 lg:grid-cols-2">
          <div className="rounded-lg border border-blue-100 bg-blue-50 p-4 text-blue-700">
            <p className="text-xs font-medium uppercase tracking-wide opacity-70">Products</p>
            <p className="mt-1 text-xl font-bold">{totalRecords}</p>
          </div>
          <div className="rounded-lg border border-emerald-100 bg-emerald-50 p-4 text-emerald-700">
            <p className="text-xs font-medium uppercase tracking-wide opacity-70">Total Closing Balance</p>
            <p className="mt-1 text-xl font-bold">{fmtQty(totalClosingBalance)}</p>
          </div>
        </div>
      )}
    />
  );
};

export default StockReportPage;
