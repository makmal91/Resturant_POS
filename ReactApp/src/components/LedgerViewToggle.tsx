import React from 'react';

interface LedgerViewToggleProps {
  auditView: boolean;
  groupByChain: boolean;
  onAuditViewChange: (value: boolean) => void;
  onGroupByChainChange: (value: boolean) => void;
}

const LedgerViewToggle: React.FC<LedgerViewToggleProps> = ({
  auditView,
  groupByChain,
  onAuditViewChange,
  onGroupByChainChange,
}) => (
  <div className="flex flex-col sm:flex-row sm:items-center gap-3 rounded-lg border border-gray-200 bg-gray-50 px-3 py-2">
    <div className="inline-flex rounded-lg border border-gray-200 bg-white p-0.5">
      <button
        type="button"
        onClick={() => onAuditViewChange(false)}
        className={`px-3 py-1.5 text-xs font-medium rounded-md transition-colors ${
          !auditView ? 'bg-blue-600 text-white' : 'text-gray-600 hover:text-gray-800'
        }`}
      >
        Clean View
      </button>
      <button
        type="button"
        onClick={() => onAuditViewChange(true)}
        className={`px-3 py-1.5 text-xs font-medium rounded-md transition-colors ${
          auditView ? 'bg-blue-600 text-white' : 'text-gray-600 hover:text-gray-800'
        }`}
      >
        Audit View
      </button>
    </div>
    {auditView && (
      <label className="inline-flex items-center gap-2 text-xs text-gray-600 cursor-pointer">
        <input
          type="checkbox"
          checked={groupByChain}
          onChange={(e) => onGroupByChainChange(e.target.checked)}
          className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
        />
        Group by transaction chain
      </label>
    )}
    <p className="text-xs text-gray-500 sm:ml-auto">
      {auditView
        ? 'Shows originals, reversals, and replacements. Closing balance matches clean view.'
        : 'Shows only effective posted transactions.'}
    </p>
  </div>
);

export default LedgerViewToggle;
