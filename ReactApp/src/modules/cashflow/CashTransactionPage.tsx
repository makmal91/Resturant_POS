import React, { useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { getApiErrorMessage } from '../../services/api';
import { useBranchStore } from '../../stores/useBranchStore';
import { cashFlowService, type CashFlowTransactionType, type CashFlowPaymentMethod } from './cashFlowService';

const TYPES: { value: CashFlowTransactionType; label: string; color: string }[] = [
  { value: 'CashIn',       label: 'Cash In',       color: 'bg-emerald-600' },
  { value: 'CashOut',      label: 'Cash Out',       color: 'bg-red-500' },
  { value: 'BankTransfer', label: 'Bank Transfer',  color: 'bg-blue-600' },
];

const METHODS: { value: CashFlowPaymentMethod; label: string }[] = [
  { value: 'Cash',   label: 'Cash' },
  { value: 'Bank',   label: 'Bank' },
  { value: 'Wallet', label: 'Wallet' },
];

export default function CashTransactionPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const defaultType = (searchParams.get('type') as CashFlowTransactionType | null) ?? 'CashIn';

  const { selectedBranchId, getSelectedBranch } = useBranchStore();
  const branchId = selectedBranchId ?? 0;
  const branchName = getSelectedBranch()?.name ?? `Branch ${branchId}`;

  const [txType, setTxType]       = useState<CashFlowTransactionType>(defaultType);
  const [payMethod, setPayMethod] = useState<CashFlowPaymentMethod>('Cash');
  const [amount, setAmount]       = useState('');
  const [description, setDesc]    = useState('');
  const [refNo, setRefNo]         = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError]         = useState<string | null>(null);
  const [success, setSuccess]     = useState(false);

  const selectedType = TYPES.find((t) => t.value === txType) ?? TYPES[0];

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const parsed = parseFloat(amount);
    if (isNaN(parsed) || parsed <= 0) {
      setError('Amount must be greater than zero.');
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      await cashFlowService.recordTransaction(branchId, txType, parsed, payMethod, description, refNo);
      setSuccess(true);
      setTimeout(() => navigate('/cashflow'), 1200);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to record transaction.'));
    } finally {
      setSubmitting(false);
    }
  };

  if (branchId <= 0) {
    return (
      <div className="flex items-center justify-center h-64 text-gray-500">
        Please select a branch first.
      </div>
    );
  }

  return (
    <div className="max-w-lg mx-auto mt-10 px-4">
      <div className="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden">
        <div className={`${selectedType.color} px-6 py-5 text-white`}>
          <h1 className="text-xl font-bold">Record Transaction</h1>
          <p className="text-white/80 text-sm mt-1">{branchName}</p>
        </div>

        <div className="p-6 space-y-5">
          {success ? (
            <div className="text-center py-8">
              <div className="text-5xl mb-3">✅</div>
              <p className="text-gray-700 font-semibold">Transaction recorded!</p>
              <p className="text-gray-400 text-sm mt-1">Redirecting to dashboard…</p>
            </div>
          ) : (
            <form onSubmit={handleSubmit} className="space-y-5">
              {/* Type selector */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Transaction Type</label>
                <div className="grid grid-cols-3 gap-2">
                  {TYPES.map((t) => (
                    <button
                      key={t.value}
                      type="button"
                      onClick={() => setTxType(t.value)}
                      className={`py-2.5 rounded-xl text-sm font-semibold border-2 transition-all ${
                        txType === t.value
                          ? `${t.color} text-white border-transparent`
                          : 'bg-white text-gray-600 border-gray-200 hover:border-gray-300'
                      }`}
                    >
                      {t.label}
                    </button>
                  ))}
                </div>
              </div>

              {/* Amount */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1.5">
                  Amount <span className="text-red-500">*</span>
                </label>
                <div className="relative">
                  <span className="absolute left-3.5 top-1/2 -translate-y-1/2 text-gray-400 font-medium">$</span>
                  <input
                    type="number"
                    min="0.01"
                    step="0.01"
                    value={amount}
                    onChange={(e) => setAmount(e.target.value)}
                    placeholder="0.00"
                    required
                    className="w-full pl-8 pr-4 py-3 border border-gray-200 rounded-xl text-gray-800 text-lg font-semibold focus:outline-none focus:ring-2 focus:ring-blue-400 focus:border-transparent transition"
                  />
                </div>
              </div>

              {/* Payment method */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Payment Method</label>
                <div className="flex gap-2">
                  {METHODS.map((m) => (
                    <button
                      key={m.value}
                      type="button"
                      onClick={() => setPayMethod(m.value)}
                      className={`flex-1 py-2 rounded-xl text-sm font-medium border-2 transition-all ${
                        payMethod === m.value
                          ? 'bg-blue-600 text-white border-transparent'
                          : 'bg-white text-gray-600 border-gray-200 hover:border-gray-300'
                      }`}
                    >
                      {m.label}
                    </button>
                  ))}
                </div>
              </div>

              {/* Reference */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1.5">Reference No. (optional)</label>
                <input
                  type="text"
                  value={refNo}
                  onChange={(e) => setRefNo(e.target.value)}
                  placeholder="e.g. INV-2026-001"
                  className="w-full px-4 py-3 border border-gray-200 rounded-xl text-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-400 transition"
                />
              </div>

              {/* Description */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1.5">Description (optional)</label>
                <textarea
                  value={description}
                  onChange={(e) => setDesc(e.target.value)}
                  rows={2}
                  placeholder="Reason for this transaction…"
                  className="w-full px-4 py-3 border border-gray-200 rounded-xl text-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-400 transition resize-none"
                />
              </div>

              {error && (
                <div className="bg-red-50 border border-red-200 text-red-600 rounded-xl px-4 py-3 text-sm">
                  {error}
                </div>
              )}

              <div className="flex gap-3">
                <button
                  type="button"
                  onClick={() => navigate('/cashflow')}
                  className="flex-1 py-3 border border-gray-200 rounded-xl text-gray-700 font-medium hover:bg-gray-50 transition-colors"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className={`flex-1 py-3 ${selectedType.color} text-white rounded-xl font-semibold hover:opacity-90 disabled:opacity-60 transition-all`}
                >
                  {submitting ? 'Recording…' : 'Record Transaction'}
                </button>
              </div>
            </form>
          )}
        </div>
      </div>
    </div>
  );
}
