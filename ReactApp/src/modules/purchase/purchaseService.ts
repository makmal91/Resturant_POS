import apiClient from '../../services/api';

export type PurchaseStatus = 'Draft' | 'Posted' | 'Cancelled';

export interface PurchaseItemPayload {
  productId: number;
  variantId?: number | null;
  unitId: number;
  quantity: number;
  conversionFactor: number;
  costPrice: number;
}

export interface PurchasePayload {
  invoiceNo: string;
  supplierId: number;
  warehouseId: number;
  purchaseDate: string;
  notes?: string;
  branchId: number;
  items: PurchaseItemPayload[];
}

export interface PurchaseItemDto {
  id: number;
  productId: number;
  productName: string;
  variantId?: number | null;
  variantName?: string | null;
  unitId: number;
  unitName: string;
  quantity: number;
  conversionFactor: number;
  baseQuantity: number;
  costPrice: number;
  totalCost: number;
}

export interface PurchaseDto {
  id: number;
  invoiceNo: string;
  supplierId: number;
  supplierName: string;
  warehouseId: number;
  warehouseName: string;
  branchId: number;
  branchName: string;
  purchaseDate: string;
  totalAmount: number;
  status: PurchaseStatus;
  notes: string;
  itemCount: number;
  createdDate: string;
  voidedAt?: string | null;
  voidedByName?: string | null;
  items?: PurchaseItemDto[];
}

export interface VoidPurchasePayload {
  businessId: number;
  branchId: number;
  voidedByName?: string;
  reason?: string;
}

export interface PurchaseLedgerEntry {
  id: number;
  type: string;
  productId: number;
  productName: string;
  variantId: number | null;
  variantName: string | null;
  warehouseId: number;
  warehouseName: string;
  quantityInBaseUnit: number;
  unitPrice: number;
  totalAmount: number;
  date: string;
  remarks: string;
}

const branchHeader = (branchId: number) => ({ headers: { 'X-Branch-Id': String(branchId) } });

export const purchaseService = {
  getAll: (branchId: number, page = 1, pageSize = 25, search?: string, status?: PurchaseStatus | null) =>
    apiClient.get('/purchase', {
      params: {
        branchId,
        page,
        pageSize,
        ...(search ? { search } : {}),
        ...(status ? { status } : {}),
      },
      ...branchHeader(branchId),
    }),

  getById: (id: number, branchId: number) =>
    apiClient.get<PurchaseDto>(`/purchase/${id}`, { params: { branchId }, ...branchHeader(branchId) }),

  create: (data: PurchasePayload) =>
    apiClient.post<PurchaseDto>('/purchase', data, branchHeader(data.branchId)),

  update: (id: number, data: PurchasePayload) =>
    apiClient.put<PurchaseDto>(`/purchase/${id}`, data, branchHeader(data.branchId)),

  post: (id: number, branchId: number) =>
    apiClient.post<PurchaseDto>(`/purchase/${id}/post`, null, {
      params: { branchId },
      ...branchHeader(branchId),
    }),

  delete: (id: number, branchId: number) =>
    apiClient.delete(`/purchase/${id}`, { params: { branchId }, ...branchHeader(branchId) }),

  void: (id: number, payload: VoidPurchasePayload) =>
    apiClient.post<PurchaseDto>(`/purchase/${id}/void`, payload, branchHeader(payload.branchId)),

  getLedgerHistory: (id: number, branchId: number) =>
    apiClient.get<PurchaseLedgerEntry[]>(`/purchase/${id}/ledger`, {
      params: { branchId },
      ...branchHeader(branchId),
    }),
};
