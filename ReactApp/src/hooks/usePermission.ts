import { useMemo } from 'react'
import { usePermissionStore } from '../stores/usePermissionStore'
import { authStorage } from '../utils/storage'
import { isGlobalAdminSession, isMasterUserSession, isSuperAdminRole } from '../types/permissions'
import type { PermissionAction } from '../types/permissions'

export const usePermission = (moduleName: string) => {
  const can = usePermissionStore((state) => state.can)

  return useMemo(
    () => ({
      canView: can(moduleName, 'view'),
      canCreate: can(moduleName, 'create'),
      canEdit: can(moduleName, 'edit'),
      canDelete: can(moduleName, 'delete'),
      canExport: can(moduleName, 'export'),
      canUpload: can(moduleName, 'upload'),
      can: (action: PermissionAction) => can(moduleName, action),
    }),
    [can, moduleName]
  )
}

export const useHasPermission = (moduleName: string, action: PermissionAction): boolean => {
  const can = usePermissionStore((state) => state.can)
  return can(moduleName, action)
}

export const useIsMasterUser = (): boolean => {
  const roleName = usePermissionStore((state) => state.roleName)
  return isMasterUserSession(roleName, authStorage.getUser())
}

export const useIsGlobalAdmin = (): boolean => {
  const roleName = usePermissionStore((state) => state.roleName)
  return isGlobalAdminSession(roleName, authStorage.getUser())
}

export const useIsSuperAdmin = (): boolean => {
  const roleName = usePermissionStore((state) => state.roleName)
  const user = authStorage.getUser()
  return isSuperAdminRole(roleName) || isSuperAdminRole(user?.roleName)
}
