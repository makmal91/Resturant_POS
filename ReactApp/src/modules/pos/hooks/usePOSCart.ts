import { useCallback, useEffect, useRef, useState } from 'react';
import {
  type CartItem,
  type PosProductLookup,
  cartKey,
  computeLineTotal,
  lookupToCartItem,
  applyUnitToCartItem,
  resolveSaleUnitPrice,
  checkStockForCartLine,
  validateCartStock,
} from '../posService';
import { lastUnitStorageKey } from '../UnitSelector';

export interface UsePOSCartOptions {
  pricingType: 'Retail' | 'Wholesale';
  stockFeatureEnabled: boolean;
}

export function usePOSCart({ pricingType, stockFeatureEnabled }: UsePOSCartOptions) {
  const [cart, setCart] = useState<CartItem[]>([]);
  const lastUnitByProductRef = useRef<Map<string, number>>(new Map());

  const addToCart = useCallback(
    (lookup: PosProductLookup): { success: boolean; error?: string } => {
      const storageKey = lastUnitStorageKey(lookup.productId, lookup.matchedVariantId ?? null);
      const preferredUnitId = lastUnitByProductRef.current.get(storageKey);
      const item = lookupToCartItem(lookup, pricingType, preferredUnitId);
      lastUnitByProductRef.current.set(storageKey, item.unitId);

      let stockErr: string | null = null;
      let merged = false;

      setCart((prev) => {
        const idx = prev.findIndex((c) => c.cartKey === item.cartKey);
        if (idx >= 0) {
          merged = true;
          const row = prev[idx];
          const updated = [...prev];
          updated[idx] = {
            ...row,
            quantity: row.quantity + 1,
            lineTotal: computeLineTotal({ ...row, quantity: row.quantity + 1 }),
          };
          return updated;
        }

        if (stockFeatureEnabled) {
          stockErr = checkStockForCartLine(
            prev,
            item.productId,
            item.variantId,
            item.variantName,
            item.productName,
            1,
            item.conversionFactor,
            item.stockBase,
            item.allowNegativeStock,
            item.baseUnitName,
          );
          if (stockErr) return prev;
        }

        return [...prev, item];
      });

      if (merged) return { success: true };
      if (stockErr) return { success: false, error: stockErr };
      return { success: true };
    },
    [pricingType, stockFeatureEnabled],
  );

  const updateQuantity = useCallback((key: string, qty: number) => {
    if (qty <= 0) {
      setCart((prev) => prev.filter((c) => c.cartKey !== key));
      return;
    }
    setCart((prev) =>
      prev.map((c) =>
        c.cartKey === key
          ? { ...c, quantity: qty, lineTotal: computeLineTotal({ ...c, quantity: qty }) }
          : c,
      ),
    );
  }, []);

  const updateItemUnit = useCallback(
    (key: string, unitId: number) => {
      setCart((prev) =>
        prev.map((c) => {
          if (c.cartKey !== key) return c;
          lastUnitByProductRef.current.set(lastUnitStorageKey(c.productId, c.variantId), unitId);
          return applyUnitToCartItem(c, unitId, pricingType);
        }),
      );
    },
    [pricingType],
  );

  const removeFromCart = useCallback((key: string) => {
    setCart((prev) => prev.filter((c) => c.cartKey !== key));
  }, []);

  const clearCart = useCallback(() => {
    setCart([]);
  }, []);

  useEffect(() => {
    setCart((prev) => {
      if (prev.length === 0) return prev;
      return prev.map((c) => {
        const unit = c.availableUnits.find((u) => u.unitId === c.unitId);
        const variant =
          c.variantId != null
            ? c.availableVariants.find((v) => v.variantId === c.variantId) ?? null
            : null;
        const baseUnit = c.availableUnits.find((u) => u.isBaseUnit) ?? c.availableUnits[0];
        const unitPrice = resolveSaleUnitPrice(
          pricingType,
          unit,
          variant,
          baseUnit?.sellingPrice ?? c.unitPrice,
          baseUnit?.wholesalePrice ?? c.unitPrice,
          c.isManualPriceOverride ? c.unitPrice : undefined,
        );
        return { ...c, unitPrice, lineTotal: computeLineTotal({ ...c, unitPrice }) };
      });
    });
  }, [pricingType]);

  const cartStockError = stockFeatureEnabled ? validateCartStock(cart) : null;

  return {
    cart,
    setCart,
    addToCart,
    updateQuantity,
    updateItemUnit,
    removeFromCart,
    clearCart,
    cartStockError,
    lastUnitByProductRef,
  };
}

export { cartKey, validateCartStock };
