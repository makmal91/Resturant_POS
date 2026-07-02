import React, { useCallback, useEffect, useMemo, useState } from 'react';
import type { Column } from '../../components/DataTable';
import { useBranchWriteAccess } from '../../hooks/useBranchWriteAccess';
import { hasBranchContext } from '../../types/permissions';
import { supplierService, type SupplierItem } from '../supplier/supplierService';
import { SupplierFilter } from './AgingSummaryCards';
import ReportPageShell from './ReportPageShell';
import { SupplierPayableAttractiveSummary } from './reportAttractiveSummaries';
import { supplierPayableExportColumns } from './reportExportColumns';
import { fmt, formatDate } from './reportFormatters';
import { reportService, type SupplierPayableRow } from './reportService';
import { useReportAggregates } from './useReportAggregates';
import { useReportExport } from './useReportExport';
import { useReportTable } from './useReportTable';

const SupplierPayableReportPage: React.FC = () => {
  const { selectedBranchId } = useBranchWriteAccess();
  const branchId = hasBranchContext(selectedBranchId) && selectedBranchId !== null ? selectedBranchId : 0;
  const [supplierId, setSupplierId] = useState(0);
  const [suppliers, setSuppliers] = useState<SupplierItem[]>([]);

  useEffect(() => {
    if (branchId <= 0) {
      setSuppliers([]);
      setSupplierId(0);
      return;
    }
    void supplierService
      .getAllActive(branchId)
      .then((res) => setSuppliers(Array.isArray(res.data) ? res.data : []))
      .catch(() => setSuppliers([]));
  }, [branchId]);

  const table = useReportTable<SupplierPayableRow>({
    branchId,
    enabled: branchId > 0,
    fetcher: reportService.getSupplierPayableReport,
    defaultSortColumn: 'payableAmount',
    includeDates: false,
    supplierId,
  });

  const fetchAggregatePage = useCallback(async (pageNumber: number, pageSize: number) => {
    const res = await reportService.getSupplierPayableReport(branchId, {
      pageNumber,
      pageSize,
      search: table.search.trim() || undefined,
      sortColumn: table.sortColumn,
      sortDirection: table.sortDirection,
      ...(supplierId > 0 ? { supplierId } : {}),
    });
    return { data: res.data.data, totalRecords: res.data.totalRecords };
  }, [branchId, supplierId, table.search, table.sortColumn, table.sortDirection]);

  const aggregate = useCallback((rows: SupplierPayableRow[]) => ({
    supplierCount: rows.length,
    totalPayable: rows.reduce((sum, row) => sum + Number(row.payableAmount ?? 0), 0),
    totalInvoices: rows.reduce((sum, row) => sum + Number(row.outstandingInvoices ?? 0), 0),
    totalInvoiceDue: rows.reduce((sum, row) => sum + Number(row.invoicePayable ?? 0), 0),
  }), []);

  const { totals, loading: aggregatesLoading } = useReportAggregates({
    enabled: branchId > 0,
    deps: [branchId, supplierId, table.search, table.sortColumn, table.sortDirection],
    fetchPage: fetchAggregatePage,
    aggregate,
  });

  const { exporting, onExport } = useReportExport(
    'supplier-payable-report',
    supplierPayableExportColumns,
    fetchAggregatePage,
    branchId > 0,
  );

  const columns: Column<SupplierPayableRow>[] = useMemo(() => [
    { key: 'supplierCode', header: 'Code', sortable: true },
    { key: 'supplierName', header: 'Supplier', sortable: true },
    { key: 'phone', header: 'Phone' },
    { key: 'invoicePayable', header: 'Invoice Due', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    { key: 'payableAmount', header: 'Payable', sortable: true, render: (v) => <span className="font-semibold text-orange-700">{fmt(Number(v ?? 0))}</span> },
    { key: 'outstandingInvoices', header: 'Invoices', sortable: true },
    { key: 'lastPurchaseDate', header: 'Last Purchase', sortable: true, render: (v) => formatDate(String(v ?? '')) },
  ], []);

  const footerRow = useMemo(() => {
    if (!totals) return undefined;
    return {
      label: 'Total',
      values: {
        supplierCode: 'Total',
        invoicePayable: fmt(totals.totalInvoiceDue),
        payableAmount: <span className="font-bold text-orange-700">{fmt(totals.totalPayable)}</span>,
        outstandingInvoices: String(totals.totalInvoices),
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
      title="Supplier Payable Report"
      description="Suppliers with GL payable balances and open credit purchases."
      showDateFilters={false}
      extraFilters={(
        <SupplierFilter
          suppliers={suppliers}
          value={supplierId}
          onChange={(id) => { setSupplierId(id); table.setPageNumber(1); }}
        />
      )}
      error={table.error}
      loading={table.loading}
      onRefresh={table.reload}
      onExport={onExport}
      exporting={exporting}
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
      footerRow={footerRow}
      summary={(
        <SupplierPayableAttractiveSummary
          loading={aggregatesLoading}
          supplierCount={totals?.supplierCount ?? 0}
          totalPayable={totals?.totalPayable ?? 0}
          totalInvoices={totals?.totalInvoices ?? 0}
        />
      )}
    />
  );
};

export default SupplierPayableReportPage;
