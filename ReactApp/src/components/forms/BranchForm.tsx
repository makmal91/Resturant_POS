import React, { useState } from 'react';
import { FormInput, FormSelect, FormButton } from './index';

interface BranchFormData {
  name: string;
  address: string;
  city: string;
  phone: string;
  taxRate: string;
  status: string;
}

interface BranchFormProps {
  initialData?: Partial<BranchFormData>;
  onSubmit: (data: BranchFormData) => void;
  isLoading?: boolean;
  submitLabel?: string;
}

const BranchForm: React.FC<BranchFormProps> = ({
  initialData = {},
  onSubmit,
  isLoading = false,
  submitLabel = 'Create Branch',
}) => {
  const [formData, setFormData] = useState<BranchFormData>({
    name: initialData.name || '',
    address: initialData.address || '',
    city: initialData.city || '',
    phone: initialData.phone || '',
    taxRate: initialData.taxRate || '',
    status: initialData.status || 'Active',
  });

  const [errors, setErrors] = useState<Partial<BranchFormData>>({});

  const validateForm = (): boolean => {
    const newErrors: Partial<BranchFormData> = {};

    if (!formData.name.trim()) newErrors.name = 'Branch name is required';
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
    <form onSubmit={handleSubmit} className="bg-white p-6 rounded-lg shadow-md max-w-md mx-auto">
      <h2 className="text-2xl font-bold mb-6 text-gray-800">Branch Information</h2>

      <FormInput
        label="Branch Name"
        name="name"
        value={formData.name}
        onChange={handleChange}
        placeholder="Enter branch name"
        required
        error={errors.name}
      />

      <FormInput
        label="Address"
        name="address"
        value={formData.address}
        onChange={handleChange}
        placeholder="Enter street address"
        required
        error={errors.address}
      />

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

      <div className="mt-6 flex gap-3">
        <FormButton type="submit" label={submitLabel} loading={isLoading} variant="primary" />
        <FormButton type="reset" label="Reset" variant="secondary" />
      </div>
    </form>
  );
};

export default BranchForm;
