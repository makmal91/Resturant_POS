import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  getApiErrorMessage,
  roleService,
  RoleListItem,
  RolePermissionItem,
  FormPermissionField,
} from '../../services/roleService';
import { authService } from '../../services/authService';
import { useAuth } from '../../contexts/AuthContext';
import { useMenuStore } from '../../stores/useMenuStore';
import { usePermissionStore } from '../../stores/usePermissionStore';
import { usePermission, useIsMasterUser, useIsSuperAdmin } from '../../hooks/usePermission';
import { isProtectedRole } from '../../types/permissions';
import { authStorage } from '../../utils/storage';
import { canAssignModulePermission } from '../../utils/permissionUtils';
import { useFormModal } from '../../contexts/FormModalContext';
import { useConfirmDialog } from '../../contexts/ConfirmDialogContext';
import { isFeatureFormCode } from '../../types/featurePermissions';

interface ModuleTreeNode {
  id: number;
  moduleName: string;
  moduleKey: string;
  parentModuleId: number | null;
  displayOrder: number;
  children: ModuleTreeNode[];
}

const ACTION_LABELS: { key: FormPermissionField; label: string; color: string }[] = [
  { key: 'canCreate', label: 'Create', color: 'bg-emerald-100 text-emerald-700 border-emerald-200' },
  { key: 'canEdit', label: 'Edit', color: 'bg-blue-100 text-blue-700 border-blue-200' },
  { key: 'canDelete', label: 'Delete', color: 'bg-red-100 text-red-700 border-red-200' },
];

const buildTreeFromFlat = (permissions: RolePermissionItem[]): ModuleTreeNode[] => {
  const nodes = permissions.map((p) => ({
    id: p.moduleId,
    moduleName: p.moduleName,
    moduleKey: p.moduleKey,
    parentModuleId: p.parentModuleId,
    displayOrder: p.displayOrder,
    children: [] as ModuleTreeNode[],
  }));

  const lookup = new Map(nodes.map((n) => [n.id, n]));
  const roots: ModuleTreeNode[] = [];

  for (const node of nodes) {
    if (node.parentModuleId && lookup.has(node.parentModuleId)) {
      lookup.get(node.parentModuleId)!.children.push(node);
    } else {
      roots.push(node);
    }
  }

  const sortNodes = (items: ModuleTreeNode[]) => {
    items.sort((a, b) => a.displayOrder - b.displayOrder || a.moduleName.localeCompare(b.moduleName));
    items.forEach((item) => sortNodes(item.children));
  };

  sortNodes(roots);
  return roots;
};

const collectDescendantIds = (node: ModuleTreeNode): number[] => {
  const ids = [node.id];
  for (const child of node.children) {
    ids.push(...collectDescendantIds(child));
  }
  return ids;
};

interface GroupCardProps {
  group: ModuleTreeNode;
  permissionMap: Map<number, RolePermissionItem>;
  expanded: boolean;
  onToggleExpand: () => void;
  onModuleAccess: (moduleId: number, enabled: boolean) => void;
  onFormActionToggle: (moduleId: number, formId: number, field: FormPermissionField) => void;
  onGroupToggleAll: (groupId: number, enabled: boolean) => void;
  canEdit: boolean;
  searchTerm: string;
}

const isViewOnlyModule = (perm?: RolePermissionItem): boolean =>
  Boolean(perm?.isViewOnly);

