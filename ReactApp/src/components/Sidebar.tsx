import React, { useEffect, useMemo, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { usePermissionStore } from '../stores/usePermissionStore';
import { useMenuStore } from '../stores/useMenuStore';
import { buildSidebarGroups } from '../services/menuService';

const Sidebar: React.FC = () => {
  const location = useLocation();
  const [collapsed, setCollapsed] = useState(false);
  const { user } = useAuth();
  const can = usePermissionStore((state) => state.can);
  const menus = useMenuStore((state) => state.menus);
  const isLoading = useMenuStore((state) => state.isLoading);
  const error = useMenuStore((state) => state.error);
  const fetchMenus = useMenuStore((state) => state.fetchMenus);

  useEffect(() => {
    if (user?.roleId) {
      void fetchMenus(user.roleId);
    }
  }, [user?.roleId, fetchMenus]);

  const visibleGroups = useMemo(
    () => buildSidebarGroups(menus, (moduleName) => can(moduleName, 'view')),
    [menus, can]
  );

  return (
    <div className={`bg-gray-900 text-white h-full transition-all duration-300 ${collapsed ? 'w-16' : 'w-64'}`}>
      <div className="p-4 border-b border-gray-700">
        <div className="flex items-center justify-between">
          {!collapsed && <h2 className="text-xl font-bold">POS System</h2>}
          <button
            onClick={() => setCollapsed(!collapsed)}
            className="p-1 rounded hover:bg-gray-800"
          >
            {collapsed ? '→' : '←'}
          </button>
        </div>
      </div>

      <nav className="mt-4">
        {isLoading && !collapsed && (
          <p className="px-4 py-2 text-sm text-gray-400">Loading menus...</p>
        )}

        {error && !collapsed && (
          <p className="px-4 py-2 text-sm text-red-400">{error}</p>
        )}

        {visibleGroups.map((group) => (
          <div key={group.id} className="mb-4">
            {!collapsed && (
              <h3 className="px-4 py-2 text-xs font-semibold text-gray-400 uppercase tracking-wider">
                {group.name}
              </h3>
            )}
            <ul>
              {group.items.map((item) => (
                <li key={item.id}>
                  <Link
                    to={item.path}
                    className={`flex items-center px-4 py-3 hover:bg-gray-800 transition-colors ${
                      location.pathname === item.path ? 'bg-blue-600 text-white' : 'text-gray-300'
                    } ${collapsed ? 'justify-center' : ''}`}
                    title={collapsed ? item.label : undefined}
                  >
                    <span className="text-lg mr-3">{item.icon}</span>
                    {!collapsed && <span>{item.label}</span>}
                  </Link>
                </li>
              ))}
            </ul>
          </div>
        ))}
      </nav>
    </div>
  );
};

export default Sidebar;
