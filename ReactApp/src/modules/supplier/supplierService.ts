import apiClient from '../../services/api';
import { ManagementFormValues } from '../shared/types';

export const supplierService = {
  getAll: () => apiClient.get('/suppliers'),
  getById: (id: number) => apiClient.get(`/suppliers/${id}`),
  create: (data: ManagementFormValues) => apiClient.post('/suppliers', data),
  update: (id: number, data: ManagementFormValues) => apiClient.put(`/suppliers/${id}`, data),
  delete: (id: number) => apiClient.delete(`/suppliers/${id}`),
};
