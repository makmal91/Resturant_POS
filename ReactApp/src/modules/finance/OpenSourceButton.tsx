import React from 'react';
import type { FinanceSourceTarget } from './financeVoucherNav';
import { financeSourceLabel } from './financeVoucherNav';

interface OpenSourceButtonProps {
  target: FinanceSourceTarget;
  onOpen: (target: FinanceSourceTarget) => void;
  className?: string;
}

export default function OpenSourceButton({ target, onOpen, className = '' }: OpenSourceButtonProps) {
  if (!target) return null;

  return (
    <button
      type="button"
      onClick={() => onOpen(target)}
      className={`inline-flex items-center rounded-md border border-blue-200 bg-blue-50 px-2.5 py-1 text-xs font-medium text-blue-700 hover:bg-blue-100 ${className}`}
    >
      {financeSourceLabel(target)}
    </button>
  );
}
