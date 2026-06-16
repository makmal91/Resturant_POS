import React, { useEffect, useMemo, useRef, useState } from 'react';
import { FormButton, FormInput, FormSelect, FormTextarea } from './index';
import CodeFieldWithGenerate from './CodeFieldWithGenerate';
import { CODE_MODULES } from '../../services/codeGeneratorService';
import AuthenticatedImage from '../AuthenticatedImage';
import { categoryService } from '../../modules/category/categoryService';
import { safeString } from '../../utils/safeValues';
import { useBranchStore } from '../../stores/useBranchStore';

export interface CategoryFormData {
  name: string;
  code: string;
  description: string;
  displayOrder: number;
  imageUrl: string;
  icon: string;
  color: string;
  status: string;
  categoryType: string;
  branchId: number;
}

interface CategoryFormProps {
  initialData?: Partial<CategoryFormData & { id?: number; status?: boolean | string; hasImage?: boolean }> | null;
  onSubmit: (data: CategoryFormData & { imageFile?: File | null; removeImage?: boolean }) => void;
  isLoading?: boolean;
  submitLabel?: string;
  lockBranch?: boolean;
}

const DEFAULT_CATEGORY_FORM_DATA: CategoryFormData = {
  name: '',
  code: '',
  description: '',
  displayOrder: 0,
  imageUrl: '',
  icon: '',
  color: '#2563eb',
  status: 'Active',
  categoryType: 'Sale',
  branchId: 0,
};

const buildCategoryFormData = (
  source?: Partial<CategoryFormData & { status?: boolean | string }> | null
): CategoryFormData => {
  const statusFromBoolean =
    typeof source?.status === 'boolean'
      ? source.status
        ? 'Active'
        : 'Inactive'
      : null;

  return {
    name: safeString(source?.name),
    code: safeString(source?.code),
    description: safeString(source?.description),
    displayOrder: Number(source?.displayOrder ?? 0),
    imageUrl: safeString(source?.imageUrl),
    icon: safeString(source?.icon),
    color: safeString(source?.color, '#2563eb') || '#2563eb',
    status: safeString(source?.status, statusFromBoolean ?? 'Active') || 'Active',
    categoryType: safeString(source?.categoryType, 'Sale') || 'Sale',
    branchId: Number(source?.branchId ?? 0),
  };
};

