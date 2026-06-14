import { create } from 'zustand'
import api, { getApiErrorMessage } from '../services/api'
import { authStorage } from '../utils/storage'
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

    const authBranches = authStorage.getBranches()
    if (authBranches.length > 0) {
      const branches = authBranches.map((branch) => ({
        id: branch.id,
        name: branch.name,
        isActive: true,
      }))

      const storedBranchId = authStorage.getSelectedBranchId()
      let nextSelectedBranchId = get().selectedBranchId ?? storedBranchId

      if (nextSelectedBranchId !== null && !branches.some((branch) => branch.id === nextSelectedBranchId)) {
        nextSelectedBranchId = branches.length === 1 ? branches[0].id : null
        authStorage.setSelectedBranchId(nextSelectedBranchId)
      } else if (nextSelectedBranchId === null && branches.length === 1) {
        nextSelectedBranchId = branches[0].id
        authStorage.setSelectedBranchId(nextSelectedBranchId)
      }

      set({ branches, selectedBranchId: nextSelectedBranchId, isLoading: false, error: null })

      if (nextSelectedBranchId !== null) {
        useTenantStore.getState().setBranchId(nextSelectedBranchId)
      }

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

      const { selectedBranchId } = get()
      const storedBranchId = authStorage.getSelectedBranchId() ?? useTenantStore.getState().session.branchId
      let nextSelectedBranchId = selectedBranchId ?? storedBranchId

      if (nextSelectedBranchId === null || nextSelectedBranchId <= 0) {
        if (branches.length === 1) {
          nextSelectedBranchId = branches[0].id
        }
      } else if (!branches.some((branch) => branch.id === nextSelectedBranchId)) {
        nextSelectedBranchId = branches.length === 1 ? branches[0].id : null
      }

      set({ branches, selectedBranchId: nextSelectedBranchId, isLoading: false, error: null })

      if (nextSelectedBranchId !== null && nextSelectedBranchId > 0) {
        authStorage.setSelectedBranchId(nextSelectedBranchId)
        useTenantStore.getState().setBranchId(nextSelectedBranchId)
      }
    } catch (error) {
      set({
        branches: [],
        isLoading: false,
        error: getApiErrorMessage(error, 'Failed to load branches.'),
      })
    }
  },

  setSelectedBranchId: (branchId) => {
    set({ selectedBranchId: branchId })
    authStorage.setSelectedBranchId(branchId)

    if (branchId !== null && branchId > 0) {
      useTenantStore.getState().setBranchId(branchId)
    }
  },

  getSelectedBranch: () => {
    const { branches, selectedBranchId } = get()
    if (selectedBranchId === null) {
      return null
    }

    return branches.find((branch) => branch.id === selectedBranchId) ?? null
  },
}))
