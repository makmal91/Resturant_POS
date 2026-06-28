import { usePermissionStore } from '../stores/usePermissionStore'
import { authStorage } from './storage'
import {
  findModulePermission,
  isMasterUserSession,
  type ModulePermission,
  type PermissionAction,
} from '../types/permissions'

/** Check whether the current session has a module permission. System Admin always passes. */
export const hasPermission = (moduleName: string, action: PermissionAction): boolean =>
  usePermissionStore.getState().can(moduleName, action)

export const PERMISSION_DENIED_MESSAGES: Record<PermissionAction, string> = {
  view: 'You do not have permission to view this page.',
  create: 'You do not have permission to create records in this module.',
  edit: 'You do not have permission to edit records in this module.',
  delete: 'You do not have permission to delete records in this module.',
  export: 'You do not have permission to export data from this module.',
  upload: 'You do not have permission to upload files in this module.',
}

export const getPermissionDeniedMessage = (
  action: PermissionAction,
  moduleName?: string,
): string => {
  const base = PERMISSION_DENIED_MESSAGES[action]
  return moduleName ? `${base} (${moduleName})` : base
}

/** Check whether the current session has a product feature flag enabled. */
export const hasFeaturePermission = (featureKey: string): boolean =>
  usePermissionStore.getState().hasFeature(featureKey)

/** Whether the actor can assign permissions for a module (requires view access). */
export const canAssignModulePermission = (
  permissions: ModulePermission[],
  moduleKey: string,
  moduleName: string,
  roleName?: string | null,
): boolean => {
  if (isMasterUserSession(roleName, authStorage.getUser())) {
    return true
  }

  const lookupKey = moduleKey || moduleName
  if (!lookupKey) {
    return false
  }

  return Boolean(findModulePermission(permissions, lookupKey)?.canView)
}

/** Filter role-permission rows to modules the actor is allowed to assign. */
export const filterAssignablePermissions = <T extends { moduleKey: string; moduleName: string }>(
  items: T[],
  permissions: ModulePermission[],
  roleName?: string | null,
): T[] =>
  items.filter((item) =>
    canAssignModulePermission(permissions, item.moduleKey, item.moduleName, roleName),
  )
