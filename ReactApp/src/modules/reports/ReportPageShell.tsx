import React from 'react';
import DataTable, { type Column } from '../../components/DataTable';

interface ReportPageShellProps<T extends Record<string, unknown>> {
  title: string;
  description: string;
  showDateFilters?: boolean;
  fromDate?: string;
  toDate?: string;
  onFromDateChange?: (value: string) => void;
  onToDateChange?: (value: string) => void;
  extraFilters?: React.ReactNode;
  summary?: React.ReactNode;
  error?: string | null;
  loading: boolean;
  onRefresh: () => void;
  columns: Column<T>[];
  rows: T[];
  searchPlaceholder: string;
  emptyMessage: string;
  pageNumber: number;
  pageSize: number;
  totalRecords: number;
  totalPages: number;
  search: string;
  sortColumn: string;
  sortDirection: 'asc' | 'desc';
  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;
  onSearchChange: (value: string) => void;
  onSortChange: (column: string, direction: 'asc' | 'desc') => void;
  footerRow?: {
    label?: string;
    values?: Partial<Record<string, React.ReactNode>>;
  };
  onExport?: () => Promise<void>;
  exporting?: boolean;
  exportLabel?: string;
  onPrint?: () => void;
}

export default function ReportPageShell<T extends Record<string, unknown>>({
  title,
  description,
  showDateFilters = true,
  fromDate,
  toDate,
  onFromDateChange,
  onToDateChange,
  extraFilters,
  summary,
  error,
  loading,
  onRefresh,
  columns,
  rows,
  searchPlaceholder,
  emptyMessage,
  pageNumber,
  pageSize,
  totalRecords,
  totalPages,
  search,
  sortColumn,
  sortDirection,
  onPageChange,
  onPageSizeChange,
  onSearchChange,
  onSortChange,
  footerRow,
  onExport,
  exporting = false,
  exportLabel = 'Export CSV',
  onPrint,
}: ReportPageShellProps<T>) {
  return (
    <div className="print-area">
      {error && (
        <div className="mb-6 rounded-md bg-red-50 p-4 text-red-800">
          <span className="font-medium">{error}</span>
        </div>
      )}

      <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h1 className="mb-2 text-3xl font-bold text-gray-900">{title}</h1>
          <p className="text-gray-600">{description}</p>
        </div>
        <div className="flex flex-wrap items-center gap-2 self-start print:hidden">
          {onPrint && (
            <button
              type="button"
              onClick={onPrint}
              disabled={loading}
              className="inline-flex items-center rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 shadow-sm hover:bg-gray-50 disabled:opacity-60"
            >
              Print
            </button>
          )}
          {onExport && (
            <button
              type="button"
              onClick={() => void onExport()}
              disabled={loading || exporting}
              className="inline-flex items-center rounded-md border border-emerald-300 bg-emerald-50 px-4 py-2 text-sm font-medium text-emerald-800 shadow-sm hover:bg-emerald-100 disabled:opacity-60"
            >
              {exporting ? 'Exporting…' : exportLabel}
            </button>
          )}
          <button
            type="button"
            onClick={() => void onRefresh()}
            disabled={loading}
            className="inline-flex items-center rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 shadow-sm hover:bg-gray-50 disabled:opacity-60"
          >
            {loading ? 'Loading…' : 'Refresh'}
          </button>
        </div>
      </div>

      <div className="mb-6 grid grid-cols-1 gap-4 rounded-xl border border-gray-100 bg-white p-5 shadow-sm print:hidden sm:grid-cols-2 lg:grid-cols-4">
        {showDateFilters && (
          <>
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">From Date</label>
              <input
                type="date"
                value={fromDate}
                onChange={(e) => onFromDateChange?.(e.target.value)}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">To Date</label>
              <input
                type="date"
                value={toDate}
                onChange={(e) => onToDateChange?.(e.target.value)}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
              />
            </div>
          </>
        )}
        {extraFilters}
      </div>

      {summary}

      <DataTable
        data={rows}
        columns={columns}
        loading={loading}
        searchable
        searchPlaceholder={searchPlaceholder}
        pagination
        pageSize={pageSize}
        pageSizeOptions={[10, 25, 50, 100]}
        onPageSizeChange={onPageSizeChange}
        emptyMessage={emptyMessage}
        serverSide
        totalRecords={totalRecords}
        totalPages={totalPages}
        currentPage={pageNumber}
        onPageChange={onPageChange}
        searchTerm={search}
        onSearchChange={onSearchChange}
        sortColumn={sortColumn}
        sortDirection={sortDirection}
        onSortChange={onSortChange}
        footerRow={footerRow}
      />
    </div>
  );
}
