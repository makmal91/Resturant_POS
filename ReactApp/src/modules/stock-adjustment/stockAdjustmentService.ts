import apiClient from '../../services/api';

export interface StockAdjustmentLinePayload {
  productId: number;
  variantId?: number | null;
  unitId: number;
  quantity: number;
  costPrice: number;
}

export interface StockAdjustmentPayload {
  adjustmentDate: string;
  warehouseId: number;
  adjustmentTypeId: number;
  remarks?: string;
  branchId: number;
  lines: StockAdjustmentLinePayload[];
}

export interface AdjustmentTypeDto {
  id: number;
  name: string;
  expenseAccountId: number;
  expenseAccountName: string;
  incomeAccountId: number;
  incomeAccountName: string;
  isActive: boolean;
}

export interface StockAdjustmentLineDto {
  id: number;
  productId: number;
  productName: string;
  productCode: string;
  variantId?: number | null;
  variantName?: string;
  unitId: number;
  unitName?: string;
  baseUnitName?: string;
  unitQuantity: number;
  conversionFactor?: number;
  baseQuantity: number;
  costPrice: number;
  totalCost: number;
}

export interface StockAdjustmentDto {
  id: number;
  adjustmentNo: string;
  adjustmentDate: string;
  warehouseId: number;
  warehouseName: string;
  adjustmentTypeId: number;
  adjustmentTypeName: string;
  remarks?: string;
  totalAmount: number;
  gainAmount: number;
  lossAmount: number;
  lineCount: number;
  branchId: number;
  branchName: string;
  createdAt: string;
  isReversed: boolean;
  reversedAt?: string | null;
  lines?: StockAdjustmentLineDto[];
}

export interface StockAdjustmentReportRow {
  id: number;
  adjustmentNo: string;
  adjustmentDate: string;
  warehouseName: string;
  adjustmentTypeName: string;
  gainAmount: number;
  lossAmount: number;
  netAmount: number;
  isReversed: boolean;
}

const branchConfig = (branchId: number) => ({
  params: { branchId },
  headers: { 'X-Branch-Id': String(branchId) },
});

export const stockAdjustmentService = {
  getAll: (
    branchId: number,
    page = 1,
    pageSize = 25,
    search?: string,
    filters?: {
      fromDate?: string;
      toDate?: string;
      warehouseId?: number;
      adjustmentTypeId?: number;
      direction?: string;
    },
  ) =>
    apiClient.get('/stock-adjustment', {
      params: {
        branchId,
        page,
        pageSize,
        search: search?.trim() || undefined,
        ...filters,
      },
      ...branchConfig(branchId),
    }),

  getTypes: (branchId: number) =>
    apiClient.get<AdjustmentTypeDto[]>('/stock-adjustment/types', branchConfig(branchId)),

  getById: (id: number, branchId: number) =>
    apiClient.get<StockAdjustmentDto>(`/stock-adjustment/${id}`, branchConfig(branchId)),

  create: (branchId: number, payload: StockAdjustmentPayload) =>
    apiClient.post('/stock-adjustment', { ...payload, branchId }, branchConfig(branchId)),

  update: (id: number, branchId: number, payload: StockAdjustmentPayload) =>
    apiClient.put(`/stock-adjustment/${id}`, { ...payload, branchId }, branchConfig(branchId)),

  delete: (id: number, branchId: number) =>
    apiClient.delete(`/stock-adjustment/${id}`, branchConfig(branchId)),

  reverse: (id: number, branchId: number, reason?: string) =>
    apiClient.post(`/stock-adjustment/${id}/reverse`, reason ? { reason } : {}, branchConfig(branchId)),

  getReport: (
    branchId: number,
    filters?: {
      fromDate?: string;
      toDate?: string;
      warehouseId?: number;
      adjustmentTypeId?: number;
      direction?: 'gain' | 'loss';
    },
  ) =>
    apiClient.get('/stock-adjustment/report', {
      params: { branchId, ...filters },
      ...branchConfig(branchId),
    }),
};
