import React, { useCallback, useEffect, useMemo, useState } from 'react';
import type { Column } from '../../components/DataTable';
import Badge from '../../components/Badge';
import { useBranchWriteAccess } from '../../hooks/useBranchWriteAccess';
import { hasBranchContext } from '../../types/permissions';
import { getApiErrorMessage } from '../../services/api';
import { safeString } from '../../utils/safeValues';
import { warehouseService, type WarehouseItem } from '../warehouse/warehouseService';
import ReportPageShell from '../reports/ReportPageShell';
import { fmt, formatDate, monthStart, todayStr } from '../reports/reportFormatters';
import {
  stockAdjustmentService,
  type AdjustmentTypeDto,
  type StockAdjustmentReportRow,
} from './stockAdjustmentService';

type ReportRow = StockAdjustmentReportRow & Record<string, unknown>;

const StockAdjustmentReportPage: React.FC = () => {
  const { selectedBranchId } = useBranchWriteAccess();
  const branchId = hasBranchContext(selectedBranchId) && selectedBranchId !== null ? selectedBranchId : 0;

  const [fromDate, setFromDate] = useState(monthStart);
  const [toDate, setToDate] = useState(todayStr);
  const [warehouseId, setWarehouseId] = useState<number | ''>('');
  const [adjustmentTypeId, setAdjustmentTypeId] = useState<number | ''>('');
  const [direction, setDirection] = useState<'all' | 'gain' | 'loss'>('all');
  const [warehouses, setWarehouses] = useState<WarehouseItem[]>([]);
  const [adjustmentTypes, setAdjustmentTypes] = useState<AdjustmentTypeDto[]>([]);
  const [rows, setRows] = useState<ReportRow[]>([]);
  const [gainTotal, setGainTotal] = useState(0);
  const [lossTotal, setLossTotal] = useState(0);
  const [netTotal, setNetTotal] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [sortColumn, setSortColumn] = useState('adjustmentDate');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('desc');

  useEffect(() => {
    if (branchId <= 0) {
      setWarehouses([]);
      setAdjustmentTypes([]);
      return;
    }
    void warehouseService.getAll(branchId, 1, 100).then((res) => {
      setWarehouses(Array.isArray(res.data?.warehouses) ? res.data.warehouses : []);
    }).catch(() => setWarehouses([]));

    void stockAdjustmentService.getTypes(branchId).then((res) => {
      setAdjustmentTypes(Array.isArray(res.data) ? res.data : []);
    }).catch(() => setAdjustmentTypes([]));
  }, [branchId]);

  const load = useCallback(async () => {
    if (branchId <= 0) return;
    setLoading(true);
    setError(null);
    try {
      const res = await stockAdjustmentService.getReport(branchId, {
        fromDate: fromDate || undefined,
        toDate: toDate || undefined,
        warehouseId: warehouseId === '' ? undefined : Number(warehouseId),
        adjustmentTypeId: adjustmentTypeId === '' ? undefined : Number(adjustmentTypeId),
        direction: direction === 'all' ? undefined : direction,
      });
      const payload = res.data as Record<string, unknown>;
      const reportRows = Array.isArray(payload?.rows) ? payload.rows : [];
      setRows(reportRows as ReportRow[]);
      setGainTotal(Number(payload?.gainTotal ?? 0));
      setLossTotal(Number(payload?.lossTotal ?? 0));
      setNetTotal(Number(payload?.netTotal ?? 0));
    } catch (err) {
      setRows([]);
      setGainTotal(0);
      setLossTotal(0);
      setNetTotal(0);
      setError(getApiErrorMessage(err, 'Failed to load stock adjustment report.'));
    } finally {
      setLoading(false);
    }
  }, [branchId, fromDate, toDate, warehouseId, adjustmentTypeId, direction]);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    setPageNumber(1);
  }, [branchId, fromDate, toDate, warehouseId, adjustmentTypeId, direction, pageSize]);

  const filteredRows = useMemo(() => {
    let list = [...rows];
    if (search.trim()) {
      const q = search.toLowerCase();
      list = list.filter(
        (row) =>
          row.adjustmentNo.toLowerCase().includes(q) ||
          row.warehouseName.toLowerCase().includes(q) ||
          row.adjustmentTypeName.toLowerCase().includes(q),
      );
    }

    list.sort((a, b) => {
      const aVal = a[sortColumn as keyof ReportRow];
      const bVal = b[sortColumn as keyof ReportRow];
      if (aVal == null && bVal == null) return 0;
      if (aVal == null) return 1;
      if (bVal == null) return -1;
      if (typeof aVal === 'number' && typeof bVal === 'number') {
        return sortDirection === 'asc' ? aVal - bVal : bVal - aVal;
      }
      const cmp = String(aVal).localeCompare(String(bVal));
      return sortDirection === 'asc' ? cmp : -cmp;
    });

    return list;
  }, [rows, search, sortColumn, sortDirection]);

  const totalRecords = filteredRows.length;
  const totalPages = Math.max(1, Math.ceil(totalRecords / pageSize));
  const pagedRows = useMemo(() => {
    const start = (pageNumber - 1) * pageSize;
    return filteredRows.slice(start, start + pageSize);
  }, [filteredRows, pageNumber, pageSize]);

  const columns: Column<ReportRow>[] = useMemo(
    () => [
      {
        key: 'adjustmentNo',
        header: 'Adjustment No',
        sortable: true,
        render: (value) => <span className="font-medium text-gray-900">{safeString(value)}</span>,
      },
      {
        key: 'adjustmentDate',
        header: 'Date',
        sortable: true,
        render: (value) => formatDate(safeString(value)),
      },
      { key: 'warehouseName', header: 'Warehouse', sortable: true },
      { key: 'adjustmentTypeName', header: 'Type', sortable: true },
      {
        key: 'gainAmount',
        header: 'Gain',
        sortable: true,
        render: (value) => <span className="text-emerald-700">{fmt(Number(value ?? 0))}</span>,
      },
      {
        key: 'lossAmount',
        header: 'Loss',
        sortable: true,
        render: (value) => <span className="text-red-700">{fmt(Number(value ?? 0))}</span>,
      },
      {
        key: 'netAmount',
        header: 'Net',
        sortable: true,
        render: (value) => fmt(Number(value ?? 0)),
      },
      {
        key: 'isReversed',
        header: 'Status',
        sortable: true,
        render: (value) =>
          value ? (
            <Badge variant="danger" size="sm" dot>
              Reversed
            </Badge>
          ) : (
            <Badge variant="success" size="sm" dot>
              Active
            </Badge>
          ),
      },
    ],
    [],
  );

  const footerRow = useMemo(
    () => ({
      label: 'Totals (active only)',
      values: {
        adjustmentNo: 'Totals',
        gainAmount: <span className="text-emerald-700">{fmt(gainTotal)}</span>,
        lossAmount: <span className="text-red-700">{fmt(lossTotal)}</span>,
        netAmount: fmt(netTotal),
      },
    }),
    [gainTotal, lossTotal, netTotal],
  );

  if (branchId <= 0) {
    return (
      <div className="rounded-md border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-700">
        Select a branch from the header to view this report.
      </div>
    );
  }

  return (
    <ReportPageShell<ReportRow>
      title="Stock Adjustment Report"
      description="Summary of stock adjustments with gain, loss, and net amounts."
      fromDate={fromDate}
      toDate={toDate}
      onFromDateChange={setFromDate}
      onToDateChange={setToDate}
      extraFilters={(
        <div className="flex flex-wrap items-center gap-3">
          <select
            value={warehouseId}
            onChange={(e) => setWarehouseId(e.target.value === '' ? '' : Number(e.target.value))}
            className="rounded-md border border-gray-300 px-3 py-2 text-sm"
          >
            <option value="">All warehouses</option>
            {warehouses.map((w) => (
              <option key={w.id} value={w.id}>
                {w.name}
              </option>
            ))}
          </select>
          <select
            value={adjustmentTypeId}
            onChange={(e) => setAdjustmentTypeId(e.target.value === '' ? '' : Number(e.target.value))}
            className="rounded-md border border-gray-300 px-3 py-2 text-sm"
          >
            <option value="">All types</option>
            {adjustmentTypes.map((t) => (
              <option key={t.id} value={t.id}>
                {t.name}
              </option>
            ))}
          </select>
          <select
            value={direction}
            onChange={(e) => setDirection(e.target.value as 'all' | 'gain' | 'loss')}
            className="rounded-md border border-gray-300 px-3 py-2 text-sm"
          >
            <option value="all">All directions</option>
            <option value="gain">Gain only</option>
            <option value="loss">Loss only</option>
          </select>
        </div>
      )}
      error={error}
      loading={loading}
      onRefresh={load}
      columns={columns}
      rows={pagedRows}
      searchPlaceholder="Search adjustment no, warehouse, or type…"
      emptyMessage="No stock adjustments found for the selected filters."
      pageNumber={pageNumber}
      pageSize={pageSize}
      totalRecords={totalRecords}
      totalPages={totalPages}
      search={search}
      sortColumn={sortColumn}
      sortDirection={sortDirection}
      onPageChange={setPageNumber}
      onPageSizeChange={(size) => {
        setPageSize(size);
        setPageNumber(1);
      }}
      onSearchChange={setSearch}
      onSortChange={(col, dir) => {
        setSortColumn(col);
        setSortDirection(dir);
      }}
      footerRow={footerRow}
    />
  );
};

export default StockAdjustmentReportPage;
