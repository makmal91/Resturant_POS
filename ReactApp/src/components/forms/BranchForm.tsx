import React, { useEffect, useMemo, useState } from 'react';
import { FormInput, FormSelect, FormButton } from './index';
import { safeString } from '../../utils/safeValues';
import { BusinessService, CountryService } from '../../services/apiService';

interface BranchFormData {
  name: string;
  code: string;
  address: string;
  phone: string;
  email: string;
  businessId: number;
  countryId: number;
  cityId: number;
  status: string;
}

interface BranchFormProps {
  initialData?: Partial<BranchFormData & { companyId?: number; isActive?: boolean }> | null;
  onSubmit: (data: BranchFormData) => void;
  isLoading?: boolean;
  submitLabel?: string;
}

interface SelectOption {
  label: string;
  value: number;
}

const DEFAULT_BRANCH_FORM_DATA: BranchFormData = {
  name: '',
  code: '',
  address: '',
  phone: '',
  email: '',
  businessId: 1,
  countryId: 0,
  cityId: 0,
  status: 'Active',
};

const buildBranchFormData = (
  source?: Partial<BranchFormData & { companyId?: number; isActive?: boolean }> | null
): BranchFormData => {
  const statusFromActive =
    typeof source?.isActive === 'boolean'
      ? source.isActive
        ? 'Active'
        : 'Inactive'
      : null;

  return {
    name: safeString(source?.name),
    code: safeString(source?.code),
    address: safeString(source?.address),
    phone: safeString(source?.phone),
    email: safeString(source?.email),
    businessId: Number(source?.businessId ?? source?.companyId ?? 1),
    countryId: Number(source?.countryId ?? 0),
    cityId: Number(source?.cityId ?? 0),
    status: safeString(source?.status, statusFromActive ?? 'Active') || 'Active',
  };
};

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
  const [errors, setErrors] = useState<Partial<Record<keyof BranchFormData, string>>>({});
  const [businesses, setBusinesses] = useState<SelectOption[]>([]);
  const [countries, setCountries] = useState<SelectOption[]>([]);
  const [cities, setCities] = useState<SelectOption[]>([]);
  const [isBusinessesLoading, setIsBusinessesLoading] = useState(false);
  const [isCountriesLoading, setIsCountriesLoading] = useState(false);
  const [isCitiesLoading, setIsCitiesLoading] = useState(false);

  useEffect(() => {
    setFormData(buildBranchFormData(safeInitialData));
    setErrors({});
  }, [safeInitialData]);

  useEffect(() => {
    let isCancelled = false;

    const loadBusinesses = async () => {
      setIsBusinessesLoading(true);
      try {
        const response = await BusinessService.getAll({ page: 1, pageSize: 100 });
        const rows = Array.isArray(response?.data?.data) ? response.data.data : [];
        const options = rows
          .map((item: { id?: unknown; name?: unknown }) => ({
            value: Number(item?.id ?? 0),
            label: String(item?.name ?? ''),
          }))
          .filter((item: SelectOption) => item.value > 0 && item.label);

        if (!isCancelled) {
          setBusinesses(options);
        }
      } catch {
        if (!isCancelled) {
          setBusinesses([]);
        }
      } finally {
        if (!isCancelled) {
          setIsBusinessesLoading(false);
        }
      }
    };

    loadBusinesses();
    return () => {
      isCancelled = true;
    };
  }, []);

  useEffect(() => {
    let isCancelled = false;

    const loadCountries = async () => {
      setIsCountriesLoading(true);
      try {
        const response = await CountryService.getAll();
        const rows = Array.isArray(response?.data) ? response.data : [];
        const options = rows
          .map((item: { id?: unknown; name?: unknown }) => ({
            value: Number(item?.id ?? 0),
            label: String(item?.name ?? ''),
          }))
          .filter((item: SelectOption) => item.value > 0 && item.label);

        if (!isCancelled) {
          setCountries(options);
        }
      } catch {
        if (!isCancelled) {
          setCountries([]);
        }
      } finally {
        if (!isCancelled) {
          setIsCountriesLoading(false);
        }
      }
    };

    loadCountries();
    return () => {
      isCancelled = true;
    };
  }, []);

  useEffect(() => {
    let isCancelled = false;

    const loadCities = async () => {
      if (formData.countryId <= 0) {
        setCities([]);
        return;
      }

      setIsCitiesLoading(true);
      try {
        const response = await CountryService.getCitiesByCountry(formData.countryId);
        const rows = Array.isArray(response?.data) ? response.data : [];
        const options = rows
          .map((item: { id?: unknown; name?: unknown }) => ({
            value: Number(item?.id ?? 0),
            label: String(item?.name ?? ''),
          }))
          .filter((item: SelectOption) => item.value > 0 && item.label);

        if (!isCancelled) {
          setCities(options);
          setFormData((prev) => {
            if (prev.cityId > 0 && options.some((option: SelectOption) => option.value === prev.cityId)) {
              return prev;
            }
            return { ...prev, cityId: 0 };
          });
        }
      } catch {
        if (!isCancelled) {
          setCities([]);
        }
      } finally {
        if (!isCancelled) {
          setIsCitiesLoading(false);
        }
      }
    };

    loadCities();
    return () => {
      isCancelled = true;
    };
  }, [formData.countryId]);

  const validateForm = (): boolean => {
    const newErrors: Partial<Record<keyof BranchFormData, string>> = {};

    if (!formData.name.trim()) newErrors.name = 'Branch name is required';
    if (!formData.code.trim()) newErrors.code = 'Branch code is required';
    if (!formData.address.trim()) newErrors.address = 'Address is required';
    if (!formData.phone.trim()) newErrors.phone = 'Phone is required';
    if (formData.businessId <= 0) newErrors.businessId = 'Business is required';
    if (formData.countryId <= 0) newErrors.countryId = 'Country is required';
    if (formData.cityId <= 0) newErrors.cityId = 'City is required';

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    const numericFields = new Set(['businessId', 'countryId', 'cityId']);

    setFormData((prev) => ({
      ...prev,
      [name]: numericFields.has(name) ? Number(value) : value,
      ...(name === 'countryId' ? { cityId: 0 } : {}),
    }));
    setErrors((prev) => ({ ...prev, [name]: '' }));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validateForm()) {
      onSubmit(formData);
    }
  };

  const handleReset = () => {
    setFormData(buildBranchFormData(safeInitialData));
    setErrors({});
  };

  return (
    <form onSubmit={handleSubmit} className="flex h-full min-h-0 flex-col">
      <div className="flex-1 overflow-y-auto min-h-0 px-6 py-4">
        <p className="text-sm text-gray-600 mb-6">Enter branch details below.</p>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
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
            label="Branch Code"
            name="code"
            value={formData.code}
            onChange={handleChange}
            placeholder="Enter unique branch code"
            required
            error={errors.code}
          />

          <FormSelect
            label="Business"
            name="businessId"
            value={formData.businessId || ''}
            onChange={handleChange}
            placeholder={isBusinessesLoading ? 'Loading businesses...' : 'Select business'}
            options={businesses}
            required
            error={errors.businessId}
            disabled={isBusinessesLoading}
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
            label="Email"
            name="email"
            type="email"
            value={formData.email}
            onChange={handleChange}
            placeholder="Enter email address"
            error={errors.email}
          />

          <FormSelect
            label="Country"
            name="countryId"
            value={formData.countryId || ''}
            onChange={handleChange}
            placeholder={isCountriesLoading ? 'Loading countries...' : 'Select country'}
            options={countries}
            required
            error={errors.countryId}
            disabled={isCountriesLoading}
          />

          <FormSelect
            label="City"
            name="cityId"
            value={formData.cityId || ''}
            onChange={handleChange}
            placeholder={
              formData.countryId <= 0
                ? 'Select country first'
                : isCitiesLoading
                  ? 'Loading cities...'
                  : 'Select city'
            }
            options={cities}
            required
            error={errors.cityId}
            disabled={formData.countryId <= 0 || isCitiesLoading}
          />

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
        </div>
      </div>

      <div className="shrink-0 border-t border-gray-200 bg-white px-6 py-4 flex justify-end gap-3">
        <FormButton type="button" label="Reset" variant="secondary" onClick={handleReset} />
        <FormButton type="submit" label={submitLabel} loading={isLoading} variant="primary" />
      </div>
    </form>
  );
};

export default BranchForm;
