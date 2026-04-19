import apiClient from '../../services/api';
import { ManagementFormValues } from '../shared/types';

export const taxService = {
  getAll: () => apiClient.get('/taxes'),
  getById: (id: number) => apiClient.get(`/taxes/${id}`),
  create: (data: ManagementFormValues) => apiClient.post('/taxes', data),
  update: (id: number, data: ManagementFormValues) => apiClient.put(`/taxes/${id}`, data),
  delete: (id: number) => apiClient.delete(`/taxes/${id}`),
};
