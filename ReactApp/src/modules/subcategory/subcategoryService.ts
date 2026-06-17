import apiClient from '../../services/api';

export interface SubCategoryPayload {
  name: string;
  code: string;
  description: string;
  displayOrder: number;
  status: boolean;
  icon: string;
  categoryId: number;
  branchId: number;
}

const buildJsonPayload = (data: SubCategoryPayload) => ({
  name: data.name,
  code: data.code,
  description: data.description,
  displayOrder: data.displayOrder,
  status: data.status,
  icon: data.icon,
  categoryId: data.categoryId,
  branchId: data.branchId,
});

export interface SubCategoryListResponse {
  subCategories: unknown[];
  totalRecords: number;
  totalPages: number;
  currentPage: number;
  pageSize: number;
}

const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.trim() || '/api';

const branchRequestConfig = (branchId: number) => ({
  headers: { 'X-Branch-Id': String(branchId) },
});

export const subCategoryService = {
  getImageEndpoint: (id: number) => `/subcategories/${id}/image`,

  getImageUrl: (id: number, branchId: number) =>
    `${apiBaseUrl.replace(/\/$/, '')}/subcategories/${id}/image?branchId=${branchId}`,

  getAll: (
    branchId: number,
    page = 1,
    pageSize = 25,
    search?: string,
    categoryId?: number,
    status?: boolean | null,
    sortBy?: string,
    sortDirection?: 'asc' | 'desc',
  ) =>
    apiClient.get<SubCategoryListResponse>('/subcategories', {
      params: {
        branchId,
        page,
        pageSize,
        ...(search ? { search } : {}),
        ...(categoryId ? { categoryId } : {}),
        ...(status !== null && status !== undefined ? { status } : {}),
        ...(sortBy ? { sortBy } : {}),
        ...(sortDirection ? { sortDirection } : {}),
      },
      ...branchRequestConfig(branchId),
    }),

  getById: (id: number, branchId: number, includeImage = true) =>
    apiClient.get(`/subcategories/${id}`, {
      params: { branchId, includeImage },
      ...branchRequestConfig(branchId),
    }),

  create: (data: FormData, branchId: number) =>
    apiClient.post('/subcategories', data, branchRequestConfig(branchId)),

  createJson: (data: SubCategoryPayload, branchId: number) =>
    apiClient.post('/subcategories', buildJsonPayload({ ...data, branchId }), branchRequestConfig(branchId)),

  update: (id: number, data: FormData, branchId: number) =>
    apiClient.put(`/subcategories/${id}`, data, branchRequestConfig(branchId)),

  updateJson: (id: number, data: SubCategoryPayload, branchId: number) =>
    apiClient.put(`/subcategories/${id}`, buildJsonPayload({ ...data, branchId }), branchRequestConfig(branchId)),

  patchStatus: (branchId: number, items: Array<{ id: number; status: boolean }>) =>
    apiClient.patch(
      '/subcategories/status',
      { branchId, items },
      branchRequestConfig(branchId)
    ),

  delete: (id: number, branchId: number) =>
    apiClient.delete(`/subcategories/${id}`, {
      params: { branchId },
      ...branchRequestConfig(branchId),
    }),
};
