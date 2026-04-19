import apiClient from '../../services/api';

export interface CategoryPayload {
  name: string;
  code: string;
  description: string;
  displayOrder: number;
  imageUrl: string;
  icon: string;
  color: string;
  status: boolean;
  categoryType: 'Sale' | 'Inventory';
  branchId: number;
}

export const categoryService = {
  getAll: (branchId: number) => apiClient.get('/categories', { params: { branchId } }),
  getById: (id: number, branchId: number) => apiClient.get(`/categories/${id}`, { params: { branchId } }),
  create: (data: CategoryPayload) => apiClient.post('/categories', data),
  update: (id: number, data: CategoryPayload) => apiClient.put(`/categories/${id}`, data),
  delete: (id: number, branchId: number) => apiClient.delete(`/categories/${id}`, { params: { branchId } }),
};
