export interface ModulePermission {
  moduleName: string
  canView: boolean
  canCreate: boolean
  canEdit: boolean
  canDelete: boolean
  canExport: boolean
  canUpload: boolean
}

export type PermissionAction =
  | 'view'
  | 'create'
  | 'edit'
  | 'delete'
  | 'export'
  | 'upload'

export const MASTER_USER_ROLE = 'System Admin'
export const SUPER_ADMIN_ROLE = 'Super Admin'

export const PROTECTED_ROLES = [MASTER_USER_ROLE, SUPER_ADMIN_ROLE] as const

export const isMasterUserRole = (roleName?: string | null): boolean =>
  roleName === MASTER_USER_ROLE

export const isSuperAdminRole = (roleName?: string | null): boolean =>
  roleName === SUPER_ADMIN_ROLE || roleName === 'SuperAdmin'

export const isProtectedRole = (roleName?: string | null): boolean =>
  isMasterUserRole(roleName) || isSuperAdminRole(roleName)

export const isGlobalAdminRole = (roleName?: string | null): boolean =>
  isMasterUserRole(roleName) || isSuperAdminRole(roleName)

export const isMasterUserSession = (
  storedRoleName?: string | null,
  user?: { roleName?: string; isMasterUser?: boolean } | null
): boolean =>
  isMasterUserRole(storedRoleName) ||
  isMasterUserRole(user?.roleName) ||
  Boolean(user?.isMasterUser)

export const isGlobalAdminSession = (
  storedRoleName?: string | null,
  user?: { roleName?: string; isMasterUser?: boolean; isGlobalAdmin?: boolean } | null
): boolean =>
  isGlobalAdminRole(storedRoleName) ||
  isGlobalAdminRole(user?.roleName) ||
  Boolean(user?.isGlobalAdmin) ||
  isMasterUserSession(storedRoleName, user)

export const canBypassPermissionsSession = (
  storedRoleName?: string | null,
  user?: { roleName?: string; isMasterUser?: boolean; isGlobalAdmin?: boolean } | null
): boolean =>
  isMasterUserSession(storedRoleName, user)

export const hasBranchContext = (branchId: number | null | undefined): boolean =>
  branchId !== null && branchId !== undefined

export const isBranchSelectionReady = (branchId: number | null | undefined): boolean =>
  hasBranchContext(branchId)

export const isGlobalBranchView = (branchId: number | null | undefined): boolean =>
  branchId === 0

/** @deprecated Use isMasterUserRole for System Admin only checks */
export const GLOBAL_BYPASS_ROLES = [MASTER_USER_ROLE, SUPER_ADMIN_ROLE]

import { modulePermissionMatches } from '../utils/modulePermissionResolver'

export { modulePermissionMatches, normalizeModuleName } from '../utils/modulePermissionResolver'

export const normalizeModulePermission = (value: Record<string, unknown>): ModulePermission => ({
  moduleName: String(value.moduleName ?? value.ModuleName ?? ''),
  canView: Boolean(value.canView ?? value.CanView),
  canCreate: Boolean(value.canCreate ?? value.CanCreate),
  canEdit: Boolean(value.canEdit ?? value.CanEdit),
  canDelete: Boolean(value.canDelete ?? value.CanDelete),
  canExport: Boolean(value.canExport ?? value.CanExport),
  canUpload: Boolean(value.canUpload ?? value.CanUpload),
})

export const findModulePermission = (
  permissions: ModulePermission[],
  moduleName: string,
): ModulePermission | undefined =>
  permissions.find((permission) => modulePermissionMatches(permission.moduleName, moduleName))
