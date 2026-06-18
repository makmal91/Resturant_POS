import React, { useMemo } from 'react';
import type { Column } from '../../components/DataTable';
import { useBranchWriteAccess } from '../../hooks/useBranchWriteAccess';
import { hasBranchContext } from '../../types/permissions';
import { safeString } from '../../utils/safeValues';
import ReportPageShell from './ReportPageShell';
import { fmt, formatDate } from './reportFormatters';
import { reportService, type CustomerOutstandingRow } from './reportService';
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
      description="Customers with opening balance and unpaid invoice amounts."
      showDateFilters={false}
      error={table.error}
      loading={table.loading}
      onRefresh={table.reload}
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
    />
  );
};

export default CustomerOutstandingReportPage;
