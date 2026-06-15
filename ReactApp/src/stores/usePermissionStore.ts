import { create } from 'zustand'
import { authStorage } from '../utils/storage'
import {
  canBypassPermissionsSession,
  normalizeModulePermission,
  type ModulePermission,
  type PermissionAction,
} from '../types/permissions'

interface PermissionState {
  permissions: ModulePermission[]
  roleName: string | null
  setPermissions: (permissions: ModulePermission[], roleName?: string | null) => void
  clearPermissions: () => void
  can: (moduleName: string, action: PermissionAction) => boolean
}

const actionMap: Record<PermissionAction, keyof ModulePermission> = {
  view: 'canView',
  create: 'canCreate',
  edit: 'canEdit',
  delete: 'canDelete',
  export: 'canExport',
  upload: 'canUpload',
}

export const usePermissionStore = create<PermissionState>((set, get) => ({
  permissions: [],
  roleName: null,

  setPermissions: (permissions, roleName = null) => {
    set({ permissions, roleName })
  },

  clearPermissions: () => {
    set({ permissions: [], roleName: null })
  },

  can: (moduleName, action) => {
    const { permissions, roleName } = get()
    if (canBypassPermissionsSession(roleName, authStorage.getUser())) {
      return true
    }

    const modulePermission = permissions.find(
      (permission) => permission.moduleName.toLowerCase() === moduleName.toLowerCase()
    )

    if (!modulePermission) {
      return false
    }

    const key = actionMap[action]
    return Boolean(modulePermission[key])
  },
}))

export const parsePermissionsResponse = (value: unknown): ModulePermission[] => {
  if (!Array.isArray(value)) {
    return []
  }

  return value.map((item) => normalizeModulePermission(item as Record<string, unknown>))
}
