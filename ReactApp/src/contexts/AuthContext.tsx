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
  if (branches.length === 1) {
    return branches[0].id
  }

  if (isGlobalAdminUser(user) && branches.length > 1) {
    return 0
  }

  if (branches.length > 1) {
    return branches[0].id
  }

  if (branches.length === 0 && isGlobalRole(user.roleName)) {
    return 0
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

const syncPermissionStore = (permissions: ModulePermission[], roleName?: string) => {
  usePermissionStore.getState().setPermissions(permissions, roleName ?? null)
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
      nextPermissions: ModulePermission[] = authStorage.getPermissions()
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
        })
        syncTenantSession(nextUser, nextBranchId)
        syncBranchStore(nextBranches, nextBranchId)
        syncPermissionStore(nextPermissions, nextUser.roleName)
        if (nextUser.roleId) {
          void useMenuStore.getState().fetchSidebarData(nextUser.roleId)
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
      const globalAdmin = isGlobalAdminUser(user)
      if (!globalAdmin && branchId > 0 && !branches.some((branch) => branch.id === branchId)) {
        throw new Error('Selected branch is no longer assigned to your account.')
      }

      if (branchId === 0 && !globalAdmin) {
        throw new Error('All branches view is only available to global admins.')
      }

      setSelectedBranchIdState(branchId)
      authStorage.setSelectedBranchId(branchId)
      useBranchStore.getState().setSelectedBranchId(branchId)
      syncTenantSession(user, branchId)
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
        response.permissions
      )

      if (isGlobalAdminUser(response.user) || autoSelectedBranchId !== null) {
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
      let branchId =
        snapshot.selectedBranchId ??
        (snapshot.branches.length === 1
          ? snapshot.branches[0].id
          : isGlobalAdminUser(snapshot.user) && snapshot.branches.length > 1
            ? 0
            : snapshot.branches.length > 0
              ? snapshot.branches[0].id
              : isGlobalAdminUser(snapshot.user)
                ? 0
                : null)

      if (snapshot.branches.length === 1 && (branchId === null || branchId === 0)) {
        branchId = snapshot.branches[0].id
      }

      applySession(
        snapshot.user,
        snapshot.token,
        snapshot.branches,
        branchId,
        snapshot.permissions
      )
    }

    setIsHydrated(true)
  }, [applySession])

  useEffect(() => {
    const handleForcedLogout = () => logout()
    window.addEventListener('auth:logout', handleForcedLogout)
    return () => window.removeEventListener('auth:logout', handleForcedLogout)
  }, [logout])

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
