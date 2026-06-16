import React, { useEffect, useMemo, useRef, useState } from 'react';
import { FormButton, FormInput, FormSelect, FormTextarea } from './index';
import AuthenticatedImage from '../AuthenticatedImage';
import { brandService } from '../../modules/brand/brandService';
import { safeString } from '../../utils/safeValues';
import { useFormBranchId } from '../../hooks/useFormBranchId';

export interface BrandFormData {
  name: string;
  description: string;
  status: string;
  branchId: number;
}

interface BrandFormProps {
  initialData?: Partial<
    BrandFormData & { id?: number; status?: boolean | string; hasImage?: boolean }
  > | null;
  onSubmit: (data: BrandFormData & { imageFile?: File | null; removeImage?: boolean }) => void;
  isLoading?: boolean;
  submitLabel?: string;
}

const DEFAULT_BRAND_FORM_DATA: BrandFormData = {
  name: '',
  description: '',
  status: 'Active',
  branchId: 0,
};

const buildBrandFormData = (
  source?: Partial<BrandFormData & { status?: boolean | string }> | null
): BrandFormData => {
  const statusFromBoolean =
    typeof source?.status === 'boolean' ? (source.status ? 'Active' : 'Inactive') : null;

  return {
    name: safeString(source?.name),
    description: safeString(source?.description),
    status: safeString(source?.status, statusFromBoolean ?? 'Active') || 'Active',
    branchId: Number(source?.branchId ?? 0),
  };
};

