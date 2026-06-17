import { useCallback, useEffect, useState } from 'react';
import { PagedListMeta, PagedListParams } from '../modules/shared/pagedList';

interface UseServerSideTableOptions<T> {
  fetcher: (params: PagedListParams) => Promise<{ items: T[]; meta: PagedListMeta }>;
  enabled?: boolean;
  debounceSearchMs?: number;
  defaultPageSize?: number;
  defaultSortColumn?: string;
  defaultSortDirection?: 'asc' | 'desc';
  resetDeps?: unknown[];
}

export function useServerSideTable<T>({
  fetcher,
  enabled = true,
  debounceSearchMs = 300,
  defaultPageSize = 10,
  defaultSortColumn = 'name',
  defaultSortDirection = 'asc',
  resetDeps = [],
}: UseServerSideTableOptions<T>) {
  const [items, setItems] = useState<T[]>([]);
  const [loading, setLoading] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(defaultPageSize);
  const [searchTerm, setSearchTerm] = useState('');
  const [sortColumn, setSortColumn] = useState(defaultSortColumn);
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>(defaultSortDirection);
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages] = useState(0);

  const load = useCallback(async () => {
    if (!enabled) {
      setItems([]);
      setTotalRecords(0);
      setTotalPages(0);
      return;
    }

    setLoading(true);
    try {
      const result = await fetcher({
        page: currentPage,
        pageSize,
        search: searchTerm.trim() || undefined,
        sortBy: sortColumn,
        sortDirection,
      });
      setItems(result.items);
      setTotalRecords(result.meta.totalRecords);
      setTotalPages(result.meta.totalPages);
    } catch {
      setItems([]);
      setTotalRecords(0);
      setTotalPages(0);
    } finally {
      setLoading(false);
    }
  }, [enabled, fetcher, currentPage, pageSize, searchTerm, sortColumn, sortDirection]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void load();
    }, searchTerm ? debounceSearchMs : 0);
    return () => window.clearTimeout(timer);
  }, [load, searchTerm, debounceSearchMs]);

  useEffect(() => {
    setCurrentPage(1);
  }, [pageSize, ...resetDeps]);

  const handleSearchChange = (value: string) => {
    setSearchTerm(value);
    setCurrentPage(1);
  };

  const handleSortChange = (column: string, direction: 'asc' | 'desc') => {
    setSortColumn(column);
    setSortDirection(direction);
    setCurrentPage(1);
  };

  const handlePageSizeChange = (size: number) => {
    setPageSize(size);
    setCurrentPage(1);
  };

  return {
    items,
    loading,
    currentPage,
    pageSize,
    searchTerm,
    sortColumn,
    sortDirection,
    totalRecords,
    totalPages,
    setCurrentPage,
    setPageSize: handlePageSizeChange,
    setSearchTerm: handleSearchChange,
    setSortColumn,
    setSortDirection,
    handleSearchChange,
    handleSortChange,
    handlePageSizeChange,
    reload: load,
  };
}