const CategoryForm: React.FC<CategoryFormProps> = ({
  initialData,
  onSubmit,
  isLoading = false,
  submitLabel = 'Create Category',
  lockBranch = false,
}) => {
  const branches = useBranchStore((state) => state.branches);
  const fetchBranches = useBranchStore((state) => state.fetchBranches);
  const selectedBranchId = useBranchStore((state) => state.selectedBranchId);

  const safeInitialData = useMemo(() => {
    const base = initialData ?? DEFAULT_CATEGORY_FORM_DATA;
    if (base.branchId && Number(base.branchId) > 0) {
      return base;
    }

    if (selectedBranchId && selectedBranchId > 0) {
      return { ...base, branchId: selectedBranchId };
    }

    return base;
  }, [initialData, selectedBranchId]);

  const [formData, setFormData] = useState<CategoryFormData>(() => buildCategoryFormData(safeInitialData));
  const [errors, setErrors] = useState<Partial<Record<keyof CategoryFormData | 'imageFile', string>>>({});
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [imagePreviewUrl, setImagePreviewUrl] = useState<string | null>(null);
  const [existingImage, setExistingImage] = useState<{ id: number; branchId: number } | null>(null);
  const [removeImage, setRemoveImage] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const isEditMode = Number(safeInitialData?.id ?? 0) > 0;

  useEffect(() => {
    void fetchBranches();
  }, [fetchBranches]);

  useEffect(() => {
    const categoryId = Number(safeInitialData?.id ?? 0);
    const branchId = Number(safeInitialData?.branchId ?? 0);
    const hasImage = Boolean(safeInitialData?.hasImage);

    if (categoryId > 0 && branchId > 0 && hasImage) {
      setExistingImage({ id: categoryId, branchId });
    } else {
      setExistingImage(null);
    }
  }, [safeInitialData]);

  useEffect(() => {
    if (imageFile) {
      const objectUrl = URL.createObjectURL(imageFile);
      setImagePreviewUrl(objectUrl);
      return () => URL.revokeObjectURL(objectUrl);
    }

    setImagePreviewUrl(null);
    return undefined;
  }, [imageFile]);

  const externalImageUrl =
    !removeImage && formData.imageUrl && !formData.imageUrl.startsWith('data:')
      ? formData.imageUrl
      : null;
  const showStoredImage = !imagePreviewUrl && !removeImage && existingImage;
  const showPreview = Boolean(imagePreviewUrl || showStoredImage || externalImageUrl);

  useEffect(() => {
    setFormData(buildCategoryFormData(safeInitialData));
    setErrors({});
    setImageFile(null);
    setImagePreviewUrl(null);
    setRemoveImage(false);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  }, [safeInitialData]);

  const branchOptions = useMemo(
    () =>
      branches
        .filter((branch) => branch.id > 0)
        .map((branch) => ({
          label: branch.name,
          value: branch.id,
        })),
    [branches]
  );

  const resolvedBranchId =
    formData.branchId > 0 ? formData.branchId : selectedBranchId && selectedBranchId > 0 ? selectedBranchId : 0;

  const branchDisplayName =
    branches.find((branch) => branch.id === resolvedBranchId)?.name ||
    (resolvedBranchId > 0 ? `Branch #${resolvedBranchId}` : '');

  const isBranchLocked = lockBranch || isEditMode || Number(initialData?.branchId ?? 0) > 0;

  const validateForm = () => {
    const nextErrors: Partial<Record<keyof CategoryFormData | 'imageFile', string>> = {};

    if (!formData.name.trim()) {
      nextErrors.name = 'Category name is required';
    }

    if (!resolvedBranchId || resolvedBranchId <= 0) {
      nextErrors.branchId = 'Branch selection is required';
    }

    if (imageFile && !imageFile.type.startsWith('image/')) {
      nextErrors.imageFile = 'Image must be an image file';
    }

    if (imageFile && imageFile.size > 5 * 1024 * 1024) {
      nextErrors.imageFile = 'Image must be 5 MB or smaller';
    }

    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const handleChange = (event: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const { name, value } = event.target;
    setFormData((prev) => ({
      ...prev,
      [name]:
        name === 'displayOrder'
          ? Number(value || 0)
          : name === 'branchId'
            ? Number(value || 0)
            : value,
    }));
    setErrors((prev) => ({ ...prev, [name]: '' }));
  };

  const handleImageChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }

    if (!file.type.startsWith('image/')) {
      setErrors((prev) => ({ ...prev, imageFile: 'Image must be an image file' }));
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      setErrors((prev) => ({ ...prev, imageFile: 'Image must be 5 MB or smaller' }));
      return;
    }

    setImageFile(file);
    setRemoveImage(false);
    setErrors((prev) => ({ ...prev, imageFile: '' }));
  };

  const handleRemoveImage = () => {
    setImageFile(null);
    setRemoveImage(true);
    setFormData((prev) => ({ ...prev, imageUrl: '' }));
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const handleReset = () => {
    setFormData(buildCategoryFormData(safeInitialData));
    setErrors({});
    setImageFile(null);
    setRemoveImage(false);
    const categoryId = Number(safeInitialData?.id ?? 0);
    const branchId = Number(safeInitialData?.branchId ?? 0);
    const hasImage = Boolean(safeInitialData?.hasImage);
    setExistingImage(categoryId > 0 && branchId > 0 && hasImage ? { id: categoryId, branchId } : null);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    if (validateForm()) {
      onSubmit({
        ...formData,
        name: formData.name.trim(),
        code: formData.code.trim(),
        displayOrder: Number(formData.displayOrder ?? 0),
        branchId: resolvedBranchId,
        imageFile,
        removeImage,
      });
    }
  };

  return (
    <form onSubmit={handleSubmit} className="flex h-full min-h-0 flex-col">
      <div className="flex-1 overflow-y-auto min-h-0 px-6 py-4">
        <p className="text-sm text-gray-600 mb-6">Enter category details below.</p>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {isBranchLocked ? (
            <FormInput
              label="Branch"
              name="branchDisplay"
              value={branchDisplayName}
              onChange={() => undefined}
              disabled
              required
            />
          ) : (
            <FormSelect
              label="Branch"
              name="branchId"
              value={formData.branchId || ''}
              onChange={handleChange}
              options={branchOptions}
              placeholder="Select branch"
              required
              error={errors.branchId}
              disabled={branchOptions.length === 0}
            />
          )}
          {isBranchLocked && errors.branchId && (
            <p className="-mt-4 mb-5 text-sm text-red-600">{errors.branchId}</p>
          )}

          <FormInput
            label="Category Name"
            name="name"
            value={formData.name}
            onChange={handleChange}
            placeholder="Enter category name"
            required
            error={errors.name}
          />

          <CodeFieldWithGenerate
            label="Code"
            name="code"
            value={formData.code}
            onChange={(code) => setFormData((prev) => ({ ...prev, code }))}
            module={CODE_MODULES.Category}
            branchId={formData.branchId}
            placeholder="Auto-generated if empty"
          />

          <FormSelect
            label="Category Type"
            name="categoryType"
            value={formData.categoryType}
            onChange={handleChange}
            options={[
              { label: 'Sale', value: 'Sale' },
              { label: 'Inventory', value: 'Inventory' },
            ]}
            required
          />

          <FormInput
            label="Display Order"
            name="displayOrder"
            type="number"
            min={0}
            value={formData.displayOrder}
            onChange={handleChange}
            placeholder="0"
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

          <div>
            <label htmlFor="color" className="block text-sm font-medium text-gray-800 mb-2">
              Color
            </label>
            <input
              id="color"
              name="color"
              type="color"
              value={formData.color}
              onChange={handleChange}
              className="h-12 w-full cursor-pointer rounded-lg border border-gray-300 bg-white px-1"
            />
          </div>

          <FormInput
            label="Icon"
            name="icon"
            value={formData.icon}
            onChange={handleChange}
            placeholder="fa-solid fa-burger"
          />

          <div className="md:col-span-2">
            <FormTextarea
              label="Description"
              name="description"
              value={formData.description}
              onChange={handleChange}
              placeholder="Enter description"
              rows={3}
            />
          </div>

          <div className="md:col-span-2">
            <label className="block text-sm font-medium text-gray-800 mb-2">
              Image <span className="text-gray-500 font-normal">(optional)</span>
            </label>
            <input
              ref={fileInputRef}
              type="file"
              accept="image/jpeg,image/png,image/gif,image/webp"
              onChange={handleImageChange}
              className="block w-full text-sm text-gray-700 file:mr-4 file:rounded-md file:border-0 file:bg-blue-50 file:px-4 file:py-2 file:text-sm file:font-medium file:text-blue-700 hover:file:bg-blue-100"
            />
            {errors.imageFile && <p className="mt-1 text-sm text-red-600">{errors.imageFile}</p>}
            <p className="mt-1 text-xs text-gray-500">JPEG, PNG, GIF, or WebP up to 5 MB.</p>
          </div>

          <div className="md:col-span-2">
            <FormInput
              label="Image URL"
              name="imageUrl"
              value={formData.imageUrl.startsWith('data:') ? '' : formData.imageUrl}
              onChange={handleChange}
              placeholder="https://cdn.example.com/category.png"
            />
          </div>

          {showPreview && (
            <div className="md:col-span-2">
              <label className="block text-sm font-medium text-gray-800 mb-2">Image Preview</label>
              <div className="flex items-center gap-4">
                {imagePreviewUrl ? (
                  <img
                    src={imagePreviewUrl}
                    alt="Category image preview"
                    className="h-20 w-20 rounded-lg border border-gray-200 object-contain bg-white"
                  />
                ) : showStoredImage && existingImage ? (
                  <AuthenticatedImage
                    endpoint={categoryService.getImageEndpoint(existingImage.id)}
                    params={{ branchId: existingImage.branchId }}
                    alt="Category image preview"
                    className="h-20 w-20 rounded-lg border border-gray-200 object-contain bg-white"
                  />
                ) : externalImageUrl ? (
                  <img
                    src={externalImageUrl}
                    alt="Category image preview"
                    className="h-20 w-20 rounded-lg border border-gray-200 object-contain bg-white"
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

          <div className="md:col-span-2 rounded-md border border-blue-200 bg-blue-50 p-3 text-xs text-blue-700">
            Sale categories are for FinishedGood products in POS. Inventory categories are for RawMaterial and
            SemiFinished items.
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

export default CategoryForm;
