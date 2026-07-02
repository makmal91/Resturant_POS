import React, { useEffect, useMemo, useState } from 'react';

export interface Column<T> {
  key: keyof T | string;
  header: string;
  render?: (value: any, item: T) => React.ReactNode;
  sortable?: boolean;
  width?: string;
}

export interface Action<T> {
  label: string;
  onClick: (item: T) => void;
  variant?: 'primary' | 'secondary' | 'danger';
  icon?: React.ReactNode;
  hidden?: (item: T) => boolean;
}

interface DataTableProps<T> {
  data: T[];
  columns: Column<T>[];
  actions?: Action<T>[];
  searchable?: boolean;
  searchPlaceholder?: string;
  pagination?: boolean;
  pageSize?: number;
  loading?: boolean;
  emptyMessage?: string;
  serverSide?: boolean;
  totalRecords?: number;
  totalPages?: number;
  currentPage?: number;
  onPageChange?: (page: number) => void;
  searchTerm?: string;
  onSearchChange?: (value: string) => void;
  sortColumn?: string | null;
  sortDirection?: 'asc' | 'desc';
  onSortChange?: (column: string, direction: 'asc' | 'desc') => void;
  pageSizeOptions?: number[];
  onPageSizeChange?: (pageSize: number) => void;
  footerRow?: {
    label?: string;
    values?: Partial<Record<string, React.ReactNode>>;
  };
  /** When true, table body scrolls inside the card; header row stays sticky. */
  fillHeight?: boolean;
}

function getVisiblePageNumbers(currentPage: number, totalPages: number): number[] {
  const safeTotalPages = Math.max(1, totalPages);

  if (safeTotalPages <= 7) {
    return Array.from({ length: safeTotalPages }, (_, index) => index + 1);
  }

  let start = Math.max(1, currentPage - 2);
  let end = Math.min(safeTotalPages, start + 4);
  start = Math.max(1, end - 4);

  return Array.from({ length: end - start + 1 }, (_, index) => start + index);
}

