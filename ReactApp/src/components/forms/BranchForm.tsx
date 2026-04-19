import React, { useEffect, useMemo, useState } from 'react';
import { FormInput, FormSelect, FormButton } from './index';
import { safeString } from '../../utils/safeValues';

interface BranchFormData {
  name: string;
  code: string;
  address: string;
  city: string;
  phone: string;
  taxRate: string;
  companyId: number;
  status: string;
}

interface BranchFormProps {
  initialData?: Partial<BranchFormData> | null;
  onSubmit: (data: BranchFormData) => void;
  isLoading?: boolean;
  submitLabel?: string;
}

const DEFAULT_BRANCH_FORM_DATA: BranchFormData = {
  name: '',
  code: '',
  address: '',
  city: '',
  phone: '',
  taxRate: '',
  companyId: 1,
  status: 'Active',
};

const buildBranchFormData = (source?: Partial<BranchFormData> | null): BranchFormData => ({
  name: safeString(source?.name),
  code: safeString(source?.code),
  address: safeString(source?.address),
  city: safeString(source?.city),
  phone: safeString(source?.phone),
  taxRate: safeString(source?.taxRate),
  companyId: Number(source?.companyId ?? 1),
  status: safeString(source?.status, 'Active') || 'Active',
});

const BranchForm: React.FC<BranchFormProps> = ({
  initialData,
  onSubmit,
  isLoading = false,
  submitLabel = 'Create Branch',
}) => {
  const safeInitialData = useMemo(
    () => initialData ?? DEFAULT_BRANCH_FORM_DATA,
    [initialData]
  );

  const [formData, setFormData] = useState<BranchFormData>(() =>
    buildBranchFormData(safeInitialData)
  );

  const [errors, setErrors] = useState<Partial<BranchFormData>>({});

  useEffect(() => {
    setFormData(buildBranchFormData(safeInitialData));
    setErrors({});
  }, [safeInitialData]);

  const validateForm = (): boolean => {
    const newErrors: Partial<BranchFormData> = {};

    if (!formData.name.trim()) newErrors.name = 'Branch name is required';
    if (!formData.code.trim()) newErrors.code = 'Branch code is required';
    if (!formData.address.trim()) newErrors.address = 'Address is required';
    if (!formData.city.trim()) newErrors.city = 'City is required';
    if (!formData.phone.trim()) newErrors.phone = 'Phone is required';
    if (!formData.taxRate) newErrors.taxRate = 'Tax rate is required';
    if (parseFloat(formData.taxRate) < 0 || parseFloat(formData.taxRate) > 100) {
      newErrors.taxRate = 'Tax rate must be between 0 and 100';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    setErrors((prev) => ({ ...prev, [name]: '' }));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validateForm()) {
      onSubmit(formData);
    }
  };

  return (
    <div className="max-w-4xl mx-auto">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Branch Management</h1>
        <p className="text-gray-600">Create and manage restaurant branches</p>
      </div>

      <div className="bg-white rounded-lg shadow-sm border border-gray-200">
        <div className="px-6 py-4 border-b border-gray-200">
          <h2 className="text-xl font-semibold text-gray-900">Branch Information</h2>
          <p className="text-sm text-gray-600 mt-1">Enter the details for the new branch</p>
        </div>

        <form onSubmit={handleSubmit} className="p-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="md:col-span-2">
              <FormInput
                label="Branch Name"
                name="name"
                value={formData.name}
                onChange={handleChange}
                placeholder="Enter branch name"
                required
                error={errors.name}
              />
            </div>

            <div className="md:col-span-2">
              <FormInput
                label="Branch Code"
                name="code"
                value={formData.code}
                onChange={handleChange}
                placeholder="Enter unique branch code"
                required
                error={errors.code}
              />
            </div>

            <div className="md:col-span-2">
              <FormInput
                label="Address"
                name="address"
                value={formData.address}
                onChange={handleChange}
                placeholder="Enter street address"
                required
                error={errors.address}
              />
            </div>

            <FormInput
              label="City"
              name="city"
              value={formData.city}
              onChange={handleChange}
              placeholder="Enter city"
              required
              error={errors.city}
            />

            <FormInput
              label="Phone"
              name="phone"
              type="tel"
              value={formData.phone}
              onChange={handleChange}
              placeholder="Enter phone number"
              required
              error={errors.phone}
            />

            <FormInput
              label="Tax Rate (%)"
              name="taxRate"
              type="number"
              value={formData.taxRate}
              onChange={handleChange}
              placeholder="Enter tax rate"
              required
              min="0"
              max="100"
              step="0.01"
              error={errors.taxRate}
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
          </div>

          <div className="mt-8 flex justify-end space-x-3">
            <FormButton type="reset" label="Reset" variant="secondary" />
            <FormButton type="submit" label={submitLabel} loading={isLoading} variant="primary" />
          </div>
        </form>
      </div>
    </div>
  );
};

export default BranchForm;
