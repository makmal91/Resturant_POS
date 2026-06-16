import React, { useEffect, useMemo, useState } from 'react';
import { FormButton, FormInput, FormSelect, FormTextarea } from './index';
import { safeString } from '../../utils/safeValues';
import { useFormBranchId } from '../../hooks/useFormBranchId';

export interface WarehouseFormData {
  name: string;
  code: string;
  address: string;
  status: string;
  branchId: number;
}

interface WarehouseFormProps {
  initialData?: Partial<WarehouseFormData & { id?: number; isActive?: boolean }> | null;
  onSubmit: (data: WarehouseFormData) => void;
  isLoading?: boolean;
  submitLabel?: string;
}

const DEFAULT_WAREHOUSE_FORM_DATA: WarehouseFormData = {
  name: '',
  code: '',
  address: '',
  status: 'Active',
  branchId: 0,
};

const buildWarehouseFormData = (
  source?: Partial<WarehouseFormData & { isActive?: boolean }> | null
): WarehouseFormData => {
  const statusFromActive =
    typeof source?.isActive === 'boolean' ? (source.isActive ? 'Active' : 'Inactive') : null;

  return {
    name: safeString(source?.name),
    code: safeString(source?.code),
    address: safeString(source?.address),
    status: safeString(source?.status, statusFromActive ?? 'Active') || 'Active',
    branchId: Number(source?.branchId ?? 0),
  };
};

const WarehouseForm: React.FC<WarehouseFormProps> = ({
  initialData,
  onSubmit,
  isLoading = false,
  submitLabel = 'Create Warehouse',
}) => {
  const { branchId: resolvedBranchId, branchError } = useFormBranchId(initialData?.branchId);

  const safeInitialData = useMemo(() => {
    const base = initialData ?? DEFAULT_WAREHOUSE_FORM_DATA;
    if (resolvedBranchId > 0) {
      return { ...base, branchId: resolvedBranchId };
    }
    return base;
  }, [initialData, resolvedBranchId]);

  const [formData, setFormData] = useState<WarehouseFormData>(() => buildWarehouseFormData(safeInitialData));
  const [errors, setErrors] = useState<Partial<Record<keyof WarehouseFormData, string>>>({});

  useEffect(() => {
    setFormData(buildWarehouseFormData(safeInitialData));
    setErrors({});
  }, [safeInitialData]);

  useEffect(() => {
    if (resolvedBranchId > 0) {
      setFormData((prev) =>
        prev.branchId === resolvedBranchId ? prev : { ...prev, branchId: resolvedBranchId },
      );
    }
  }, [resolvedBranchId]);

  const validateForm = () => {
    const nextErrors: Partial<Record<keyof WarehouseFormData, string>> = {};
    if (!formData.name.trim()) nextErrors.name = 'Warehouse name is required';
    if (resolvedBranchId <= 0) nextErrors.branchId = branchError ?? 'Branch is required';
    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));
    setErrors((prev) => ({ ...prev, [name]: '' }));
  };

  const handleReset = () => {
    setFormData(buildWarehouseFormData(safeInitialData));
    setErrors({});
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validateForm()) onSubmit({ ...formData, branchId: resolvedBranchId });
  };

  return (
    <form onSubmit={handleSubmit} className="flex h-full min-h-0 flex-col">
      <div className="min-h-0 flex-1 overflow-y-auto px-6 py-4">
        <p className="mb-6 text-sm text-gray-600">Enter warehouse details below.</p>

        <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
          {errors.branchId && (
            <p className="md:col-span-2 -mt-2 text-sm text-red-600">{errors.branchId}</p>
          )}

          <FormInput
            label="Warehouse Name"
            name="name"
            value={formData.name}
            onChange={handleChange}
            placeholder="e.g. Main Warehouse"
            required
            error={errors.name}
          />

          <FormInput
            label="Code"
            name="code"
            value={formData.code}
            onChange={handleChange}
            placeholder="e.g. WH-001"
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
            <FormTextarea
              label="Address"
              name="address"
              value={formData.address}
              onChange={handleChange}
              placeholder="Enter warehouse address (optional)"
              rows={3}
            />
          </div>
        </div>
      </div>

      <div className="flex shrink-0 justify-end gap-3 border-t border-gray-200 bg-white px-6 py-4">
        <FormButton type="button" label="Reset" variant="secondary" onClick={handleReset} />
        <FormButton type="submit" label={submitLabel} loading={isLoading} variant="primary" />
      </div>
    </form>
  );
};

export default WarehouseForm;
