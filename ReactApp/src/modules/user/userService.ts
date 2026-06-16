import apiClient, { getApiErrorMessage } from '../../services/api';

export interface UserBranchAssignment {
  branchId: number;
  branchName: string;
}

export interface UserListItem {
  id: number;
  fullName: string;
  username: string;
  email: string;
  phone: string;
  roleId: number;
  roleName: string;
  isActive: boolean;
  branches: UserBranchAssignment[];
  assignedBranchesDisplay: string;
  primaryBranchId: number;
  primaryBranchName: string;
}

export interface UserFormPayload {
  fullName: string;
  username: string;
  email: string;
  phone: string;
  password?: string;
  roleId: number;
  isActive: boolean;
  branchIds: number[];
}

export interface RoleListItem {
  id: number;
  name: string;
  description: string;
  isActive: boolean;
}

interface PagedUsersResponse {
  data: UserListItem[];
  totalRecords: number;
  totalPages: number;
  currentPage: number;
}

const normalizeUser = (row: Record<string, unknown>): UserListItem => ({
  id: Number(row.id ?? row.Id ?? 0),
  fullName: String(row.fullName ?? row.FullName ?? ''),
  username: String(row.username ?? row.Username ?? ''),
  email: String(row.email ?? row.Email ?? ''),
  phone: String(row.phone ?? row.Phone ?? ''),
  roleId: Number(row.roleId ?? row.RoleId ?? 0),
  roleName: String(row.roleName ?? row.RoleName ?? ''),
  isActive: Boolean(row.isActive ?? row.IsActive ?? true),
  branches: Array.isArray(row.branches ?? row.Branches)
    ? ((row.branches ?? row.Branches) as Record<string, unknown>[]).map((branch) => ({
        branchId: Number(branch.branchId ?? branch.BranchId ?? 0),
        branchName: String(branch.branchName ?? branch.BranchName ?? ''),
      }))
    : [],
  assignedBranchesDisplay: String(row.assignedBranchesDisplay ?? row.AssignedBranchesDisplay ?? ''),
  primaryBranchId: Number(row.primaryBranchId ?? row.PrimaryBranchId ?? 0),
  primaryBranchName: String(row.primaryBranchName ?? row.PrimaryBranchName ?? ''),
});

export const userService = {
  async getAll(params: {
    branchId: number;
    page?: number;
    pageSize?: number;
    search?: string;
    sortBy?: string;
    sortDirection?: 'asc' | 'desc';
  }): Promise<PagedUsersResponse> {
    const response = await apiClient.get('/users', {
      params: {
        branchId: params.branchId,
        page: params.page ?? 1,
        pageSize: params.pageSize ?? 25,
        search: params.search?.trim() || undefined,
        sortBy: params.sortBy || undefined,
        sortDirection: params.sortDirection || undefined,
      },
      headers: { 'X-Branch-Id': String(params.branchId) },
    });

    const payload = response.data as Record<string, unknown>;
    const rows = Array.isArray(payload.data) ? payload.data : [];

    return {
      data: rows.map((row) => normalizeUser(row as Record<string, unknown>)),
      totalRecords: Number(payload.totalRecords ?? rows.length),
      totalPages: Number(payload.totalPages ?? 1),
      currentPage: Number(payload.currentPage ?? 1),
    };
  },

  async getById(id: number, branchId: number): Promise<UserListItem> {
    const response = await apiClient.get(`/users/${id}`, {
      params: { branchId },
      headers: { 'X-Branch-Id': String(branchId) },
    });
    return normalizeUser(response.data as Record<string, unknown>);
  },

  async create(payload: UserFormPayload, branchId: number): Promise<UserListItem> {
    const response = await apiClient.post('/users', payload, {
      headers: { 'X-Branch-Id': String(branchId) },
    });
    return normalizeUser(response.data as Record<string, unknown>);
  },

  async update(id: number, payload: UserFormPayload, branchId: number): Promise<UserListItem> {
    const response = await apiClient.put(`/users/${id}`, payload, {
      params: { branchId },
      headers: { 'X-Branch-Id': String(branchId) },
    });
    return normalizeUser(response.data as Record<string, unknown>);
  },

  async delete(id: number, branchId: number): Promise<void> {
    await apiClient.delete(`/users/${id}`, {
      params: { branchId },
      headers: { 'X-Branch-Id': String(branchId) },
    });
  },

  async getRoles(): Promise<RoleListItem[]> {
    const response = await apiClient.get('/roles');
    const rows = Array.isArray(response.data) ? response.data : [];
    return rows.map((row: Record<string, unknown>) => ({
      id: Number(row.id ?? row.Id ?? 0),
      name: String(row.name ?? row.Name ?? ''),
      description: String(row.description ?? row.Description ?? ''),
      isActive: Boolean(row.isActive ?? row.IsActive ?? true),
    }));
  },
};

export { getApiErrorMessage };
