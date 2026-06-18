import React, { useMemo, useState } from 'react';
import type { Column } from '../../components/DataTable';
import { useBranchWriteAccess } from '../../hooks/useBranchWriteAccess';
import { hasBranchContext } from '../../types/permissions';
import ReportPageShell from './ReportPageShell';
import { fmt, formatDate, monthStart, todayStr } from './reportFormatters';
import { reportService, type ProfitLossRow } from './reportService';
import { useReportTable } from './useReportTable';

const ProfitLossReportPage: React.FC = () => {
  const { selectedBranchId } = useBranchWriteAccess();
  const branchId = hasBranchContext(selectedBranchId) && selectedBranchId !== null ? selectedBranchId : 0;
  const [fromDate, setFromDate] = useState(monthStart);
  const [toDate, setToDate] = useState(todayStr);

  const table = useReportTable<ProfitLossRow>({
    branchId,
    enabled: branchId > 0,
    fetcher: reportService.getProfitLossReport,
    defaultSortColumn: 'date',
    fromDate,
    toDate,
  });

  const columns: Column<ProfitLossRow>[] = useMemo(() => [
    { key: 'date', header: 'Date', sortable: true, render: (v) => formatDate(String(v)) },
    { key: 'revenue', header: 'Revenue', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    { key: 'costOfGoodsSold', header: 'COGS', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    { key: 'grossProfit', header: 'Gross Profit', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    { key: 'expenses', header: 'Expenses', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    { key: 'netProfit', header: 'Net Profit', sortable: true, render: (v) => <span className="font-semibold text-emerald-700">{fmt(Number(v ?? 0))}</span> },
    { key: 'salesCount', header: 'Sales', sortable: true },
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
      title="Profit & Loss Report"
      description="Daily revenue, COGS, expenses, and net profit."
      fromDate={fromDate}
      toDate={toDate}
      onFromDateChange={setFromDate}
      onToDateChange={setToDate}
      error={table.error}
      loading={table.loading}
      onRefresh={table.reload}
      columns={columns}
      rows={table.rows}
      searchPlaceholder="Search by date (yyyy-mm-dd)..."
      emptyMessage="No profit & loss data for this period."
      pageNumber={table.pageNumber}
      pageSize={table.pageSize}
      totalRecords={table.totalRecords}
      totalPages={table.totalPages}
      search={table.search}
      sortColumn={table.sortColumn}
      sortDirection={table.sortDirection}
      onPageChange={table.setPageNumber}
      onPageSizeChange={table.onPageSizeChange}
      onSearchChange={table.onSearchChange}
      onSortChange={table.onSortChange}
    />
  );
};

export default ProfitLossReportPage;
