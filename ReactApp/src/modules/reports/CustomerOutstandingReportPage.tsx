import React, { useCallback, useMemo } from 'react';
import type { Column } from '../../components/DataTable';
import { useBranchWriteAccess } from '../../hooks/useBranchWriteAccess';
import { hasBranchContext } from '../../types/permissions';
import { safeString } from '../../utils/safeValues';
import { CustomerOutstandingAttractiveSummary } from './reportAttractiveSummaries';
import ReportPageShell from './ReportPageShell';
import { customerOutstandingExportColumns } from './reportExportColumns';
import { fmt, formatDate } from './reportFormatters';
import { reportService, type CustomerOutstandingRow } from './reportService';
import { useReportAggregates } from './useReportAggregates';
import { useReportExport } from './useReportExport';
import { useReportTable } from './useReportTable';

const CustomerOutstandingReportPage: React.FC = () => {
  const { selectedBranchId } = useBranchWriteAccess();
  const branchId = hasBranchContext(selectedBranchId) && selectedBranchId !== null ? selectedBranchId : 0;

  const table = useReportTable<CustomerOutstandingRow>({
    branchId,
    enabled: branchId > 0,
    fetcher: reportService.getCustomerOutstandingReport,
    defaultSortColumn: 'outstandingAmount',
    includeDates: false,
  });

  const fetchAggregatePage = useCallback(async (pageNumber: number, pageSize: number) => {
    const res = await reportService.getCustomerOutstandingReport(branchId, {
      pageNumber,
      pageSize,
      search: table.search.trim() || undefined,
      sortColumn: table.sortColumn,
      sortDirection: table.sortDirection,
    });
    return { data: res.data.data, totalRecords: res.data.totalRecords };
  }, [branchId, table.search, table.sortColumn, table.sortDirection]);

  const aggregate = useCallback((rows: CustomerOutstandingRow[]) => ({
    customerCount: rows.length,
    totalOutstanding: rows.reduce((sum, row) => sum + Number(row.outstandingAmount ?? 0), 0),
    totalOpening: rows.reduce((sum, row) => sum + Number(row.openingBalance ?? 0), 0),
    totalInvoiceDue: rows.reduce((sum, row) => sum + Number(row.invoiceOutstanding ?? 0), 0),
  }), []);

  const { totals, loading: aggregatesLoading } = useReportAggregates({
    enabled: branchId > 0,
    deps: [branchId, table.search, table.sortColumn, table.sortDirection],
    fetchPage: fetchAggregatePage,
    aggregate,
  });

  const { exporting, onExport } = useReportExport(
    'customer-outstanding-report',
    customerOutstandingExportColumns,
    fetchAggregatePage,
    branchId > 0,
  );

  const columns: Column<CustomerOutstandingRow>[] = useMemo(() => [
    { key: 'customerCode', header: 'Code', sortable: true },
    { key: 'customerName', header: 'Customer', sortable: true },
    { key: 'phone', header: 'Phone', render: (v) => safeString(v) || '—' },
    { key: 'openingBalance', header: 'Opening', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    { key: 'invoiceOutstanding', header: 'Invoice Due', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    { key: 'outstandingAmount', header: 'Outstanding', sortable: true, render: (v) => <span className="font-semibold text-blue-700">{fmt(Number(v ?? 0))}</span> },
    { key: 'outstandingInvoices', header: 'Invoices', sortable: true },
    { key: 'lastSaleDate', header: 'Last Sale', sortable: true, render: (v) => formatDate(String(v ?? '')) },
  ], []);

  const footerRow = useMemo(() => {
    if (!totals) return undefined;
    return {
      label: 'Total',
      values: {
        customerCode: 'Total',
        openingBalance: fmt(totals.totalOpening),
        invoiceOutstanding: fmt(totals.totalInvoiceDue),
        outstandingAmount: <span className="font-bold text-blue-700">{fmt(totals.totalOutstanding)}</span>,
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
      title="Customer Outstanding Report"
      description="Customers with GL receivable balances and open credit invoices."
      showDateFilters={false}
      error={table.error}
      loading={table.loading}
      onRefresh={table.reload}
      onExport={onExport}
      exporting={exporting}
      columns={columns}
      rows={table.rows}
      searchPlaceholder="Search customer..."
      emptyMessage="No customers with outstanding balance."
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
        <CustomerOutstandingAttractiveSummary
          loading={aggregatesLoading}
          customerCount={totals?.customerCount ?? 0}
          totalOutstanding={totals?.totalOutstanding ?? 0}
          totalOpening={totals?.totalOpening ?? 0}
          totalInvoiceDue={totals?.totalInvoiceDue ?? 0}
        />
      )}
    />
  );
};

export default CustomerOutstandingReportPage;
