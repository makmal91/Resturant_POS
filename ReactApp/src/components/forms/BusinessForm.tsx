import React, { useEffect, useMemo, useRef, useState } from 'react';
import { FormButton, FormInput, FormSelect, FormTextarea } from './index';
import { safeString } from '../../utils/safeValues';
import { BusinessService } from '../../services/apiService';

interface BusinessFormData {
  name: string;
  legalName: string;
  phone: string;
  email: string;
  address: string;
  taxNumber: string;
  currency: string;
  timeZone: string;
  status: string;
}

interface BusinessFormProps {
  initialData?: Partial<BusinessFormData & { id?: number; isActive?: boolean; hasLogo?: boolean }> | null;
  onSubmit: (data: BusinessFormData & { logoFile?: File | null; removeLogo?: boolean }) => void;
  isLoading?: boolean;
  submitLabel?: string;
}

const DEFAULT_BUSINESS_FORM_DATA: BusinessFormData = {
  name: '',
  legalName: '',
  phone: '',
  email: '',
  address: '',
  taxNumber: '',
  currency: 'USD',
  timeZone: 'UTC',
  status: 'Active',
};

const buildBusinessFormData = (
  source?: Partial<BusinessFormData & { isActive?: boolean }> | null
): BusinessFormData => {
  const statusFromActive =
    typeof source?.isActive === 'boolean'
      ? source.isActive
        ? 'Active'
        : 'Inactive'
      : null;

  return {
    name: safeString(source?.name),
    legalName: safeString(source?.legalName),
    phone: safeString(source?.phone),
    email: safeString(source?.email),
    address: safeString(source?.address),
    taxNumber: safeString(source?.taxNumber),
    currency: safeString(source?.currency, 'USD') || 'USD',
    timeZone: safeString(source?.timeZone, 'UTC') || 'UTC',
    status: safeString(source?.status, statusFromActive ?? 'Active') || 'Active',
  };
};

