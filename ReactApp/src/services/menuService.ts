import api, { getApiErrorMessage } from './api'

// ---------------------------------------------------------------------------
// Legacy flat menu types (kept for backward compatibility with /api/menus)
// ---------------------------------------------------------------------------

export interface NavigationMenuItem {
  id: number
  name: string
  route: string | null
  icon: string | null
  moduleName: string | null
  parentId: number | null
  displayOrder: number
}

const normalizeMenu = (value: Record<string, unknown>): NavigationMenuItem => ({
  id: Number(value.id ?? value.Id ?? 0),
  name: String(value.name ?? value.Name ?? ''),
  route: value.route != null || value.Route != null ? String(value.route ?? value.Route) : null,
  icon: value.icon != null || value.Icon != null ? String(value.icon ?? value.Icon) : null,
  moduleName:
    value.moduleName != null || value.ModuleName != null
      ? String(value.moduleName ?? value.ModuleName)
      : null,
  parentId:
    value.parentId != null || value.ParentId != null
      ? Number(value.parentId ?? value.ParentId)
      : null,
  displayOrder: Number(value.displayOrder ?? value.DisplayOrder ?? 0),
})

export const menuService = {
  async getMenus(roleId: number): Promise<NavigationMenuItem[]> {
    try {
      const response = await api.get('/menus', { params: { roleId } })
      const data = response.data

      if (!Array.isArray(data)) {
        return []
      }

      return data.map((item) => normalizeMenu(item as Record<string, unknown>))
    } catch (error) {
      throw new Error(getApiErrorMessage(error, 'Failed to load navigation menus.'))
    }
  },
}

// ---------------------------------------------------------------------------
// Tree-based sidebar types (used by new /api/sidebar/modules endpoint)
// ---------------------------------------------------------------------------

export interface SidebarTreeItem {
  id: number
  name: string
  route: string | null
  icon: string | null
  moduleName: string | null
  displayOrder: number
  children: SidebarTreeItem[]
}

export interface BusinessInfo {
  id: number
  name: string
  hasLogo: boolean
}

const normalizeSidebarItem = (value: Record<string, unknown>): SidebarTreeItem => ({
  id: Number(value.id ?? value.Id ?? 0),
  name: String(value.name ?? value.Name ?? ''),
  route: value.route != null || value.Route != null ? String(value.route ?? value.Route) : null,
  icon: value.icon != null || value.Icon != null ? String(value.icon ?? value.Icon) : null,
  moduleName:
    value.moduleName != null || value.ModuleName != null
      ? String(value.moduleName ?? value.ModuleName)
      : null,
  displayOrder: Number(value.displayOrder ?? value.DisplayOrder ?? 0),
  children: Array.isArray(value.children ?? value.Children)
    ? ((value.children ?? value.Children) as unknown[]).map((c) =>
        normalizeSidebarItem(c as Record<string, unknown>)
      )
    : [],
})

export const sidebarService = {
  async getSidebarTree(): Promise<SidebarTreeItem[]> {
    try {
      const response = await api.get('/sidebar/modules')
      const data = response.data

      if (!Array.isArray(data)) {
        return []
      }

      return data.map((item) => normalizeSidebarItem(item as Record<string, unknown>))
    } catch (error) {
      throw new Error(getApiErrorMessage(error, 'Failed to load sidebar navigation.'))
    }
  },

  async getBusinessInfo(): Promise<BusinessInfo> {
    try {
      const response = await api.get('/businesses/my')
      const d = response.data as Record<string, unknown>
      return {
        id: Number(d.id ?? d.Id ?? 0),
        name: String(d.name ?? d.Name ?? ''),
        hasLogo: Boolean(d.hasLogo ?? d.HasLogo ?? false),
      }
    } catch {
      return { id: 0, name: '', hasLogo: false }
    }
  },

  async getBusinessLogoUrl(): Promise<string | null> {
    try {
      const response = await api.get('/businesses/my/logo', { responseType: 'blob' })
      return URL.createObjectURL(response.data as Blob)
    } catch {
      return null
    }
  },
}

// ---------------------------------------------------------------------------
// Legacy helper used by old Sidebar (kept until fully migrated away)
// ---------------------------------------------------------------------------

export interface SidebarMenuGroup {
  id: number
  name: string
  items: Array<{
    id: number
    path: string
    label: string
    icon: string
    moduleName: string | null
  }>
}

export const buildSidebarGroups = (
  menus: NavigationMenuItem[],
  canView: (moduleName: string) => boolean
): SidebarMenuGroup[] => {
  const visibleMenus = menus.filter(
    (menu) => !menu.moduleName || canView(menu.moduleName)
  )

  const groups = visibleMenus
    .filter((menu) => menu.parentId === null)
    .sort((a, b) => a.displayOrder - b.displayOrder)

  return groups
    .map((group) => ({
      id: group.id,
      name: group.name,
      items: visibleMenus
        .filter((menu) => menu.parentId === group.id && menu.route)
        .sort((a, b) => a.displayOrder - b.displayOrder)
        .map((menu) => ({
          id: menu.id,
          path: menu.route!,
          label: menu.name,
          icon: menu.icon ?? '',
          moduleName: menu.moduleName,
        })),
    }))
    .filter((group) => group.items.length > 0)
}
