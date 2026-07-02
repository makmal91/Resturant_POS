import React from 'react';

interface LedgerStatusBadgeProps {
  isSuperseded?: boolean;
  isReversal?: boolean;
  isReplacement?: boolean;
  /** @deprecated use isSuperseded */
  isEdited?: boolean;
  /** @deprecated use isReplacement */
  isUpdated?: boolean;
}

const LedgerStatusBadge: React.FC<LedgerStatusBadgeProps> = ({
  isSuperseded = false,
  isReversal = false,
  isReplacement = false,
  isEdited = false,
  isUpdated = false,
}) => {
  const superseded = isSuperseded || isEdited;
  const replacement = isReplacement || isUpdated;

  if (!superseded && !isReversal && !replacement) return null;

  return (
    <span className="inline-flex flex-wrap gap-1 ml-2 align-middle">
      {superseded && (
        <span className="text-[10px] font-semibold uppercase tracking-wide px-1.5 py-0.5 rounded bg-amber-100 text-amber-800">
          Superseded
        </span>
      )}
      {isReversal && (
        <span className="text-[10px] font-semibold uppercase tracking-wide px-1.5 py-0.5 rounded bg-rose-100 text-rose-800">
          Reversal
        </span>
      )}
      {replacement && (
        <span className="text-[10px] font-semibold uppercase tracking-wide px-1.5 py-0.5 rounded bg-sky-100 text-sky-800">
          Updated
        </span>
      )}
    </span>
  );
};

export default LedgerStatusBadge;
