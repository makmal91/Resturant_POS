import React, { useCallback, useEffect, useMemo, useState } from 'react';
import type { Column } from '../../components/DataTable';
import { useBranchWriteAccess } from '../../hooks/useBranchWriteAccess';
import { getApiErrorMessage } from '../../services/api';
import { hasBranchContext } from '../../types/permissions';
import ProfitLossAttractiveSummary from './ProfitLossAttractiveSummary';
import ProfitLossStatementView from './ProfitLossStatementView';
import ReportPageShell from './ReportPageShell';
import ReportPeriodFilter from './ReportPeriodFilter';
import { exportStatementCsv } from './reportExport';
import { profitLossExportColumns } from './reportExportColumns';
import { fmt, formatDate } from './reportFormatters';
import {
  createDefaultPeriodState,
  formatPeriodLabel,
  resolvePeriodRange,
  type ProfitLossGroupBy,
  type ReportPeriodState,
} from './reportPeriodUtils';
import {
  reportService,
  type ProfitLossReportSummary,
  type ProfitLossRow,
  type ProfitLossStatement,
} from './reportService';
import { useReportExport } from './useReportExport';
import './reports.css';

type ReportView = 'statement' | 'details';

const groupByLabels: Record<ProfitLossGroupBy, string> = {
  day: 'Daily',
  month: 'Monthly',
  year: 'Yearly',
};

const formatPeriodCell = (value: string, groupBy: ProfitLossGroupBy) => {
  if (!value) return '—';
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return '—';
  if (groupBy === 'year') return String(d.getFullYear());
  if (groupBy === 'month') {
    return d.toLocaleDateString(undefined, { year: 'numeric', month: 'long' });
  }
  return formatDate(value);
};

