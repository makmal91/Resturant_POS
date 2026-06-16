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
import MenuIcon from './MenuIcon'

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
