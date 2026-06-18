import { useCallback, useState } from 'react';
import { exportReportListing } from './reportExport';
import type { ReportExportColumn } from './reportExport';

export function useReportExport<T extends Record<string, unknown>>(
  filename: string,
  columns: ReportExportColumn<T>[],
  fetchPage: (pageNumber: number, pageSize: number) => Promise<{ data: T[]; totalRecords: number }>,
  enabled = true,
) {
  const [exporting, setExporting] = useState(false);

  const onExport = useCallback(async () => {
    if (!enabled) return;
    setExporting(true);
    try {
      await exportReportListing(filename, columns, fetchPage);
    } finally {
      setExporting(false);
    }
  }, [enabled, filename, columns, fetchPage]);

  return { exporting, onExport };
}
