import apiClient from '../../services/api';
import { PagedListParams } from '../shared/pagedList';

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

const branchRequestConfig = (branchId: number) => ({
  headers: { 'X-Branch-Id': String(branchId) },
});

export const categoryService = {
  getImageEndpoint: (id: number) => `/categories/${id}/image`,

  getImageUrl: (id: number, branchId: number) => {
    const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.trim() || '/api';
    return `${apiBaseUrl.replace(/\/$/, '')}/categories/${id}/image?branchId=${branchId}`;
  },

  getAll: (
    branchId: number,
    page = 1,
    pageSize = 25,
    params: Omit<PagedListParams, 'page' | 'pageSize'> = {},
  ) =>
    apiClient.get('/categories', {
      params: {
        branchId,
        page,
        pageSize,
        search: params.search?.trim() || undefined,
        sortBy: params.sortBy || undefined,
        sortDirection: params.sortDirection || undefined,
      },
      ...branchRequestConfig(branchId),
    }),

  getById: (id: number, branchId: number) =>
    apiClient.get(`/categories/${id}`, { params: { branchId }, ...branchRequestConfig(branchId) }),

  create: (data: FormData, branchId: number) =>
    apiClient.post('/categories', data, branchRequestConfig(branchId)),

  update: (id: number, data: FormData, branchId: number) =>
    apiClient.put(`/categories/${id}`, data, branchRequestConfig(branchId)),

  delete: (id: number, branchId: number) =>
    apiClient.delete(`/categories/${id}`, { params: { branchId }, ...branchRequestConfig(branchId) }),
};
