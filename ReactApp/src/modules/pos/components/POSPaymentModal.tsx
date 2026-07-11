import React, { useEffect, useRef, useState } from 'react';
import { useBusinessCurrency } from '../../../hooks/useBusinessCurrency';

export interface POSPaymentModalProps {
  grandTotal: number;
  hasCustomer: boolean;
  onClose: () => void;
  onConfirm: (
    method: 'Cash' | 'Card' | 'Mixed' | 'Credit',
    paid: number,
    cash: number,
    card: number,
  ) => void;
  loading: boolean;
}

const POSPaymentModal: React.FC<POSPaymentModalProps> = ({
  grandTotal,
  hasCustomer,
  onClose,
  onConfirm,
  loading,
}) => {
  const { fmt } = useBusinessCurrency();
  const [method, setMethod] = useState<'Cash' | 'Card' | 'Mixed' | 'Credit'>('Cash');
  const [cash, setCash] = useState(grandTotal);
  const [card, setCard] = useState(0);
  const cashRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    setCash(grandTotal);
    setCard(0);
  }, [grandTotal]);

  useEffect(() => {
    setTimeout(() => {
      cashRef.current?.focus();
      cashRef.current?.select();
    }, 50);
  }, [method]);

  const paid =
    method === 'Credit' ? 0 : method === 'Mixed' ? cash + card : method === 'Cash' ? cash : card;
  const change = method === 'Credit' ? 0 : Math.max(0, paid - grandTotal);
  const isValid =
    method === 'Credit'
      ? hasCustomer
      : method === 'Mixed'
        ? Math.abs(cash + card - grandTotal) < 0.01
        : paid >= grandTotal;

  const handleConfirm = () => {
    if (!isValid) return;
    onConfirm(
      method,
      paid,
      method === 'Cash' || method === 'Mixed' ? cash : 0,
      method === 'Card' || method === 'Mixed' ? card : 0,
    );
  };

  const methodBtns: { key: 'Cash' | 'Card' | 'Mixed' | 'Credit'; label: string; icon: string }[] = [
    { key: 'Cash', label: 'Cash', icon: '💵' },
    { key: 'Card', label: 'Card', icon: '💳' },
    { key: 'Mixed', label: 'Mixed', icon: '🔀' },
    { key: 'Credit', label: 'Credit', icon: '📝' },
  ];

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-md mx-4 overflow-hidden border border-gray-200">
        <div className="bg-blue-600 px-6 py-5">
          <p className="text-blue-100 text-sm font-medium">Total Due</p>
          <p className="text-4xl font-black text-white mt-1">{fmt(grandTotal)}</p>
        </div>

        <div className="p-6 space-y-5">
          <div className="grid grid-cols-2 gap-2">
            {methodBtns.map((m) => (
              <button
                key={m.key}
                type="button"
                onClick={() => {
                  setMethod(m.key);
                  if (m.key === 'Cash') {
                    setCash(grandTotal);
                    setCard(0);
                  } else if (m.key === 'Card') {
                    setCash(0);
                    setCard(grandTotal);
                  } else if (m.key === 'Mixed') {
                    setCash(grandTotal);
                    setCard(0);
                  }
                }}
                className={`py-3 rounded-xl font-semibold text-sm transition-all border active:scale-95 ${
                  method === m.key
                    ? 'bg-blue-600 border-blue-600 text-white shadow-sm'
                    : 'bg-white border-gray-200 text-gray-600 hover:border-blue-300 hover:text-blue-600'
                }`}
              >
                <span className="mr-1">{m.icon}</span>
                {m.label}
              </button>
            ))}
          </div>

          {method === 'Credit' && (
            <div
              className={`rounded-xl p-4 text-center ${hasCustomer ? 'bg-amber-50 border border-amber-200' : 'bg-red-50 border border-red-200'}`}
            >
              <p className="text-sm font-medium text-gray-700">
                Credit Sale — full amount on customer account
              </p>
              <p className="text-2xl font-black text-gray-800 mt-1">{fmt(grandTotal)}</p>
              {!hasCustomer && (
                <p className="text-red-600 text-xs mt-2">
                  Select a customer before completing a credit sale.
                </p>
              )}
            </div>
          )}

          {method !== 'Credit' && method === 'Cash' && (
            <div>
              <label className="block text-sm font-semibold text-gray-700 mb-1.5">Cash Received</label>
              <input
                ref={cashRef}
                type="number"
                value={cash}
                onChange={(e) => setCash(parseFloat(e.target.value) || 0)}
                className="w-full px-4 py-3 text-2xl font-bold border-2 border-gray-200 rounded-xl focus:outline-none focus:border-blue-500 transition"
              />
            </div>
          )}
          {method !== 'Credit' && method === 'Card' && (
            <div>
              <label className="block text-sm font-semibold text-gray-700 mb-1.5">Card Amount</label>
              <input
                type="number"
                value={card}
                onChange={(e) => setCard(parseFloat(e.target.value) || 0)}
                className="w-full px-4 py-3 text-2xl font-bold border-2 border-gray-200 rounded-xl focus:outline-none focus:border-blue-500 transition"
              />
            </div>
          )}
          {method !== 'Credit' && method === 'Mixed' && (
            <div className="space-y-3">
              <div>
                <label className="block text-sm font-semibold text-gray-700 mb-1">Cash</label>
                <input
                  ref={cashRef}
                  type="number"
                  value={cash}
                  onChange={(e) => {
                    const c = parseFloat(e.target.value) || 0;
                    setCash(c);
                    setCard(Math.max(0, grandTotal - c));
                  }}
                  className="w-full px-4 py-3 text-lg font-bold border-2 border-gray-200 rounded-xl focus:outline-none focus:border-blue-500 transition"
                />
              </div>
              <div>
                <label className="block text-sm font-semibold text-gray-700 mb-1">Card</label>
                <input
                  type="number"
                  value={card}
                  onChange={(e) => {
                    const c = parseFloat(e.target.value) || 0;
                    setCard(c);
                    setCash(Math.max(0, grandTotal - c));
                  }}
                  className="w-full px-4 py-3 text-lg font-bold border-2 border-gray-200 rounded-xl focus:outline-none focus:border-blue-500 transition"
                />
              </div>
              {!isValid && <p className="text-red-500 text-xs">Cash + Card must equal the total.</p>}
            </div>
          )}

          {method !== 'Credit' && (
            <div
              className={`rounded-xl p-4 text-center ${change > 0 ? 'bg-green-50 border border-green-200' : 'bg-gray-50 border border-gray-200'}`}
            >
              <p className="text-xs font-medium text-gray-500 uppercase tracking-wide">Change</p>
              <p className={`text-3xl font-black mt-1 ${change > 0 ? 'text-green-600' : 'text-gray-400'}`}>
                {fmt(change)}
              </p>
            </div>
          )}
        </div>

        <div className="px-6 pb-6 flex gap-3">
          <button
            type="button"
            onClick={onClose}
            className="flex-1 py-3.5 rounded-xl border border-gray-200 text-gray-600 font-semibold hover:bg-gray-50 transition active:scale-95"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={handleConfirm}
            disabled={!isValid || loading}
            className="flex-1 py-3.5 rounded-xl bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white font-bold text-base transition shadow-sm active:scale-95"
          >
            {loading ? 'Processing…' : method === 'Credit' ? 'Confirm Credit Sale' : 'Confirm Payment'}
          </button>
        </div>
      </div>
    </div>
  );
};

export default POSPaymentModal;
