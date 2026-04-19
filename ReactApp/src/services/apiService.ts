import axios, { AxiosInstance } from 'axios';

const API_BASE_URL = 'https://localhost:7183/api';

const apiClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add token to requests if it exists
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('authToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export const BranchService = {
  getAll: () => apiClient.get('/branches'),
  getById: (id: number) => apiClient.get(`/branches/${id}`),
  create: (data: any) => apiClient.post('/branches', data),
  update: (id: number, data: any) => apiClient.put(`/branches/${id}`, data),
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
  getAll: () => apiClient.get('/menu'),
  getById: (id: number) => apiClient.get(`/menu/${id}`),
  create: (data: any) => apiClient.post('/menu', data),
  update: (id: number, data: any) => apiClient.put(`/menu/${id}`, data),
  delete: (id: number) => apiClient.delete(`/menu/${id}`),
  getCategories: () => apiClient.get('/menu/categories'),
};

export const InventoryService = {
  getAll: () => apiClient.get('/inventory'),
  getById: (id: number) => apiClient.get(`/inventory/${id}`),
  create: (data: any) => apiClient.post('/inventory', data),
  update: (id: number, data: any) => apiClient.put(`/inventory/${id}`, data),
  delete: (id: number) => apiClient.delete(`/inventory/${id}`),
};

export const OrderService = {
  getAll: () => apiClient.get('/orders'),
  getById: (id: number) => apiClient.get(`/orders/${id}`),
  create: (data: any) => apiClient.post('/orders', data),
  update: (id: number, data: any) => apiClient.put(`/orders/${id}`, data),
  delete: (id: number) => apiClient.delete(`/orders/${id}`),
};

export default apiClient;
