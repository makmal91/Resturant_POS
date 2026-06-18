import { useCallback, useEffect, useState } from 'react';
import { fetchAllReportPages } from './reportExport';

interface UseReportAggregatesOptions<T extends Record<string, unknown>> {
  enabled: boolean;
  deps: unknown[];
  fetchPage: (pageNumber: number, pageSize: number) => Promise<{ data: T[]; totalRecords: number }>;
  aggregate: (rows: T[]) => Record<string, number>;
}

export function useReportAggregates<T extends Record<string, unknown>>({
  enabled,
  deps,
  fetchPage,
  aggregate,
}: UseReportAggregatesOptions<T>) {
  const [totals, setTotals] = useState<Record<string, number> | null>(null);
  const [loading, setLoading] = useState(false);

  const reload = useCallback(async () => {
    if (!enabled) {
      setTotals(null);
      return;
    }
    setLoading(true);
    try {
      const rows = await fetchAllReportPages(fetchPage);
      setTotals(aggregate(rows));
    } catch {
      setTotals(null);
    } finally {
      setLoading(false);
    }
  }, [enabled, fetchPage, aggregate]);

  useEffect(() => {
    void reload();
  }, [reload, ...deps]);

  return { totals, loading, reload };
}
