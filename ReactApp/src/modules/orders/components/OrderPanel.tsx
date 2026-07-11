import React, { useCallback, useState } from 'react';
import { useBusinessCurrency } from '../../../hooks/useBusinessCurrency';
import type { CartItem } from '../../pos/posService';
import OrderItem from './OrderItem';
import OrderSummary from './OrderSummary';
import type { OrderType } from '../localHeldOrders';
import { POS_INTERACTION, POS_THEME } from '../theme';

export interface OrderPanelProps {
  orderType: OrderType;
  cart: CartItem[];
  cartItemCount: number;
  subtotalLabel: string;
  totalLabel: string;
  taxLabel: string;
  showTax: boolean;
  heldCount: number;
  warehouseId: number;
  cartStockError: string | null;
  onIncreaseQty: (key: string) => void;
  onDecreaseQty: (key: string) => void;
  onRemove: (key: string) => void;
  onHold: () => void;
  onOpenHeld: () => void;
  onPay: () => void;
}

const OrderPanel: React.FC<OrderPanelProps> = React.memo(
  ({
    orderType,
    cart,
    cartItemCount,
    subtotalLabel,
    totalLabel,
    taxLabel,
    showTax,
    heldCount,
    warehouseId,
    cartStockError,
    onIncreaseQty,
    onDecreaseQty,
    onRemove,
    onHold,
    onOpenHeld,
    onPay,
  }) => {
    const { fmt } = useBusinessCurrency();
    const [selectedKey, setSelectedKey] = useState<string | null>(null);

    const handleSelect = useCallback((key: string) => {
      setSelectedKey((prev) => (prev === key ? null : key));
    }, []);

    return (
      <aside className="w-72 sm:w-80 flex-shrink-0 flex flex-col min-h-0 rounded-lg border border-gray-200 bg-white shadow-sm overflow-hidden">
        <div className="px-4 py-3 border-b border-gray-200 bg-white">
          <h2 className="text-sm font-semibold text-gray-900">Current Order</h2>
          <p className="text-xs text-gray-500 mt-0.5">{orderType}</p>
        </div>

        <div className="flex-1 overflow-y-auto p-3 space-y-2 bg-white">
          {cart.length === 0 ? (
            <div className="text-center py-16 px-4">
              <p className="text-sm font-medium text-gray-600">Tap products to add</p>
              <p className="text-xs text-gray-400 mt-1">Items will appear here</p>
            </div>
          ) : (
            cart.map((item) => (
              <OrderItem
                key={item.cartKey}
                item={item}
                priceLabel={`${fmt(item.unitPrice)} × ${item.quantity}`}
                lineTotalLabel={fmt(item.lineTotal)}
                selected={selectedKey === item.cartKey}
                onSelect={() => handleSelect(item.cartKey)}
                onIncrease={() => onIncreaseQty(item.cartKey)}
                onDecrease={() => onDecreaseQty(item.cartKey)}
                onRemove={() => onRemove(item.cartKey)}
              />
            ))
          )}
        </div>

        <div className="flex-shrink-0 border-t border-gray-200 bg-white p-4 space-y-3">
          <OrderSummary
            itemCount={cartItemCount}
            subtotalLabel={subtotalLabel}
            totalLabel={totalLabel}
            showTax={showTax}
            taxLabel={taxLabel}
          />

          <div className="grid grid-cols-2 gap-2">
            <button
              type="button"
              onClick={onHold}
              disabled={cart.length === 0 || !warehouseId}
              className={`min-h-[48px] rounded-lg border border-gray-200 bg-white text-gray-700 text-sm font-medium disabled:opacity-40 ${POS_INTERACTION.button}`}
            >
              Hold
            </button>
            <button
              type="button"
              onClick={onOpenHeld}
              className={`min-h-[48px] rounded-lg border border-gray-200 bg-white text-gray-700 text-sm font-medium ${POS_INTERACTION.button}`}
            >
              Held ({heldCount})
            </button>
          </div>

          <button
            type="button"
            onClick={onPay}
            disabled={cart.length === 0 || !warehouseId || !!cartStockError}
            className={`w-full py-3 rounded-lg text-white text-base font-semibold disabled:opacity-40 disabled:cursor-not-allowed hover:opacity-90 active:scale-95 transition-all duration-75`}
            style={{ backgroundColor: POS_THEME.primary }}
          >
            Pay Now
          </button>
        </div>
      </aside>
    );
  },
);

OrderPanel.displayName = 'OrderPanel';

export default OrderPanel;
