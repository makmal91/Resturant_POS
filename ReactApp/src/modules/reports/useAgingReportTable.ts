import { useCallback, useEffect, useState } from 'react';
import { getApiErrorMessage } from '../../services/api';
import type { AgingReportSummary, ReportQueryParams } from './reportService';

export interface AgingReportPagedResponse<T> {
  data: T[];
  totalRecords: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  summary: AgingReportSummary;
}

interface UseAgingReportTableOptions<T> {
  branchId: number;
  enabled: boolean;
  fetcher: (branchId: number, params: ReportQueryParams) => Promise<{ data: AgingReportPagedResponse<T> }>;
  defaultSortColumn?: string;
  defaultSortDirection?: 'asc' | 'desc';
  defaultPageSize?: number;
  agingBucket?: string;
  customerId?: number;
  supplierId?: number;
}

export function useAgingReportTable<T>({
  branchId,
  enabled,
  fetcher,
  defaultSortColumn = 'daysOverdue',
  defaultSortDirection = 'desc',
  defaultPageSize = 25,
  agingBucket = '',
  customerId,
  supplierId,
}: UseAgingReportTableOptions<T>) {
  const [rows, setRows] = useState<T[]>([]);
  const [summary, setSummary] = useState<AgingReportSummary | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(defaultPageSize);
  const [search, setSearch] = useState('');
  const [sortColumn, setSortColumn] = useState(defaultSortColumn);
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>(defaultSortDirection);
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages] = useState(0);

  const load = useCallback(async () => {
    if (!enabled || branchId <= 0) {
      setRows([]);
      setSummary(null);
      setTotalRecords(0);
      setTotalPages(0);
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const res = await fetcher(branchId, {
        pageNumber,
        pageSize,
        search: search.trim() || undefined,
        sortColumn,
        sortDirection,
        agingBucket: agingBucket || undefined,
        customerId: customerId && customerId > 0 ? customerId : undefined,
        supplierId: supplierId && supplierId > 0 ? supplierId : undefined,
      });
      const payload = res.data;
      setRows(Array.isArray(payload?.data) ? payload.data : []);
      setSummary(payload?.summary ?? null);
      setTotalRecords(payload?.totalRecords ?? 0);
      setTotalPages(payload?.totalPages ?? 0);
    } catch (err) {
      setRows([]);
      setSummary(null);
      setTotalRecords(0);
      setTotalPages(0);
      setError(getApiErrorMessage(err, 'Failed to load aging report.'));
    } finally {
      setLoading(false);
    }
  }, [branchId, enabled, fetcher, agingBucket, customerId, supplierId, pageNumber, pageSize, search, sortColumn, sortDirection]);

  useEffect(() => {
    const timer = setTimeout(() => { void load(); }, search ? 300 : 0);
    return () => clearTimeout(timer);
  }, [load, search]);

  useEffect(() => {
    setPageNumber(1);
  }, [branchId, agingBucket, customerId, supplierId, pageSize]);

  return {
    rows,
    summary,
    loading,
    error,
    pageNumber,
    pageSize,
    search,
    sortColumn,
    sortDirection,
    totalRecords,
    totalPages,
    setPageNumber,
    onSearchChange: (value: string) => { setSearch(value); setPageNumber(1); },
    onSortChange: (column: string, direction: 'asc' | 'desc') => {
      setSortColumn(column);
      setSortDirection(direction);
      setPageNumber(1);
    },
    onPageSizeChange: (size: number) => { setPageSize(size); setPageNumber(1); },
    reload: load,
  };
}
