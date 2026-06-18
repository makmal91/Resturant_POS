import React from 'react';

export type ReportSummaryTone =
  | 'blue'
  | 'emerald'
  | 'violet'
  | 'amber'
  | 'rose'
  | 'sky'
  | 'indigo'
  | 'orange';

const TONE_CLASSES: Record<ReportSummaryTone, string> = {
  blue: 'border-blue-100 bg-blue-50 text-blue-700',
  emerald: 'border-emerald-100 bg-emerald-50 text-emerald-700',
  violet: 'border-violet-100 bg-violet-50 text-violet-700',
  amber: 'border-amber-100 bg-amber-50 text-amber-700',
  rose: 'border-rose-100 bg-rose-50 text-rose-700',
  sky: 'border-sky-100 bg-sky-50 text-sky-700',
  indigo: 'border-indigo-100 bg-indigo-50 text-indigo-700',
  orange: 'border-orange-100 bg-orange-50 text-orange-700',
};

export interface ReportSummaryItem {
  key: string;
  label: string;
  value: string;
  tone: ReportSummaryTone;
}

interface ReportSummaryStripProps {
  items: ReportSummaryItem[];
  columnsClassName?: string;
}

export default function ReportSummaryStrip({
  items,
  columnsClassName = 'grid-cols-2 lg:grid-cols-4',
}: ReportSummaryStripProps) {
  if (items.length === 0) return null;

  return (
    <div className={`mb-6 grid gap-4 ${columnsClassName}`}>
      {items.map(({ key, label, value, tone }) => (
        <div key={key} className={`rounded-lg border p-4 ${TONE_CLASSES[tone]}`}>
          <p className="text-xs font-medium uppercase tracking-wide opacity-70">{label}</p>
          <p className="mt-1 text-xl font-bold tabular-nums">{value}</p>
        </div>
      ))}
    </div>
  );
}
