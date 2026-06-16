import React, { useEffect, useMemo, useState } from 'react';
import { FormButton, FormInput, FormSelect, FormTextarea, SearchableSelect } from './index';
import CodeFieldWithGenerate from './CodeFieldWithGenerate';
import { CODE_MODULES } from '../../services/codeGeneratorService';
import { safeString } from '../../utils/safeValues';
import { useBranchStore } from '../../stores/useBranchStore';
import { CountryService } from '../../services/apiService';

export interface CustomerFormData {
  customerCode: string;
  name: string;
  phone: string;
  email: string;
  address: string;
  countryId: number;
  cityId: number;
  cnic: string;
  customerType: string;
  creditLimit: string;
  openingBalance: string;
  status: string;
  branchId: number;
}

interface CustomerFormProps {
  initialData?: Partial<CustomerFormData & { id?: number; isActive?: boolean; status?: string; cityName?: string }> | null;
  onSubmit: (data: CustomerFormData) => void;
  isLoading?: boolean;
  submitLabel?: string;
  lockBranch?: boolean;
}

interface SelectOption {
  label: string;
  value: number;
}

const DEFAULT_CUSTOMER_FORM_DATA: CustomerFormData = {
  customerCode: '',
  name: '',
  phone: '',
  email: '',
  address: '',
  countryId: 0,
  cityId: 0,
  cnic: '',
  customerType: 'Retail',
  creditLimit: '0',
  openingBalance: '0',
  status: 'Active',
  branchId: 0,
};

const buildCustomerFormData = (
  source?: Partial<CustomerFormData & { isActive?: boolean }> | null
): CustomerFormData => {
  const statusFromActive =
    typeof source?.isActive === 'boolean' ? (source.isActive ? 'Active' : 'Inactive') : null;

  return {
    customerCode:  safeString(source?.customerCode),
    name:            safeString(source?.name),
    phone:           safeString(source?.phone),
    email:           safeString(source?.email),
    address:         safeString(source?.address),
    countryId:       Number(source?.countryId ?? 0),
    cityId:          Number(source?.cityId ?? 0),
    cnic:            safeString(source?.cnic),
    customerType:    safeString(source?.customerType, 'Retail') || 'Retail',
    creditLimit:     String(source?.creditLimit ?? '0'),
    openingBalance:  String(source?.openingBalance ?? '0'),
    status:          safeString(source?.status, statusFromActive ?? 'Active') || 'Active',
    branchId:        Number(source?.branchId ?? 0),
  };
};

