import apiClient from './api';

const DEFAULT_BRANCH_ID = 1;

export const BranchService = {
  getAll: () => apiClient.get('/branches'),
  getById: (id: number) => apiClient.get(`/branches/${id}`),
  create: (data: any) => {
    const payload = {
      name: String(data?.name ?? ''),
      code: String(data?.code ?? ''),
      address: String(data?.address ?? ''),
      city: String(data?.city ?? ''),
      phone: String(data?.phone ?? ''),
      email: String(data?.email ?? ''),
      taxRate: Number(data?.taxRate ?? 0),
      currency: String(data?.currency ?? 'USD'),
      companyId: Number(data?.companyId ?? 0),
      isActive: Boolean(data?.isActive ?? true),
    };

    return apiClient.post('/branches', payload);
  },
  update: (id: number, data: any) => apiClient.put(`/branches/${id}`, {
    name: String(data?.name ?? ''),
    code: String(data?.code ?? ''),
    address: String(data?.address ?? ''),
    city: String(data?.city ?? ''),
    phone: String(data?.phone ?? ''),
    email: String(data?.email ?? ''),
    taxRate: Number(data?.taxRate ?? 0),
    currency: String(data?.currency ?? 'USD'),
    companyId: Number(data?.companyId ?? 0),
    isActive: Boolean(data?.isActive ?? true),
  }),
  delete: (id: number) => apiClient.delete(`/branches/${id}`),
};

export const UserService = {
  getAll: () => apiClient.get('/users'),
  getById: (id: number) => apiClient.get(`/users/${id}`),
  create: (data: any) => apiClient.post('/users', data),
  update: (id: number, data: any) => apiClient.put(`/users/${id}`, data),
  delete: (id: number) => apiClient.delete(`/users/${id}`),
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
