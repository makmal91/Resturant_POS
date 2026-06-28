import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import { authService } from '../services/authService'
import { useBranchStore } from '../stores/useBranchStore'
import { usePermissionStore } from '../stores/usePermissionStore'
import { useMenuStore } from '../stores/useMenuStore'
import { useTenantStore, type TenantRole } from '../stores/useTenantStore'
import type { ModulePermission } from '../types/permissions'
import { isGlobalAdminSession, isMasterUserRole } from '../types/permissions'
import {
  authStorage,
  isTokenExpired,
  type StoredBranch,
  type StoredUser,
} from '../utils/storage'

interface AuthContextValue {
  user: StoredUser | null
  token: string | null
  branches: StoredBranch[]
  selectedBranchId: number | null
  isAuthenticated: boolean
  isHydrated: boolean
  login: (username: string, password: string) => Promise<'/' | '/select-branch'>
  logout: () => void
  setBranch: (branchId: number) => void
  refreshBranches: () => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

const isGlobalRole = (roleName?: string) => isGlobalAdminSession(roleName)

const isGlobalAdminUser = (user?: StoredUser | null) =>
  isGlobalAdminSession(user?.roleName, user)

const resolveInitialBranchId = (
  user: StoredUser,
  branches: StoredBranch[]
): number | null => {
  if (isGlobalAdminUser(user)) {
    if (branches.length === 1) {
      return branches[0].id
    }

    return 0
  }

  if (branches.length === 1) {
    return branches[0].id
  }

  if (branches.length > 1) {
    return branches[0].id
  }

  return null
}

const isValidBranchSelection = (
  branchId: number | null,
  branches: StoredBranch[],
  globalAdmin: boolean
): boolean => {
  if (branchId === null) {
    return false
  }

  if (branchId === 0) {
    return globalAdmin
  }

  return branches.some((branch) => branch.id === branchId)
}

const syncPermissionStore = (permissions: ModulePermission[], roleName?: string, features?: string[]) => {
  usePermissionStore.getState().setPermissions(permissions, roleName ?? null, features ?? [])
}

const mapRole = (roleName?: string): TenantRole => {
  if (isMasterUserRole(roleName) || roleName === 'SuperAdmin' || roleName === 'Super Admin') {
    return 'SuperAdmin'
  }

  if (roleName === 'Admin' || roleName === 'BusinessAdmin') {
    return 'BusinessAdmin'
  }

  return 'BranchUser'
}

const syncTenantSession = (user: StoredUser | null, selectedBranchId: number | null) => {
  if (!user) {
    return
  }

  const tenantStore = useTenantStore.getState()
  tenantStore.setBusinessId(user.businessId && user.businessId > 0 ? user.businessId : 1)
  tenantStore.setRole(mapRole(user.roleName))

  if (selectedBranchId !== null) {
    tenantStore.setBranchId(selectedBranchId)
  }
}

const syncBranchStore = (branches: StoredBranch[], selectedBranchId: number | null) => {
  useBranchStore.setState({
    branches: branches.map((branch) => ({
      id: branch.id,
      name: branch.name,
      isActive: true,
    })),
    selectedBranchId,
    isLoading: false,
    error: null,
  })
}

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<StoredUser | null>(null)
  const [token, setToken] = useState<string | null>(null)
  const [branches, setBranches] = useState<StoredBranch[]>([])
  const [selectedBranchId, setSelectedBranchIdState] = useState<number | null>(null)
  const [isHydrated, setIsHydrated] = useState(false)

  const applySession = useCallback(
    (
      nextUser: StoredUser | null,
      nextToken: string | null,
      nextBranches: StoredBranch[],
      nextBranchId: number | null,
      nextPermissions: ModulePermission[] = authStorage.getPermissions(),
      nextFeatures: string[] = authStorage.getFeatures(),
    ) => {
      setUser(nextUser)
      setToken(nextToken)
      setBranches(nextBranches)
      setSelectedBranchIdState(nextBranchId)

      if (nextUser && nextToken) {
        authStorage.saveSession({
          user: nextUser,
          token: nextToken,
          branches: nextBranches,
          selectedBranchId: nextBranchId,
          permissions: nextPermissions,
          features: nextFeatures,
        })
        syncTenantSession(nextUser, nextBranchId)
        syncBranchStore(nextBranches, nextBranchId)
        syncPermissionStore(nextPermissions, nextUser.roleName, nextFeatures)
        if (nextUser.roleId) {
          void useMenuStore.getState().refreshSidebarData(nextUser.roleId)
        }
      }
    },
    []
  )

  const logout = useCallback(() => {
    authStorage.clear()
    setUser(null)
    setToken(null)
    setBranches([])
    setSelectedBranchIdState(null)
    usePermissionStore.getState().clearPermissions()
    useMenuStore.getState().clearMenus()
    useBranchStore.setState({
      branches: [],
      selectedBranchId: null,
      isLoading: false,
      error: null,
    })
    useTenantStore.setState({
      session: {
        businessId: 1,
        branchId: 0,
        role: 'BranchUser',
      },
    })
  }, [])

