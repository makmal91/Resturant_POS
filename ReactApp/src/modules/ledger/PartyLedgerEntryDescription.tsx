import React from 'react';
import LedgerStatusBadge from '../../components/LedgerStatusBadge';
import { LEDGER_TYPE_LABELS, type PartyLedgerEntry } from './partyLedgerService';
import { partyLedgerVoucherLabel } from './partyLedgerVoucher';

interface PartyLedgerEntryDescriptionProps {
  row: PartyLedgerEntry;
  expanded?: boolean;
  onToggle?: () => void;
  fmt?: (amount: number) => string;
}

export default function PartyLedgerEntryDescription({
  row,
  expanded = false,
  onToggle,
  fmt,
}: PartyLedgerEntryDescriptionProps) {
  const typeLabel = LEDGER_TYPE_LABELS[row.type] ?? row.type;
  const voucherLabel = partyLedgerVoucherLabel(row);
  const titleText = row.description?.trim() || typeLabel;

  return (
    <div className="min-w-0">
      <div className="flex items-start gap-2">
        {row.hasInvoiceBreakdown && onToggle ? (
          <button
            type="button"
            onClick={onToggle}
            className="mt-0.5 shrink-0 rounded p-1 text-gray-500 hover:bg-gray-100 hover:text-gray-800"
            aria-expanded={expanded}
            aria-label={expanded ? 'Hide invoice details' : 'View invoice details'}
            title={expanded ? 'Hide details' : 'View details'}
          >
            <span className="block text-[10px] leading-none">{expanded ? '▼' : '▶'}</span>
          </button>
        ) : (
          <span className="w-5 shrink-0" aria-hidden />
        )}

        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
            <p className="text-gray-800 text-sm inline-flex items-center flex-wrap gap-1">
              <span>{titleText}</span>
              <LedgerStatusBadge
                isSuperseded={row.isSuperseded}
                isReversal={row.isReversal}
                isReplacement={row.isReplacement}
              />
            </p>
            {row.hasInvoiceBreakdown && onToggle && (
              <button
                type="button"
                onClick={onToggle}
                className="text-xs font-medium text-blue-600 hover:text-blue-800"
              >
                {expanded ? 'Hide details' : 'View details'}
              </button>
            )}
          </div>

          <p className="text-xs text-gray-400 mt-0.5">
            {typeLabel}
            {voucherLabel !== titleText ? (
              <span className="text-gray-500"> · {voucherLabel}</span>
            ) : null}
          </p>

          {expanded && row.hasInvoiceBreakdown && row.invoiceAllocations.length > 0 && fmt && (
            <ul className="mt-2 space-y-1 border-l-2 border-blue-200 pl-3">
              {row.invoiceAllocations.map((allocation) => (
                <li key={allocation.invoiceId} className="text-xs text-gray-600 tabular-nums">
                  <span className="text-gray-700">
                    Invoice: {allocation.invoiceNo || `#${allocation.invoiceId}`}
                  </span>
                  <span className="text-gray-400 mx-1.5">→</span>
                  <span className="font-medium text-gray-800">{fmt(allocation.appliedAmount)}</span>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </div>
  );
}
