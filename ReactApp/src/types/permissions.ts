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

export const isMasterUserRole = (roleName?: string | null): boolean =>
  roleName === MASTER_USER_ROLE

/** @deprecated Use isMasterUserRole for permission bypass; kept for backward compatibility */
export const GLOBAL_BYPASS_ROLES = [MASTER_USER_ROLE]

export const normalizeModulePermission = (value: Record<string, unknown>): ModulePermission => ({
  moduleName: String(value.moduleName ?? value.ModuleName ?? ''),
  canView: Boolean(value.canView ?? value.CanView),
  canCreate: Boolean(value.canCreate ?? value.CanCreate),
  canEdit: Boolean(value.canEdit ?? value.CanEdit),
  canDelete: Boolean(value.canDelete ?? value.CanDelete),
  canExport: Boolean(value.canExport ?? value.CanExport),
  canUpload: Boolean(value.canUpload ?? value.CanUpload),
})
