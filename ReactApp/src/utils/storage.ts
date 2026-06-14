import type { ModulePermission } from '../types/permissions'

export interface StoredUser {
  id: number
  username: string
  fullName?: string
  businessId?: number
  roleId?: number
  roleName?: string
  isMasterUser?: boolean
}

export interface StoredBranch {
  id: number
  name: string
}

export interface AuthStorageSnapshot {
  user: StoredUser | null
  token: string | null
  branches: StoredBranch[]
  selectedBranchId: number | null
  permissions: ModulePermission[]
}

const STORAGE_KEYS = {
  user: 'user',
  token: 'token',
  branches: 'branches',
  selectedBranchId: 'selectedBranchId',
  permissions: 'permissions',
} as const

const parseJson = <T>(value: string | null): T | null => {
  if (!value) {
    return null
  }

  try {
    return JSON.parse(value) as T
  } catch {
    return null
  }
}

export const authStorage = {
  getUser(): StoredUser | null {
    return parseJson<StoredUser>(localStorage.getItem(STORAGE_KEYS.user))
  },

  setUser(user: StoredUser | null): void {
    if (user) {
      localStorage.setItem(STORAGE_KEYS.user, JSON.stringify(user))
    } else {
      localStorage.removeItem(STORAGE_KEYS.user)
    }
  },

  getToken(): string | null {
    return localStorage.getItem(STORAGE_KEYS.token)
  },

  setToken(token: string | null): void {
    if (token) {
      localStorage.setItem(STORAGE_KEYS.token, token)
    } else {
      localStorage.removeItem(STORAGE_KEYS.token)
    }
  },

  getBranches(): StoredBranch[] {
    return parseJson<StoredBranch[]>(localStorage.getItem(STORAGE_KEYS.branches)) ?? []
  },

  setBranches(branches: StoredBranch[]): void {
    localStorage.setItem(STORAGE_KEYS.branches, JSON.stringify(branches))
  },

  getSelectedBranchId(): number | null {
    const raw = localStorage.getItem(STORAGE_KEYS.selectedBranchId)
    if (raw === null || raw === '') {
      return null
    }

    const parsed = Number(raw)
    return Number.isFinite(parsed) && parsed >= 0 ? parsed : null
  },

  setSelectedBranchId(branchId: number | null): void {
    if (branchId !== null && branchId > 0) {
      localStorage.setItem(STORAGE_KEYS.selectedBranchId, String(branchId))
    } else {
      localStorage.removeItem(STORAGE_KEYS.selectedBranchId)
    }
  },

  getPermissions(): ModulePermission[] {
    return parseJson<ModulePermission[]>(localStorage.getItem(STORAGE_KEYS.permissions)) ?? []
  },

  setPermissions(permissions: ModulePermission[]): void {
    localStorage.setItem(STORAGE_KEYS.permissions, JSON.stringify(permissions))
  },

  getSnapshot(): AuthStorageSnapshot {
    return {
      user: this.getUser(),
      token: this.getToken(),
      branches: this.getBranches(),
      selectedBranchId: this.getSelectedBranchId(),
      permissions: this.getPermissions(),
    }
  },

  saveSession(data: {
    user: StoredUser
    token: string
    branches: StoredBranch[]
    selectedBranchId: number | null
    permissions?: ModulePermission[]
  }): void {
    this.setUser(data.user)
    this.setToken(data.token)
    this.setBranches(data.branches)
    this.setSelectedBranchId(data.selectedBranchId)
    if (data.permissions) {
      this.setPermissions(data.permissions)
    }
  },

  clear(): void {
    localStorage.removeItem(STORAGE_KEYS.user)
    localStorage.removeItem(STORAGE_KEYS.token)
    localStorage.removeItem(STORAGE_KEYS.branches)
    localStorage.removeItem(STORAGE_KEYS.selectedBranchId)
    localStorage.removeItem(STORAGE_KEYS.permissions)
    localStorage.removeItem('tenantSession')
    localStorage.removeItem('businessId')
    localStorage.removeItem('branchId')
    localStorage.removeItem('authToken')
  },
}

export const isTokenExpired = (token: string | null): boolean => {
  if (!token) {
    return true
  }

  try {
    const payload = token.split('.')[1]
    if (!payload) {
      return true
    }

    const decoded = JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/'))) as { exp?: number }
    if (!decoded.exp) {
      return false
    }

    return decoded.exp * 1000 <= Date.now()
  } catch {
    return true
  }
}