const BusinessForm: React.FC<BusinessFormProps> = ({
  initialData,
  onSubmit,
  isLoading = false,
  submitLabel = 'Create Business',
}) => {
  const safeInitialData = useMemo(
    () => initialData ?? DEFAULT_BUSINESS_FORM_DATA,
    [initialData]
  );

  const [formData, setFormData] = useState<BusinessFormData>(() =>
    buildBusinessFormData(safeInitialData)
  );
  const [errors, setErrors] = useState<Partial<Record<keyof BusinessFormData | 'logoFile', string>>>({});
  const [logoFile, setLogoFile] = useState<File | null>(null);
  const [logoPreviewUrl, setLogoPreviewUrl] = useState<string | null>(null);
  const [existingLogoUrl, setExistingLogoUrl] = useState<string | null>(null);
  const [removeLogo, setRemoveLogo] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    setFormData(buildBusinessFormData(safeInitialData));
    setErrors({});
    setLogoFile(null);
    setRemoveLogo(false);

    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }

    const businessId = Number(safeInitialData?.id ?? 0);
    const hasLogo = Boolean(safeInitialData?.hasLogo);

    if (businessId > 0 && hasLogo) {
      setExistingLogoUrl(BusinessService.getLogoUrl(businessId));
    } else {
      setExistingLogoUrl(null);
    }
  }, [safeInitialData]);

  useEffect(() => {
    if (logoFile) {
      const objectUrl = URL.createObjectURL(logoFile);
      setLogoPreviewUrl(objectUrl);
      return () => URL.revokeObjectURL(objectUrl);
    }

    setLogoPreviewUrl(null);
    return undefined;
  }, [logoFile]);

  const previewSource = logoPreviewUrl ?? (removeLogo ? null : existingLogoUrl);

  const validateForm = () => {
    const nextErrors: Partial<Record<keyof BusinessFormData | 'logoFile', string>> = {};

    if (!formData.name.trim()) {
      nextErrors.name = 'Business name is required';
    }

    if (!formData.legalName.trim()) {
      nextErrors.legalName = 'Legal name is required';
    }

    if (!formData.timeZone.trim()) {
      nextErrors.timeZone = 'Time zone is required';
    }

    if (formData.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      nextErrors.email = 'Please enter a valid email address';
    }

    if (logoFile && !logoFile.type.startsWith('image/')) {
      nextErrors.logoFile = 'Logo must be an image file';
    }

    if (logoFile && logoFile.size > 5 * 1024 * 1024) {
      nextErrors.logoFile = 'Logo must be 5 MB or smaller';
    }

    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    setErrors((prev) => ({ ...prev, [name]: '' }));
  };

  const handleLogoChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0] ?? null;
    setLogoFile(file);
    setRemoveLogo(false);
    setErrors((prev) => ({ ...prev, logoFile: '' }));
  };

  const handleRemoveLogo = () => {
    setLogoFile(null);
    setRemoveLogo(true);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const handleReset = () => {
    setFormData(buildBusinessFormData(safeInitialData));
    setErrors({});
    setLogoFile(null);
    setRemoveLogo(false);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }

    const businessId = Number(safeInitialData?.id ?? 0);
    const hasLogo = Boolean(safeInitialData?.hasLogo);
    setExistingLogoUrl(businessId > 0 && hasLogo ? BusinessService.getLogoUrl(businessId) : null);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validateForm()) {
      onSubmit({
        ...formData,
        logoFile,
        removeLogo,
      });
    }
  };

  return (
    <form onSubmit={handleSubmit} className="flex h-full min-h-0 flex-col">
      <div className="flex-1 overflow-y-auto min-h-0 px-6 py-4">
        <p className="text-sm text-gray-600 mb-6">Enter business details below.</p>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <FormInput
              label="Business Name"
              name="name"
              value={formData.name}
              onChange={handleChange}
              placeholder="Enter business name"
              required
              error={errors.name}
            />

            <FormInput
              label="Legal Name"
              name="legalName"
              value={formData.legalName}
              onChange={handleChange}
              placeholder="Enter legal entity name"
              required
              error={errors.legalName}
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

            <FormInput
              label="Currency"
              name="currency"
              value={formData.currency}
              onChange={handleChange}
              placeholder="USD"
            />

            <FormInput
              label="Time Zone"
              name="timeZone"
              value={formData.timeZone}
              onChange={handleChange}
              placeholder="UTC"
              required
              error={errors.timeZone}
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

            <div className="md:col-span-2">
              <label className="block text-sm font-medium text-gray-800 mb-2">
                Logo <span className="text-gray-500 font-normal">(optional)</span>
              </label>
              <input
                ref={fileInputRef}
                type="file"
                accept="image/jpeg,image/png,image/gif,image/webp"
                onChange={handleLogoChange}
                className="block w-full text-sm text-gray-700 file:mr-4 file:rounded-md file:border-0 file:bg-blue-50 file:px-4 file:py-2 file:text-sm file:font-medium file:text-blue-700 hover:file:bg-blue-100"
              />
              {errors.logoFile && (
                <p className="mt-1 text-sm text-red-600">{errors.logoFile}</p>
              )}
              <p className="mt-1 text-xs text-gray-500">JPEG, PNG, GIF, or WebP up to 5 MB.</p>
            </div>

            {previewSource && (
              <div className="md:col-span-2">
                <label className="block text-sm font-medium text-gray-800 mb-2">Logo Preview</label>
                <div className="flex items-center gap-4">
                  <img
                    src={previewSource}
                    alt="Business logo preview"
                    className="h-20 w-20 rounded-lg border border-gray-200 object-contain bg-white"
                  />
                  <button
                    type="button"
                    onClick={handleRemoveLogo}
                    className="text-sm text-red-600 hover:text-red-800"
                  >
                    Remove logo
                  </button>
                </div>
              </div>
            )}
        </div>
      </div>

      <div className="shrink-0 border-t border-gray-200 bg-white px-6 py-4 flex justify-end gap-3">
        <FormButton type="button" label="Reset" variant="secondary" onClick={handleReset} />
        <FormButton type="submit" label={submitLabel} loading={isLoading} variant="primary" />
      </div>
    </form>
  );
};

export default BusinessForm;
