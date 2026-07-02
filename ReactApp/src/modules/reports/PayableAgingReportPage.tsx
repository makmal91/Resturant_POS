import React, { useCallback, useEffect, useMemo, useState } from 'react';
import type { Column } from '../../components/DataTable';
import Badge from '../../components/Badge';
import { useBranchWriteAccess } from '../../hooks/useBranchWriteAccess';
import { hasBranchContext } from '../../types/permissions';
import { safeString } from '../../utils/safeValues';
import { supplierService, type SupplierItem } from '../supplier/supplierService';
import { AgingBucketFilter, SupplierFilter } from './AgingSummaryCards';
import { AgingAttractiveSummary } from './reportAttractiveSummaries';
import ReportPageShell from './ReportPageShell';
import { payableAgingExportColumns } from './reportExportColumns';
import { fmt, formatDate } from './reportFormatters';
import { reportService, type PayableAgingRow } from './reportService';
import { useAgingReportTable } from './useAgingReportTable';
import { useReportExport } from './useReportExport';

const bucketVariant = (bucket: string) => {
  if (bucket === '0-30') return 'success' as const;
  if (bucket === '31-60') return 'info' as const;
  if (bucket === '61-90') return 'warning' as const;
  return 'danger' as const;
};

const PayableAgingReportPage: React.FC = () => {
  const { selectedBranchId } = useBranchWriteAccess();
  const branchId = hasBranchContext(selectedBranchId) && selectedBranchId !== null ? selectedBranchId : 0;
  const [agingBucket, setAgingBucket] = useState('');
  const [supplierId, setSupplierId] = useState(0);
  const [suppliers, setSuppliers] = useState<SupplierItem[]>([]);
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');

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

  const table = useAgingReportTable<PayableAgingRow>({
    branchId,
    enabled: branchId > 0,
    fetcher: reportService.getPayableAgingReport,
    defaultSortColumn: 'daysOverdue',
    agingBucket,
    supplierId,
    fromDate,
    toDate,
  });

  const fetchExportPage = useCallback(async (pageNumber: number, pageSize: number) => {
    const res = await reportService.getPayableAgingReport(branchId, {
      pageNumber,
      pageSize,
      search: table.search.trim() || undefined,
      sortColumn: table.sortColumn,
      sortDirection: table.sortDirection,
      agingBucket: agingBucket || undefined,
      ...(supplierId > 0 ? { supplierId } : {}),
      ...(fromDate ? { fromDate } : {}),
      ...(toDate ? { toDate } : {}),
    });
    return { data: res.data.data, totalRecords: res.data.totalRecords };
  }, [branchId, table.search, table.sortColumn, table.sortDirection, agingBucket, supplierId, fromDate, toDate]);

  const { exporting, onExport } = useReportExport(
    'payable-aging-report',
    payableAgingExportColumns,
    fetchExportPage,
    branchId > 0,
  );

  const columns: Column<PayableAgingRow>[] = useMemo(() => [
    { key: 'supplierName', header: 'Supplier', sortable: true },
    { key: 'invoiceNo', header: 'Invoice No', sortable: true, render: (v) => <span className="font-mono text-xs">{safeString(v)}</span> },
    { key: 'invoiceDate', header: 'Invoice Date', sortable: true, render: (v) => formatDate(String(v)) },
    { key: 'totalAmount', header: 'Total', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    { key: 'paidAmount', header: 'Paid', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    { key: 'outstanding', header: 'Outstanding', sortable: true, render: (v) => <span className="font-semibold text-orange-700">{fmt(Number(v ?? 0))}</span> },
    { key: 'daysOverdue', header: 'Days Overdue', sortable: true },
    {
      key: 'agingBucket',
      header: 'Bucket',
      sortable: true,
      render: (v) => <Badge variant={bucketVariant(String(v))} size="sm">{String(v)}</Badge>,
    },
  ], []);

  const footerRow = useMemo(() => {
    if (!table.summary) return undefined;
    return {
      label: 'Total',
      values: {
        supplierName: 'Total',
        outstanding: <span className="font-bold text-orange-700">{fmt(table.summary.totalOutstanding)}</span>,
      },
    };
  }, [table.summary]);

  if (branchId <= 0) {
    return (
      <div className="rounded-md border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-700">
        Select a branch from the header to view this report.
      </div>
    );
  }

  return (
    <ReportPageShell
      title="Payable Aging Report"
      description="Open credit purchase balances from GL charges minus payments, grouped by aging buckets."
      fromDate={fromDate}
      toDate={toDate}
      onFromDateChange={(v) => { setFromDate(v); table.setPageNumber(1); }}
      onToDateChange={(v) => { setToDate(v); table.setPageNumber(1); }}
      extraFilters={(
        <>
          <SupplierFilter
            suppliers={suppliers}
            value={supplierId}
            onChange={(id) => { setSupplierId(id); table.setPageNumber(1); }}
          />
          <AgingBucketFilter value={agingBucket} onChange={(v) => { setAgingBucket(v); table.setPageNumber(1); }} />
        </>
      )}
      summary={<AgingAttractiveSummary summary={table.summary} variant="payable" loading={table.loading} />}
      error={table.error}
      loading={table.loading}
      onRefresh={table.reload}
      onExport={onExport}
      exporting={exporting}
      columns={columns}
      rows={table.rows}
      searchPlaceholder="Search supplier or invoice..."
      emptyMessage="No outstanding payables found."
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
    />
  );
};

export default PayableAgingReportPage;
