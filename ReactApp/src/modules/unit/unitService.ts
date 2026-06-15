import apiClient from '../../services/api';
import { ManagementFormValues } from '../shared/types';

const buildPayload = (data: ManagementFormValues) => ({
  name: data.name,
  code: data.code ?? '',
  description: data.description,
  conversionFactor: Number(data.conversionFactor ?? 1),
  status: Boolean(data.isActive ?? true),
  isActive: Boolean(data.isActive ?? true),
  branchId: Number(data.branchId ?? 0),
});

export const unitService = {
  getAll: () => apiClient.get('/units'),
  getById: (id: number) => apiClient.get(`/units/${id}`),
  create: (data: ManagementFormValues) => apiClient.post('/units', buildPayload(data)),
  update: (id: number, data: ManagementFormValues) => apiClient.put(`/units/${id}`, buildPayload(data)),
  delete: (id: number) => apiClient.delete(`/units/${id}`),
};
