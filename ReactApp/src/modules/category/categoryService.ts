import apiClient from '../../services/api';
import { ManagementFormValues } from '../shared/types';

const DEFAULT_BRANCH_ID = 1;

export const categoryService = {
  getAll: () => apiClient.get('/categories', { params: { branchId: DEFAULT_BRANCH_ID } }),
  getById: (id: number) => apiClient.get(`/categories/${id}`, { params: { branchId: DEFAULT_BRANCH_ID } }),
  create: (data: ManagementFormValues) =>
    apiClient.post('/categories', {
      name: data.name,
      description: data.description,
      categoryType: data.categoryType ?? 'Sale',
      branchId: Number(data.branchId ?? DEFAULT_BRANCH_ID),
    }),
  update: (id: number, data: ManagementFormValues) =>
    apiClient.put(`/categories/${id}`, {
      name: data.name,
      description: data.description,
      categoryType: data.categoryType ?? 'Sale',
      branchId: Number(data.branchId ?? DEFAULT_BRANCH_ID),
    }),
  delete: (id: number) => apiClient.delete(`/categories/${id}`, { params: { branchId: DEFAULT_BRANCH_ID } }),
};
