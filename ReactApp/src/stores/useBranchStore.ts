import { create } from 'zustand'
import api, { getApiErrorMessage } from '../services/api'
import { dispatchBranchChanged } from '../services/branchContext'
import { authStorage } from '../utils/storage'
import { isGlobalAdminSession } from '../types/permissions'
import { useTenantStore } from './useTenantStore'

export interface BranchOption {
  id: number
  name: string
  code?: string
  isActive?: boolean
}

interface BranchState {
  branches: BranchOption[]
  selectedBranchId: number | null
  isLoading: boolean
  error: string | null
  fetchBranches: () => Promise<void>
  setSelectedBranchId: (branchId: number | null) => void
  getSelectedBranch: () => BranchOption | null
}

export const useBranchStore = create<BranchState>((set, get) => ({
  branches: [],
  selectedBranchId: null,
  isLoading: false,
  error: null,

  fetchBranches: async () => {
    set({ isLoading: true, error: null })

    const authUser = authStorage.getUser()
    const globalAdmin = isGlobalAdminSession(undefined, authUser)
    const authBranches = authStorage.getBranches()

    const applyBranches = (rawBranches: BranchOption[], preferStoredSelection = true) => {
      const activeBranches = rawBranches.filter((branch) => branch.isActive !== false)
      const storedBranchId = authStorage.getSelectedBranchId()
      let nextSelectedBranchId = get().selectedBranchId ?? storedBranchId

      // Single active branch: always auto-select globally (no manual selection).
      if (activeBranches.length === 1) {
        nextSelectedBranchId = activeBranches[0].id
        authStorage.setSelectedBranchId(nextSelectedBranchId)
      } else if (activeBranches.length > 1) {
        if (
          nextSelectedBranchId !== null &&
          nextSelectedBranchId !== 0 &&
          !activeBranches.some((branch) => branch.id === nextSelectedBranchId)
        ) {
          nextSelectedBranchId = globalAdmin ? 0 : activeBranches[0].id
          authStorage.setSelectedBranchId(nextSelectedBranchId)
        } else if (nextSelectedBranchId === null && globalAdmin) {
          nextSelectedBranchId = 0
          authStorage.setSelectedBranchId(nextSelectedBranchId)
        } else if (nextSelectedBranchId === null) {
          nextSelectedBranchId = activeBranches[0].id
          authStorage.setSelectedBranchId(nextSelectedBranchId)
        } else if (nextSelectedBranchId === 0 && !globalAdmin) {
          nextSelectedBranchId = activeBranches[0].id
          authStorage.setSelectedBranchId(nextSelectedBranchId)
        } else if (preferStoredSelection && nextSelectedBranchId === null && globalAdmin) {
          nextSelectedBranchId = 0
          authStorage.setSelectedBranchId(nextSelectedBranchId)
        }
      } else {
        nextSelectedBranchId = null
        authStorage.setSelectedBranchId(null)
      }

      set({ branches: rawBranches, selectedBranchId: nextSelectedBranchId, isLoading: false, error: null })

      if (nextSelectedBranchId !== null) {
        useTenantStore.getState().setBranchId(nextSelectedBranchId)
      }
    }

    if (!globalAdmin && authBranches.length > 0) {
      applyBranches(
        authBranches.map((branch) => ({
          id: branch.id,
          name: branch.name,
          isActive: true,
        }))
      )
      return
    }

    try {
      const response = await api.get('/branches')
      const branches = Array.isArray(response.data)
        ? response.data.map((item: Record<string, unknown>) => ({
            id: Number(item.id ?? item.Id),
            name: String(item.name ?? item.Name ?? ''),
            code: String(item.code ?? item.Code ?? ''),
            isActive: Boolean(item.isActive ?? item.IsActive ?? true),
          }))
        : []

      if (globalAdmin && branches.length > 0) {
        authStorage.setBranches(
          branches.map((branch) => ({
            id: branch.id,
            name: branch.name,
          }))
        )
      }

      applyBranches(branches, false)
    } catch (error) {
      if (authBranches.length > 0) {
        applyBranches(
          authBranches.map((branch) => ({
            id: branch.id,
            name: branch.name,
            isActive: true,
          }))
        )
        return
      }

      set({
        branches: [],
        isLoading: false,
        error: getApiErrorMessage(error, 'Failed to load branches.'),
      })
    }
  },

  setSelectedBranchId: (branchId) => {
    const previous = get().selectedBranchId
    set({ selectedBranchId: branchId })
    authStorage.setSelectedBranchId(branchId)

    if (branchId !== null && branchId >= 0) {
      useTenantStore.getState().setBranchId(branchId)
    }

    if (previous !== branchId) {
      dispatchBranchChanged(branchId)
    }
  },

  getSelectedBranch: () => {
    const { branches, selectedBranchId } = get()
    if (selectedBranchId === null || selectedBranchId === 0) {
      return null
    }

    return branches.find((branch) => branch.id === selectedBranchId) ?? null
  },
}))
