import apiClient from '../../services/api';

export interface WarehousePayload {
  name: string;
  code?: string;
  address?: string;
  isActive: boolean;
  branchId: number;
}

export interface WarehouseItem {
  id: number;
  name: string;
  code: string;
  address: string;
  isActive: boolean;
  branchId: number;
  branchName: string;
  createdDate: string;
  updatedDate?: string;
}

const branchHeader = (branchId: number) => ({ headers: { 'X-Branch-Id': String(branchId) } });

export const warehouseService = {
  getAll: (branchId: number, page = 1, pageSize = 25, search?: string, isActive?: boolean | null) =>
    apiClient.get('/warehouses', {
      params: {
        branchId,
        page,
        pageSize,
        ...(search ? { search } : {}),
        ...(isActive !== null && isActive !== undefined ? { isActive } : {}),
      },
      ...branchHeader(branchId),
    }),

  getAllActive: (branchId: number) =>
    apiClient.get<WarehouseItem[]>('/warehouses/active', {
      params: { branchId },
      ...branchHeader(branchId),
    }),

  getById: (id: number, branchId: number) =>
    apiClient.get(`/warehouses/${id}`, { params: { branchId }, ...branchHeader(branchId) }),

  create: (data: WarehousePayload) =>
    apiClient.post('/warehouses', data, branchHeader(data.branchId)),

  update: (id: number, data: WarehousePayload) =>
    apiClient.put(`/warehouses/${id}`, data, branchHeader(data.branchId)),

  delete: (id: number, branchId: number) =>
    apiClient.delete(`/warehouses/${id}`, { params: { branchId }, ...branchHeader(branchId) }),
};