const GroupCard: React.FC<GroupCardProps> = ({
  group,
  permissionMap,
  expanded,
  onToggleExpand,
  onModuleAccess,
  onFormActionToggle,
  onGroupToggleAll,
  canEdit,
  searchTerm,
}) => {
  const groupPerm = permissionMap.get(group.id);
  const leafChildren = group.children.filter((c) => c.moduleKey !== '');

  const visibleChildren = leafChildren.filter((child) => {
    if (!searchTerm) return true;
    return child.moduleName.toLowerCase().includes(searchTerm);
  });

  if (searchTerm && visibleChildren.length === 0 && !group.moduleName.toLowerCase().includes(searchTerm)) {
    return null;
  }

  const enabledCount = leafChildren.filter((c) => permissionMap.get(c.id)?.canView).length;

  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
      <div className="flex items-center gap-3 px-4 py-3 bg-gradient-to-r from-slate-50 to-white border-b border-gray-100">
        <button
          type="button"
          onClick={onToggleExpand}
          className="w-7 h-7 flex items-center justify-center rounded-lg hover:bg-gray-100 text-gray-500"
        >
          <svg
            className={`w-4 h-4 transition-transform ${expanded ? 'rotate-180' : ''}`}
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            strokeWidth={2}
          >
            <path strokeLinecap="round" strokeLinejoin="round" d="M19 9l-7 7-7-7" />
          </svg>
        </button>

        <div className="flex-1 min-w-0">
          <h3 className="text-sm font-semibold text-gray-900">{group.moduleName}</h3>
          <p className="text-xs text-gray-500">
            {enabledCount} of {leafChildren.length} modules enabled
          </p>
        </div>

        {canEdit && (
          <div className="flex items-center gap-2 flex-shrink-0">
            <button
              type="button"
              onClick={() => onGroupToggleAll(group.id, true)}
              className="text-xs px-2.5 py-1 rounded-md bg-blue-50 text-blue-700 hover:bg-blue-100 font-medium"
            >
              Enable All
            </button>
            <button
              type="button"
              onClick={() => onGroupToggleAll(group.id, false)}
              className="text-xs px-2.5 py-1 rounded-md bg-gray-100 text-gray-600 hover:bg-gray-200 font-medium"
            >
              Disable All
            </button>
          </div>
        )}
      </div>

      {expanded && (
        <div className="divide-y divide-gray-50">
          {visibleChildren.length === 0 ? (
            <p className="px-4 py-6 text-sm text-gray-400 text-center">No matching modules</p>
          ) : (
            visibleChildren.map((child) => {
              const perm = permissionMap.get(child.id);
              const hasAccess = perm?.canView ?? false;
              const forms = (perm?.forms ?? []).filter((f) => !isFeatureFormCode(f.formCode));

              return (
                <div
                  key={child.id}
                  className={`px-4 py-3 flex flex-col gap-3 ${
                    hasAccess ? 'bg-white' : 'bg-gray-50/50'
                  }`}
                >
                  <div className="flex items-center gap-3 flex-1 min-w-0">
                    <label className="relative inline-flex items-center cursor-pointer flex-shrink-0">
                      <input
                        type="checkbox"
                        checked={hasAccess}
                        onChange={(e) => onModuleAccess(child.id, e.target.checked)}
                        disabled={!canEdit}
                        className="sr-only peer"
                      />
                      <div className="w-10 h-5 bg-gray-200 rounded-full peer peer-checked:bg-blue-600 peer-disabled:opacity-50 after:content-[''] after:absolute after:top-0.5 after:left-0.5 after:bg-white after:rounded-full after:h-4 after:w-4 after:transition-all peer-checked:after:translate-x-5" />
                    </label>
                    <div className="min-w-0">
                      <p className={`text-sm font-medium truncate ${hasAccess ? 'text-gray-900' : 'text-gray-500'}`}>
                        {child.moduleName}
                      </p>
                      <p className="text-xs text-gray-400">
                        {isViewOnlyModule(perm) ? 'View only' : 'Menu access'}
                      </p>
                    </div>
                  </div>

                  {hasAccess && forms.length > 0 && !isViewOnlyModule(perm) && (
                    <div className="ml-12 space-y-2 border-l-2 border-gray-100 pl-4">
                      {forms.map((form) => (
                        <div key={form.formId} className="flex flex-col sm:flex-row sm:items-center gap-2">
                          <p className="text-xs font-medium text-gray-600 min-w-[140px] truncate">
                            {form.formName}
                          </p>
                          <div className="flex items-center gap-2 flex-wrap">
                            {ACTION_LABELS.map((action) => {
                              const active = form[action.key];
                              return (
                                <button
                                  key={`${form.formId}-${action.key}`}
                                  type="button"
                                  disabled={!canEdit}
                                  onClick={() => onFormActionToggle(child.id, form.formId, action.key)}
                                  className={`text-xs px-3 py-1 rounded-full border font-medium transition-all disabled:opacity-50 ${
                                    active
                                      ? action.color
                                      : 'bg-white text-gray-400 border-gray-200 hover:border-gray-300'
                                  }`}
                                >
                                  {action.label}
                                </button>
                              );
                            })}
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              );
            })
          )}
        </div>
      )}
    </div>
  );
};

const RolePermissionPage: React.FC = () => {
  const { user } = useAuth();
  const refreshSidebar = useMenuStore((s) => s.refreshSidebarData);
  const { canCreate, canEdit, canDelete } = usePermission('Roles');
  const isSuperAdmin = useIsSuperAdmin();
  const isMasterUser = useIsMasterUser();
  const actorPermissions = usePermissionStore((s) => s.permissions);
  const actorRoleName = usePermissionStore((s) => s.roleName);
  const { openForm, isOpen } = useFormModal();
  const { showConfirm } = useConfirmDialog();

  const [roles, setRoles] = useState<RoleListItem[]>([]);
  const [selectedRoleId, setSelectedRoleId] = useState<number | null>(null);
  const [permissions, setPermissions] = useState<RolePermissionItem[]>([]);
  const [loadingRoles, setLoadingRoles] = useState(true);
  const [loadingPermissions, setLoadingPermissions] = useState(false);
  const [saving, setSaving] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [expandedGroups, setExpandedGroups] = useState<Set<number>>(new Set());
  const [notification, setNotification] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  const showNotification = useCallback((type: 'success' | 'error', message: string) => {
    setNotification({ type, message });
    setTimeout(() => setNotification(null), 5000);
  }, []);

  const loadRoles = useCallback(async (selectRoleId?: number | null) => {
    setLoadingRoles(true);
    try {
      const roleList = await roleService.getRoles();
      setRoles(roleList);
      if (selectRoleId && roleList.some((role) => role.id === selectRoleId)) {
        setSelectedRoleId(selectRoleId);
      } else if (roleList.length > 0) {
        setSelectedRoleId((current) =>
          current && roleList.some((role) => role.id === current) ? current : roleList[0].id,
        );
      } else {
        setSelectedRoleId(null);
      }
    } catch (error) {
      showNotification('error', getApiErrorMessage(error, 'Failed to load roles.'));
    } finally {
      setLoadingRoles(false);
    }
  }, [showNotification]);

  useEffect(() => {
    void loadRoles();
  }, [loadRoles]);

  useEffect(() => {
    if (!isOpen) {
      void loadRoles(selectedRoleId);
    }
  }, [isOpen, loadRoles, selectedRoleId]);

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
        setExpandedGroups(new Set(data.filter((p) => p.moduleKey === '').map((p) => p.moduleId)));
      } catch (error) {
        showNotification('error', getApiErrorMessage(error, 'Failed to load permissions.'));
        setPermissions([]);
      } finally {
        setLoadingPermissions(false);
      }
    };

    loadPermissions();
  }, [selectedRoleId, showNotification]);

  const tree = useMemo(() => buildTreeFromFlat(permissions), [permissions]);
  const permissionMap = useMemo(() => new Map(permissions.map((p) => [p.moduleId, p])), [permissions]);

  const isModuleAssignable = useCallback(
    (moduleKey: string, moduleName: string) =>
      canAssignModulePermission(actorPermissions, moduleKey, moduleName, actorRoleName),
    [actorPermissions, actorRoleName],
  );

  const filterVisibleTree = useCallback(
    (nodes: ModuleTreeNode[]): ModuleTreeNode[] =>
      nodes
        .map((node) => {
          if (node.moduleKey === '') {
            const children = filterVisibleTree(node.children);
            if (children.length === 0) {
              return null;
            }
            return { ...node, children };
          }

          if (!isModuleAssignable(node.moduleKey, node.moduleName)) {
            return null;
          }

          return { ...node, children: [] };
        })
        .filter((node): node is ModuleTreeNode => node !== null),
    [isModuleAssignable],
  );

  const visibleTree = useMemo(() => filterVisibleTree(tree), [tree, filterVisibleTree]);

  const selectedRole = roles.find((role) => role.id === selectedRoleId);
  const isProtectedRoleSelected = isProtectedRole(selectedRole?.name);
  const canEditSelectedRole = canEdit && (isMasterUser || !isProtectedRoleSelected);
  const canDeleteSelectedRole = canDelete && (isMasterUser || !isProtectedRoleSelected);
  const normalizedSearch = searchTerm.trim().toLowerCase();

  const handleCreateRole = () => {
    openForm('role');
  };

  const handleEditRole = () => {
    if (!selectedRole) return;
    openForm('role', {
      id: selectedRole.id,
      name: selectedRole.name,
      description: selectedRole.description,
      isActive: selectedRole.isActive,
      status: selectedRole.isActive ? 'Active' : 'Inactive',
    });
  };

  const handleDeleteRole = () => {
    if (!selectedRole || !canDeleteSelectedRole) return;
    showConfirm({
      title: 'Delete Role?',
      message: `Delete role "${selectedRole.name}"? This cannot be undone if the role has no assigned users.`,
      confirmLabel: 'Delete Role',
      variant: 'danger',
      onConfirm: async () => {
        try {
          await roleService.deleteRole(selectedRole.id);
          showNotification('success', `Role "${selectedRole.name}" deleted.`);
          await loadRoles(null);
        } catch (error) {
          showNotification('error', getApiErrorMessage(error, 'Failed to delete role.'));
        }
      },
    });
  };

  const standaloneModules = visibleTree.filter((n) => n.moduleKey !== '' && n.children.length === 0);
  const groupModules = visibleTree.filter((n) => n.moduleKey === '' && n.children.length > 0);
  const showAssignableHint = !isMasterUser && !isSuperAdmin && actorPermissions.length > 0;

  const applyToModules = useCallback((moduleIds: number[], updater: (item: RolePermissionItem) => RolePermissionItem) => {
    setPermissions((current) =>
      current.map((item) => (moduleIds.includes(item.moduleId) ? updater(item) : item))
    );
  }, []);

  const setModuleAccess = useCallback(
    (moduleId: number, enabled: boolean) => {
      if (!canEditSelectedRole) return;

      applyToModules([moduleId], (item) => {
        const viewOnly = isViewOnlyModule(item);
        return {
          ...item,
          canView: enabled,
          canCreate: enabled && !viewOnly ? item.canCreate : false,
          canEdit: enabled && !viewOnly ? item.canEdit : false,
          canDelete: enabled && !viewOnly ? item.canDelete : false,
          canExport: enabled ? item.canExport : false,
          canUpload: enabled && !viewOnly ? item.canUpload : false,
          forms: item.forms.map((f) => ({
            ...f,
            canView: enabled,
            canCreate: enabled && !viewOnly ? f.canCreate : false,
            canEdit: enabled && !viewOnly ? f.canEdit : false,
            canDelete: enabled && !viewOnly ? f.canDelete : false,
          })),
        };
      });
    },
    [applyToModules, canEditSelectedRole]
  );

  const setGroupToggleAll = useCallback(
    (groupId: number, enabled: boolean) => {
      if (!canEditSelectedRole) return;
      const group = tree.find((g) => g.id === groupId);
      if (!group) return;
      const ids = collectDescendantIds(group).filter((id) => {
        const p = permissionMap.get(id);
        return p && p.moduleKey !== '';
      });
      applyToModules(ids, (item) => {
        const viewOnly = isViewOnlyModule(item);
        const enableActions = enabled && !viewOnly;
        return {
          ...item,
          canView: enabled,
          canCreate: enableActions,
          canEdit: enableActions,
          canDelete: enableActions,
          canExport: enabled,
          canUpload: enableActions,
          forms: item.forms.map((f) => ({
            ...f,
            canView: enabled,
            canCreate: enableActions,
            canEdit: enableActions,
            canDelete: enableActions,
          })),
        };
      });
    },
    [applyToModules, canEditSelectedRole, permissionMap, tree]
  );

  const toggleFormAction = useCallback(
    (moduleId: number, formId: number, field: FormPermissionField) => {
      if (!canEditSelectedRole) return;

      setPermissions((current) =>
        current.map((item) => {
          if (item.moduleId !== moduleId || !item.canView) return item;

          const forms = item.forms.map((form) => {
            if (form.formId !== formId) return form;
            const nextValue = !form[field];
            const updated = { ...form, [field]: nextValue };
            if (field !== 'canView' && nextValue) updated.canView = true;
            return updated;
          });

          const primary = forms[0];
          return {
            ...item,
            canCreate: primary?.canCreate ?? false,
            canEdit: primary?.canEdit ?? false,
            canDelete: primary?.canDelete ?? false,
            forms,
          };
        })
      );
    },
    [canEditSelectedRole]
  );

  const refreshSessionIfNeeded = async (roleId: number) => {
    if (user?.roleId !== roleId) return;

    try {
      const fresh = await authService.getPermissions();
      authStorage.saveSession({
        user: authStorage.getUser()!,
        token: authStorage.getToken()!,
        branches: authStorage.getBranches(),
        selectedBranchId: authStorage.getSelectedBranchId(),
        permissions: fresh.permissions,
        features: fresh.features,
      });
      usePermissionStore.getState().setPermissions(fresh.permissions, user.roleName ?? null, fresh.features);
      await refreshSidebar(roleId);
    } catch {
      showNotification('error', 'Permissions saved but sidebar refresh failed. Please log out and log in again.');
    }
  };

  const handleSave = async () => {
    if (!selectedRoleId || !canEditSelectedRole) return;

    setSaving(true);
    try {
      const saved = await roleService.saveRolePermissions(selectedRoleId, permissions);
      setPermissions(saved);
      await refreshSessionIfNeeded(selectedRoleId);
      showNotification(
        'success',
        user?.roleId === selectedRoleId
          ? 'Permissions saved. Sidebar updated for your account.'
          : 'Permissions saved. Affected users must log in again to see changes.'
      );
    } catch (error) {
      showNotification('error', getApiErrorMessage(error, 'Failed to save permissions.'));
    } finally {
      setSaving(false);
    }
  };

  const expandAll = () => setExpandedGroups(new Set(groupModules.map((g) => g.id)));
  const collapseAll = () => setExpandedGroups(new Set());

  return (
    <div className="p-4 md:p-6 max-w-6xl mx-auto">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">User Roles & Access</h1>
        <p className="text-sm text-gray-600 mt-1">
          Create and manage user roles, then control which menu items appear in the sidebar and what actions users can perform.
        </p>
        {showAssignableHint && (
          <p className="text-sm text-amber-700 bg-amber-50 border border-amber-200 rounded-lg px-3 py-2 mt-3">
            You can only assign permissions that are granted to your own role.
          </p>
        )}
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

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Role selector */}
        <div className="lg:col-span-3">
          <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-4 sticky top-4">
            <label htmlFor="role-select" className="block text-sm font-semibold text-gray-800 mb-2">
              Select Role
            </label>
            <select
              id="role-select"
              value={selectedRoleId ?? ''}
              onChange={(e) => setSelectedRoleId(Number(e.target.value))}
              disabled={loadingRoles}
              className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              {roles.map((role) => (
                <option key={role.id} value={role.id}>
                  {role.name}{!role.isActive ? ' (Inactive)' : ''}
                </option>
              ))}
            </select>
            {selectedRole?.description && (
              <p className="mt-3 text-xs text-gray-500 leading-relaxed">{selectedRole.description}</p>
            )}
            {isSuperAdmin && isProtectedRoleSelected && !isMasterUser && (
              <p className="mt-3 text-xs text-amber-700 bg-amber-50 rounded-lg p-2">
                This role is read-only for your account.
              </p>
            )}
            <div className="mt-4 w-full flex flex-wrap gap-2 justify-end">
              {canCreate && (
                <button
                  type="button"
                  onClick={handleCreateRole}
                  className="rounded-lg bg-blue-600 px-3 py-2 text-xs font-semibold text-white hover:bg-blue-700"
                >
                  New Role
                </button>
              )}
              {canEdit && selectedRole && (
                <button
                  type="button"
                  onClick={handleEditRole}
                  disabled={!canEditSelectedRole}
                  className="rounded-lg border border-gray-300 px-3 py-2 text-xs font-semibold text-gray-700 hover:bg-gray-50 disabled:opacity-50"
                >
                  Edit Role
                </button>
              )}
              {canDelete && selectedRole && (
                <button
                  type="button"
                  onClick={handleDeleteRole}
                  disabled={!canDeleteSelectedRole}
                  className="rounded-lg border border-red-200 px-3 py-2 text-xs font-semibold text-red-600 hover:bg-red-50 disabled:opacity-50"
                >
                  Delete
                </button>
              )}
            </div>
            <div className="mt-4 pt-4 border-t border-gray-100 space-y-2 text-xs text-gray-500">
              <p><span className="font-medium text-gray-700">Menu access</span> — show/hide in sidebar</p>
              <p><span className="font-medium text-gray-700">Create / Edit / Delete</span> — allowed actions (hidden for reports)</p>
            </div>
          </div>
        </div>

        {/* Permissions panel */}
        <div className="lg:col-span-9 space-y-4">
          <div className="flex flex-col sm:flex-row gap-3 sm:items-center sm:justify-between">
            <input
              type="search"
              placeholder="Search module name..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="flex-1 rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            <div className="flex items-center gap-2 flex-shrink-0">
              <button type="button" onClick={expandAll} className="text-xs px-3 py-2 rounded-lg border border-gray-200 text-gray-600 hover:bg-gray-50">
                Expand All
              </button>
              <button type="button" onClick={collapseAll} className="text-xs px-3 py-2 rounded-lg border border-gray-200 text-gray-600 hover:bg-gray-50">
                Collapse All
              </button>
              <button
                type="button"
                onClick={handleSave}
                disabled={saving || loadingPermissions || !selectedRoleId || !canEditSelectedRole}
                className="px-5 py-2 rounded-lg bg-blue-600 text-white text-sm font-medium hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {saving ? 'Saving...' : 'Save Changes'}
              </button>
            </div>
          </div>

          {loadingPermissions ? (
            <div className="bg-white rounded-xl border border-gray-200 p-12 text-center text-sm text-gray-500">
              Loading permissions...
            </div>
          ) : (
            <>
              {/* Standalone modules e.g. Dashboard */}
              {standaloneModules
                .filter((m) => !normalizedSearch || m.moduleName.toLowerCase().includes(normalizedSearch))
                .map((mod) => {
                  const perm = permissionMap.get(mod.id);
                  const hasAccess = perm?.canView ?? false;
                  return (
                    <div key={mod.id} className="bg-white rounded-xl border border-gray-200 shadow-sm px-4 py-3 flex items-center justify-between">
                      <div>
                        <p className="text-sm font-semibold text-gray-900">{mod.moduleName}</p>
                        <p className="text-xs text-gray-400">
                          {isViewOnlyModule(perm) ? 'View only' : 'Standalone module'}
                        </p>
                      </div>
                      <label className="relative inline-flex items-center cursor-pointer">
                        <input
                          type="checkbox"
                          checked={hasAccess}
                          onChange={(e) => setModuleAccess(mod.id, e.target.checked)}
                          disabled={!canEditSelectedRole}
                          className="sr-only peer"
                        />
                        <div className="w-10 h-5 bg-gray-200 rounded-full peer peer-checked:bg-blue-600 peer-disabled:opacity-50 after:content-[''] after:absolute after:top-0.5 after:left-0.5 after:bg-white after:rounded-full after:h-4 after:w-4 after:transition-all peer-checked:after:translate-x-5" />
                      </label>
                    </div>
                  );
                })}

              {/* Group cards */}
              {groupModules.map((group) => (
                <GroupCard
                  key={group.id}
                  group={group}
                  permissionMap={permissionMap}
                  expanded={expandedGroups.has(group.id)}
                  onToggleExpand={() =>
                    setExpandedGroups((prev) => {
                      const next = new Set(prev);
                      if (next.has(group.id)) next.delete(group.id);
                      else next.add(group.id);
                      return next;
                    })
                  }
                  onModuleAccess={setModuleAccess}
                  onFormActionToggle={toggleFormAction}
                  onGroupToggleAll={setGroupToggleAll}
                  canEdit={canEditSelectedRole}
                  searchTerm={normalizedSearch}
                />
              ))}
            </>
          )}
        </div>
      </div>
    </div>
  );
};

export default RolePermissionPage;
