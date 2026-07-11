import React from 'react';
import type { CartItem } from '../../pos/posService';
import { POS_INTERACTION } from '../theme';

export interface OrderItemProps {
  item: CartItem;
  priceLabel: string;
  lineTotalLabel: string;
  selected: boolean;
  onSelect: () => void;
  onIncrease: () => void;
  onDecrease: () => void;
  onRemove: () => void;
}

const OrderItem: React.FC<OrderItemProps> = React.memo(
  ({ item, priceLabel, lineTotalLabel, selected, onSelect, onIncrease, onDecrease, onRemove }) => (
    <div
      role="button"
      tabIndex={0}
      onClick={onSelect}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') onSelect();
      }}
      className={`rounded-lg border p-3 cursor-pointer ${POS_INTERACTION.card} ${
        selected
          ? 'border-[#0a3c6d] bg-[#0a3c6d]/5'
          : 'border-gray-200 bg-gray-50 hover:bg-gray-50/80'
      }`}
    >
      <div className="flex items-start justify-between gap-3 mb-3">
        <div className="min-w-0">
          <p className="font-bold text-gray-900 text-sm leading-snug line-clamp-2">{item.productName}</p>
          {item.variantName && <p className="text-xs text-gray-500 mt-0.5">{item.variantName}</p>}
          <p className="text-xs text-gray-500 mt-1 tabular-nums">{priceLabel}</p>
        </div>
        <p className="font-semibold text-sm text-gray-900 tabular-nums shrink-0">{lineTotalLabel}</p>
      </div>

      <div className="flex items-center gap-2" onClick={(e) => e.stopPropagation()}>
        <button
          type="button"
          onClick={onDecrease}
          className={`min-w-[44px] min-h-[44px] rounded-lg border border-gray-200 bg-white text-gray-700 font-semibold text-lg ${POS_INTERACTION.button}`}
          aria-label="Decrease quantity"
        >
          −
        </button>
        <span className="min-w-[44px] text-center font-bold text-gray-900 tabular-nums">
          {item.quantity}
        </span>
        <button
          type="button"
          onClick={onIncrease}
          className={`min-w-[44px] min-h-[44px] rounded-lg border border-gray-200 bg-white text-gray-700 font-semibold text-lg ${POS_INTERACTION.button}`}
          aria-label="Increase quantity"
        >
          +
        </button>
        <button
          type="button"
          onClick={onRemove}
          className={`ml-auto min-h-[44px] px-3 rounded-lg border border-gray-200 bg-white text-gray-600 text-xs font-medium ${POS_INTERACTION.button}`}
        >
          Remove
        </button>
      </div>
    </div>
  ),
);

OrderItem.displayName = 'OrderItem';

export default OrderItem;