function DataTable<T extends Record<string, any>>({
  data,
  columns,
  actions = [],
  searchable = true,
  searchPlaceholder = 'Search...',
  pagination = true,
  pageSize = 10,
  loading = false,
  emptyMessage = 'No data available',
  serverSide = false,
  totalRecords,
  totalPages: serverTotalPages,
  currentPage: controlledCurrentPage,
  onPageChange,
  searchTerm: controlledSearchTerm,
  onSearchChange,
  sortColumn: controlledSortColumn,
  sortDirection: controlledSortDirection,
  onSortChange,
  pageSizeOptions,
  onPageSizeChange,
  footerRow,
  fillHeight = false,
}: DataTableProps<T>) {
  const [internalSearchTerm, setInternalSearchTerm] = useState('');
  const [internalCurrentPage, setInternalCurrentPage] = useState(1);
  const [internalSortColumn, setInternalSortColumn] = useState<string | null>(null);
  const [internalSortDirection, setInternalSortDirection] = useState<'asc' | 'desc'>('asc');

  const searchTerm = controlledSearchTerm ?? internalSearchTerm;
  const currentPage = controlledCurrentPage ?? internalCurrentPage;
  const sortColumn = controlledSortColumn ?? internalSortColumn;
  const sortDirection = controlledSortDirection ?? internalSortDirection;

  const filteredData = useMemo(() => {
    if (serverSide || !searchTerm) {
      return data;
    }

    return data.filter((item) =>
      columns.some((column) => {
        const value = item[column.key as keyof T];
        return value?.toString().toLowerCase().includes(searchTerm.toLowerCase());
      })
    );
  }, [data, searchTerm, columns, serverSide]);

  const sortedData = useMemo(() => {
    if (serverSide || !sortColumn) {
      return filteredData;
    }

    return [...filteredData].sort((a, b) => {
      const aValue = a[sortColumn as keyof T];
      const bValue = b[sortColumn as keyof T];

      if (aValue < bValue) return sortDirection === 'asc' ? -1 : 1;
      if (aValue > bValue) return sortDirection === 'asc' ? 1 : -1;
      return 0;
    });
  }, [filteredData, sortColumn, sortDirection, serverSide]);

  const paginatedData = useMemo(() => {
    if (!pagination || serverSide) {
      return sortedData;
    }

    const startIndex = (currentPage - 1) * pageSize;
    return sortedData.slice(startIndex, startIndex + pageSize);
  }, [sortedData, currentPage, pageSize, pagination, serverSide]);

  const resolvedTotalRecords = serverSide ? (totalRecords ?? data.length) : sortedData.length;
  const resolvedTotalPages = serverSide
    ? Math.max(
        serverTotalPages ?? 0,
        resolvedTotalRecords > 0 ? Math.ceil(resolvedTotalRecords / pageSize) : 1
      )
    : Math.max(1, Math.ceil(sortedData.length / pageSize));

  const visiblePages = getVisiblePageNumbers(currentPage, resolvedTotalPages);
  const displayFrom = resolvedTotalRecords === 0 ? 0 : ((currentPage - 1) * pageSize) + 1;
  const displayTo = resolvedTotalRecords === 0 ? 0 : Math.min(currentPage * pageSize, resolvedTotalRecords);
  const canGoPrevious = currentPage > 1;
  const canGoNext = currentPage < resolvedTotalPages && resolvedTotalRecords > 0;

  const handleSort = (columnKey: string) => {
    if (serverSide && onSortChange) {
      const nextDirection =
        sortColumn === columnKey && sortDirection === 'asc' ? 'desc' : 'asc';
      onSortChange(columnKey, nextDirection);
      return;
    }

    if (sortColumn === columnKey) {
      setInternalSortDirection(sortDirection === 'asc' ? 'desc' : 'asc');
    } else {
      setInternalSortColumn(columnKey);
      setInternalSortDirection('asc');
    }
  };

  const handleSearchChange = (value: string) => {
    if (onSearchChange) {
      onSearchChange(value);
    } else {
      setInternalSearchTerm(value);
      setInternalCurrentPage(1);
    }
  };

  const handlePageChange = (page: number) => {
    if (page < 1 || page > resolvedTotalPages) {
      return;
    }

    if (onPageChange) {
      onPageChange(page);
    } else {
      setInternalCurrentPage(page);
    }
  };

  useEffect(() => {
    if (!serverSide) {
      setInternalCurrentPage(1);
    }
  }, [searchTerm, serverSide]);

  if (loading) {
    return (
      <div
        className={`bg-white rounded-lg shadow-sm border border-gray-200 ${
          fillHeight ? 'flex h-full min-h-0 flex-col' : ''
        }`}
      >
        <div className={`p-8 text-center ${fillHeight ? 'flex flex-1 items-center justify-center' : ''}`}>
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-2 text-gray-600">Loading...</p>
        </div>
      </div>
    );
  }

  return (
    <div
      className={`bg-white rounded-lg shadow-sm border border-gray-200 ${
        fillHeight ? 'flex h-full min-h-0 flex-col overflow-hidden' : ''
      }`}
    >
      {searchable && (
        <div className="shrink-0 p-4 border-b border-gray-200 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div className="relative max-w-md w-full">
            <input
              type="text"
              placeholder={searchPlaceholder}
              value={searchTerm}
              onChange={(e) => handleSearchChange(e.target.value)}
              className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            />
            <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
              <svg className="h-5 w-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
            </div>
          </div>

          {pageSizeOptions && pageSizeOptions.length > 0 && onPageSizeChange && (
            <div className="flex items-center gap-2 text-sm text-gray-700 sm:ml-auto">
              <label htmlFor="page-size-select">Rows per page</label>
              <select
                id="page-size-select"
                value={pageSize}
                onChange={(e) => onPageSizeChange(Number(e.target.value))}
                className="rounded-md border border-gray-300 px-2 py-1.5 focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                {pageSizeOptions.map((option) => (
                  <option key={option} value={option}>
                    {option}
                  </option>
                ))}
              </select>
            </div>
          )}
        </div>
      )}

      {!searchable && pageSizeOptions && pageSizeOptions.length > 0 && onPageSizeChange && (
        <div className="shrink-0 px-4 py-3 border-b border-gray-200 flex justify-end">
          <div className="flex items-center gap-2 text-sm text-gray-700">
            <label htmlFor="page-size-select">Rows per page</label>
            <select
              id="page-size-select"
              value={pageSize}
              onChange={(e) => onPageSizeChange(Number(e.target.value))}
              className="rounded-md border border-gray-300 px-2 py-1.5 focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              {pageSizeOptions.map((option) => (
                <option key={option} value={option}>
                  {option}
                </option>
              ))}
            </select>
          </div>
        </div>
      )}

      <div className={fillHeight ? 'min-h-0 flex-1 overflow-auto' : 'overflow-x-auto'}>
        <table className="min-w-full divide-y divide-gray-200">
          <thead className={`bg-gray-50 ${fillHeight ? 'sticky top-0 z-10 shadow-sm' : ''}`}>
            <tr>
              {columns.map((column) => (
                <th
                  key={column.key as string}
                  className={`px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider ${
                    column.sortable ? 'cursor-pointer hover:bg-gray-100 select-none' : ''
                  }`}
                  style={{ width: column.width }}
                  onClick={() => column.sortable && handleSort(column.key as string)}
                >
                  <div className="flex items-center space-x-1">
                    <span>{column.header}</span>
                    {column.sortable && sortColumn === column.key && (
                      <svg className={`w-4 h-4 ${sortDirection === 'desc' ? 'transform rotate-180' : ''}`} fill="currentColor" viewBox="0 0 20 20">
                        <path fillRule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clipRule="evenodd" />
                      </svg>
                    )}
                  </div>
                </th>
              ))}
              {actions.length > 0 && (
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Actions
                </th>
              )}
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {paginatedData.length === 0 ? (
              <tr>
                <td colSpan={columns.length + (actions.length > 0 ? 1 : 0)} className="px-6 py-12 text-center text-gray-500">
                  {emptyMessage}
                </td>
              </tr>
            ) : (
              paginatedData.map((item, index) => (
                <tr key={index} className="hover:bg-gray-50">
                  {columns.map((column) => (
                    <td key={column.key as string} className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {column.render
                        ? column.render(item[column.key as keyof T], item)
                        : String(item[column.key as keyof T] || '')
                      }
                    </td>
                  ))}
                  {actions.length > 0 && (
                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                      {actions.map((action, actionIndex) => {
                        if (action.hidden?.(item)) {
                          return null;
                        }

                        return (
                        <button
                          key={actionIndex}
                          onClick={() => action.onClick(item)}
                          className={`inline-flex items-center px-3 py-1 rounded-md text-sm font-medium ${
                            action.variant === 'danger'
                              ? 'text-red-600 hover:text-red-900 hover:bg-red-50'
                              : action.variant === 'primary'
                              ? 'text-blue-600 hover:text-blue-900 hover:bg-blue-50'
                              : 'text-gray-600 hover:text-gray-900 hover:bg-gray-50'
                          }`}
                        >
                          {action.icon && <span className="mr-1">{action.icon}</span>}
                          {action.label}
                        </button>
                        );
                      })}
                    </td>
                  )}
                </tr>
              ))
            )}
          </tbody>
          {footerRow && (serverSide ? resolvedTotalRecords > 0 : paginatedData.length > 0) && (
            <tfoot>
              <tr className="border-t-2 border-gray-200 bg-gray-50 font-semibold text-gray-800">
                {columns.map((column, index) => {
                  const key = column.key as string;
                  const content = index === 0
                    ? (footerRow.values?.[key] ?? footerRow.label ?? 'Total')
                    : footerRow.values?.[key] ?? '—';

                  return (
                    <td key={key} className="px-6 py-3 whitespace-nowrap text-sm">
                      {content}
                    </td>
                  );
                })}
                {actions.length > 0 && <td className="px-6 py-3" />}
              </tr>
            </tfoot>
          )}
        </table>
      </div>

      {pagination && (
        <div className="shrink-0 px-4 py-3 border-t border-gray-200 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div className="text-sm text-gray-700">
            {resolvedTotalRecords === 0
              ? 'No results found'
              : `Showing ${displayFrom} to ${displayTo} of ${resolvedTotalRecords} results`}
            {searchTerm && resolvedTotalRecords > 0 && (
              <span className="text-gray-500"> (filtered)</span>
            )}
          </div>

          <div className="flex flex-wrap items-center gap-2">
            <span className="text-sm text-gray-500 mr-1">
              Page {currentPage} of {resolvedTotalPages}
            </span>

            <button
              type="button"
              onClick={() => handlePageChange(currentPage - 1)}
              disabled={!canGoPrevious}
              className="px-3 py-1.5 text-sm border border-gray-300 rounded-md disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
            >
              Previous
            </button>

            {visiblePages.map((pageNumber) => (
              <button
                key={pageNumber}
                type="button"
                onClick={() => handlePageChange(pageNumber)}
                disabled={resolvedTotalRecords === 0}
                className={`min-w-[2.25rem] px-3 py-1.5 text-sm border rounded-md ${
                  currentPage === pageNumber
                    ? 'bg-blue-600 text-white border-blue-600'
                    : 'border-gray-300 hover:bg-gray-50'
                } disabled:opacity-50 disabled:cursor-not-allowed`}
              >
                {pageNumber}
              </button>
            ))}

            <button
              type="button"
              onClick={() => handlePageChange(currentPage + 1)}
              disabled={!canGoNext}
              className="px-3 py-1.5 text-sm border border-gray-300 rounded-md disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
            >
              Next
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

export default DataTable;
