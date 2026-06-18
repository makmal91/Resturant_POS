import React, { useMemo } from 'react';
import {
  monthLabels,
  periodModeLabels,
  resolvePeriodRange,
  type ReportPeriodMode,
  type ReportPeriodState,
  yearOptions,
} from './reportPeriodUtils';

interface ReportPeriodFilterProps {
  value: ReportPeriodState;
  onChange: (next: ReportPeriodState) => void;
}

const inputClass =
  'w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none';

export default function ReportPeriodFilter({ value, onChange }: ReportPeriodFilterProps) {
  const years = yearOptions();
  const resolved = useMemo(() => resolvePeriodRange(value), [value]);

  const displayFrom = value.mode === 'custom' ? value.fromDate : resolved.fromDate;
  const displayTo = value.mode === 'custom' ? value.toDate : resolved.toDate;

  const setMode = (mode: ReportPeriodMode) => {
    const next = { ...value, mode };
    if (mode !== 'custom') {
      const range = resolvePeriodRange(next);
      onChange({ ...next, fromDate: range.fromDate, toDate: range.toDate });
    } else {
      onChange(next);
    }
  };

  const setFromDate = (fromDate: string) => {
    onChange({ ...value, mode: 'custom', fromDate, toDate: displayTo });
  };

  const setToDate = (toDate: string) => {
    onChange({ ...value, mode: 'custom', fromDate: displayFrom, toDate });
  };

  return (
    <>
      <div>
        <label className="mb-1 block text-sm font-medium text-gray-700">Period</label>
        <select
          value={value.mode}
          onChange={(e) => setMode(e.target.value as ReportPeriodMode)}
          className={inputClass}
        >
          {(Object.keys(periodModeLabels) as ReportPeriodMode[]).map((mode) => (
            <option key={mode} value={mode}>{periodModeLabels[mode]}</option>
          ))}
        </select>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium text-gray-700">From Date</label>
        <input
          type="date"
          value={displayFrom}
          onChange={(e) => setFromDate(e.target.value)}
          className={inputClass}
        />
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium text-gray-700">To Date</label>
        <input
          type="date"
          value={displayTo}
          onChange={(e) => setToDate(e.target.value)}
          className={inputClass}
        />
      </div>

      {value.mode === 'month' && (
        <>
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Year</label>
            <select
              value={value.year}
              onChange={(e) => {
                const next = { ...value, year: Number(e.target.value) };
                const range = resolvePeriodRange(next);
                onChange({ ...next, fromDate: range.fromDate, toDate: range.toDate });
              }}
              className={inputClass}
            >
              {years.map((y) => <option key={y} value={y}>{y}</option>)}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Month</label>
            <select
              value={value.month}
              onChange={(e) => {
                const next = { ...value, month: Number(e.target.value) };
                const range = resolvePeriodRange(next);
                onChange({ ...next, fromDate: range.fromDate, toDate: range.toDate });
              }}
              className={inputClass}
            >
              {monthLabels.map((label, i) => (
                <option key={label} value={i + 1}>{label}</option>
              ))}
            </select>
          </div>
        </>
      )}

      {value.mode === 'year' && (
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">Year</label>
          <select
            value={value.year}
            onChange={(e) => {
              const next = { ...value, year: Number(e.target.value) };
              const range = resolvePeriodRange(next);
              onChange({ ...next, fromDate: range.fromDate, toDate: range.toDate });
            }}
            className={inputClass}
          >
            {years.map((y) => <option key={y} value={y}>{y}</option>)}
          </select>
        </div>
      )}
    </>
  );
}