const ProfitLossReportPage: React.FC = () => {
  const { selectedBranchId } = useBranchWriteAccess();
  const branchId = hasBranchContext(selectedBranchId) && selectedBranchId !== null ? selectedBranchId : 0;

  const [period, setPeriod] = useState<ReportPeriodState>(createDefaultPeriodState);
  const [view, setView] = useState<ReportView>('statement');
  const [groupBy, setGroupBy] = useState<ProfitLossGroupBy>('day');

  const { fromDate, toDate, groupBy: suggestedGroupBy } = useMemo(
    () => resolvePeriodRange(period),
    [period],
  );

  useEffect(() => {
    setGroupBy(suggestedGroupBy);
  }, [suggestedGroupBy]);

  const periodLabel = useMemo(
    () => formatPeriodLabel(fromDate, toDate),
    [fromDate, toDate],
  );

  const [statement, setStatement] = useState<ProfitLossStatement | null>(null);
  const [rows, setRows] = useState<ProfitLossRow[]>([]);
  const [summary, setSummary] = useState<ProfitLossReportSummary | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [search, setSearch] = useState('');
  const [sortColumn, setSortColumn] = useState('date');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('desc');
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [exportingStatement, setExportingStatement] = useState(false);

  const loadStatement = useCallback(async () => {
    if (branchId <= 0) return;
    setLoading(true);
    setError(null);
    try {
      const res = await reportService.getProfitLossStatement(branchId, { fromDate, toDate });
      setStatement(res.data);
      setSummary(res.data.summary);
    } catch (err) {
      setStatement(null);
      setSummary(null);
      setError(getApiErrorMessage(err, 'Failed to load profit & loss statement.'));
    } finally {
      setLoading(false);
    }
  }, [branchId, fromDate, toDate]);

  const loadDetails = useCallback(async () => {
    if (branchId <= 0) return;
    setLoading(true);
    setError(null);
    try {
      const res = await reportService.getProfitLossReport(branchId, {
        fromDate,
        toDate,
        groupBy,
        pageNumber,
        pageSize,
        search: search.trim() || undefined,
        sortColumn,
        sortDirection,
      });
      const payload = res.data;
      setRows(Array.isArray(payload?.data) ? payload.data : []);
      setTotalRecords(payload?.totalRecords ?? 0);
      setTotalPages(payload?.totalPages ?? 0);
      setSummary(payload?.summary ?? null);
    } catch (err) {
      setRows([]);
      setSummary(null);
      setError(getApiErrorMessage(err, 'Failed to load profit & loss details.'));
    } finally {
      setLoading(false);
    }
  }, [branchId, fromDate, toDate, groupBy, pageNumber, pageSize, search, sortColumn, sortDirection]);

  const load = view === 'statement' ? loadStatement : loadDetails;

  useEffect(() => {
    const timer = setTimeout(() => { void load(); }, view === 'details' && search ? 300 : 0);
    return () => clearTimeout(timer);
  }, [load, search, view]);

  useEffect(() => {
    setPageNumber(1);
  }, [branchId, fromDate, toDate, pageSize, groupBy, view]);

  const fetchExportPage = useCallback(async (exportPageNumber: number, exportPageSize: number) => {
    const res = await reportService.getProfitLossReport(branchId, {
      fromDate,
      toDate,
      groupBy,
      pageNumber: exportPageNumber,
      pageSize: exportPageSize,
      search: search.trim() || undefined,
      sortColumn,
      sortDirection,
    });
    return { data: res.data.data, totalRecords: res.data.totalRecords };
  }, [branchId, fromDate, toDate, groupBy, search, sortColumn, sortDirection]);

  const { exporting, onExport: onExportDetails } = useReportExport(
    `profit-loss-details-${fromDate}-${toDate}`,
    profitLossExportColumns,
    fetchExportPage,
    branchId > 0 && view === 'details',
  );

  const onExportStatement = useCallback(async () => {
    if (branchId <= 0) return;
    setExportingStatement(true);
    try {
      const res = await reportService.getProfitLossStatement(branchId, { fromDate, toDate });
      const data = res.data;
      const s = data.summary;
      const lines = [
        { section: 'Income', label: 'Sales Revenue', amount: fmt(s.totalRevenue) },
        ...(s.totalDiscounts > 0
          ? [{ section: 'Income', label: 'Less: Discounts', amount: `(${fmt(s.totalDiscounts)})` }]
          : []),
        { section: 'Income', label: 'Net Revenue', amount: fmt(s.totalRevenue) },
        { section: 'COGS', label: 'Cost of Goods Sold', amount: `(${fmt(s.totalCostOfGoodsSold)})` },
        { section: 'COGS', label: 'Gross Profit', amount: fmt(s.totalGrossProfit) },
        ...data.expenseLines.map((line) => ({
          section: 'Expenses',
          label: line.categoryName,
          amount: `(${fmt(line.amount)})`,
        })),
        { section: 'Expenses', label: 'Total Expenses', amount: `(${fmt(s.totalExpenses)})` },
        {
          section: 'Result',
          label: s.totalNetProfit >= 0 ? 'Net Profit' : 'Net Loss',
          amount: fmt(Math.abs(s.totalNetProfit)),
        },
      ];
      exportStatementCsv(`profit-loss-statement-${fromDate}-${toDate}`, lines);
    } finally {
      setExportingStatement(false);
    }
  }, [branchId, fromDate, toDate]);

  const onExport = view === 'statement' ? onExportStatement : onExportDetails;
  const isExporting = view === 'statement' ? exportingStatement : exporting;

  const onPrint = useCallback(() => {
    window.print();
  }, []);

  const periodColumnHeader = groupByLabels[groupBy];

  const columns: Column<ProfitLossRow>[] = useMemo(() => [
    {
      key: 'date',
      header: periodColumnHeader,
      sortable: true,
      render: (v) => formatPeriodCell(String(v), groupBy),
    },
    { key: 'revenue', header: 'Revenue', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    { key: 'costOfGoodsSold', header: 'COGS', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    { key: 'grossProfit', header: 'Gross Profit', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    { key: 'expenses', header: 'Expenses', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    {
      key: 'netProfit',
      header: 'Net Profit',
      sortable: true,
      render: (v) => <span className="font-semibold text-emerald-700">{fmt(Number(v ?? 0))}</span>,
    },
    { key: 'salesCount', header: 'Sales', sortable: true },
  ], [groupBy, periodColumnHeader]);

  const footerRow = useMemo(() => {
    if (!summary || view !== 'details') return undefined;
    const netPositive = summary.totalNetProfit >= 0;
    return {
      label: 'Total',
      values: {
        date: 'Total',
        revenue: fmt(summary.totalRevenue),
        costOfGoodsSold: fmt(summary.totalCostOfGoodsSold),
        grossProfit: fmt(summary.totalGrossProfit),
        expenses: fmt(summary.totalExpenses),
        netProfit: (
          <span className={`font-bold ${netPositive ? 'text-emerald-700' : 'text-red-600'}`}>
            {fmt(summary.totalNetProfit)}
          </span>
        ),
        salesCount: String(summary.totalSalesCount),
      },
    };
  }, [summary, view]);

  if (branchId <= 0) {
    return (
      <div className="rounded-md border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-700">
        Select a branch from the header to view this report.
      </div>
    );
  }

  const extraFilters = (
    <>
      <ReportPeriodFilter value={period} onChange={setPeriod} />
      {view === 'details' && (
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">Group By</label>
          <select
            value={groupBy}
            onChange={(e) => setGroupBy(e.target.value as ProfitLossGroupBy)}
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
          >
            {(Object.keys(groupByLabels) as ProfitLossGroupBy[]).map((key) => (
              <option key={key} value={key}>{groupByLabels[key]}</option>
            ))}
          </select>
        </div>
      )}
    </>
  );

  return (
    <div className="print-area">
      <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between print:hidden">
        <div className="inline-flex rounded-lg border border-gray-200 bg-gray-50 p-1">
          <button
            type="button"
            onClick={() => setView('statement')}
            className={`rounded-md px-4 py-2 text-sm font-medium transition-colors ${
              view === 'statement'
                ? 'bg-white text-gray-900 shadow-sm'
                : 'text-gray-600 hover:text-gray-900'
            }`}
          >
            Statement
          </button>
          <button
            type="button"
            onClick={() => setView('details')}
            className={`rounded-md px-4 py-2 text-sm font-medium transition-colors ${
              view === 'details'
                ? 'bg-white text-gray-900 shadow-sm'
                : 'text-gray-600 hover:text-gray-900'
            }`}
          >
            Details
          </button>
        </div>
        <p className="text-sm text-gray-500">{periodLabel}</p>
      </div>

      {view === 'statement' ? (
        <>
          <div className="mb-6 grid grid-cols-1 gap-4 rounded-xl border border-gray-100 bg-white p-5 shadow-sm print:hidden sm:grid-cols-2 lg:grid-cols-4">
            {extraFilters}
          </div>

          {error && (
            <div className="mb-6 rounded-md bg-red-50 p-4 text-red-800 print:hidden">
              <span className="font-medium">{error}</span>
            </div>
          )}

          <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
            <div>
              <h1 className="mb-2 text-3xl font-bold text-gray-900">Profit &amp; Loss Statement</h1>
              <p className="text-gray-600">Formal income statement with expense breakdown for the selected period.</p>
            </div>
            <div className="flex flex-wrap items-center gap-2 self-start print:hidden">
              <button
                type="button"
                onClick={onPrint}
                disabled={loading}
                className="inline-flex items-center rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 shadow-sm hover:bg-gray-50 disabled:opacity-60"
              >
                Print
              </button>
              <button
                type="button"
                onClick={() => void onExport()}
                disabled={loading || isExporting}
                className="inline-flex items-center rounded-md border border-emerald-300 bg-emerald-50 px-4 py-2 text-sm font-medium text-emerald-800 shadow-sm hover:bg-emerald-100 disabled:opacity-60"
              >
                {isExporting ? 'Exporting…' : 'Export CSV'}
              </button>
              <button
                type="button"
                onClick={() => void load()}
                disabled={loading}
                className="inline-flex items-center rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 shadow-sm hover:bg-gray-50 disabled:opacity-60"
              >
                {loading ? 'Loading…' : 'Refresh'}
              </button>
            </div>
          </div>

          <div className="mb-6 print:hidden">
            <ProfitLossAttractiveSummary summary={summary} loading={loading} />
          </div>

          <ProfitLossStatementView
            statement={statement}
            loading={loading}
            periodLabel={periodLabel}
          />
        </>
      ) : (
        <ReportPageShell
          title="Profit & Loss Details"
          description={`${periodColumnHeader} breakdown of revenue, COGS, expenses, and net profit.`}
          showDateFilters={false}
          extraFilters={extraFilters}
          error={error}
          loading={loading}
          onRefresh={load}
          onExport={onExport}
          exporting={isExporting}
          onPrint={onPrint}
          columns={columns}
          rows={rows}
          searchPlaceholder={`Search by ${groupBy === 'day' ? 'date (yyyy-mm-dd)' : 'period'}…`}
          emptyMessage="No profit & loss data for this period."
          pageNumber={pageNumber}
          pageSize={pageSize}
          totalRecords={totalRecords}
          totalPages={totalPages}
          search={search}
          sortColumn={sortColumn}
          sortDirection={sortDirection}
          onPageChange={setPageNumber}
          onPageSizeChange={(size) => { setPageSize(size); setPageNumber(1); }}
          onSearchChange={(value) => { setSearch(value); setPageNumber(1); }}
          onSortChange={(column, direction) => { setSortColumn(column); setSortDirection(direction); setPageNumber(1); }}
          footerRow={footerRow}
          summary={<ProfitLossAttractiveSummary summary={summary} loading={loading} />}
        />
      )}
    </div>
  );
};

export default ProfitLossReportPage;
