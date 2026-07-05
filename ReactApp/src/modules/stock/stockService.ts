import apiClient from '../../services/api';

export type StockLedgerType =
  | 'PurchaseEntry'
  | 'SaleEntry'
  | 'PurchaseReturn'
  | 'SaleReturn'
  | 'Adjustment'
  | 'AdjustmentReversal'
  | 'TransferOut'
  | 'TransferIn'
  | 'Opening'
  | 'OpeningReversal'
  | 'SaleReversal'
  | 'PurchaseReversal';

export interface StockLedgerEntry {
  id: number;
  productId: number;
  productName: string;
  variantId?: number | null;
  variantName?: string | null;
  warehouseId: number;
  warehouseName: string;
  type: StockLedgerType;
  referenceId?: number | null;
  quantityInBaseUnit: number;
  unitPrice: number;
  totalAmount: number;
  date: string;
  remarks: string;
  branchId: number;
  branchName: string;
}

export interface StockBalance {
  productId: number;
  productName: string;
  productCode: string;
  variantId?: number | null;
  variantName?: string | null;
  warehouseId: number;
  warehouseName: string;
  quantity: number;
  enableLowStockAlert?: boolean;
  lowStockAlertLevel?: number | null;
}

export interface StockAlertSettings {
  enableLowStockAlert?: boolean;
  lowStockAlertLevel?: number | null;
}

export type StockStatus = 'out_of_stock' | 'low_stock' | 'in_stock';

export function getStockStatus(qty: number, settings?: StockAlertSettings): StockStatus {
  if (qty <= 0) return 'out_of_stock';
  if (
    settings?.enableLowStockAlert &&
    settings.lowStockAlertLevel != null &&
    qty <= settings.lowStockAlertLevel
  ) {
    return 'low_stock';
  }
  return 'in_stock';
}

export function stockStatusBadgeVariant(
  status: StockStatus,
): 'danger' | 'warning' | 'success' {
  if (status === 'out_of_stock') return 'danger';
  if (status === 'low_stock') return 'warning';
  return 'success';
}

export function stockStatusLabel(status: StockStatus): string {
  if (status === 'out_of_stock') return 'Out of Stock';
  if (status === 'low_stock') return 'Low Stock';
  return 'In Stock';
}

export function stockStatusQtyColor(status: StockStatus): string {
  if (status === 'out_of_stock') return 'text-red-700';
  if (status === 'low_stock') return 'text-yellow-700';
  return 'text-green-700';
}

export interface StockTransferPayload {
  productId: number;
  variantId?: number | null;
  fromWarehouseId: number;
  toWarehouseId: number;
  quantity: number;
  remarks?: string;
  branchId: number;
}

const branchHeader = (branchId: number) => ({ headers: { 'X-Branch-Id': String(branchId) } });

export const stockService = {
  getLedger: (params: {
    branchId: number;
    productId?: number;
    variantId?: number;
    warehouseId?: number;
    type?: StockLedgerType;
    dateFrom?: string;
    dateTo?: string;
    page?: number;
    pageSize?: number;
  }) =>
    apiClient.get('/stock/ledger', {
      params: { ...params },
      ...branchHeader(params.branchId),
    }),

  getBalances: (branchId: number, warehouseId?: number, productId?: number, variantId?: number, variantWise = false) =>
    apiClient.get<StockBalance[]>('/stock/balances', {
      params: {
        branchId,
        variantWise,
        ...(warehouseId ? { warehouseId } : {}),
        ...(productId ? { productId } : {}),
        ...(variantId ? { variantId } : {}),
      },
      ...branchHeader(branchId),
    }),

  getCurrentStock: (branchId: number, productId: number, warehouseId: number, variantId?: number) =>
    apiClient.get('/stock/current', {
      params: { branchId, productId, warehouseId, ...(variantId ? { variantId } : {}) },
      ...branchHeader(branchId),
    }),

  transfer: (data: StockTransferPayload) =>
    apiClient.post('/stock/transfer', data, branchHeader(data.branchId)),

  getLowStockAlerts: (branchId: number, warehouseId?: number) =>
    apiClient.get('/stock/low-stock-alerts', {
      params: { branchId, ...(warehouseId ? { warehouseId } : {}) },
      ...branchHeader(branchId),
    }),
};
