import React, { useEffect, useMemo, useState } from 'react';
import { FormButton, FormInput, FormSelect, FormTextarea } from './index';
import CodeFieldWithGenerate from './CodeFieldWithGenerate';
import { CODE_MODULES } from '../../services/codeGeneratorService';
import { safeString } from '../../utils/safeValues';
import { useBranchStore } from '../../stores/useBranchStore';

export interface SupplierFormData {
  supplierCode: string;
  name: string;
  contactPerson: string;
  phone: string;
  email: string;
  address: string;
  taxNumber: string;
  status: string;
  branchId: number;
}

interface SupplierFormProps {
  initialData?: Partial<SupplierFormData & { id?: number; isActive?: boolean }> | null;
  onSubmit: (data: SupplierFormData) => void;
  isLoading?: boolean;
  submitLabel?: string;
  lockBranch?: boolean;
}

const DEFAULT_SUPPLIER_FORM_DATA: SupplierFormData = {
  supplierCode: '',
  name: '',
  contactPerson: '',
  phone: '',
  email: '',
  address: '',
  taxNumber: '',
  status: 'Active',
  branchId: 0,
};

const buildSupplierFormData = (
  source?: Partial<SupplierFormData & { isActive?: boolean }> | null
): SupplierFormData => {
  const statusFromActive =
    typeof source?.isActive === 'boolean' ? (source.isActive ? 'Active' : 'Inactive') : null;

  return {
    supplierCode: safeString(source?.supplierCode),
    name: safeString(source?.name),
    contactPerson: safeString(source?.contactPerson),
    phone: safeString(source?.phone),
    email: safeString(source?.email),
    address: safeString(source?.address),
    taxNumber: safeString(source?.taxNumber),
    status: safeString(source?.status, statusFromActive ?? 'Active') || 'Active',
    branchId: Number(source?.branchId ?? 0),
  };
};

const SupplierForm: React.FC<SupplierFormProps> = ({
  initialData,
  onSubmit,
  isLoading = false,
  submitLabel = 'Create Supplier',
  lockBranch = false,
}) => {
  const branches = useBranchStore((state) => state.branches);
  const fetchBranches = useBranchStore((state) => state.fetchBranches);
  const selectedBranchId = useBranchStore((state) => state.selectedBranchId);

  const safeInitialData = useMemo(() => {
    const base = initialData ?? DEFAULT_SUPPLIER_FORM_DATA;
    if (base.branchId && Number(base.branchId) > 0) return base;
    if (selectedBranchId && selectedBranchId > 0) return { ...base, branchId: selectedBranchId };
    return base;
  }, [initialData, selectedBranchId]);

  const [formData, setFormData] = useState<SupplierFormData>(() => buildSupplierFormData(safeInitialData));
  const [errors, setErrors] = useState<Partial<Record<keyof SupplierFormData, string>>>({});

  useEffect(() => {
    void fetchBranches();
  }, [fetchBranches]);

  useEffect(() => {
    setFormData(buildSupplierFormData(safeInitialData));
    setErrors({});
  }, [safeInitialData]);

  const validateForm = () => {
    const nextErrors: Partial<Record<keyof SupplierFormData, string>> = {};

    if (!formData.name.trim()) {
      nextErrors.name = 'Supplier name is required';
    }

    if (formData.branchId <= 0) {
      nextErrors.branchId = 'Branch selection is required';
    }

    if (formData.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      nextErrors.email = 'Please enter a valid email address';
    }

    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: name === 'branchId' ? Number(value || 0) : value,
    }));
    setErrors((prev) => ({ ...prev, [name]: '' }));
  };

  const handleReset = () => {
    setFormData(buildSupplierFormData(safeInitialData));
    setErrors({});
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validateForm()) onSubmit(formData);
  };

  return (
    <form onSubmit={handleSubmit} className="flex h-full min-h-0 flex-col">
      <div className="flex-1 overflow-y-auto min-h-0 px-6 py-4">
        <p className="text-sm text-gray-600 mb-6">Enter supplier details below.</p>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {!lockBranch ? (
            <FormSelect
              label="Branch"
              name="branchId"
              value={String(formData.branchId || '')}
              onChange={handleChange}
              options={[
                { label: 'Select branch', value: '' },
                ...branches.map((branch) => ({ label: branch.name, value: String(branch.id) })),
              ]}
              required
              error={errors.branchId}
            />
          ) : (
            <div>
              <label className="mb-2 block text-sm font-medium text-gray-800">Branch</label>
              <div className="rounded-lg border border-gray-200 bg-gray-50 px-3 py-2 text-sm text-gray-700">
                {branches.find((branch) => branch.id === formData.branchId)?.name ??
                  `Branch #${formData.branchId}`}
              </div>
            </div>
          )}

          {!initialData?.id && (
            <CodeFieldWithGenerate
              label="Supplier Code"
              name="supplierCode"
              value={formData.supplierCode}
              onChange={(supplierCode) => setFormData((prev) => ({ ...prev, supplierCode }))}
              module={CODE_MODULES.Supplier}
              branchId={formData.branchId}
              placeholder="Auto-generated if empty"
            />
          )}

          <FormInput
            label="Supplier Name"
            name="name"
            value={formData.name}
            onChange={handleChange}
            placeholder="Enter supplier name"
            required
            error={errors.name}
          />

          <FormInput
            label="Contact Person"
            name="contactPerson"
            value={formData.contactPerson}
            onChange={handleChange}
            placeholder="Enter contact person"
          />

          <FormInput
            label="Email"
            name="email"
            type="email"
            value={formData.email}
            onChange={handleChange}
            placeholder="Enter email"
            error={errors.email}
          />

          <FormInput
            label="Phone"
            name="phone"
            type="tel"
            value={formData.phone}
            onChange={handleChange}
            placeholder="Enter phone"
          />

          <FormInput
            label="Tax Number"
            name="taxNumber"
            value={formData.taxNumber}
            onChange={handleChange}
            placeholder="Enter tax number"
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
              placeholder="Enter address"
              rows={3}
            />
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

export default SupplierForm;
