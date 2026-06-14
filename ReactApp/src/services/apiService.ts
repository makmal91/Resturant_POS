import apiClient from './api';

const DEFAULT_BRANCH_ID = 1;

export const BranchService = {
  getAll: (businessId?: number) =>
    apiClient.get('/branches', {
      params: businessId && businessId > 0 ? { businessId } : undefined,
    }),
  getById: (id: number, businessId?: number) =>
    apiClient.get(`/branches/${id}`, {
      params: businessId && businessId > 0 ? { businessId } : undefined,
    }),
  create: (data: any) => {
    const payload = {
      name: String(data?.name ?? ''),
      code: String(data?.code ?? ''),
      address: String(data?.address ?? ''),
      phone: String(data?.phone ?? ''),
      email: String(data?.email ?? ''),
      businessId: Number(data?.businessId ?? data?.companyId ?? 1),
      companyId: Number(data?.companyId ?? data?.businessId ?? 0),
      countryId: Number(data?.countryId ?? 0),
      cityId: Number(data?.cityId ?? 0),
      isActive: Boolean(data?.isActive ?? String(data?.status ?? 'Active').toLowerCase() !== 'inactive'),
    };

    return apiClient.post('/branches', payload);
  },
  update: (id: number, data: any) =>
    apiClient.put(`/branches/${id}`, {
      name: String(data?.name ?? ''),
      code: String(data?.code ?? ''),
      address: String(data?.address ?? ''),
      phone: String(data?.phone ?? ''),
      email: String(data?.email ?? ''),
      businessId: Number(data?.businessId ?? data?.companyId ?? 1),
      companyId: Number(data?.companyId ?? data?.businessId ?? 0),
      countryId: Number(data?.countryId ?? 0),
      cityId: Number(data?.cityId ?? 0),
      isActive: Boolean(data?.isActive ?? String(data?.status ?? 'Active').toLowerCase() !== 'inactive'),
    }),
  delete: (id: number, businessId?: number) =>
    apiClient.delete(`/branches/${id}`, {
      params: businessId && businessId > 0 ? { businessId } : undefined,
    }),
};

export const CountryService = {
  getAll: () => apiClient.get('/countries'),
  getCitiesByCountry: (countryId: number) => apiClient.get(`/countries/${countryId}/cities`),
};

export const BusinessService = {
  getLogoUrl: (id: number) => {
    const baseUrl = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.trim() || '/api';
    return `${baseUrl.replace(/\/$/, '')}/businesses/${id}/logo`;
  },
  getAll: (params?: {
    page?: number;
    pageSize?: number;
    search?: string;
    sortBy?: string;
    sortDirection?: 'asc' | 'desc';
  }) =>
    apiClient.get('/businesses', {
      params: {
        page: params?.page ?? 1,
        pageSize: params?.pageSize ?? 10,
        search: params?.search?.trim() || undefined,
        sortBy: params?.sortBy || undefined,
        sortDirection: params?.sortDirection || undefined,
      },
    }),
  getById: (id: number) => apiClient.get(`/businesses/${id}`),
  create: (data: FormData) => apiClient.post('/businesses', data),
  update: (id: number, data: FormData) => apiClient.put(`/businesses/${id}`, data),
  delete: (id: number) => apiClient.delete(`/businesses/${id}`),
};

export const UserService = {
  getAll: (params: {
    branchId: number;
    page?: number;
    pageSize?: number;
    search?: string;
    sortBy?: string;
    sortDirection?: 'asc' | 'desc';
  }) =>
    apiClient.get('/users', {
      params,
      headers: { 'X-Branch-Id': String(params.branchId) },
    }),
  getById: (id: number, branchId: number) =>
    apiClient.get(`/users/${id}`, {
      params: { branchId },
      headers: { 'X-Branch-Id': String(branchId) },
    }),
  create: (data: unknown, branchId: number) =>
    apiClient.post('/users', data, { headers: { 'X-Branch-Id': String(branchId) } }),
  update: (id: number, data: unknown, branchId: number) =>
    apiClient.put(`/users/${id}`, data, {
      params: { branchId },
      headers: { 'X-Branch-Id': String(branchId) },
    }),
  delete: (id: number, branchId: number) =>
    apiClient.delete(`/users/${id}`, {
      params: { branchId },
      headers: { 'X-Branch-Id': String(branchId) },
    }),
  getRoles: () => apiClient.get('/roles'),
};

export const MenuService = {
  getAll: (branchId: number = DEFAULT_BRANCH_ID) =>
    apiClient.get('/menu/pos', { params: { branchId } }),
  getAllMenu: (branchId: number = DEFAULT_BRANCH_ID) =>
    apiClient.get('/menu/all', { params: { branchId } }),
  getById: (id: number, branchId: number = DEFAULT_BRANCH_ID) =>
    apiClient.get('/menu/all', { params: { branchId, id } }),
  create: (data: any) => apiClient.post('/menu/items', data),
  update: (id: number, data: any) => apiClient.put(`/menu/${id}`, data),
  delete: (id: number) => apiClient.delete(`/menu/${id}`),
  getCategories: (branchId: number = DEFAULT_BRANCH_ID, includeAll: boolean = false) =>
    apiClient.get(includeAll ? '/menu/all' : '/menu/pos', { params: { branchId } }),
};

export const InventoryService = {
  getAll: (branchId: number = DEFAULT_BRANCH_ID) => apiClient.get('/inventory', { params: { branchId } }),
  getById: (id: number) => apiClient.get(`/inventory/${id}`),
  create: (data: any) => apiClient.post('/inventory', data),
  update: (id: number, data: any) => apiClient.put(`/inventory/${id}`, data),
  delete: (id: number) => apiClient.delete(`/inventory/${id}`),
  purchase: (data: any) => apiClient.post('/inventory/purchase', data),
  adjust: (data: any) => apiClient.post('/inventory/adjust', data),
};

export const OrderService = {
  getAll: () => apiClient.get('/orders'),
  getById: (id: number) => apiClient.get(`/orders/${id}`),
  create: (data: any) => apiClient.post('/orders', data),
  update: (id: number, data: any) => apiClient.put(`/orders/${id}`, data),
  delete: (id: number) => apiClient.delete(`/orders/${id}`),
};

export default apiClient;
