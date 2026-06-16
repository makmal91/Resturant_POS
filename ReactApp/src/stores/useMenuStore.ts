import { create } from 'zustand'
import { menuService, sidebarService, type NavigationMenuItem, type SidebarTreeItem } from '../services/menuService'

interface MenuState {
  // Legacy flat menus (kept for backward compat)
  menus: NavigationMenuItem[]

  // ERP sidebar tree
  sidebarTree: SidebarTreeItem[]

  isLoading: boolean
  error: string | null

  // The roleId last fetched — used to skip redundant re-fetches
  _lastFetchedRoleId: number | null

  fetchMenus: (roleId: number) => Promise<void>
  fetchSidebarData: (roleId: number) => Promise<void>
  refreshSidebarData: (roleId: number) => Promise<void>
  clearMenus: () => void
}

export const useMenuStore = create<MenuState>((set, get) => ({
  menus: [],
  sidebarTree: [],
  isLoading: false,
  error: null,
  _lastFetchedRoleId: null,

  fetchMenus: async (roleId: number) => {
    set({ isLoading: true, error: null })

    try {
      const menus = await menuService.getMenus(roleId)
      set({ menus, isLoading: false, error: null })
    } catch (error) {
      set({
        menus: [],
        isLoading: false,
        error: error instanceof Error ? error.message : 'Failed to load navigation menus.',
      })
    }
  },

  fetchSidebarData: async (roleId: number) => {
    if (get()._lastFetchedRoleId === roleId && get().sidebarTree.length > 0) {
      return
    }

    set({ isLoading: true, error: null })

    try {
      const sidebarTree = await sidebarService.getSidebarTree()
      set({ sidebarTree, isLoading: false, error: null, _lastFetchedRoleId: roleId })
    } catch (error) {
      set({
        sidebarTree: [],
        isLoading: false,
        error: error instanceof Error ? error.message : 'Failed to load sidebar navigation.',
        _lastFetchedRoleId: null,
      })
    }
  },

  refreshSidebarData: async (roleId: number) => {
    set({ _lastFetchedRoleId: null })
    await get().fetchSidebarData(roleId)
  },

  clearMenus: () => {
    set({ menus: [], sidebarTree: [], isLoading: false, error: null, _lastFetchedRoleId: null })
  },
}))
