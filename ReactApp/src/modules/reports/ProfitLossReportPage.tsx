import React, { useCallback, useEffect, useMemo, useState } from 'react';
import type { Column } from '../../components/DataTable';
import { useBranchWriteAccess } from '../../hooks/useBranchWriteAccess';
import { getApiErrorMessage } from '../../services/api';
import { hasBranchContext } from '../../types/permissions';
import ReportPageShell from './ReportPageShell';
import ProfitLossAttractiveSummary from './ProfitLossAttractiveSummary';
import { profitLossExportColumns } from './reportExportColumns';
import { fmt, formatDate, monthStart, todayStr } from './reportFormatters';
import {
  reportService,
  type ProfitLossReportSummary,
  type ProfitLossRow,
} from './reportService';
import { useReportExport } from './useReportExport';

const ProfitLossReportPage: React.FC = () => {
  const { selectedBranchId } = useBranchWriteAccess();
  const branchId = hasBranchContext(selectedBranchId) && selectedBranchId !== null ? selectedBranchId : 0;
  const [fromDate, setFromDate] = useState(monthStart);
  const [toDate, setToDate] = useState(todayStr);
  const [rows, setRows] = useState<ProfitLossRow[]>([]);
  const [summary, setSummary] = useState<ProfitLossReportSummary | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [search, setSearch] = useState('');
  const [sortColumn, setSortColumn] = useState('date');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('desc');
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages] = useState(0);

  const load = useCallback(async () => {
    if (branchId <= 0) return;
    setLoading(true);
    setError(null);
    try {
      const res = await reportService.getProfitLossReport(branchId, {
        fromDate,
        toDate,
        pageNumber,
        pageSize,
        search: search.trim() || undefined,
        sortColumn,
        sortDirection,
      });
      const payload = res.data;
      setRows(Array.isArray(payload?.data) ? payload.data : []);
      setTotalRecords(payload?.totalRecords ?? 0);
      setTotalPages(payload?.totalPages ?? 0);
      setSummary(payload?.summary ?? null);
    } catch (err) {
      setRows([]);
      setSummary(null);
      setError(getApiErrorMessage(err, 'Failed to load profit & loss report.'));
    } finally {
      setLoading(false);
    }
  }, [branchId, fromDate, toDate, pageNumber, pageSize, search, sortColumn, sortDirection]);

  useEffect(() => {
    const timer = setTimeout(() => { void load(); }, search ? 300 : 0);
    return () => clearTimeout(timer);
  }, [load, search]);

  useEffect(() => {
    setPageNumber(1);
  }, [branchId, fromDate, toDate, pageSize]);

  const fetchExportPage = useCallback(async (exportPageNumber: number, exportPageSize: number) => {
    const res = await reportService.getProfitLossReport(branchId, {
      fromDate,
      toDate,
      pageNumber: exportPageNumber,
      pageSize: exportPageSize,
      search: search.trim() || undefined,
      sortColumn,
      sortDirection,
    });
    return { data: res.data.data, totalRecords: res.data.totalRecords };
  }, [branchId, fromDate, toDate, search, sortColumn, sortDirection]);

  const { exporting, onExport } = useReportExport(
    `profit-loss-report-${fromDate}-${toDate}`,
    profitLossExportColumns,
    fetchExportPage,
    branchId > 0,
  );

  const columns: Column<ProfitLossRow>[] = useMemo(() => [
    { key: 'date', header: 'Date', sortable: true, render: (v) => formatDate(String(v)) },
    { key: 'revenue', header: 'Revenue', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    { key: 'costOfGoodsSold', header: 'COGS', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    { key: 'grossProfit', header: 'Gross Profit', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    { key: 'expenses', header: 'Expenses', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    {
      key: 'netProfit',
      header: 'Net Profit',
      sortable: true,
      render: (v) => <span className="font-semibold text-emerald-700">{fmt(Number(v ?? 0))}</span>,
    },
    { key: 'salesCount', header: 'Sales', sortable: true },
  ], []);

  const footerRow = useMemo(() => {
    if (!summary) return undefined;
    const netPositive = summary.totalNetProfit >= 0;
    return {
      label: 'Total',
      values: {
        date: 'Total',
        revenue: fmt(summary.totalRevenue),
        costOfGoodsSold: fmt(summary.totalCostOfGoodsSold),
        grossProfit: fmt(summary.totalGrossProfit),
        expenses: fmt(summary.totalExpenses),
        netProfit: (
          <span className={`font-bold ${netPositive ? 'text-emerald-700' : 'text-red-600'}`}>
            {fmt(summary.totalNetProfit)}
          </span>
        ),
        salesCount: String(summary.totalSalesCount),
      },
    };
  }, [summary]);

  if (branchId <= 0) {
    return (
      <div className="rounded-md border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-700">
        Select a branch from the header to view this report.
      </div>
    );
  }

  return (
    <ReportPageShell
      title="Profit & Loss Report"
      description="Daily revenue, COGS, expenses, and net profit."
      fromDate={fromDate}
      toDate={toDate}
      onFromDateChange={setFromDate}
      onToDateChange={setToDate}
      error={error}
      loading={loading}
      onRefresh={load}
      onExport={onExport}
      exporting={exporting}
      columns={columns}
      rows={rows}
      searchPlaceholder="Search by date (yyyy-mm-dd)..."
      emptyMessage="No profit & loss data for this period."
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
      footerRow={footerRow}
      summary={(
        <ProfitLossAttractiveSummary summary={summary} loading={loading} />
      )}
    />
  );
};

export default ProfitLossReportPage;