const BrandForm: React.FC<BrandFormProps> = ({
  initialData,
  onSubmit,
  isLoading = false,
  submitLabel = 'Create Brand',
}) => {
  const { branchId: resolvedBranchId, branchError } = useFormBranchId(initialData?.branchId);

  const safeInitialData = useMemo(() => {
    const base = initialData ?? DEFAULT_BRAND_FORM_DATA;
    if (resolvedBranchId > 0) {
      return { ...base, branchId: resolvedBranchId };
    }
    return base;
  }, [initialData, resolvedBranchId]);

  const [formData, setFormData] = useState<BrandFormData>(() => buildBrandFormData(safeInitialData));
  const [errors, setErrors] = useState<Partial<Record<keyof BrandFormData | 'imageFile', string>>>({});
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [imagePreviewUrl, setImagePreviewUrl] = useState<string | null>(null);
  const [existingImage, setExistingImage] = useState<{ id: number; branchId: number } | null>(null);
  const [removeImage, setRemoveImage] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    setFormData(buildBrandFormData(safeInitialData));
    setErrors({});
    setImageFile(null);
    setRemoveImage(false);

    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }

    const brandId = Number(safeInitialData?.id ?? 0);
    const branchId = Number(safeInitialData?.branchId ?? 0);
    const hasImage = Boolean(safeInitialData?.hasImage);

    if (brandId > 0 && branchId > 0 && hasImage) {
      setExistingImage({ id: brandId, branchId });
    } else {
      setExistingImage(null);
    }
  }, [safeInitialData]);

  useEffect(() => {
    if (resolvedBranchId > 0) {
      setFormData((prev) =>
        prev.branchId === resolvedBranchId ? prev : { ...prev, branchId: resolvedBranchId },
      );
    }
  }, [resolvedBranchId]);

  useEffect(() => {
    if (imageFile) {
      const objectUrl = URL.createObjectURL(imageFile);
      setImagePreviewUrl(objectUrl);
      return () => URL.revokeObjectURL(objectUrl);
    }

    setImagePreviewUrl(null);
    return undefined;
  }, [imageFile]);

  const showStoredImage = !imagePreviewUrl && !removeImage && existingImage;
  const showPreview = Boolean(imagePreviewUrl || showStoredImage);

  const validateForm = () => {
    const nextErrors: Partial<Record<keyof BrandFormData | 'imageFile', string>> = {};

    if (!formData.name.trim()) {
      nextErrors.name = 'Brand name is required';
    }

    if (resolvedBranchId <= 0) {
      nextErrors.branchId = branchError ?? 'Branch is required';
    }

    if (imageFile) {
      const allowedTypes = ['image/jpeg', 'image/png', 'image/webp'];
      if (!allowedTypes.includes(imageFile.type)) {
        nextErrors.imageFile = 'Image must be JPG, PNG, or WebP';
      }
    }

    if (imageFile && imageFile.size > 5 * 1024 * 1024) {
      nextErrors.imageFile = 'Image must be 5 MB or smaller';
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
      [name]: value,
    }));
    setErrors((prev) => ({ ...prev, [name]: '' }));
  };

  const handleImageChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0] ?? null;
    setImageFile(file);
    setRemoveImage(false);
    setErrors((prev) => ({ ...prev, imageFile: '' }));
  };

  const handleRemoveImage = () => {
    setImageFile(null);
    setRemoveImage(true);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const handleReset = () => {
    setFormData(buildBrandFormData(safeInitialData));
    setErrors({});
    setImageFile(null);
    setRemoveImage(false);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }

    const brandId = Number(safeInitialData?.id ?? 0);
    const branchId = Number(safeInitialData?.branchId ?? 0);
    const hasImage = Boolean(safeInitialData?.hasImage);
    setExistingImage(brandId > 0 && branchId > 0 && hasImage ? { id: brandId, branchId } : null);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validateForm()) {
      onSubmit({
        ...formData,
        branchId: resolvedBranchId,
        imageFile,
        removeImage,
      });
    }
  };

  return (
    <form onSubmit={handleSubmit} className="flex h-full min-h-0 flex-col">
      <div className="min-h-0 flex-1 overflow-y-auto px-6 py-4">
        <p className="mb-6 text-sm text-gray-600">Enter brand details below.</p>

        <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
          {errors.branchId && (
            <p className="md:col-span-2 -mt-2 text-sm text-red-600">{errors.branchId}</p>
          )}

          <FormInput
            label="Brand Name"
            name="name"
            value={formData.name}
            onChange={handleChange}
            placeholder="Enter brand name"
            required
            error={errors.name}
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
              label="Description"
              name="description"
              value={formData.description}
              onChange={handleChange}
              placeholder="Enter description (optional)"
              rows={3}
            />
          </div>

          <div className="md:col-span-2">
            <label className="mb-2 block text-sm font-medium text-gray-800">
              Brand Logo <span className="font-normal text-gray-500">(optional)</span>
            </label>
            <input
              ref={fileInputRef}
              type="file"
              accept="image/jpeg,image/png,image/webp"
              onChange={handleImageChange}
              className="block w-full text-sm text-gray-700 file:mr-4 file:rounded-md file:border-0 file:bg-blue-50 file:px-4 file:py-2 file:text-sm file:font-medium file:text-blue-700 hover:file:bg-blue-100"
            />
            {errors.imageFile && <p className="mt-1 text-sm text-red-600">{errors.imageFile}</p>}
            <p className="mt-1 text-xs text-gray-500">JPG, PNG, or WebP up to 5 MB.</p>
          </div>

          {showPreview && (
            <div className="md:col-span-2">
              <label className="mb-2 block text-sm font-medium text-gray-800">Logo Preview</label>
              <div className="flex items-center gap-4">
                {imagePreviewUrl ? (
                  <img
                    src={imagePreviewUrl}
                    alt="Brand logo preview"
                    className="h-20 w-20 rounded-lg border border-gray-200 bg-white object-cover"
                  />
                ) : showStoredImage && existingImage ? (
                  <AuthenticatedImage
                    endpoint={brandService.getImageEndpoint(existingImage.id)}
                    params={{ branchId: existingImage.branchId }}
                    alt="Brand logo preview"
                    className="h-20 w-20 rounded-lg border border-gray-200 bg-white object-cover"
                  />
                ) : null}
                <button
                  type="button"
                  onClick={handleRemoveImage}
                  className="text-sm text-red-600 hover:text-red-800"
                >
                  Remove image
                </button>
              </div>
            </div>
          )}
        </div>
      </div>

      <div className="flex shrink-0 justify-end gap-3 border-t border-gray-200 bg-white px-6 py-4">
        <FormButton type="button" label="Reset" variant="secondary" onClick={handleReset} />
        <FormButton type="submit" label={submitLabel} loading={isLoading} variant="primary" />
      </div>
    </form>
  );
};

export default BrandForm;
