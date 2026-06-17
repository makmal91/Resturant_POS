import apiClient, { getApiErrorMessage } from '../../services/api';
import { masterDataService, type MasterType } from '../../services/masterDataService';

export interface MasterManageItem {
  id: number;
  name: string;
  hexCode?: string | null;
  description?: string | null;
  sortOrder?: number;
  isActive: boolean;
  countryId?: number | null;
}

export interface SaveMasterPayload {
  name: string;
  code?: string;
  description?: string;
  hexCode?: string;
  sortOrder?: number;
  isActive: boolean;
  branchId?: number;
  countryId?: number;
}

const GLOBAL_TYPES: MasterType[] = ['country', 'city'];

const isGlobalType = (type: MasterType) => GLOBAL_TYPES.includes(type);

const branchConfig = (branchId: number) => ({
  headers: branchId > 0 ? { 'X-Branch-Id': String(branchId) } : undefined,
});

export const masterService = {
  async listForManagement(
    type: MasterType,
    branchId = 0,
    options?: { countryId?: number; includeInactive?: boolean },
  ): Promise<MasterManageItem[]> {
    const res = await apiClient.get<MasterManageItem[]>(`/masters/${type}`, {
      params: {
        branchId: !isGlobalType(type) && branchId > 0 ? branchId : undefined,
        countryId: options?.countryId && options.countryId > 0 ? options.countryId : undefined,
        forManagement: true,
        includeInactive: options?.includeInactive ?? true,
      },
      ...branchConfig(branchId),
    });

    const rows = Array.isArray(res.data) ? res.data : [];
    return rows.map((row) => ({
      id: Number(row.id ?? 0),
      name: String(row.name ?? ''),
      hexCode: row.hexCode ? String(row.hexCode) : null,
      description: row.description ? String(row.description) : null,
      sortOrder: Number(row.sortOrder ?? 0),
      isActive: Boolean(row.isActive ?? true),
      countryId: row.countryId != null ? Number(row.countryId) : null,
    })).filter((row) => row.id > 0);
  },

  async create(type: MasterType, branchId: number, payload: SaveMasterPayload) {
    const res = await apiClient.post(`/masters/${type}`, payload, branchConfig(branchId));
    masterDataService.clearMasterCache(type, isGlobalType(type) ? undefined : branchId);
    return res;
  },

  async update(type: MasterType, id: number, branchId: number, payload: SaveMasterPayload) {
    const res = await apiClient.put(`/masters/${type}/${id}`, payload, branchConfig(branchId));
    masterDataService.clearMasterCache(type, isGlobalType(type) ? undefined : branchId);
    return res;
  },

  async remove(type: MasterType, id: number, branchId: number, countryId?: number) {
    const res = await apiClient.delete(`/masters/${type}/${id}`, {
      params: {
        branchId: !isGlobalType(type) && branchId > 0 ? branchId : undefined,
        countryId: countryId && countryId > 0 ? countryId : undefined,
      },
      ...branchConfig(branchId),
    });
    masterDataService.clearMasterCache(type, isGlobalType(type) ? undefined : branchId);
    return res;
  },

  getErrorMessage(error: unknown, fallback: string) {
    return getApiErrorMessage(error, fallback);
  },
};
