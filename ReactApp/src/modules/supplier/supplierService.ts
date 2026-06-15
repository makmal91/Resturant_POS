import apiClient from '../../services/api';

export interface SupplierPayload {
  name: string;
  contactPerson?: string;
  phone?: string;
  email?: string;
  address?: string;
  taxNumber?: string;
  isActive: boolean;
  branchId: number;
}

export interface SupplierItem {
  id: number;
  name: string;
  contactPerson: string;
  phone: string;
  email: string;
  address: string;
  taxNumber: string;
  isActive: boolean;
  branchId: number;
  branchName: string;
  createdDate: string;
}

const branchHeader = (branchId: number) => ({ headers: { 'X-Branch-Id': String(branchId) } });

export const supplierService = {
  getAll: (branchId: number, page = 1, pageSize = 25, search?: string, isActive?: boolean | null) =>
    apiClient.get('/suppliers', {
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
    apiClient.get<SupplierItem[]>('/suppliers/active', {
      params: { branchId },
      ...branchHeader(branchId),
    }),

  getById: (id: number, branchId: number) =>
    apiClient.get(`/suppliers/${id}`, { params: { branchId }, ...branchHeader(branchId) }),

  create: (data: SupplierPayload) =>
    apiClient.post('/suppliers', data, branchHeader(data.branchId)),

  update: (id: number, data: SupplierPayload) =>
    apiClient.put(`/suppliers/${id}`, data, branchHeader(data.branchId)),

  delete: (id: number, branchId: number) =>
    apiClient.delete(`/suppliers/${id}`, { params: { branchId }, ...branchHeader(branchId) }),
};
