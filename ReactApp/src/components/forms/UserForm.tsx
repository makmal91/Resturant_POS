import React, { useEffect, useMemo, useState } from 'react';
import { FormInput, FormSelect, FormButton } from './index';
import { safeString } from '../../utils/safeValues';
import { RoleListItem } from '../../modules/user/userService';

export interface UserFormData {
  fullName: string;
  username: string;
  email: string;
  phone: string;
  password: string;
  roleId: string;
  status: string;
  branchIds: number[];
}

interface UserFormProps {
  initialData?: Partial<UserFormData & { id?: number; isActive?: boolean }> | null;
  onSubmit: (data: UserFormData) => void;
  branches?: { id: number; name: string }[];
  roles?: RoleListItem[];
  isLoading?: boolean;
  submitLabel?: string;
  isEditMode?: boolean;
}

const DEFAULT_USER_FORM_DATA: UserFormData = {
  fullName: '',
  username: '',
  email: '',
  phone: '',
  password: '',
  roleId: '',
  status: 'Active',
  branchIds: [],
};

const buildUserFormData = (
  source?: Partial<UserFormData & { id?: number; isActive?: boolean }> | null
): UserFormData => {
  const statusFromActive =
    typeof source?.isActive === 'boolean'
      ? source.isActive
        ? 'Active'
        : 'Inactive'
      : null;

  return {
    fullName: safeString(source?.fullName),
    username: safeString(source?.username),
    email: safeString(source?.email),
    phone: safeString(source?.phone),
    password: safeString(source?.password),
    roleId: source?.roleId != null ? String(source.roleId) : '',
    status: safeString(source?.status, statusFromActive ?? 'Active') || 'Active',
    branchIds: Array.isArray(source?.branchIds) ? source.branchIds.map(Number).filter((id) => id > 0) : [],
  };
};

const UserForm: React.FC<UserFormProps> = ({
  initialData,
  onSubmit,
  branches = [],
  roles = [],
  isLoading = false,
  submitLabel = 'Create User',
  isEditMode = false,
}) => {
  const safeInitialData = useMemo(() => initialData ?? DEFAULT_USER_FORM_DATA, [initialData]);
  const [formData, setFormData] = useState<UserFormData>(() => buildUserFormData(safeInitialData));
  const [errors, setErrors] = useState<Partial<Record<keyof UserFormData | 'branchIds', string>>>({});

  useEffect(() => {
    setFormData(buildUserFormData(safeInitialData));
    setErrors({});
  }, [safeInitialData]);

  const validateForm = (): boolean => {
    const nextErrors: Partial<Record<keyof UserFormData | 'branchIds', string>> = {};

    if (!formData.fullName.trim()) nextErrors.fullName = 'Full name is required';
    if (!formData.username.trim()) nextErrors.username = 'Username is required';
    if (!formData.email.trim()) nextErrors.email = 'Email is required';
    if (!isEditMode && !formData.password.trim()) nextErrors.password = 'Password is required';
    if (!formData.roleId) nextErrors.roleId = 'Role is required';
    if (formData.branchIds.length === 0) nextErrors.branchIds = 'At least one branch must be selected';

    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    setErrors((prev) => ({ ...prev, [name]: '' }));
  };

  const toggleBranch = (branchId: number) => {
    setFormData((prev) => {
      const exists = prev.branchIds.includes(branchId);
      const branchIds = exists
        ? prev.branchIds.filter((id) => id !== branchId)
        : [...prev.branchIds, branchId];
      return { ...prev, branchIds };
    });
    setErrors((prev) => ({ ...prev, branchIds: '' }));
  };

  const handleReset = () => {
    setFormData(buildUserFormData(safeInitialData));
    setErrors({});
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validateForm()) {
      onSubmit(formData);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="flex h-full min-h-0 flex-col">
      <div className="flex-1 overflow-y-auto px-6 py-4">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <FormInput
            label="Full Name"
            name="fullName"
            value={formData.fullName}
            onChange={handleChange}
            placeholder="Enter full name"
            required
            error={errors.fullName}
          />

          <FormInput
            label="Username"
            name="username"
            value={formData.username}
            onChange={handleChange}
            placeholder="Enter username"
            required
            error={errors.username}
          />

          <FormInput
            label="Email"
            name="email"
            type="email"
            value={formData.email}
            onChange={handleChange}
            placeholder="Enter email"
            required
            error={errors.email}
          />

          <FormInput
            label="Phone"
            name="phone"
            value={formData.phone}
            onChange={handleChange}
            placeholder="Enter phone number"
            error={errors.phone}
          />

          <FormInput
            label={isEditMode ? 'Password' : 'Password'}
            name="password"
            type="password"
            value={formData.password}
            onChange={handleChange}
            placeholder={isEditMode ? 'Leave blank to keep current password' : 'Enter password'}
            required={!isEditMode}
            error={errors.password}
          />

          <FormSelect
            label="Role"
            name="roleId"
            value={formData.roleId}
            onChange={handleChange}
            options={roles.map((role) => ({ label: role.name, value: String(role.id) }))}
            placeholder="Select role"
            required
            error={errors.roleId}
          />

          <FormSelect
            label="Status"
            name="status"
            value={formData.status}
            onChange={handleChange}
            options={[
              { label: 'Active', value: 'Active' },
              { label: 'Inactive', value: 'Inactive' },
            ]}
            required
          />

          <div className="md:col-span-2">
            <label className="block text-sm font-medium text-gray-800 mb-2">
              Assigned Branches <span className="text-red-500">*</span>
            </label>
            <div className="rounded-lg border border-gray-300 bg-white p-4 max-h-48 overflow-y-auto space-y-2">
              {branches.length === 0 ? (
                <p className="text-sm text-gray-500">No branches available.</p>
              ) : (
                branches.map((branch) => (
                  <label key={branch.id} className="flex items-center gap-2 text-sm text-gray-700">
                    <input
                      type="checkbox"
                      checked={formData.branchIds.includes(branch.id)}
                      onChange={() => toggleBranch(branch.id)}
                      className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                    />
                    {branch.name}
                  </label>
                ))
              )}
            </div>
            {errors.branchIds && <p className="mt-1 text-sm text-red-600">{errors.branchIds}</p>}
          </div>
        </div>
      </div>

      <div className="shrink-0 border-t border-gray-200 bg-white px-6 py-4 flex justify-end gap-3">
        <FormButton type="button" label="Reset" variant="secondary" onClick={handleReset} />
        <FormButton type="submit" label={submitLabel} loading={isLoading} variant="primary" />
      </div>
    </form>
  );
};

export default UserForm;
