import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  getApiErrorMessage,
  roleService,
  RoleListItem,
  RolePermissionItem,
  PermissionField,
} from '../../services/roleService';
import { usePermission, useIsMasterUser, useIsSuperAdmin } from '../../hooks/usePermission';
import { isProtectedRole } from '../../types/permissions';
const PERMISSION_COLUMNS: { key: PermissionField; label: string }[] = [
  { key: 'canView', label: 'View' },
  { key: 'canCreate', label: 'Add' },
  { key: 'canEdit', label: 'Edit' },
  { key: 'canDelete', label: 'Delete' },
  { key: 'canExport', label: 'Export' },
  { key: 'canUpload', label: 'Upload' },
];

interface ModuleTreeNode {
  id: number;
  moduleName: string;
  moduleKey: string;
  parentModuleId: number | null;
  displayOrder: number;
  children: ModuleTreeNode[];
}

const buildHierarchyRows = (
  permissions: RolePermissionItem[],
  modules: ModuleTreeNode[]
): Array<{ permission: RolePermissionItem; depth: number; isGroup: boolean }> => {
  const permissionMap = new Map(permissions.map((p) => [p.moduleId, p]));
  const rows: Array<{ permission: RolePermissionItem; depth: number; isGroup: boolean }> = [];

  const walk = (items: ModuleTreeNode[], depth: number) => {
    for (const item of items) {
      const isGroup = item.moduleKey === '';
      if (isGroup) {
        rows.push({
          permission: {
            moduleId: item.id,
            moduleName: item.moduleName,
            moduleKey: item.moduleKey,
            parentModuleId: item.parentModuleId,
            displayOrder: item.displayOrder,
            canView: false,
            canCreate: false,
            canEdit: false,
            canDelete: false,
            canExport: false,
            canUpload: false,
          },
          depth,
          isGroup: true,
        });
        if (item.children.length > 0) {
          walk(item.children, depth + 1);
        }
        continue;
      }

      const permission = permissionMap.get(item.id);
      if (permission) {
        rows.push({ permission, depth, isGroup: false });
      }
    }
  };

  walk(modules, 0);
  return rows;
};

