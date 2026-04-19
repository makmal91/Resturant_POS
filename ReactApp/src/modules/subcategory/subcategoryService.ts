import apiClient from '../../services/api';

export interface SubCategoryPayload {
  name: string;
  description: string;
  displayOrder: number;
  status: boolean;
  icon: string;
  categoryId: number;
  branchId: number;
}

export const subCategoryService = {
  getAll: (branchId: number, categoryId?: number) =>
    apiClient.get('/subcategories', {
      params: {
        branchId,
        ...(categoryId ? { categoryId } : {}),
      },
    }),
  getById: (id: number, branchId: number) => apiClient.get(`/subcategories/${id}`, { params: { branchId } }),
  create: (data: SubCategoryPayload) => apiClient.post('/subcategories', data),
  update: (id: number, data: SubCategoryPayload) => apiClient.put(`/subcategories/${id}`, data),
  delete: (id: number, branchId: number) => apiClient.delete(`/subcategories/${id}`, { params: { branchId } }),
};
