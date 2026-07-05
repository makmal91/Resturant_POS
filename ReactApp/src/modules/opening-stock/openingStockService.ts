import apiClient from '../../services/api';

export interface OpeningStockLinePayload {
  productId: number;
  variantId?: number | null;
  unitId: number;
  quantity: number;
  costPrice: number;
}

export interface OpeningStockVoucherPayload {
  voucherDate: string;
  description?: string;
  warehouseId: number;
  branchId: number;
  lines: OpeningStockLinePayload[];
}

export interface ReverseOpeningStockPayload {
  reason?: string;
}

export interface OpeningStockLineDto {
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
  costPrice: number;
  totalAmount: number;
  baseUnitName?: string;
}

export interface OpeningStockVoucherDto {
  id: number;
  voucherNo: string;
  voucherDate: string;
  description?: string;
  warehouseId: number;
  warehouseName: string;
  totalAmount: number;
  branchId: number;
  branchName: string;
  createdBy?: number | null;
  createdByName?: string;
  createdAt: string;
  isReversed: boolean;
  reversedAt?: string | null;
  referenceVoucherId?: number | null;
  reversalVoucherId?: number | null;
  lines?: OpeningStockLineDto[];
}

const branchConfig = (branchId: number) => ({
  params: { branchId },
  headers: { 'X-Branch-Id': String(branchId) },
});

export const openingStockService = {
  getAll: (
    branchId: number,
    page = 1,
    pageSize = 25,
    search?: string,
  ) =>
    apiClient.get('/opening-stock', {
      params: {
        branchId,
        page,
        pageSize,
        search: search?.trim() || undefined,
      },
      ...branchConfig(branchId),
    }),

  getById: (id: number, branchId: number) =>
    apiClient.get<OpeningStockVoucherDto>(`/opening-stock/${id}`, branchConfig(branchId)),

  create: (branchId: number, payload: OpeningStockVoucherPayload) =>
    apiClient.post('/opening-stock', { ...payload, branchId }, branchConfig(branchId)),

  update: (id: number, branchId: number, payload: OpeningStockVoucherPayload) =>
    apiClient.put(`/opening-stock/${id}`, { ...payload, branchId }, branchConfig(branchId)),

  reverse: (id: number, branchId: number, payload?: ReverseOpeningStockPayload) =>
    apiClient.post(
      `/opening-stock/${id}/reverse`,
      payload ?? {},
      branchConfig(branchId),
    ),
};
