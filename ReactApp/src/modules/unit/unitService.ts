import apiClient from '../../services/api';
import { ManagementFormValues } from '../shared/types';
import { PagedListParams } from '../shared/pagedList';

const buildPayload = (data: ManagementFormValues) => ({
  name: data.name,
  code: data.code ?? '',
  defaultConversionFactor: Number(data.defaultConversionFactor ?? data.conversionFactor ?? 1),
  status: Boolean(data.isActive ?? true),
  isActive: Boolean(data.isActive ?? true),
  branchId: Number(data.branchId ?? 0),
});

const branchConfig = (branchId: number) => ({
  params: { branchId },
  headers: { 'X-Branch-Id': String(branchId) },
});

export const unitService = {
  getPaged: (branchId: number, params: PagedListParams = {}) =>
    apiClient.get('/units', {
      params: {
        branchId,
        page: params.page ?? 1,
        pageSize: params.pageSize ?? 10,
        search: params.search?.trim() || undefined,
        sortBy: params.sortBy || undefined,
        sortDirection: params.sortDirection || undefined,
      },
      ...branchConfig(branchId),
    }),

  getById: (id: number, branchId = 0) =>
    apiClient.get(`/units/${id}`, branchConfig(branchId)),

  create: (data: ManagementFormValues) =>
    apiClient.post('/units', buildPayload(data)),

  update: (id: number, data: ManagementFormValues) =>
    apiClient.put(`/units/${id}`, buildPayload(data)),

  delete: (id: number, branchId = 0) =>
    apiClient.delete(`/units/${id}`, branchConfig(branchId)),

  listKey: 'units',
};
