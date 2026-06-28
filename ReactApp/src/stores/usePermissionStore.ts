import { create } from 'zustand'
import { authStorage } from '../utils/storage'
import {
  canBypassPermissionsSession,
  findModulePermission,
  normalizeModulePermission,
  type ModulePermission,
  type PermissionAction,
} from '../types/permissions'
import { FEATURE_MODULE_MAP, parseFeaturesResponse } from '../types/featurePermissions'

interface PermissionState {
  permissions: ModulePermission[]
  features: string[]
  roleName: string | null
  setPermissions: (permissions: ModulePermission[], roleName?: string | null, features?: string[]) => void
  clearPermissions: () => void
  can: (moduleName: string, action: PermissionAction) => boolean
  hasFeature: (featureKey: string) => boolean
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
  features: [],
  roleName: null,

  setPermissions: (permissions, roleName = null, features = []) => {
    set({ permissions, roleName, features: parseFeaturesResponse(features) })
  },

  clearPermissions: () => {
    set({ permissions: [], features: [], roleName: null })
  },

  can: (moduleName, action) => {
    const { permissions, roleName } = get()
    if (canBypassPermissionsSession(roleName, authStorage.getUser())) {
      return true
    }

    const modulePermission = findModulePermission(permissions, moduleName)

    if (!modulePermission) {
      return false
    }

    const key = actionMap[action]
    return Boolean(modulePermission[key])
  },

  hasFeature: (featureKey) => {
    const { permissions, roleName } = get()
    if (canBypassPermissionsSession(roleName, authStorage.getUser())) {
      return true
    }

    if (!featureKey) return false

    const moduleName = FEATURE_MODULE_MAP[featureKey]
    if (!moduleName) return false

    const modulePermission = findModulePermission(permissions, moduleName)
    return Boolean(modulePermission?.canView)
  },
}))

export const parsePermissionsResponse = (value: unknown): ModulePermission[] => {
  if (!Array.isArray(value)) {
    return []
  }

  return value.map((item) => normalizeModulePermission(item as Record<string, unknown>))
}
