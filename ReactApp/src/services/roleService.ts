import apiClient, { getApiErrorMessage } from '../services/api';

export interface RoleListItem {
  id: number;
  name: string;
  description: string;
  isActive: boolean;
}

export interface ModuleListItem {
  id: number;
  moduleName: string;
  moduleKey: string;
  parentModuleId: number | null;
  displayOrder: number;
  isActive: boolean;
  children: ModuleListItem[];
}

export interface RolePermissionItem {
  moduleId: number;
  moduleName: string;
  moduleKey: string;
  parentModuleId: number | null;
  displayOrder: number;
  canView: boolean;
  canCreate: boolean;
  canEdit: boolean;
  canDelete: boolean;
  canExport: boolean;
  canUpload: boolean;
}

export type PermissionField =
  | 'canView'
  | 'canCreate'
  | 'canEdit'
  | 'canDelete'
  | 'canExport'
  | 'canUpload';

const toRecord = (value: unknown): Record<string, unknown> =>
  typeof value === 'object' && value !== null ? (value as Record<string, unknown>) : {};

const normalizeRole = (row: Record<string, unknown>): RoleListItem => ({
  id: Number(row.id ?? row.Id ?? 0),
  name: String(row.name ?? row.Name ?? ''),
  description: String(row.description ?? row.Description ?? ''),
  isActive: Boolean(row.isActive ?? row.IsActive ?? true),
});

const normalizeModule = (row: Record<string, unknown>): ModuleListItem => ({
  id: Number(row.id ?? row.Id ?? 0),
  moduleName: String(row.moduleName ?? row.ModuleName ?? ''),
  moduleKey: String(row.moduleKey ?? row.ModuleKey ?? ''),
  parentModuleId:
    row.parentModuleId === null || row.ParentModuleId === null
      ? null
      : Number(row.parentModuleId ?? row.ParentModuleId ?? 0),
  displayOrder: Number(row.displayOrder ?? row.DisplayOrder ?? 0),
  isActive: Boolean(row.isActive ?? row.IsActive ?? true),
  children: Array.isArray(row.children ?? row.Children)
    ? (row.children ?? row.Children).map((child) => normalizeModule(toRecord(child)))
    : [],
});

const normalizePermission = (row: Record<string, unknown>): RolePermissionItem => ({
  moduleId: Number(row.moduleId ?? row.ModuleId ?? 0),
  moduleName: String(row.moduleName ?? row.ModuleName ?? ''),
  moduleKey: String(row.moduleKey ?? row.ModuleKey ?? ''),
  parentModuleId:
    row.parentModuleId === null || row.ParentModuleId === null
      ? null
      : Number(row.parentModuleId ?? row.ParentModuleId ?? 0),
  displayOrder: Number(row.displayOrder ?? row.DisplayOrder ?? 0),
  canView: Boolean(row.canView ?? row.CanView),
  canCreate: Boolean(row.canCreate ?? row.CanCreate),
  canEdit: Boolean(row.canEdit ?? row.CanEdit),
  canDelete: Boolean(row.canDelete ?? row.CanDelete),
  canExport: Boolean(row.canExport ?? row.CanExport),
  canUpload: Boolean(row.canUpload ?? row.CanUpload),
});

export const roleService = {
  async getRoles(): Promise<RoleListItem[]> {
    const response = await apiClient.get('/roles');
    const rows = Array.isArray(response.data) ? response.data : [];
    return rows.map((row) => normalizeRole(toRecord(row)));
  },

  async getModules(): Promise<ModuleListItem[]> {
    const response = await apiClient.get('/modules');
    const rows = Array.isArray(response.data) ? response.data : [];
    return rows.map((row) => normalizeModule(toRecord(row)));
  },

  async getRolePermissions(roleId: number): Promise<RolePermissionItem[]> {
    const response = await apiClient.get(`/role-permissions/${roleId}`);
    const rows = Array.isArray(response.data) ? response.data : [];
    return rows.map((row) => normalizePermission(toRecord(row)));
  },

  async saveRolePermissions(
    roleId: number,
    permissions: RolePermissionItem[]
  ): Promise<RolePermissionItem[]> {
    const response = await apiClient.post('/role-permissions', {
      roleId,
      permissions: permissions.map((item) => ({
        moduleId: item.moduleId,
        canView: item.canView,
        canCreate: item.canCreate,
        canEdit: item.canEdit,
        canDelete: item.canDelete,
        canExport: item.canExport,
        canUpload: item.canUpload,
      })),
    });
    const rows = Array.isArray(response.data) ? response.data : [];
    return rows.map((row) => normalizePermission(toRecord(row)));
  },
};

export { getApiErrorMessage };
