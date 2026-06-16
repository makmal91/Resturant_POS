import React, { useEffect, useMemo, useState } from 'react';
import { FormButton, FormInput, FormSelect, FormTextarea } from './index';
import { safeString } from '../../utils/safeValues';

export interface RoleFormData {
  name: string;
  description: string;
  status: string;
}

interface RoleFormProps {
  initialData?: Partial<RoleFormData & { id?: number; isActive?: boolean }> | null;
  onSubmit: (data: RoleFormData) => void;
  isLoading?: boolean;
  submitLabel?: string;
  nameReadOnly?: boolean;
}

const DEFAULT_ROLE_FORM_DATA: RoleFormData = {
  name: '',
  description: '',
  status: 'Active',
};

const buildRoleFormData = (
  source?: Partial<RoleFormData & { isActive?: boolean }> | null,
): RoleFormData => {
  const statusFromActive =
    typeof source?.isActive === 'boolean'
      ? source.isActive
        ? 'Active'
        : 'Inactive'
      : null;

  return {
    name: safeString(source?.name),
    description: safeString(source?.description),
    status: safeString(source?.status, statusFromActive ?? 'Active') || 'Active',
  };
};

const RoleForm: React.FC<RoleFormProps> = ({
  initialData,
  onSubmit,
  isLoading = false,
  submitLabel = 'Create Role',
  nameReadOnly = false,
}) => {
  const safeInitialData = useMemo(() => initialData ?? DEFAULT_ROLE_FORM_DATA, [initialData]);
  const [formData, setFormData] = useState<RoleFormData>(() => buildRoleFormData(safeInitialData));
  const [errors, setErrors] = useState<Partial<Record<keyof RoleFormData, string>>>({});

  useEffect(() => {
    setFormData(buildRoleFormData(safeInitialData));
    setErrors({});
  }, [safeInitialData]);

  const validateForm = () => {
    const nextErrors: Partial<Record<keyof RoleFormData, string>> = {};
    if (!formData.name.trim()) {
      nextErrors.name = 'Role name is required';
    } else if (formData.name.trim().length < 2) {
      nextErrors.name = 'Role name must be at least 2 characters';
    }
    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>,
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    setErrors((prev) => ({ ...prev, [name]: '' }));
  };

  const handleReset = () => {
    setFormData(buildRoleFormData(safeInitialData));
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
      <div className="flex-1 overflow-y-auto min-h-0 px-6 py-4">
        <p className="text-sm text-gray-600 mb-6">
          Define the role name and description. Permissions can be configured after the role is created.
        </p>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <FormInput
            label="Role Name"
            name="name"
            value={formData.name}
            onChange={handleChange}
            placeholder="e.g. Cashier, Store Manager"
            required
            error={errors.name}
            disabled={nameReadOnly}
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
          />

          <div className="md:col-span-2">
            <FormTextarea
              label="Description"
              name="description"
              value={formData.description}
              onChange={handleChange}
              placeholder="Brief description of this role and its responsibilities"
              rows={4}
            />
          </div>
        </div>
      </div>

      <div className="shrink-0 border-t border-gray-200 px-6 py-4 flex justify-end gap-3 bg-gray-50">
        <FormButton type="button" label="Reset" variant="secondary" onClick={handleReset} disabled={isLoading} />
        <FormButton type="submit" label={submitLabel} loading={isLoading} variant="primary" />
      </div>
    </form>
  );
};

export default RoleForm;
