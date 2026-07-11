import { useMemo } from 'react';
import type { CartItem } from '../posService';

export interface UsePOSBillingTotalsOptions {
  cart: CartItem[];
  discountMode: 'percent' | 'amount';
  discountInput: string;
}

export function usePOSBillingTotals({ cart, discountMode, discountInput }: UsePOSBillingTotalsOptions) {
  return useMemo(() => {
    const subTotal = cart.reduce((s, c) => s + c.quantity * c.unitPrice, 0);
    const totalItemDiscount = cart.reduce((s, c) => {
      const d =
        c.discountAmount > 0
          ? c.discountAmount
          : (c.quantity * c.unitPrice * c.discountPercent) / 100;
      return s + d;
    }, 0);
    const totalTax = cart.reduce((s, c) => {
      const net =
        c.quantity * c.unitPrice -
        (c.discountAmount > 0
          ? c.discountAmount
          : (c.quantity * c.unitPrice * c.discountPercent) / 100);
      return s + (net * c.taxPercent) / 100;
    }, 0);
    const discountRaw = Math.max(0, parseFloat(discountInput) || 0);
    const billDiscount =
      discountMode === 'percent'
        ? (subTotal * Math.min(100, discountRaw)) / 100
        : Math.min(subTotal, discountRaw);
    const grandTotal = Math.max(0, subTotal - totalItemDiscount - billDiscount + totalTax);

    return { subTotal, totalItemDiscount, totalTax, billDiscount, grandTotal };
  }, [cart, discountMode, discountInput]);
}
