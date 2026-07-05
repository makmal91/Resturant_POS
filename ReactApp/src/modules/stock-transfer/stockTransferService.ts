import apiClient from '../../services/api';

export interface StockTransferLinePayload {
  productId: number;
  variantId?: number | null;
  unitId: number;
  quantity: number;
}

export interface StockTransferPayload {
  transferDate: string;
  description?: string;
  fromWarehouseId: number;
  toWarehouseId: number;
  branchId: number;
  lines: StockTransferLinePayload[];
}

export interface StockTransferLineDto {
  id: number;
  productId: number;
  productName: string;
  productCode: string;
  variantId?: number | null;
  variantName?: string;
  unitId?: number | null;
  unitName?: string;
  unitQuantity?: number;
  conversionFactor?: number;
  quantity: number;
  baseUnitName?: string;
}

export interface StockTransferDto {
  id: number;
  transferNo: string;
  transferDate: string;
  description?: string;
  fromWarehouseId: number;
  fromWarehouseName: string;
  toWarehouseId: number;
  toWarehouseName: string;
  lineCount: number;
  branchId: number;
  branchName: string;
  createdAt: string;
  isReversed: boolean;
  reversedAt?: string | null;
  lines?: StockTransferLineDto[];
}

const branchConfig = (branchId: number) => ({
  params: { branchId },
  headers: { 'X-Branch-Id': String(branchId) },
});

export const stockTransferService = {
  getAll: (branchId: number, page = 1, pageSize = 25, search?: string) =>
    apiClient.get('/stock-transfer', {
      params: { branchId, page, pageSize, search: search?.trim() || undefined },
      ...branchConfig(branchId),
    }),

  getById: (id: number, branchId: number) =>
    apiClient.get<StockTransferDto>(`/stock-transfer/${id}`, branchConfig(branchId)),

  create: (branchId: number, payload: StockTransferPayload) =>
    apiClient.post('/stock-transfer', { ...payload, branchId }, branchConfig(branchId)),

  update: (id: number, branchId: number, payload: StockTransferPayload) =>
    apiClient.put(`/stock-transfer/${id}`, { ...payload, branchId }, branchConfig(branchId)),

  reverse: (id: number, branchId: number, reason?: string) =>
    apiClient.post(`/stock-transfer/${id}/reverse`, reason ? { reason } : {}, branchConfig(branchId)),
};
