import apiClient from '../../services/api';

export interface CodeSequenceItem {
  id: number;
  moduleName: string;
  branchId?: number | null;
  branchName?: string | null;
  prefix: string;
  lastNumber: number;
  nextCodePreview: string;
  resetType: string;
  lastResetDate?: string | null;
}

const branchHeaders = (branchId?: number) =>
  branchId && branchId > 0 ? { headers: { 'X-Branch-Id': String(branchId) } } : {};

export const codeSequenceService = {
  getAll: async (branchId?: number) => {
    const response = await apiClient.get<CodeSequenceItem[]>('/code-sequences', {
      params: branchId && branchId > 0 ? { branchId } : undefined,
      ...branchHeaders(branchId),
    });
    return response.data;
  },

  updateLastNumber: async (id: number, lastNumber: number) => {
    const response = await apiClient.put<CodeSequenceItem>(`/code-sequences/${id}`, { lastNumber });
    return response.data;
  },
};