  const setBranch = useCallback(
    (branchId: number) => {
      const currentUser = user ?? authStorage.getUser()
      const currentBranches = branches.length > 0 ? branches : authStorage.getBranches()
      const globalAdmin = isGlobalAdminUser(currentUser)

      if (!globalAdmin && branchId > 0 && !currentBranches.some((branch) => branch.id === branchId)) {
        throw new Error('Selected branch is no longer assigned to your account.')
      }

      if (branchId === 0 && !globalAdmin) {
        throw new Error('All branches view is only available to global admins.')
      }

      setSelectedBranchIdState(branchId)
      authStorage.setSelectedBranchId(branchId)
      useBranchStore.getState().setSelectedBranchId(branchId)
      syncTenantSession(currentUser, branchId)
    },
    [branches, user]
  )

  const refreshBranches = useCallback(() => {
    const storedBranches = authStorage.getBranches()
    const storedBranchId = authStorage.getSelectedBranchId()
    const globalAdmin = isGlobalAdminUser(authStorage.getUser())
    setBranches(storedBranches)

    if (
      storedBranchId !== null &&
      !isValidBranchSelection(storedBranchId, storedBranches, globalAdmin)
    ) {
      setSelectedBranchIdState(null)
      authStorage.setSelectedBranchId(null)
      useBranchStore.getState().setSelectedBranchId(null)
      return
    }

    syncBranchStore(storedBranches, storedBranchId)
  }, [])

  const login = useCallback(
    async (username: string, password: string): Promise<'/' | '/select-branch'> => {
      const response = await authService.login({ username, password })

      if (response.branches.length === 0 && !isGlobalRole(response.user.roleName)) {
        throw new Error('No branch assigned.')
      }

      const autoSelectedBranchId = resolveInitialBranchId(response.user, response.branches)

      applySession(
        response.user,
        response.token,
        response.branches,
        autoSelectedBranchId,
        response.permissions,
        response.features,
      )

      if (autoSelectedBranchId !== null) {
        return '/'
      }

      return '/select-branch'
    },
    [applySession]
  )

  useEffect(() => {
    const snapshot = authStorage.getSnapshot()

    if (snapshot.token && isTokenExpired(snapshot.token)) {
      authStorage.clear()
      setIsHydrated(true)
      return
    }

    if (snapshot.token && snapshot.user) {
      const branchId =
        snapshot.selectedBranchId ??
        resolveInitialBranchId(snapshot.user, snapshot.branches)

      applySession(
        snapshot.user,
        snapshot.token,
        snapshot.branches,
        branchId,
        snapshot.permissions,
        snapshot.features,
      )
    }

    setIsHydrated(true)
  }, [applySession])

  useEffect(() => {
    const refreshSessionFromServer = async () => {
      const snapshot = authStorage.getSnapshot()
      if (!snapshot.token || !snapshot.user || isTokenExpired(snapshot.token)) {
        return
      }

      try {
        const fresh = await authService.getPermissions()
        authStorage.saveSession({
          user: snapshot.user,
          token: snapshot.token,
          branches: snapshot.branches,
          selectedBranchId: snapshot.selectedBranchId,
          permissions: fresh.permissions,
          features: fresh.features,
        })
        syncPermissionStore(fresh.permissions, snapshot.user.roleName, fresh.features)
        if (snapshot.user.roleId) {
          await useMenuStore.getState().refreshSidebarData(snapshot.user.roleId)
        }
      } catch {
        // Keep cached session when refresh fails (offline / API down).
      }
    }

    if (isHydrated && token && user) {
      void refreshSessionFromServer()
    }
  }, [isHydrated, token, user])

  useEffect(() => {
    const handleForcedLogout = () => logout()
    window.addEventListener('auth:logout', handleForcedLogout)
    return () => window.removeEventListener('auth:logout', handleForcedLogout)
  }, [logout])

  useEffect(() => {
    const refreshPermissionsFromServer = async () => {
      const snapshot = authStorage.getSnapshot()
      if (!snapshot.token || !snapshot.user || isTokenExpired(snapshot.token)) {
        return
      }

      try {
        const fresh = await authService.getPermissions()
        authStorage.saveSession({
          user: snapshot.user,
          token: snapshot.token,
          branches: snapshot.branches,
          selectedBranchId: snapshot.selectedBranchId,
          permissions: fresh.permissions,
          features: fresh.features,
        })
        syncPermissionStore(fresh.permissions, snapshot.user.roleName, fresh.features)
        if (snapshot.user.roleId) {
          await useMenuStore.getState().refreshSidebarData(snapshot.user.roleId)
        }
      } catch {
        // Ignore background refresh failures; session remains valid until next login.
      }
    }

    const handleVisibilityChange = () => {
      if (document.visibilityState === 'visible') {
        void refreshPermissionsFromServer()
      }
    }

    document.addEventListener('visibilitychange', handleVisibilityChange)
    return () => document.removeEventListener('visibilitychange', handleVisibilityChange)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      token,
      branches,
      selectedBranchId,
      isAuthenticated: Boolean(token && user && !isTokenExpired(token)),
      isHydrated,
      login,
      logout,
      setBranch,
      refreshBranches,
    }),
    [user, token, branches, selectedBranchId, isHydrated, login, logout, setBranch, refreshBranches]
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export const useAuth = (): AuthContextValue => {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider')
  }

  return context
}
