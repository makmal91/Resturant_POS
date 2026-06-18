export type ReportPeriodMode =
  | 'custom'
  | 'this_month'
  | 'last_month'
  | 'this_year'
  | 'last_year'
  | 'month'
  | 'year';

export type ProfitLossGroupBy = 'day' | 'month' | 'year';

export interface ReportPeriodState {
  mode: ReportPeriodMode;
  fromDate: string;
  toDate: string;
  year: number;
  month: number;
}

const pad = (n: number) => String(n).padStart(2, '0');

export const toDateStr = (d: Date) =>
  `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;

export const currentYear = () => new Date().getFullYear();

export const yearOptions = (count = 8) => {
  const y = currentYear();
  return Array.from({ length: count }, (_, i) => y - i);
};

export const monthLabels = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
];

export function createDefaultPeriodState(): ReportPeriodState {
  const now = new Date();
  const from = new Date(now.getFullYear(), now.getMonth(), 1);
  return {
    mode: 'this_month',
    fromDate: toDateStr(from),
    toDate: toDateStr(now),
    year: now.getFullYear(),
    month: now.getMonth() + 1,
  };
}

export function resolvePeriodRange(state: ReportPeriodState): {
  fromDate: string;
  toDate: string;
  groupBy: ProfitLossGroupBy;
} {
  const now = new Date();

  switch (state.mode) {
    case 'this_month': {
      const from = new Date(now.getFullYear(), now.getMonth(), 1);
      return { fromDate: toDateStr(from), toDate: toDateStr(now), groupBy: 'day' };
    }
    case 'last_month': {
      const from = new Date(now.getFullYear(), now.getMonth() - 1, 1);
      const to = new Date(now.getFullYear(), now.getMonth(), 0);
      return { fromDate: toDateStr(from), toDate: toDateStr(to), groupBy: 'day' };
    }
    case 'this_year': {
      const from = new Date(now.getFullYear(), 0, 1);
      return { fromDate: toDateStr(from), toDate: toDateStr(now), groupBy: 'month' };
    }
    case 'last_year': {
      const y = now.getFullYear() - 1;
      return {
        fromDate: `${y}-01-01`,
        toDate: `${y}-12-31`,
        groupBy: 'month',
      };
    }
    case 'month': {
      const y = state.year;
      const m = state.month;
      const from = new Date(y, m - 1, 1);
      const to = new Date(y, m, 0);
      return { fromDate: toDateStr(from), toDate: toDateStr(to), groupBy: 'day' };
    }
    case 'year': {
      const y = state.year;
      const to = y === now.getFullYear() ? toDateStr(now) : `${y}-12-31`;
      return { fromDate: `${y}-01-01`, toDate: to, groupBy: 'month' };
    }
    case 'custom':
    default:
      return {
        fromDate: state.fromDate,
        toDate: state.toDate,
        groupBy: suggestGroupBy(state.fromDate, state.toDate),
      };
  }
}

function suggestGroupBy(fromDate: string, toDate: string): ProfitLossGroupBy {
  const from = new Date(fromDate);
  const to = new Date(toDate);
  if (Number.isNaN(from.getTime()) || Number.isNaN(to.getTime())) return 'day';

  const days = Math.max(1, Math.ceil((to.getTime() - from.getTime()) / 86_400_000) + 1);
  if (days > 730) return 'year';
  if (days > 62) return 'month';
  return 'day';
}

export function formatPeriodLabel(fromDate: string, toDate: string): string {
  const from = new Date(fromDate);
  const to = new Date(toDate);
  if (Number.isNaN(from.getTime()) || Number.isNaN(to.getTime())) return `${fromDate} – ${toDate}`;

  const opts: Intl.DateTimeFormatOptions = { year: 'numeric', month: 'short', day: 'numeric' };
  return `${from.toLocaleDateString(undefined, opts)} – ${to.toLocaleDateString(undefined, opts)}`;
}

export const periodModeLabels: Record<ReportPeriodMode, string> = {
  custom: 'Custom Date Range',
  this_month: 'This Month',
  last_month: 'Last Month',
  this_year: 'This Year',
  last_year: 'Last Year',
  month: 'Specific Month',
  year: 'Specific Year',
};
