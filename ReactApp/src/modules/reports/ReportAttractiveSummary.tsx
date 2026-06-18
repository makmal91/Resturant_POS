import React from 'react';
import MenuIcon, { type MenuIconKey } from '../../components/MenuIcon';

export type AttractiveSummaryCard = {
  key: string;
  label: string;
  value: string;
  sub?: string;
  iconKey: MenuIconKey;
  cardClass: string;
  iconWrapClass: string;
  valueClass: string;
};

export interface AttractiveReportHero {
  title: string;
  value: string;
  subtitle: string;
  badgeLabel: string;
  badgeValue: string;
  badgeIconKey?: MenuIconKey;
  gradientClass?: string;
}

interface ReportAttractiveSummaryProps {
  hero: AttractiveReportHero;
  cards: AttractiveSummaryCard[];
  loading?: boolean;
  columnsClassName?: string;
}

function SummaryCardSkeleton() {
  return (
    <div className="rounded-xl border border-gray-100 bg-white p-5 shadow-sm animate-pulse">
      <div className="flex items-start gap-4">
        <div className="h-11 w-11 rounded-xl bg-gray-100" />
        <div className="flex-1 space-y-2">
          <div className="h-3 w-20 rounded bg-gray-100" />
          <div className="h-7 w-28 rounded bg-gray-100" />
          <div className="h-3 w-16 rounded bg-gray-100" />
        </div>
      </div>
    </div>
  );
}

function SummaryCard({ card }: { card: AttractiveSummaryCard }) {
  return (
    <div
      className={`group rounded-xl border p-5 shadow-sm transition-all duration-200 hover:-translate-y-0.5 hover:shadow-md ${card.cardClass}`}
    >
      <div className="flex items-start gap-4">
        <div
          className={`flex h-11 w-11 flex-shrink-0 items-center justify-center rounded-xl shadow-sm transition-transform duration-200 group-hover:scale-105 ${card.iconWrapClass}`}
        >
          <MenuIcon iconKey={card.iconKey} className="h-5 w-5" />
        </div>
        <div className="min-w-0 flex-1">
          <p className="text-xs font-semibold uppercase tracking-wide text-gray-500">{card.label}</p>
          <p className={`mt-1 truncate text-2xl font-bold tabular-nums ${card.valueClass}`}>{card.value}</p>
          {card.sub && <p className="mt-1 text-xs text-gray-400">{card.sub}</p>}
        </div>
      </div>
    </div>
  );
}

function HeroSkeleton() {
  return (
    <div className="mb-4 h-32 animate-pulse rounded-xl border border-gray-100 bg-gray-100" />
  );
}

export default function ReportAttractiveSummary({
  hero,
  cards,
  loading = false,
  columnsClassName = 'sm:grid-cols-2 xl:grid-cols-5',
}: ReportAttractiveSummaryProps) {
  if (loading && cards.length === 0) {
    return (
      <div className="mb-6 space-y-4">
        <HeroSkeleton />
        <div className={`grid grid-cols-1 gap-4 ${columnsClassName}`}>
          {Array.from({ length: 5 }).map((_, index) => (
            <SummaryCardSkeleton key={index} />
          ))}
        </div>
      </div>
    );
  }

  if (cards.length === 0) return null;

  const gradientClass = hero.gradientClass
    ?? 'border-blue-200 bg-gradient-to-r from-blue-600 via-indigo-600 to-violet-600';

  return (
    <div className="mb-6 space-y-4">
      <div className={`overflow-hidden rounded-xl border p-5 text-white shadow-md ${gradientClass}`}>
        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <p className="text-sm font-medium text-blue-100">{hero.title}</p>
            <p className="mt-1 text-3xl font-extrabold tracking-tight tabular-nums">{hero.value}</p>
            <p className="mt-1 text-sm text-blue-100">{hero.subtitle}</p>
          </div>
          <div className="flex items-center gap-4 rounded-xl bg-white/10 px-5 py-4 backdrop-blur-sm">
            <div className="text-right">
              <p className="text-xs font-medium uppercase tracking-wide text-blue-100">{hero.badgeLabel}</p>
              <p className="text-2xl font-bold tabular-nums">{hero.badgeValue}</p>
            </div>
            <div className="flex h-12 w-12 items-center justify-center rounded-full bg-white/20">
              <MenuIcon iconKey={hero.badgeIconKey ?? 'reports'} className="h-6 w-6 text-white" />
            </div>
          </div>
        </div>
      </div>

      <div className={`grid grid-cols-1 gap-4 ${columnsClassName}`}>
        {cards.map((card) => (
          <SummaryCard key={card.key} card={card} />
        ))}
      </div>
    </div>
  );
}
