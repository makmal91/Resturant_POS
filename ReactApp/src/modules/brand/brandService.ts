import apiClient from '../../services/api';

export interface BrandPayload {
  name: string;
  description: string;
  status: boolean;
  branchId: number;
}

const buildJsonPayload = (data: BrandPayload) => ({
  name: data.name,
  description: data.description,
  status: data.status,
  branchId: data.branchId,
});

export interface BrandListResponse {
  brands: unknown[];
  totalRecords: number;
  totalPages: number;
  currentPage: number;
  pageSize: number;
}

const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.trim() || '/api';

const branchRequestConfig = (branchId: number) => ({
  headers: { 'X-Branch-Id': String(branchId) },
});

export const brandService = {
  getImageEndpoint: (id: number) => `/brands/${id}/image`,

  getImageUrl: (id: number, branchId: number) =>
    `${apiBaseUrl.replace(/\/$/, '')}/brands/${id}/image?branchId=${branchId}`,

  getAll: (
    branchId: number,
    page = 1,
    pageSize = 25,
    search?: string,
    status?: boolean | null
  ) =>
    apiClient.get<BrandListResponse>('/brands', {
      params: {
        branchId,
        page,
        pageSize,
        ...(search ? { search } : {}),
        ...(status !== null && status !== undefined ? { status } : {}),
      },
      ...branchRequestConfig(branchId),
    }),

  getById: (id: number, branchId: number) =>
    apiClient.get(`/brands/${id}`, {
      params: { branchId },
      ...branchRequestConfig(branchId),
    }),

  create: (data: FormData, branchId: number) =>
    apiClient.post('/brands', data, branchRequestConfig(branchId)),

  createJson: (data: BrandPayload, branchId: number) =>
    apiClient.post('/brands', buildJsonPayload({ ...data, branchId }), branchRequestConfig(branchId)),

  update: (id: number, data: FormData, branchId: number) =>
    apiClient.put(`/brands/${id}`, data, branchRequestConfig(branchId)),

  updateJson: (id: number, data: BrandPayload, branchId: number) =>
    apiClient.put(`/brands/${id}`, buildJsonPayload({ ...data, branchId }), branchRequestConfig(branchId)),

  patchStatus: (branchId: number, items: Array<{ id: number; status: boolean }>) =>
    apiClient.patch('/brands/status', { branchId, items }, branchRequestConfig(branchId)),

  delete: (id: number, branchId: number) =>
    apiClient.delete(`/brands/${id}`, {
      params: { branchId },
      ...branchRequestConfig(branchId),
    }),
};
