import React from 'react';
import { POS_THEME } from '../theme';

export interface OrderSummaryProps {
  subtotalLabel: string;
  totalLabel: string;
  itemCount: number;
  showTax?: boolean;
  taxLabel?: string;
}

const OrderSummary: React.FC<OrderSummaryProps> = React.memo(
  ({ subtotalLabel, totalLabel, itemCount, showTax = false, taxLabel }) => (
    <div className="rounded-lg bg-gray-50 border border-gray-200 p-4 space-y-2 text-sm">
      <div className="flex items-center justify-between text-gray-600">
        <span>Items</span>
        <span className="tabular-nums font-medium">{itemCount}</span>
      </div>
      <div className="flex items-center justify-between text-gray-700">
        <span>Subtotal</span>
        <span className="font-medium tabular-nums">{subtotalLabel}</span>
      </div>
      {showTax && taxLabel && (
        <div className="flex items-center justify-between text-gray-700">
          <span>Tax</span>
          <span className="font-medium tabular-nums">{taxLabel}</span>
        </div>
      )}
      <div className="flex items-center justify-between pt-3 mt-1 border-t border-gray-200">
        <span className="font-semibold text-gray-900">Total</span>
        <span
          className="text-xl font-bold tabular-nums"
          style={{ color: POS_THEME.primary }}
        >
          {totalLabel}
        </span>
      </div>
    </div>
  ),
);

OrderSummary.displayName = 'OrderSummary';

export default OrderSummary;
