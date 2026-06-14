import { create } from 'zustand'
import { menuService, type NavigationMenuItem } from '../services/menuService'

interface MenuState {
  menus: NavigationMenuItem[]
  isLoading: boolean
  error: string | null
  fetchMenus: (roleId: number) => Promise<void>
  clearMenus: () => void
}

export const useMenuStore = create<MenuState>((set) => ({
  menus: [],
  isLoading: false,
  error: null,

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

  clearMenus: () => {
    set({ menus: [], isLoading: false, error: null })
  },
}))
