import apiClient from '../../services/api';
import { ManagementFormValues } from '../shared/types';

export const discountService = {
  getAll: () => apiClient.get('/discounts'),
  getById: (id: number) => apiClient.get(`/discounts/${id}`),
  create: (data: ManagementFormValues) => apiClient.post('/discounts', data),
  update: (id: number, data: ManagementFormValues) => apiClient.put(`/discounts/${id}`, data),
  delete: (id: number) => apiClient.delete(`/discounts/${id}`),
};
