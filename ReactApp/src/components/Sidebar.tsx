import React, {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react'
import { Link, useLocation } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'
import { useMenuStore } from '../stores/useMenuStore'
import { sidebarService, type SidebarTreeItem } from '../services/menuService'

// ---------------------------------------------------------------------------
// SVG Icon system — clean outline icons, no colors
// ---------------------------------------------------------------------------

const ICON_PATHS: Record<string, string> = {
  dashboard:      'M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6',
  pos:            'M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z',
  products:       'M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4',
  categories:     'M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z',
  subcategories:  'M4 5a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1H5a1 1 0 01-1-1V5zm10 0a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1h-4a1 1 0 01-1-1V5zM4 15a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1H5a1 1 0 01-1-1v-4zm10 0a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1h-4a1 1 0 01-1-1v-4z',
  brands:         'M5 3v4M3 5h4M6 17v4m-2-2h4m5-16l2.286 6.857L21 12l-5.714 2.143L13 21l-2.286-6.857L5 12l5.714-2.143L13 3z',
  units:          'M7 7h10M7 12h10M7 17h4m4-14v18',
  users:          'M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z',
  customers:      'M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z',
  roles:          'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z',
  businesses:     'M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4',
  branches:       'M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0zM15 11a3 3 0 11-6 0 3 3 0 016 0z',
  warehouses:     'M8 14v3m4-3v3m4-3v3M3 21h18M3 10h18M3 7l9-4 9 4M4 10h16v11H4V10z',
  suppliers:      'M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4',
  purchase:       'M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 11-4 0 2 2 0 014 0z',
  sales:          'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01',
  invoices:       'M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z',
  inventory:      'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4',
  stock:          'M16 8v8m-4-5v5m-4-2v2m-2 4h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z',
  reports:        'M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z',
  orders:         'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 4h.01M9 12h.01M9 16h.01M13 8h2m-2 4h2m-2 4h2',
  menu:           'M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4',
  expenses:       'M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z',
  cashflow:       'M7 12l3-3 3 3 4-4M8 21l4-4 4 4M3 4h18M4 4h16v12a1 1 0 01-1 1H5a1 1 0 01-1-1V4z',
  default:        'M4 6h16M4 12h16M4 18h7',
}

/** Resolve an icon key from the DB icon string or module name */
const resolveIconKey = (icon: string | null | undefined, name: string): string => {
  // Try icon value first, then fall back to module/item name
  const candidates = [icon, name].filter(Boolean) as string[]

  for (const raw of candidates) {
    const key = raw.toLowerCase().replace(/[\s_-]+/g, '')
    const map: Record<string, string> = {
      // icon codes (short DB values)
      d: 'dashboard',
      pos: 'pos',
      p: 'products',
      c: 'categories',
      sc: 'subcategories',
      bn: 'brands',
      un: 'units',
      u: 'users',
      cu: 'customers',
      r: 'roles',
      b: 'businesses',
      br: 'branches',
      w: 'warehouses',
      su: 'suppliers',
      pu: 'purchase',
      i: 'invoices',
      s: 'sales',
      st: 'stock',
      rp: 'reports',
      inv: 'inventory',
      o: 'orders',
      m: 'menu',
      // full name keys
      dashboard: 'dashboard',
      posbilling: 'pos',
      products: 'products',
      categories: 'categories',
      businessmanagement: 'businesses',
      userrolemanagement: 'users',
      productmanagement: 'products',
      inventorymanagement: 'stock',
      purchasemanagement: 'purchase',
      salesmanagement: 'pos',
      finance: 'expenses',
      settings: 'default',
      variants: 'products',
      stocktransfer: 'inventory',
      salesreports: 'reports',
      purchasereports: 'reports',
      stockreports: 'reports',
      systemsettings: 'default',
      cs: 'default',
      codesequences: 'default',
      invoices: 'invoices',
      rolespermissions: 'roles',
      subcategories: 'subcategories',
      brands: 'brands',
      units: 'units',
      users: 'users',
      customers: 'customers',
      roles: 'roles',
      businesses: 'businesses',
      branches: 'branches',
      warehouses: 'warehouses',
      suppliers: 'suppliers',
      purchase: 'purchase',
      sales: 'sales',
      invoicehistory: 'invoices',
      saleslist: 'sales',
      inventory: 'inventory',
      stock: 'stock',
      stockin: 'stock',
      stockout: 'stock',
      reports: 'reports',
      orders: 'orders',
      menu: 'menu',
      masterdata: 'products',
      operations: 'pos',
      accounts: 'cashflow',
      expenses: 'expenses',
      cashflow: 'cashflow',
      cashdashboard: 'cashflow',
      cashledger: 'cashflow',
      cashsummary: 'cashflow',
      exp: 'expenses',
      cf: 'cashflow',
      cfl: 'cashflow',
      cfs: 'cashflow',
    }
    if (map[key]) return map[key]
  }

  return 'default'
}

interface MenuIconProps {
  icon: string | null | undefined
  name: string
  className?: string
}

const MenuIcon: React.FC<MenuIconProps> = ({ icon, name, className = 'w-[18px] h-[18px]' }) => {
  const key = resolveIconKey(icon, name)
  const d = ICON_PATHS[key] ?? ICON_PATHS.default

  return (
    <svg
      className={`flex-shrink-0 ${className}`}
      fill="none"
      viewBox="0 0 24 24"
      stroke="currentColor"
      strokeWidth={1.6}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden
    >
      <path d={d} />
    </svg>
  )
}

// ---------------------------------------------------------------------------
// Floating submenu panel (collapsed mode hover)
// ---------------------------------------------------------------------------

interface FloatingSubmenuProps {
  item: SidebarTreeItem
  topPx: number
  currentPath: string
  onMouseEnter: () => void
  onMouseLeave: () => void
}

const FloatingSubmenu: React.FC<FloatingSubmenuProps> = ({
  item,
  topPx,
  currentPath,
  onMouseEnter,
  onMouseLeave,
}) => (
  <div
    style={{ top: topPx, left: 64 }}
    className="fixed z-50 w-56 bg-gray-900 border border-gray-700/80 rounded-r-xl shadow-2xl overflow-hidden"
    onMouseEnter={onMouseEnter}
    onMouseLeave={onMouseLeave}
  >
    <div className="px-4 py-2.5 border-b border-gray-700/60 bg-gray-800/60">
      <span className="text-[11px] font-semibold text-gray-400 uppercase tracking-widest">
        {item.name}
      </span>
    </div>

    {item.children.length > 0
      ? item.children.map((child) => {
          if (!child.route) return null
          const isActive = currentPath === child.route
          return (
            <Link
              key={child.id}
              to={child.route}
              className={`
                flex items-center gap-3 px-4 py-2.5 text-sm transition-colors
                ${isActive
                  ? 'bg-blue-600 text-white'
                  : 'text-gray-300 hover:bg-gray-800 hover:text-white'}
              `}
            >
              <MenuIcon icon={child.icon} name={child.name} className={`w-4 h-4 ${isActive ? 'text-white' : 'text-gray-500'}`} />
              <span>{child.name}</span>
            </Link>
          )
        })
      : item.route
        ? (
          <Link
            to={item.route}
            className={`
              flex items-center gap-3 px-4 py-2.5 text-sm transition-colors
              ${currentPath === item.route
                ? 'bg-blue-600 text-white'
                : 'text-gray-300 hover:bg-gray-800 hover:text-white'}
            `}
          >
            <MenuIcon icon={item.icon} name={item.name} className="w-4 h-4 text-gray-500" />
            <span>{item.name}</span>
          </Link>
        )
        : null}
  </div>
)

// ---------------------------------------------------------------------------
// Accordion menu group (expanded mode)
// ---------------------------------------------------------------------------

interface AccordionGroupProps {
  item: SidebarTreeItem
  currentPath: string
}

const AccordionGroup: React.FC<AccordionGroupProps> = ({ item, currentPath }) => {
  const hasChildren = item.children.length > 0
  const isDirectLink = !hasChildren && Boolean(item.route)

  const isChildActive = useMemo(
    () => item.children.some((c) => c.route === currentPath),
    [item.children, currentPath]
  )
  const isSelfActive = Boolean(item.route) && item.route === currentPath

  // Auto-open when a child is active; otherwise default closed
  const [open, setOpen] = useState(() => isChildActive)

  // Re-open when navigating to a child in this group
  useEffect(() => {
    if (isChildActive) setOpen(true)
  }, [isChildActive])

  // ---- Direct link (no children) ----
  if (isDirectLink) {
    return (
      <li>
        <Link
          to={item.route!}
          className={`
            group flex items-center gap-3 px-3 py-2.5 mx-2 rounded-lg text-sm font-medium
            transition-colors duration-150
            ${isSelfActive
              ? 'bg-blue-600 text-white'
              : 'text-gray-300 hover:bg-gray-800 hover:text-white'}
          `}
        >
          <MenuIcon
            icon={item.icon}
            name={item.name}
            className={`w-[18px] h-[18px] flex-shrink-0 ${isSelfActive ? 'text-white' : 'text-gray-400 group-hover:text-gray-200'}`}
          />
          <span className="truncate">{item.name}</span>
        </Link>
      </li>
    )
  }

  // ---- Collapsible group ----
  return (
    <li className="overflow-hidden">
      {/* Group header — clickable toggle */}
      <button
        type="button"
        onClick={() => setOpen((o) => !o)}
        className={`
          group w-full flex items-center gap-3 px-3 py-2.5 mx-0 pl-3 pr-2
          text-sm font-medium transition-colors duration-150 rounded-none
          ${isChildActive
            ? 'text-blue-400 bg-blue-600/10'
            : 'text-gray-300 hover:bg-gray-800/60 hover:text-white'}
        `}
        style={{ width: 'calc(100% - 0px)' }}
      >
        <MenuIcon
          icon={item.icon}
          name={item.name}
          className={`w-[18px] h-[18px] flex-shrink-0 ${isChildActive ? 'text-blue-400' : 'text-gray-400 group-hover:text-gray-200'}`}
        />
        <span className="flex-1 text-left truncate">{item.name}</span>
        {/* Chevron */}
        <svg
          className={`w-4 h-4 flex-shrink-0 text-gray-500 transition-transform duration-200 ${open ? 'rotate-180' : ''}`}
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
          strokeWidth={2}
        >
          <path strokeLinecap="round" strokeLinejoin="round" d="M19 9l-7 7-7-7" />
        </svg>
      </button>

      {/* Children — animated slide */}
      <div
        className="overflow-hidden transition-all duration-200 ease-in-out"
        style={{ maxHeight: open ? `${item.children.length * 44}px` : '0px' }}
      >
        <ul className="pt-0.5 pb-1">
              {item.children.map((child) => {
                if (!child.route) return null
                const isActive = currentPath === child.route
                return (
                  <li key={child.id}>
                    <Link
                      to={child.route}
                      className={`
                        group flex items-center gap-3 pl-8 pr-3 py-2 mx-2 rounded-lg text-[13px]
                        transition-colors duration-150
                        ${isActive
                          ? 'bg-blue-600 text-white'
                          : 'text-gray-400 hover:bg-gray-800 hover:text-white'}
                      `}
                    >
                      <MenuIcon
                        icon={child.icon}
                        name={child.name}
                        className={`w-4 h-4 flex-shrink-0 ${isActive ? 'text-white' : 'text-gray-500 group-hover:text-gray-300'}`}
                      />
                      <span className="truncate">{child.name}</span>
                    </Link>
                  </li>
                )
              })}
        </ul>
      </div>
    </li>
  )
}

// ---------------------------------------------------------------------------
// Main Sidebar
// ---------------------------------------------------------------------------

const Sidebar: React.FC = () => {
  const location = useLocation()
  const { user, logout } = useAuth()

  const sidebarTree = useMenuStore((state) => state.sidebarTree)
  const isLoading = useMenuStore((state) => state.isLoading)
  const fetchSidebarData = useMenuStore((state) => state.fetchSidebarData)

  const [collapsed, setCollapsed] = useState(false)

  // Business info & logo
  const [businessName, setBusinessName] = useState<string>('POS')
  const [logoFallback, setLogoFallback] = useState<string>('P')
  const [logoUrl, setLogoUrl] = useState<string | null>(null)
  const logoObjectUrlRef = useRef<string | null>(null)

  // Collapsed hover submenu
  const [activeSubmenu, setActiveSubmenu] = useState<{ itemId: number; top: number } | null>(null)
  const openTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const closeTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  // Fetch sidebar tree
  useEffect(() => {
    if (user?.roleId) void fetchSidebarData(user.roleId)
  }, [user?.roleId, fetchSidebarData])

  // Fetch business info + logo
  useEffect(() => {
    let cancelled = false
    const run = async () => {
      try {
        const info = await sidebarService.getBusinessInfo()
        if (cancelled) return
        const name = info.name || 'POS'
        setBusinessName(name)
        setLogoFallback((name[0] ?? 'P').toUpperCase())
        if (info.hasLogo) {
          if (logoObjectUrlRef.current) URL.revokeObjectURL(logoObjectUrlRef.current)
          const url = await sidebarService.getBusinessLogoUrl()
          if (cancelled) { if (url) URL.revokeObjectURL(url); return }
          logoObjectUrlRef.current = url
          setLogoUrl(url)
        } else {
          setLogoUrl(null)
        }
      } catch { /* silent */ }
    }
    void run()
    return () => { cancelled = true }
  }, [user?.businessId])

  useEffect(() => () => {
    if (logoObjectUrlRef.current) URL.revokeObjectURL(logoObjectUrlRef.current)
  }, [])

  // Close floating submenu on route change
  useEffect(() => { setActiveSubmenu(null) }, [location.pathname])
  useEffect(() => { setActiveSubmenu(null) }, [collapsed])

  // Timers cleanup
  useEffect(() => () => {
    if (openTimerRef.current) clearTimeout(openTimerRef.current)
    if (closeTimerRef.current) clearTimeout(closeTimerRef.current)
  }, [])

  // Hover handlers (collapsed mode)
  const handleIconEnter = useCallback((itemId: number, el: HTMLElement) => {
    if (!collapsed) return
    if (closeTimerRef.current) clearTimeout(closeTimerRef.current)
    openTimerRef.current = setTimeout(() => {
      setActiveSubmenu({ itemId, top: el.getBoundingClientRect().top })
    }, 150)
  }, [collapsed])

  const handleIconLeave = useCallback(() => {
    if (!collapsed) return
    if (openTimerRef.current) clearTimeout(openTimerRef.current)
    closeTimerRef.current = setTimeout(() => setActiveSubmenu(null), 120)
  }, [collapsed])

  const handleSubmenuEnter = useCallback(() => {
    if (closeTimerRef.current) clearTimeout(closeTimerRef.current)
  }, [])

  const handleSubmenuLeave = useCallback(() => {
    closeTimerRef.current = setTimeout(() => setActiveSubmenu(null), 120)
  }, [])

  const activeSubmenuItem = useMemo(
    () => activeSubmenu ? sidebarTree.find((i) => i.id === activeSubmenu.itemId) ?? null : null,
    [activeSubmenu, sidebarTree]
  )

  const userDisplayName = user?.fullName || user?.username || 'User'
  const userInitial = (userDisplayName[0] ?? 'U').toUpperCase()

  return (
    <>
      <aside
        className={`
          relative flex flex-col bg-gray-900 h-screen flex-shrink-0 select-none
          transition-[width] duration-300 ease-in-out
          ${collapsed ? 'w-16' : 'w-64'}
        `}
      >
        {/* ─── TOP: logo + name ─── */}
        <div
          className={`
            flex items-center flex-shrink-0 h-14 border-b border-white/5
            ${collapsed ? 'justify-center px-3' : 'gap-3 px-4'}
          `}
        >
          {/* Logo */}
          <div className="flex-shrink-0 w-8 h-8 rounded-lg overflow-hidden bg-blue-600 flex items-center justify-center">
            {logoUrl
              ? <img src={logoUrl} alt={businessName} className="w-full h-full object-contain" draggable={false} />
              : <span className="text-white font-bold text-sm">{logoFallback}</span>}
          </div>

          {!collapsed && (
            <div className="flex-1 min-w-0">
              <p className="text-[13px] font-semibold text-white truncate leading-tight">{businessName}</p>
              <p className="text-[11px] text-gray-500 leading-tight">POS System</p>
            </div>
          )}

          <button
            onClick={() => setCollapsed((c) => !c)}
            className={`
              flex-shrink-0 w-7 h-7 flex items-center justify-center rounded-lg
              text-gray-500 hover:bg-gray-800 hover:text-gray-300 transition-colors
              ${collapsed ? '' : 'ml-auto'}
            `}
            aria-label={collapsed ? 'Expand' : 'Collapse'}
          >
            <svg
              className={`w-4 h-4 transition-transform duration-300 ${collapsed ? 'rotate-180' : ''}`}
              fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}
            >
              <path strokeLinecap="round" strokeLinejoin="round" d="M11 19l-7-7 7-7M18 19l-7-7 7-7" />
            </svg>
          </button>
        </div>

        {/* ─── MIDDLE: scrollable menu ─── */}
        <nav className="flex-1 overflow-y-auto overflow-x-hidden py-2">
          {isLoading && (
            <div className={`py-4 ${collapsed ? 'flex justify-center' : 'px-4'}`}>
              <div className="w-5 h-5 rounded-full border-2 border-gray-700 border-t-blue-500 animate-spin" />
            </div>
          )}

          {/* EXPANDED */}
          {!collapsed && !isLoading && (
            <ul className="space-y-0.5 pb-2">
              {sidebarTree.map((item) => (
                <AccordionGroup key={item.id} item={item} currentPath={location.pathname} />
              ))}
            </ul>
          )}

          {/* COLLAPSED — icons only */}
          {collapsed && !isLoading && (
            <ul className="space-y-0.5 px-2">
              {sidebarTree.map((item) => {
                const isActive =
                  item.children.some((c) => c.route === location.pathname) ||
                  item.route === location.pathname

                const cls = `
                  flex justify-center items-center w-full h-10 rounded-lg cursor-pointer
                  transition-colors duration-150
                  ${isActive ? 'bg-blue-600/15 text-blue-400' : 'text-gray-400 hover:bg-gray-800 hover:text-gray-200'}
                `
                const icon = (
                  <MenuIcon
                    icon={item.icon}
                    name={item.name}
                    className={`w-5 h-5 ${isActive ? 'text-blue-400' : ''}`}
                  />
                )

                return (
                  <li key={item.id}>
                    {item.children.length === 0 && item.route
                      ? (
                        <Link to={item.route} className={cls}
                          onMouseEnter={(e) => handleIconEnter(item.id, e.currentTarget)}
                          onMouseLeave={handleIconLeave}
                          title={item.name}
                        >{icon}</Link>
                      )
                      : (
                        <div className={cls}
                          onMouseEnter={(e) => handleIconEnter(item.id, e.currentTarget)}
                          onMouseLeave={handleIconLeave}
                          title={item.name}
                        >{icon}</div>
                      )}
                  </li>
                )
              })}
            </ul>
          )}
        </nav>

        {/* ─── BOTTOM: user + logout ─── */}
        <div className="flex-shrink-0 border-t border-white/5 p-3">
          {collapsed
            ? (
              <div className="flex flex-col items-center gap-2">
                <div className="w-8 h-8 rounded-full bg-gray-700 flex items-center justify-center text-gray-300 text-xs font-bold">
                  {userInitial}
                </div>
                <button onClick={logout} title="Logout"
                  className="w-8 h-8 flex items-center justify-center rounded-lg hover:bg-gray-800 text-gray-500 hover:text-red-400 transition-colors"
                >
                  <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
                  </svg>
                </button>
              </div>
            )
            : (
              <div className="flex items-center gap-3">
                <div className="w-8 h-8 rounded-full bg-gray-700 flex items-center justify-center text-gray-300 text-xs font-bold flex-shrink-0">
                  {userInitial}
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-[13px] font-medium text-gray-200 truncate leading-tight">{userDisplayName}</p>
                  {user?.roleName && <p className="text-[11px] text-gray-500 truncate leading-tight">{user.roleName}</p>}
                </div>
                <button onClick={logout} title="Logout"
                  className="w-8 h-8 flex items-center justify-center rounded-lg hover:bg-gray-800 text-gray-500 hover:text-red-400 transition-colors flex-shrink-0"
                >
                  <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
                  </svg>
                </button>
              </div>
            )}
        </div>
      </aside>

      {/* Floating submenu (collapsed hover) */}
      {collapsed && activeSubmenuItem && activeSubmenu && (
        <FloatingSubmenu
          item={activeSubmenuItem}
          topPx={activeSubmenu.top}
          currentPath={location.pathname}
          onMouseEnter={handleSubmenuEnter}
          onMouseLeave={handleSubmenuLeave}
        />
      )}
    </>
  )
}

export default Sidebar
