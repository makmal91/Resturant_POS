import { useCallback, useEffect, useState } from 'react';
import { getApiErrorMessage } from '../../services/api';
import type { ReportPagedResponse, ReportQueryParams } from './reportService';

interface UseReportTableOptions<T> {
  branchId: number;
  enabled: boolean;
  fetcher: (branchId: number, params: ReportQueryParams) => Promise<{ data: ReportPagedResponse<T> }>;
  defaultSortColumn: string;
  defaultSortDirection?: 'asc' | 'desc';
  defaultPageSize?: number;
  includeDates?: boolean;
  fromDate?: string;
  toDate?: string;
  customerId?: number;
  supplierId?: number;
}

export function useReportTable<T>({
  branchId,
  enabled,
  fetcher,
  defaultSortColumn,
  defaultSortDirection = 'desc',
  defaultPageSize = 25,
  includeDates = true,
  fromDate = '',
  toDate = '',
  customerId,
  supplierId,
}: UseReportTableOptions<T>) {
  const [rows, setRows] = useState<T[]>([]);
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
      setTotalRecords(0);
      setTotalPages(0);
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const params: ReportQueryParams = {
        pageNumber,
        pageSize,
        search: search.trim() || undefined,
        sortColumn,
        sortDirection,
        ...(includeDates ? { fromDate, toDate } : {}),
        ...(customerId && customerId > 0 ? { customerId } : {}),
        ...(supplierId && supplierId > 0 ? { supplierId } : {}),
      };
      const res = await fetcher(branchId, params);
      const payload = res.data;
      setRows(Array.isArray(payload?.data) ? payload.data : []);
      setTotalRecords(payload?.totalRecords ?? 0);
      setTotalPages(payload?.totalPages ?? 0);
    } catch (err) {
      setRows([]);
      setTotalRecords(0);
      setTotalPages(0);
      setError(getApiErrorMessage(err, 'Failed to load report.'));
    } finally {
      setLoading(false);
    }
  }, [
    branchId,
    enabled,
    fetcher,
    includeDates,
    fromDate,
    toDate,
    customerId,
    supplierId,
    pageNumber,
    pageSize,
    search,
    sortColumn,
    sortDirection,
  ]);

  useEffect(() => {
    const timer = setTimeout(() => { void load(); }, search ? 300 : 0);
    return () => clearTimeout(timer);
  }, [load, search]);

  useEffect(() => {
    setPageNumber(1);
  }, [branchId, fromDate, toDate, pageSize, customerId, supplierId]);

  const onSearchChange = (value: string) => {
    setSearch(value);
    setPageNumber(1);
  };

  const onSortChange = (column: string, direction: 'asc' | 'desc') => {
    setSortColumn(column);
    setSortDirection(direction);
    setPageNumber(1);
  };

  const onPageSizeChange = (size: number) => {
    setPageSize(size);
    setPageNumber(1);
  };

  return {
    rows,
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
    onSearchChange,
    onSortChange,
    onPageSizeChange,
    reload: load,
  };
}
