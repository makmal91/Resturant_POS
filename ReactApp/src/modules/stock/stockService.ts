import apiClient from '../../services/api';

export type StockLedgerType =
  | 'PurchaseEntry'
  | 'SaleEntry'
  | 'PurchaseReturn'
  | 'SaleReturn'
  | 'Adjustment'
  | 'TransferOut'
  | 'TransferIn';

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
};
