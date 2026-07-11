import { type CartItem, type PosSearchGroup } from '../../pos/posService';

export type OrderType = 'Dine-in' | 'Takeaway' | 'Delivery';

export interface LocalHeldOrder {
  id: string;
  heldAt: string;
  orderType: OrderType;
  cart: CartItem[];
  discountMode: 'percent' | 'amount';
  discountInput: string;
  pricingType: 'Retail' | 'Wholesale';
  warehouseId: number;
  customerId: number | null;
  customerName: string | null;
}

const storageKey = (branchId: number) => `restaurant-pos-held-${branchId}`;

export function loadLocalHeldOrders(branchId: number): LocalHeldOrder[] {
  try {
    const raw = localStorage.getItem(storageKey(branchId));
    if (!raw) return [];
    const parsed = JSON.parse(raw) as LocalHeldOrder[];
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

export function saveLocalHeldOrder(branchId: number, order: LocalHeldOrder): void {
  const existing = loadLocalHeldOrders(branchId);
  localStorage.setItem(storageKey(branchId), JSON.stringify([order, ...existing]));
}

export function removeLocalHeldOrder(branchId: number, id: string): void {
  const existing = loadLocalHeldOrders(branchId).filter((order) => order.id !== id);
  localStorage.setItem(storageKey(branchId), JSON.stringify(existing));
}

export interface VariantPickerState {
  group: PosSearchGroup;
  productName: string;
}
