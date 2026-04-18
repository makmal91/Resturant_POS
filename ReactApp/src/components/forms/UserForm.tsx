import React, { useState } from 'react';
import { FormInput, FormSelect, FormButton } from './index';

interface UserFormData {
  name: string;
  role: string;
  branch: string;
  salary: string;
  shift: string;
}

interface UserFormProps {
  initialData?: Partial<UserFormData>;
  onSubmit: (data: UserFormData) => void;
  branches?: { id: string; name: string }[];
  isLoading?: boolean;
  submitLabel?: string;
}

const UserForm: React.FC<UserFormProps> = ({
  initialData = {},
  onSubmit,
  branches = [],
  isLoading = false,
  submitLabel = 'Create User',
}) => {
  const [formData, setFormData] = useState<UserFormData>({
    name: initialData.name || '',
    role: initialData.role || '',
    branch: initialData.branch || '',
    salary: initialData.salary || '',
    shift: initialData.shift || 'Morning',
  });

  const [errors, setErrors] = useState<Partial<UserFormData>>({});

  const validateForm = (): boolean => {
    const newErrors: Partial<UserFormData> = {};

    if (!formData.name.trim()) newErrors.name = 'Name is required';
    if (!formData.role) newErrors.role = 'Role is required';
    if (!formData.branch) newErrors.branch = 'Branch is required';
    if (!formData.salary) newErrors.salary = 'Salary is required';
    if (parseFloat(formData.salary) < 0) newErrors.salary = 'Salary must be a positive number';

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
      <h2 className="text-2xl font-bold mb-6 text-gray-800">User Information</h2>

      <FormInput
        label="Full Name"
        name="name"
        value={formData.name}
        onChange={handleChange}
        placeholder="Enter full name"
        required
        error={errors.name}
      />

      <FormSelect
        label="Role"
        name="role"
        value={formData.role}
        onChange={handleChange}
        options={[
          { label: 'Manager', value: 'Manager' },
          { label: 'Cashier', value: 'Cashier' },
          { label: 'Chef', value: 'Chef' },
          { label: 'Waiter', value: 'Waiter' },
          { label: 'Admin', value: 'Admin' },
        ]}
        placeholder="Select role"
        required
        error={errors.role}
      />

      <FormSelect
        label="Assigned Branch"
        name="branch"
        value={formData.branch}
        onChange={handleChange}
        options={branches.map((branch) => ({ label: branch.name, value: branch.id }))}
        placeholder="Select branch"
        required
        error={errors.branch}
      />

      <FormInput
        label="Monthly Salary"
        name="salary"
        type="number"
        value={formData.salary}
        onChange={handleChange}
        placeholder="Enter salary"
        required
        min="0"
        step="0.01"
        error={errors.salary}
      />

      <FormSelect
        label="Shift"
        name="shift"
        value={formData.shift}
        onChange={handleChange}
        options={[
          { label: 'Morning', value: 'Morning' },
          { label: 'Afternoon', value: 'Afternoon' },
          { label: 'Evening', value: 'Evening' },
          { label: 'Night', value: 'Night' },
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

export default UserForm;
