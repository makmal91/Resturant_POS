import React, { useMemo } from 'react';
import type { Column } from '../../components/DataTable';
import { useBranchWriteAccess } from '../../hooks/useBranchWriteAccess';
import { hasBranchContext } from '../../types/permissions';
import ReportPageShell from './ReportPageShell';
import { fmt, formatDate } from './reportFormatters';
import { reportService, type SupplierPayableRow } from './reportService';
import { useReportTable } from './useReportTable';

const SupplierPayableReportPage: React.FC = () => {
  const { selectedBranchId } = useBranchWriteAccess();
  const branchId = hasBranchContext(selectedBranchId) && selectedBranchId !== null ? selectedBranchId : 0;

  const table = useReportTable<SupplierPayableRow>({
    branchId,
    enabled: branchId > 0,
    fetcher: reportService.getSupplierPayableReport,
    defaultSortColumn: 'payableAmount',
    includeDates: false,
  });

  const columns: Column<SupplierPayableRow>[] = useMemo(() => [
    { key: 'supplierCode', header: 'Code', sortable: true },
    { key: 'supplierName', header: 'Supplier', sortable: true },
    { key: 'phone', header: 'Phone' },
    { key: 'invoicePayable', header: 'Invoice Due', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    { key: 'payableAmount', header: 'Payable', sortable: true, render: (v) => <span className="font-semibold text-orange-700">{fmt(Number(v ?? 0))}</span> },
    { key: 'outstandingInvoices', header: 'Invoices', sortable: true },
    { key: 'lastPurchaseDate', header: 'Last Purchase', sortable: true, render: (v) => formatDate(String(v ?? '')) },
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
      title="Supplier Payable Report"
      description="Suppliers with unpaid purchase invoice balances."
      showDateFilters={false}
      error={table.error}
      loading={table.loading}
      onRefresh={table.reload}
      columns={columns}
      rows={table.rows}
      searchPlaceholder="Search supplier..."
      emptyMessage="No suppliers with payable balance."
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

export default SupplierPayableReportPage;
