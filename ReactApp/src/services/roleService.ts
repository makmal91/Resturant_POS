import apiClient, { getApiErrorMessage } from '../services/api';

export interface RoleListItem {
  id: number;
  name: string;
  description: string;
  isActive: boolean;
}

export interface ModuleFormItem {
  id: number;
  moduleId: number;
  formName: string;
  formCode: string;
  route: string | null;
  sortOrder: number;
  isActive: boolean;
}

export interface ModuleListItem {
  id: number;
  moduleName: string;
  moduleKey: string;
  parentModuleId: number | null;
  route: string | null;
  icon: string | null;
  displayOrder: number;
  isActive: boolean;
  children: ModuleListItem[];
  forms: ModuleFormItem[];
}

export interface FormPermissionItem {
  formId: number;
  moduleId: number;
  formName: string;
  formCode: string;
  canView: boolean;
  canCreate: boolean;
  canEdit: boolean;
  canDelete: boolean;
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
  forms: FormPermissionItem[];
}

export type PermissionField =
  | 'canView'
  | 'canCreate'
  | 'canEdit'
  | 'canDelete'
  | 'canExport'
  | 'canUpload';

export type FormPermissionField = 'canView' | 'canCreate' | 'canEdit' | 'canDelete';

const toRecord = (value: unknown): Record<string, unknown> =>
  typeof value === 'object' && value !== null ? (value as Record<string, unknown>) : {};

const normalizeForm = (row: Record<string, unknown>): ModuleFormItem => ({
  id: Number(row.id ?? row.Id ?? 0),
  moduleId: Number(row.moduleId ?? row.ModuleId ?? 0),
  formName: String(row.formName ?? row.FormName ?? ''),
  formCode: String(row.formCode ?? row.FormCode ?? ''),
  route: row.route === null || row.Route === null ? null : String(row.route ?? row.Route ?? ''),
  sortOrder: Number(row.sortOrder ?? row.SortOrder ?? 0),
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
  route: row.route === null || row.Route === null ? null : String(row.route ?? row.Route ?? ''),
  icon: row.icon === null || row.Icon === null ? null : String(row.icon ?? row.Icon ?? ''),
  displayOrder: Number(row.displayOrder ?? row.DisplayOrder ?? 0),
  isActive: Boolean(row.isActive ?? row.IsActive ?? true),
  children: Array.isArray(row.children ?? row.Children)
    ? ((row.children ?? row.Children) as unknown[]).map((child) => normalizeModule(toRecord(child)))
    : [],
  forms: Array.isArray(row.forms ?? row.Forms)
    ? ((row.forms ?? row.Forms) as unknown[]).map((form) => normalizeForm(toRecord(form)))
    : [],
});

const normalizeFormPermission = (row: Record<string, unknown>): FormPermissionItem => ({
  formId: Number(row.formId ?? row.FormId ?? 0),
  moduleId: Number(row.moduleId ?? row.ModuleId ?? 0),
  formName: String(row.formName ?? row.FormName ?? ''),
  formCode: String(row.formCode ?? row.FormCode ?? ''),
  canView: Boolean(row.canView ?? row.CanView),
  canCreate: Boolean(row.canCreate ?? row.CanCreate),
  canEdit: Boolean(row.canEdit ?? row.CanEdit),
  canDelete: Boolean(row.canDelete ?? row.CanDelete),
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
  forms: Array.isArray(row.forms ?? row.Forms)
    ? ((row.forms ?? row.Forms) as unknown[]).map((form) => normalizeFormPermission(toRecord(form)))
    : [],
});

export const roleService = {
  async getRoles(): Promise<RoleListItem[]> {
    const response = await apiClient.get('/roles');
    const rows = Array.isArray(response.data) ? response.data : [];
    return rows.map((row) => ({
      id: Number(toRecord(row).id ?? toRecord(row).Id ?? 0),
      name: String(toRecord(row).name ?? toRecord(row).Name ?? ''),
      description: String(toRecord(row).description ?? toRecord(row).Description ?? ''),
      isActive: Boolean(toRecord(row).isActive ?? toRecord(row).IsActive ?? true),
    }));
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
    const formPermissions = permissions.flatMap((item) =>
      item.forms.map((form) => ({
        formId: form.formId,
        canView: form.canView,
        canCreate: form.canCreate,
        canEdit: form.canEdit,
        canDelete: form.canDelete,
      }))
    );

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
      formPermissions,
    });
    const rows = Array.isArray(response.data) ? response.data : [];
    return rows.map((row) => normalizePermission(toRecord(row)));
  },
};

export { getApiErrorMessage };
