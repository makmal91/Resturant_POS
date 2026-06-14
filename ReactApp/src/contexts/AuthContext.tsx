import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import { authService } from '../services/authService'
import { useBranchStore } from '../stores/useBranchStore'
import { useTenantStore, type TenantRole } from '../stores/useTenantStore'
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

const mapRole = (roleName?: string): TenantRole => {
  if (roleName === 'SuperAdmin' || roleName === 'Super Admin' || roleName === 'System Admin') {
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
    (nextUser: StoredUser | null, nextToken: string | null, nextBranches: StoredBranch[], nextBranchId: number | null) => {
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
        })
        syncTenantSession(nextUser, nextBranchId)
        syncBranchStore(nextBranches, nextBranchId)
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
      if (!branches.some((branch) => branch.id === branchId)) {
        throw new Error('Selected branch is no longer assigned to your account.')
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
    setBranches(storedBranches)

    if (storedBranchId !== null && !storedBranches.some((branch) => branch.id === storedBranchId)) {
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

      if (response.branches.length === 0) {
        throw new Error('No branch assigned.')
      }

      const autoSelectedBranchId = response.branches.length === 1 ? response.branches[0].id : null
      applySession(response.user, response.token, response.branches, autoSelectedBranchId)

      return autoSelectedBranchId !== null ? '/' : '/select-branch'
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
      applySession(snapshot.user, snapshot.token, snapshot.branches, snapshot.selectedBranchId)
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
