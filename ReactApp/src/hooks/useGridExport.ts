import { useCallback, useState } from 'react';
import { exportGridListing, type GridExportColumn } from '../utils/gridExport';

export function useGridExport<T extends Record<string, unknown>>(
  filename: string,
  columns: GridExportColumn<T>[],
  fetchPage: (pageNumber: number, pageSize: number) => Promise<{ data: T[]; totalRecords: number }>,
  enabled = true,
) {
  const [exporting, setExporting] = useState(false);

  const onExport = useCallback(async () => {
    if (!enabled) return;
    setExporting(true);
    try {
      await exportGridListing(filename, columns, fetchPage);
    } finally {
      setExporting(false);
    }
  }, [enabled, filename, columns, fetchPage]);

  return { exporting, onExport };
}
