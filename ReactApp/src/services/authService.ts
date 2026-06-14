import api, { getApiErrorMessage } from './api'
import type { StoredBranch, StoredUser } from '../utils/storage'
import { parsePermissionsResponse } from '../stores/usePermissionStore'
import type { ModulePermission } from '../types/permissions'

export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResponse {
  token: string
  user: StoredUser
  branches: StoredBranch[]
  permissions: ModulePermission[]
}

const normalizeUser = (value: Record<string, unknown>): StoredUser => ({
  id: Number(value.id ?? value.Id),
  username: String(value.username ?? value.Username ?? ''),
  fullName: String(value.fullName ?? value.FullName ?? ''),
  businessId: Number(value.businessId ?? value.BusinessId ?? 0),
  roleId: Number(value.roleId ?? value.RoleId ?? 0),
  roleName: String(value.roleName ?? value.RoleName ?? ''),
  isMasterUser: Boolean(
    value.isMasterUser ??
      value.IsMasterUser ??
      String(value.roleName ?? value.RoleName ?? '') === 'System Admin'
  ),
})

const normalizeBranch = (value: Record<string, unknown>): StoredBranch => ({
  id: Number(value.id ?? value.Id ?? value.branchId ?? value.BranchId),
  name: String(value.name ?? value.Name ?? value.branchName ?? value.BranchName ?? ''),
})

export const authService = {
  async login(request: LoginRequest): Promise<LoginResponse> {
    try {
      const response = await api.post('/auth/login', request)
      const data = response.data as Record<string, unknown>
      const userRaw = (data.user ?? data.User) as Record<string, unknown> | undefined
      const branchesRaw = (data.branches ?? data.Branches) as Record<string, unknown>[] | undefined
      const permissionsRaw = data.permissions ?? data.Permissions

      return {
        token: String(data.token ?? data.Token ?? ''),
        user: normalizeUser(userRaw ?? {}),
        branches: Array.isArray(branchesRaw) ? branchesRaw.map(normalizeBranch) : [],
        permissions: parsePermissionsResponse(permissionsRaw),
      }
    } catch (error) {
      throw new Error(getApiErrorMessage(error, 'Login failed. Please try again.'))
    }
  },

  async getPermissions(): Promise<ModulePermission[]> {
    try {
      const response = await api.get('/auth/permissions')
      return parsePermissionsResponse(response.data)
    } catch (error) {
      throw new Error(getApiErrorMessage(error, 'Failed to load permissions.'))
    }
  },
}