const RolePermissionPage: React.FC = () => {
  const { canEdit } = usePermission('Roles');
  const isSuperAdmin = useIsSuperAdmin();
  const isMasterUser = useIsMasterUser();
  const [roles, setRoles] = useState<RoleListItem[]>([]);
  const [modules, setModules] = useState<ModuleTreeNode[]>([]);
  const [selectedRoleId, setSelectedRoleId] = useState<number | null>(null);
  const [permissions, setPermissions] = useState<RolePermissionItem[]>([]);
  const [loadingRoles, setLoadingRoles] = useState(true);
  const [loadingPermissions, setLoadingPermissions] = useState(false);
  const [saving, setSaving] = useState(false);
  const [notification, setNotification] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  const showNotification = useCallback((type: 'success' | 'error', message: string) => {
    setNotification({ type, message });
    setTimeout(() => setNotification(null), 4000);
  }, []);

  useEffect(() => {
    const loadInitial = async () => {
      setLoadingRoles(true);
      try {
        const [roleList, moduleList] = await Promise.all([
          roleService.getRoles(),
          roleService.getModules(),
        ]);
        setRoles(roleList.filter((role) => role.isActive));
        setModules(moduleList as ModuleTreeNode[]);
        if (roleList.length > 0) {
          setSelectedRoleId(roleList[0].id);
        }
      } catch (error) {
        showNotification('error', getApiErrorMessage(error, 'Failed to load roles.'));
      } finally {
        setLoadingRoles(false);
      }
    };

    loadInitial();
  }, [showNotification]);

  useEffect(() => {
    if (!selectedRoleId) {
      setPermissions([]);
      return;
    }

    const loadPermissions = async () => {
      setLoadingPermissions(true);
      try {
        const data = await roleService.getRolePermissions(selectedRoleId);
        setPermissions(data);
      } catch (error) {
        showNotification('error', getApiErrorMessage(error, 'Failed to load permissions.'));
        setPermissions([]);
      } finally {
        setLoadingPermissions(false);
      }
    };

    loadPermissions();
  }, [selectedRoleId, showNotification]);

  const hierarchyRows = useMemo(
    () => buildHierarchyRows(permissions, modules),
    [permissions, modules]
  );

  const handleToggle = (moduleId: number, field: PermissionField) => {
    if (!canEditSelectedRole) return;

    setPermissions((current) =>
      current.map((item) => {
        if (item.moduleId !== moduleId) return item;

        const nextValue = !item[field];
        const updated = { ...item, [field]: nextValue };

        if (field !== 'canView' && nextValue) {
          updated.canView = true;
        }

        if (field === 'canView' && !nextValue) {
          updated.canCreate = false;
          updated.canEdit = false;
          updated.canDelete = false;
          updated.canExport = false;
          updated.canUpload = false;
        }

        return updated;
      })
    );
  };

  const handleSave = async () => {
    if (!selectedRoleId || !canEditSelectedRole) return;

    setSaving(true);
    try {
      const saved = await roleService.saveRolePermissions(selectedRoleId, permissions);
      setPermissions(saved);
      showNotification('success', 'Role permissions saved successfully.');
    } catch (error) {
      showNotification('error', getApiErrorMessage(error, 'Failed to save permissions.'));
    } finally {
      setSaving(false);
    }
  };

  const selectedRole = roles.find((role) => role.id === selectedRoleId);
  const isProtectedRoleSelected = isProtectedRole(selectedRole?.name);

  // MasterUser can edit any role.
  // Everyone else (SuperAdmin, Admin) can only edit non-protected roles.
  // canEdit gate ensures the user has the Roles permission at all.
  const canEditSelectedRole = canEdit && (isMasterUser || !isProtectedRoleSelected);

  return (
    <div className="p-6">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Role Permissions</h1>
        <p className="text-sm text-gray-600 mt-1">
          Assign module access for each role. Changes apply on next login for affected users.
        </p>
      </div>

      {notification && (
        <div
          className={`mb-4 rounded-lg px-4 py-3 text-sm ${
            notification.type === 'success'
              ? 'bg-green-50 text-green-800 border border-green-200'
              : 'bg-red-50 text-red-800 border border-red-200'
          }`}
        >
          {notification.message}
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
        <div className="lg:col-span-1">
          <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4">
            <label htmlFor="role-select" className="block text-sm font-medium text-gray-700 mb-2">
              Select Role
            </label>
            <select
              id="role-select"
              value={selectedRoleId ?? ''}
              onChange={(event) => setSelectedRoleId(Number(event.target.value))}
              disabled={loadingRoles}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              {roles.map((role) => (
                <option key={role.id} value={role.id}>
                  {role.name}
                </option>
              ))}
            </select>
            {selectedRole?.description && (
              <p className="mt-3 text-xs text-gray-500">{selectedRole.description}</p>
            )}
            {isSuperAdmin && isProtectedRoleSelected && !isMasterUser && (
              <p className="mt-3 text-xs text-amber-700">
                Permissions for {selectedRole?.name} are read-only for your account.
              </p>
            )}
          </div>
        </div>

        <div className="lg:col-span-3">
          <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
            <div className="px-4 py-3 border-b border-gray-200 flex items-center justify-between">
              <h2 className="text-sm font-semibold text-gray-800">Module Permissions</h2>
              <button
                type="button"
                onClick={handleSave}
                disabled={saving || loadingPermissions || !selectedRoleId || !canEditSelectedRole}
                title={!canEditSelectedRole ? 'You do not have permission to edit this role.' : undefined}
                className="inline-flex items-center rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {saving ? 'Saving...' : 'Save Permissions'}
              </button>
            </div>

            {loadingPermissions ? (
              <div className="p-8 text-center text-sm text-gray-500">Loading permissions...</div>
            ) : (
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-600">
                        Module
                      </th>
                      {PERMISSION_COLUMNS.map((column) => (
                        <th
                          key={column.key}
                          className="px-3 py-3 text-center text-xs font-semibold uppercase tracking-wide text-gray-600"
                        >
                          {column.label}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100">
                    {hierarchyRows.map(({ permission, depth, isGroup }) => (
                      <tr
                        key={`${permission.moduleId}-${isGroup ? 'group' : 'leaf'}`}
                        className={isGroup ? 'bg-gray-50' : 'bg-white'}
                      >
                        <td className="px-4 py-3 text-sm text-gray-900">
                          <span style={{ paddingLeft: `${depth * 16}px` }} className="inline-block">
                            {isGroup ? (
                              <span className="font-semibold text-gray-700">{permission.moduleName}</span>
                            ) : (
                              permission.moduleName
                            )}
                          </span>
                        </td>
                        {PERMISSION_COLUMNS.map((column) => (
                          <td key={column.key} className="px-3 py-3 text-center">
                            {isGroup ? (
                              <span className="text-gray-300">—</span>
                            ) : (
                              <input
                                type="checkbox"
                                checked={permission[column.key]}
                                onChange={() => handleToggle(permission.moduleId, column.key)}
                                disabled={!canEditSelectedRole}
                                className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500 disabled:opacity-50"
                                aria-label={`${permission.moduleName} ${column.label}`}
                              />
                            )}
                          </td>
                        ))}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default RolePermissionPage;
