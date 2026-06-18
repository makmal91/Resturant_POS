import React, { useCallback, useMemo, useState } from 'react';
import type { Column } from '../../components/DataTable';
import { useBranchWriteAccess } from '../../hooks/useBranchWriteAccess';
import { hasBranchContext } from '../../types/permissions';
import { safeString } from '../../utils/safeValues';
import { PurchaseAttractiveSummary } from './reportAttractiveSummaries';
import ReportPageShell from './ReportPageShell';
import { purchaseExportColumns } from './reportExportColumns';
import { fmt, formatDate, monthStart, todayStr } from './reportFormatters';
import { reportService, type PurchaseReportRow } from './reportService';
import { useReportAggregates } from './useReportAggregates';
import { useReportExport } from './useReportExport';
import { useReportTable } from './useReportTable';

const PurchaseReportPage: React.FC = () => {
  const { selectedBranchId } = useBranchWriteAccess();
  const branchId = hasBranchContext(selectedBranchId) && selectedBranchId !== null ? selectedBranchId : 0;
  const [fromDate, setFromDate] = useState(monthStart);
  const [toDate, setToDate] = useState(todayStr);

  const table = useReportTable<PurchaseReportRow>({
    branchId,
    enabled: branchId > 0,
    fetcher: reportService.getPurchaseReport,
    defaultSortColumn: 'purchaseDate',
    fromDate,
    toDate,
  });

  const fetchAggregatePage = useCallback(async (pageNumber: number, pageSize: number) => {
    const res = await reportService.getPurchaseReport(branchId, {
      fromDate,
      toDate,
      pageNumber,
      pageSize,
      search: table.search.trim() || undefined,
      sortColumn: table.sortColumn,
      sortDirection: table.sortDirection,
    });
    return { data: res.data.data, totalRecords: res.data.totalRecords };
  }, [branchId, fromDate, toDate, table.search, table.sortColumn, table.sortDirection]);

  const aggregate = useCallback((rows: PurchaseReportRow[]) => ({
    invoiceCount: rows.length,
    totalAmount: rows.reduce((sum, row) => sum + Number(row.totalAmount ?? 0), 0),
    paidAmount: rows.reduce((sum, row) => sum + Number(row.paidAmount ?? 0), 0),
    balanceDue: rows.reduce((sum, row) => sum + Number(row.balanceDue ?? 0), 0),
  }), []);

  const { totals, loading: aggregatesLoading } = useReportAggregates({
    enabled: branchId > 0,
    deps: [branchId, fromDate, toDate, table.search, table.sortColumn, table.sortDirection],
    fetchPage: fetchAggregatePage,
    aggregate,
  });

  const { exporting, onExport } = useReportExport(
    `purchase-report-${fromDate}-${toDate}`,
    purchaseExportColumns,
    fetchAggregatePage,
    branchId > 0,
  );

  const columns: Column<PurchaseReportRow>[] = useMemo(() => [
    { key: 'invoiceNo', header: 'Invoice', sortable: true, render: (v) => <span className="font-mono text-xs">{safeString(v)}</span> },
    { key: 'purchaseDate', header: 'Date', sortable: true, render: (v) => formatDate(String(v)) },
    { key: 'supplierName', header: 'Supplier', sortable: true },
    { key: 'totalAmount', header: 'Total', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    { key: 'paidAmount', header: 'Paid', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    { key: 'balanceDue', header: 'Balance', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    { key: 'status', header: 'Status', sortable: true },
  ], []);

  const footerRow = useMemo(() => {
    if (!totals) return undefined;
    return {
      label: 'Total',
      values: {
        invoiceNo: 'Total',
        totalAmount: fmt(totals.totalAmount),
        paidAmount: fmt(totals.paidAmount),
        balanceDue: fmt(totals.balanceDue),
      },
    };
  }, [totals]);

  if (branchId <= 0) {
    return (
      <div className="rounded-md border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-700">
        Select a branch from the header to view this report.
      </div>
    );
  }

  return (
    <ReportPageShell
      title="Purchase Report"
      description="Posted purchase invoices with payment balances."
      fromDate={fromDate}
      toDate={toDate}
      onFromDateChange={setFromDate}
      onToDateChange={setToDate}
      error={table.error}
      loading={table.loading}
      onRefresh={table.reload}
      onExport={onExport}
      exporting={exporting}
      columns={columns}
      rows={table.rows}
      searchPlaceholder="Search invoice or supplier..."
      emptyMessage="No purchases found for this period."
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
      footerRow={footerRow}
      summary={(
        <PurchaseAttractiveSummary
          loading={aggregatesLoading}
          invoiceCount={totals?.invoiceCount ?? 0}
          totalAmount={totals?.totalAmount ?? 0}
          paidAmount={totals?.paidAmount ?? 0}
          balanceDue={totals?.balanceDue ?? 0}
        />
      )}
    />
  );
};

export default PurchaseReportPage;
