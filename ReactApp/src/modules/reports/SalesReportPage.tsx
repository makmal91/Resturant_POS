import React, { useEffect, useMemo, useState } from 'react';
import type { Column } from '../../components/DataTable';
import Badge from '../../components/Badge';
import { useBranchWriteAccess } from '../../hooks/useBranchWriteAccess';
import { hasBranchContext } from '../../types/permissions';
import { safeString } from '../../utils/safeValues';
import ReportPageShell from './ReportPageShell';
import { fmt, formatDate, monthStart, todayStr } from './reportFormatters';
import { reportService, type SalesReportRow, type SalesSummaryDto } from './reportService';
import { useReportTable } from './useReportTable';

const SalesReportPage: React.FC = () => {
  const { selectedBranchId } = useBranchWriteAccess();
  const branchId = hasBranchContext(selectedBranchId) && selectedBranchId !== null ? selectedBranchId : 0;
  const [fromDate, setFromDate] = useState(monthStart);
  const [toDate, setToDate] = useState(todayStr);
  const [summary, setSummary] = useState<SalesSummaryDto | null>(null);

  const table = useReportTable<SalesReportRow>({
    branchId,
    enabled: branchId > 0,
    fetcher: reportService.getSalesReport,
    defaultSortColumn: 'saleDate',
    fromDate,
    toDate,
  });

  useEffect(() => {
    if (branchId <= 0) {
      setSummary(null);
      return;
    }
    void reportService.getSalesSummary(branchId, { fromDate, toDate })
      .then((res) => setSummary(res.data))
      .catch(() => setSummary(null));
  }, [branchId, fromDate, toDate]);

  const columns: Column<SalesReportRow>[] = useMemo(() => [
    { key: 'invoiceNo', header: 'Invoice', sortable: true, render: (v) => <span className="font-mono text-xs">{safeString(v)}</span> },
    { key: 'saleDate', header: 'Date', sortable: true, render: (v) => formatDate(String(v)) },
    { key: 'customerName', header: 'Customer', sortable: true },
    { key: 'grandTotal', header: 'Total', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    { key: 'paidAmount', header: 'Paid', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    { key: 'balanceDue', header: 'Balance', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    { key: 'paymentMethod', header: 'Payment', sortable: true },
    {
      key: 'isCreditSale',
      header: 'Type',
      render: (v) => <Badge variant={v ? 'warning' : 'success'} size="sm">{v ? 'Credit' : 'Cash'}</Badge>,
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
      title="Sales Report"
      description="Completed sale invoices with server-side filtering and pagination."
      fromDate={fromDate}
      toDate={toDate}
      onFromDateChange={setFromDate}
      onToDateChange={setToDate}
      error={table.error}
      loading={table.loading}
      onRefresh={table.reload}
      columns={columns}
      rows={table.rows}
      searchPlaceholder="Search invoice or customer..."
      emptyMessage="No sales found for this period."
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
      summary={summary ? (
        <div className="mb-6 grid grid-cols-2 gap-4 lg:grid-cols-4">
          {[
            { label: 'Total Sales', value: fmt(summary.totalSales) },
            { label: 'Invoices', value: String(summary.totalInvoices) },
            { label: 'Cash', value: fmt(summary.totalCash) },
            { label: 'Card', value: fmt(summary.totalCard) },
          ].map(({ label, value }) => (
            <div key={label} className="rounded-lg border border-gray-100 bg-white p-4">
              <p className="text-xs font-medium text-gray-500">{label}</p>
              <p className="mt-1 text-lg font-semibold text-gray-800">{value}</p>
            </div>
          ))}
        </div>
      ) : null}
    />
  );
};

export default SalesReportPage;
