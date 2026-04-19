import apiClient from '../../services/api';
import { ManagementFormValues } from '../shared/types';

export const customerService = {
  getAll: () => apiClient.get('/customers'),
  getById: (id: number) => apiClient.get(`/customers/${id}`),
  create: (data: ManagementFormValues) => apiClient.post('/customers', data),
  update: (id: number, data: ManagementFormValues) => apiClient.put(`/customers/${id}`, data),
  delete: (id: number) => apiClient.delete(`/customers/${id}`),
};
