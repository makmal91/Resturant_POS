import React, { useCallback, useEffect, useMemo, useState } from 'react';
import type { Column } from '../../components/DataTable';
import ReportPageShell from '../reports/ReportPageShell';
import { getApiErrorMessage } from '../../services/api';
import { useBranchStore } from '../../stores/useBranchStore';
import { useBusinessCurrency } from '../../hooks/useBusinessCurrency';
import {
  cashFlowService,
  type PosRegisterDto,
  type RegisterSessionDto,
} from './cashFlowService';

type HistoryRow = RegisterSessionDto & Record<string, unknown>;

const formatDate = (value: string) => {
  const d = new Date(value);
  return Number.isNaN(d.getTime()) ? value : d.toLocaleDateString();
};

export default function RegisterHistoryReportPage() {
  const { selectedBranchId } = useBranchStore();
  const { fmt } = useBusinessCurrency();
  const branchId = selectedBranchId ?? 0;

  const [registers, setRegisters] = useState<PosRegisterDto[]>([]);
  const [registerId, setRegisterId] = useState<number | ''>('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [rows, setRows] = useState<HistoryRow[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [search, setSearch] = useState('');
  const [sortColumn, setSortColumn] = useState('sessionDate');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('desc');

  useEffect(() => {
    if (branchId <= 0) return;
    cashFlowService.getRegisters(branchId).then((res) => setRegisters(res.data ?? []));
  }, [branchId]);

  const load = useCallback(async () => {
    if (branchId <= 0) return;
    setLoading(true);
    setError(null);
    try {
      const res = await cashFlowService.getRegisterHistory(branchId, {
        posRegisterId: registerId === '' ? undefined : registerId,
        from: fromDate || undefined,
        to: toDate || undefined,
        page,
        pageSize,
      });
      setRows((res.data.items ?? []) as HistoryRow[]);
      setTotalRecords(res.data.totalRecords ?? 0);
      setTotalPages(res.data.totalPages ?? 0);
    } catch (e) {
      setError(getApiErrorMessage(e, 'Failed to load register history.'));
    } finally {
      setLoading(false);
    }
  }, [branchId, registerId, fromDate, toDate, page, pageSize]);

  useEffect(() => { void load(); }, [load]);

  const filteredRows = useMemo(() => {
    if (!search.trim()) return rows;
    const q = search.toLowerCase();
    return rows.filter(
      (r) =>
        r.registerName.toLowerCase().includes(q) ||
        (r.openedByName ?? '').toLowerCase().includes(q) ||
        (r.closedByName ?? '').toLowerCase().includes(q),
    );
  }, [rows, search]);

  const columns: Column<HistoryRow>[] = useMemo(
    () => [
      { key: 'sessionDate', header: 'Date', sortable: true, render: (v) => formatDate(String(v ?? '')) },
      { key: 'registerName', header: 'Register', sortable: true },
      { key: 'openingBalance', header: 'Opening', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
      { key: 'totalCashSales', header: 'Sales', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
      { key: 'totalExpensesCash', header: 'Expenses', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
      { key: 'expectedClosing', header: 'Expected', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
      { key: 'physicalCash', header: 'Physical', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
      {
        key: 'difference',
        header: 'Difference',
        sortable: true,
        render: (v) => {
          const d = Number(v ?? 0);
          return (
            <span className={d < 0 ? 'text-red-600 font-medium' : d > 0 ? 'text-emerald-600 font-medium' : ''}>
              {fmt(d)}
            </span>
          );
        },
      },
      { key: 'closedByName', header: 'Closed By', render: (v) => (v as string) || '—' },
    ],
    [fmt],
  );

  if (branchId <= 0) {
    return (
      <div className="flex h-64 items-center justify-center text-gray-500">
        Please select a branch first.
      </div>
    );
  }

  return (
    <ReportPageShell<HistoryRow>
      title="Register History"
      description="Opening and closing sessions per cash drawer — sales, expenses, and cash differences."
      fromDate={fromDate}
      toDate={toDate}
      onFromDateChange={(v) => { setFromDate(v); setPage(1); }}
      onToDateChange={(v) => { setToDate(v); setPage(1); }}
      extraFilters={
        <select
          value={registerId}
          onChange={(e) => {
            setRegisterId(e.target.value === '' ? '' : Number(e.target.value));
            setPage(1);
          }}
          className="rounded-md border border-gray-300 px-3 py-2 text-sm"
        >
          <option value="">All registers</option>
          {registers.map((r) => (
            <option key={r.id} value={r.id}>
              {r.name}
            </option>
          ))}
        </select>
      }
      error={error}
      loading={loading}
      onRefresh={load}
      columns={columns}
      rows={filteredRows}
      searchPlaceholder="Search register or user…"
      emptyMessage="No closed register sessions found for this period."
      pageNumber={page}
      pageSize={pageSize}
      totalRecords={totalRecords}
      totalPages={totalPages}
      search={search}
      sortColumn={sortColumn}
      sortDirection={sortDirection}
      onPageChange={setPage}
      onPageSizeChange={(s) => { setPageSize(s); setPage(1); }}
      onSearchChange={setSearch}
      onSortChange={(col, dir) => { setSortColumn(col); setSortDirection(dir); }}
    />
  );
}
