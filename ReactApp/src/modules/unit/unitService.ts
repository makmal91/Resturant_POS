import apiClient from '../../services/api';
import { ManagementFormValues } from '../shared/types';

export const unitService = {
  getAll: () => apiClient.get('/units'),
  getById: (id: number) => apiClient.get(`/units/${id}`),
  create: (data: ManagementFormValues) => apiClient.post('/units', data),
  update: (id: number, data: ManagementFormValues) => apiClient.put(`/units/${id}`, data),
  delete: (id: number) => apiClient.delete(`/units/${id}`),
};
