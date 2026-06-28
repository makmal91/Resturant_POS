import React from 'react';
import type { PosProductUnit } from './posService';

export interface UnitSelectorProps {
  units: PosProductUnit[];
  selectedUnitId: number;
  onSelect: (unitId: number) => void;
  disabled?: boolean;
}

/** One-click unit pills for POS cart lines. Hidden when product has a single unit. */
export const UnitSelector: React.FC<UnitSelectorProps> = ({
  units,
  selectedUnitId,
  onSelect,
  disabled = false,
}) => {
  if (units.length <= 1) return null;

  const ordered = [...units].sort((a, b) => {
    if (a.isBaseUnit !== b.isBaseUnit) return a.isBaseUnit ? -1 : 1;
    return a.unitName.localeCompare(b.unitName);
  });

  const selectedIndex = ordered.findIndex((u) => u.unitId === selectedUnitId);

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (disabled || ordered.length < 2) return;
    if (e.key !== 'ArrowLeft' && e.key !== 'ArrowRight') return;
    e.preventDefault();
    const dir = e.key === 'ArrowRight' ? 1 : -1;
    const next = (selectedIndex + dir + ordered.length) % ordered.length;
    onSelect(ordered[next].unitId);
  };

  return (
    <div
      className="flex flex-wrap gap-1 mt-1.5"
      role="group"
      aria-label="Select unit"
      onKeyDown={handleKeyDown}
    >
      {ordered.map((u) => {
        const active = u.unitId === selectedUnitId;
        return (
          <button
            key={u.unitId}
            type="button"
            disabled={disabled}
            tabIndex={active ? 0 : -1}
            onClick={(e) => {
              e.stopPropagation();
              if (!active) onSelect(u.unitId);
            }}
            className={`min-w-[2.5rem] px-2.5 py-1 rounded-md text-xs font-semibold transition border focus:outline-none focus:ring-2 focus:ring-blue-400 focus:ring-offset-1 ${
              active
                ? 'bg-blue-600 border-blue-600 text-white shadow-sm'
                : 'bg-white border-gray-200 text-gray-600 hover:border-blue-400 hover:bg-blue-50 hover:text-blue-700'
            }`}
          >
            {u.unitName}
          </button>
        );
      })}
    </div>
  );
};

export const lastUnitStorageKey = (productId: number, variantId: number | null): string =>
  `${productId}:${variantId ?? 0}`;