const CustomerForm: React.FC<CustomerFormProps> = ({
  initialData,
  onSubmit,
  isLoading = false,
  submitLabel = 'Create Customer',
  lockBranch = false,
}) => {
  const branches = useBranchStore((state) => state.branches);
  const fetchBranches = useBranchStore((state) => state.fetchBranches);
  const selectedBranchId = useBranchStore((state) => state.selectedBranchId);

  const safeInitialData = useMemo(() => {
    const base = initialData ?? DEFAULT_CUSTOMER_FORM_DATA;
    if (base.branchId && Number(base.branchId) > 0) return base;
    if (selectedBranchId && selectedBranchId > 0) return { ...base, branchId: selectedBranchId };
    return base;
  }, [initialData, selectedBranchId]);

  const [formData, setFormData] = useState<CustomerFormData>(() => buildCustomerFormData(safeInitialData));
  const [errors, setErrors] = useState<Partial<Record<keyof CustomerFormData, string>>>({});
  const [countries, setCountries] = useState<SelectOption[]>([]);
  const [cities, setCities] = useState<SelectOption[]>([]);
  const [isCountriesLoading, setIsCountriesLoading] = useState(false);
  const [isCitiesLoading, setIsCitiesLoading] = useState(false);

  useEffect(() => { void fetchBranches(); }, [fetchBranches]);
  useEffect(() => { setFormData(buildCustomerFormData(safeInitialData)); setErrors({}); }, [safeInitialData]);

  useEffect(() => {
    let cancelled = false;
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
        if (!cancelled) setCountries(options);
      } catch {
        if (!cancelled) setCountries([]);
      } finally {
        if (!cancelled) setIsCountriesLoading(false);
      }
    };
    void loadCountries();
    return () => { cancelled = true; };
  }, []);

  useEffect(() => {
    let cancelled = false;
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
        if (!cancelled) {
          setCities(options);
          setFormData((prev) => {
            if (prev.cityId > 0 && options.some((option) => option.value === prev.cityId)) {
              return prev;
            }
            return { ...prev, cityId: 0 };
          });
        }
      } catch {
        if (!cancelled) setCities([]);
      } finally {
        if (!cancelled) setIsCitiesLoading(false);
      }
    };
    void loadCities();
    return () => { cancelled = true; };
  }, [formData.countryId]);

  const validateForm = (): boolean => {
    const errs: Partial<Record<keyof CustomerFormData, string>> = {};
    if (!formData.name.trim()) errs.name = 'Customer name is required';
    if (formData.branchId <= 0) errs.branchId = 'Branch selection is required';
    if (formData.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email))
      errs.email = 'Please enter a valid email address';
    if (formData.cityId > 0 && formData.countryId <= 0)
      errs.countryId = 'Country is required when city is selected';
    if (isNaN(Number(formData.creditLimit)) || Number(formData.creditLimit) < 0)
      errs.creditLimit = 'Credit limit must be a non-negative number';
    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: name === 'branchId' ? Number(value || 0) : value }));
    setErrors((prev) => ({ ...prev, [name]: '' }));
  };

  const handleLookupChange = (name: string, value: string | number) => {
    const numeric = Number(value);
    setFormData((prev) => ({
      ...prev,
      [name]: numeric,
      ...(name === 'countryId' ? { cityId: 0 } : {}),
    }));
    setErrors((prev) => ({ ...prev, [name]: '' }));
  };

  const handleReset = () => { setFormData(buildCustomerFormData(safeInitialData)); setErrors({}); };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validateForm()) onSubmit(formData);
  };

  return (
    <form onSubmit={handleSubmit} className="flex h-full min-h-0 flex-col">
      <div className="flex-1 overflow-y-auto min-h-0 px-6 py-4">
        <p className="text-sm text-gray-600 mb-6">Enter customer details below.</p>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">

          {!lockBranch ? (
            <FormSelect
              label="Branch"
              name="branchId"
              value={String(formData.branchId || '')}
              onChange={handleChange}
              options={[
                { label: 'Select branch', value: '' },
                ...branches.map((b) => ({ label: b.name, value: String(b.id) })),
              ]}
              required
              error={errors.branchId}
            />
          ) : (
            <div>
              <label className="mb-2 block text-sm font-medium text-gray-800">Branch</label>
              <div className="rounded-lg border border-gray-200 bg-gray-50 px-3 py-2 text-sm text-gray-700">
                {branches.find((b) => b.id === formData.branchId)?.name ?? `Branch #${formData.branchId}`}
              </div>
            </div>
          )}

          {!initialData?.id && (
            <CodeFieldWithGenerate
              label="Customer Code"
              name="customerCode"
              value={formData.customerCode}
              onChange={(customerCode) => setFormData((prev) => ({ ...prev, customerCode }))}
              module={CODE_MODULES.Customer}
              branchId={formData.branchId}
              placeholder="Auto-generated if empty"
            />
          )}

          <FormInput
            label="Full Name"
            name="name"
            value={formData.name}
            onChange={handleChange}
            placeholder="Enter customer name"
            required
            error={errors.name}
          />

          <FormInput
            label="Phone Number"
            name="phone"
            type="tel"
            value={formData.phone}
            onChange={handleChange}
            placeholder="e.g. 03001234567"
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
            label="CNIC"
            name="cnic"
            value={formData.cnic}
            onChange={handleChange}
            placeholder="00000-0000000-0"
          />

          <SearchableSelect
            label="Country"
            name="countryId"
            value={formData.countryId || ''}
            onChange={handleLookupChange}
            placeholder={isCountriesLoading ? 'Loading countries…' : 'Select country'}
            options={countries}
            error={errors.countryId}
            disabled={isCountriesLoading}
            loading={isCountriesLoading}
          />

          <SearchableSelect
            label="City"
            name="cityId"
            value={formData.cityId || ''}
            onChange={handleLookupChange}
            placeholder={
              formData.countryId <= 0
                ? 'Select country first'
                : isCitiesLoading
                  ? 'Loading cities…'
                  : 'Select city'
            }
            options={cities}
            error={errors.cityId}
            disabled={formData.countryId <= 0 || isCitiesLoading}
            loading={isCitiesLoading}
          />

          <FormSelect
            label="Customer Type"
            name="customerType"
            value={formData.customerType}
            onChange={handleChange}
            options={[
              { label: 'Retail',    value: 'Retail' },
              { label: 'Wholesale', value: 'Wholesale' },
              { label: 'VIP',       value: 'VIP' },
            ]}
            required
          />

          <FormInput
            label="Credit Limit"
            name="creditLimit"
            type="number"
            value={formData.creditLimit}
            onChange={handleChange}
            placeholder="0.00"
            error={errors.creditLimit}
          />

          <FormInput
            label="Opening Balance"
            name="openingBalance"
            type="number"
            value={formData.openingBalance}
            onChange={handleChange}
            placeholder="0.00"
          />

          <FormSelect
            label="Status"
            name="status"
            value={formData.status}
            onChange={handleChange}
            options={[
              { label: 'Active',   value: 'Active' },
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

export default CustomerForm;
