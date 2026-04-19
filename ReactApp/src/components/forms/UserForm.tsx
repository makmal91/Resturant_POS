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
    <div className="max-w-3xl mx-auto">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">User Management</h1>
        <p className="text-gray-600">Manage staff and user accounts</p>
      </div>

      <div className="bg-white rounded-lg shadow-sm border border-gray-200">
        <div className="px-6 py-4 border-b border-gray-200">
          <h2 className="text-xl font-semibold text-gray-900">
            {initialData.name ? 'Edit User' : 'Add New User'}
          </h2>
          <p className="text-sm text-gray-600 mt-1">
            {initialData.name ? 'Update user information' : 'Create a new staff member account'}
          </p>
        </div>

        <form onSubmit={handleSubmit} className="p-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {/* Personal Information Section */}
            <div className="md:col-span-2">
              <h3 className="text-base font-semibold text-gray-900 mb-4">Personal Information</h3>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
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
              </div>
            </div>

            {/* Assignment Section */}
            <div className="md:col-span-2">
              <h3 className="text-base font-semibold text-gray-900 mb-4">Assignment</h3>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
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
              </div>
            </div>

            {/* Compensation Section */}
            <div className="md:col-span-2">
              <h3 className="text-base font-semibold text-gray-900 mb-4">Compensation</h3>
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
            </div>
          </div>

          {/* Action Buttons */}
          <div className="mt-8 flex justify-end space-x-3">
            <FormButton type="reset" label="Clear" variant="secondary" />
            <FormButton type="submit" label={submitLabel} loading={isLoading} variant="primary" />
          </div>
        </form>
      </div>
    </div>
  );
};

export default UserForm;
