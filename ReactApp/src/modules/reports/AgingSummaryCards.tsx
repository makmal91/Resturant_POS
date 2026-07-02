import React from 'react';
import { fmt, formatDate } from './reportFormatters';
import type { AgingReportSummary } from './reportService';

interface AgingSummaryCardsProps {
  summary: AgingReportSummary | null;
}

const cards = [
  { key: 'bucket0To30' as const, label: '0–30 Days', color: 'text-emerald-700 bg-emerald-50 border-emerald-100' },
  { key: 'bucket31To60' as const, label: '31–60 Days', color: 'text-blue-700 bg-blue-50 border-blue-100' },
  { key: 'bucket61To90' as const, label: '61–90 Days', color: 'text-amber-700 bg-amber-50 border-amber-100' },
  { key: 'bucket90Plus' as const, label: '90+ Days', color: 'text-red-700 bg-red-50 border-red-100' },
];

export default function AgingSummaryCards({ summary }: AgingSummaryCardsProps) {
  if (!summary) return null;

  return (
    <div className="mb-6 space-y-4">
      <div className="rounded-lg border border-gray-200 bg-white p-4">
        <p className="text-xs font-medium uppercase tracking-wide text-gray-500">Total Outstanding</p>
        <p className="mt-1 text-2xl font-bold text-gray-900">{fmt(summary.totalOutstanding)}</p>
        {summary.asOfDate && (
          <p className="mt-1 text-xs text-gray-500">As of {formatDate(summary.asOfDate)}</p>
        )}
      </div>
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        {cards.map(({ key, label, color }) => (
          <div key={key} className={`rounded-lg border p-4 ${color}`}>
            <p className="text-xs font-medium uppercase tracking-wide opacity-70">{label}</p>
            <p className="mt-1 text-lg font-bold">{fmt(summary[key])}</p>
          </div>
        ))}
      </div>
    </div>
  );
}

export const AGING_BUCKET_OPTIONS = [
  { value: '', label: 'All Buckets' },
  { value: '0-30', label: '0–30 Days' },
  { value: '31-60', label: '31–60 Days' },
  { value: '61-90', label: '61–90 Days' },
  { value: '90+', label: '90+ Days' },
] as const;

export function AgingBucketFilter({
  value,
  onChange,
}: {
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <div>
      <label className="mb-1 block text-sm font-medium text-gray-700">Aging Bucket</label>
      <select
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
      >
        {AGING_BUCKET_OPTIONS.map((opt) => (
          <option key={opt.value || 'all'} value={opt.value}>{opt.label}</option>
        ))}
      </select>
    </div>
  );
}

export function CustomerFilter({
  customers,
  value,
  onChange,
}: {
  customers: Array<{ id: number; name: string; customerCode?: string }>;
  value: number;
  onChange: (customerId: number) => void;
}) {
  return (
    <div>
      <label className="mb-1 block text-sm font-medium text-gray-700">Customer</label>
      <select
        value={value || ''}
        onChange={(e) => onChange(e.target.value === '' ? 0 : Number(e.target.value))}
        className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
      >
        <option value="">All Customers</option>
        {customers.map((c) => (
          <option key={c.id} value={c.id}>
            {c.customerCode ? `${c.customerCode} — ` : ''}{c.name}
          </option>
        ))}
      </select>
    </div>
  );
}

export function SupplierFilter({
  suppliers,
  value,
  onChange,
}: {
  suppliers: Array<{ id: number; name: string; supplierCode?: string }>;
  value: number;
  onChange: (supplierId: number) => void;
}) {
  return (
    <div>
      <label className="mb-1 block text-sm font-medium text-gray-700">Supplier</label>
      <select
        value={value || ''}
        onChange={(e) => onChange(e.target.value === '' ? 0 : Number(e.target.value))}
        className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
      >
        <option value="">All Suppliers</option>
        {suppliers.map((s) => (
          <option key={s.id} value={s.id}>
            {s.supplierCode ? `${s.supplierCode} — ` : ''}{s.name}
          </option>
        ))}
      </select>
    </div>
  );
}
