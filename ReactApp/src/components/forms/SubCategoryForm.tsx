import React, { useEffect, useMemo, useRef, useState } from 'react';
import { FormButton, FormInput, FormSelect, FormTextarea } from './index';
import CodeFieldWithGenerate from './CodeFieldWithGenerate';
import { CODE_MODULES } from '../../services/codeGeneratorService';
import AuthenticatedImage from '../AuthenticatedImage';
import { categoryService } from '../../modules/category/categoryService';
import { subCategoryService } from '../../modules/subcategory/subcategoryService';
import { safeString } from '../../utils/safeValues';
import { useBranchStore } from '../../stores/useBranchStore';

export interface SubCategoryFormData {
  name: string;
  code: string;
  description: string;
  displayOrder: number;
  icon: string;
  status: string;
  categoryId: number;
  branchId: number;
}

interface CategoryOption {
  id: number;
  name: string;
}

interface SubCategoryFormProps {
  initialData?: Partial<
    SubCategoryFormData & { id?: number; status?: boolean | string; hasImage?: boolean }
  > | null;
  onSubmit: (data: SubCategoryFormData & { imageFile?: File | null; removeImage?: boolean }) => void;
  isLoading?: boolean;
  submitLabel?: string;
  lockBranch?: boolean;
}

const DEFAULT_SUBCATEGORY_FORM_DATA: SubCategoryFormData = {
  name: '',
  code: '',
  description: '',
  displayOrder: 0,
  icon: '',
  status: 'Active',
  categoryId: 0,
  branchId: 0,
};

const buildSubCategoryFormData = (
  source?: Partial<SubCategoryFormData & { status?: boolean | string }> | null
): SubCategoryFormData => {
  const statusFromBoolean =
    typeof source?.status === 'boolean' ? (source.status ? 'Active' : 'Inactive') : null;

  return {
    name: safeString(source?.name),
    code: safeString(source?.code),
    description: safeString(source?.description),
    displayOrder: Number(source?.displayOrder ?? 0),
    icon: safeString(source?.icon),
    status: safeString(source?.status, statusFromBoolean ?? 'Active') || 'Active',
    categoryId: Number(source?.categoryId ?? 0),
    branchId: Number(source?.branchId ?? 0),
  };
};

const SubCategoryForm: React.FC<SubCategoryFormProps> = ({
  initialData,
  onSubmit,
  isLoading = false,
  submitLabel = 'Create Sub Category',
  lockBranch = false,
}) => {
  const branches = useBranchStore((state) => state.branches);
  const fetchBranches = useBranchStore((state) => state.fetchBranches);
  const selectedBranchId = useBranchStore((state) => state.selectedBranchId);

  const safeInitialData = useMemo(() => {
    const base = initialData ?? DEFAULT_SUBCATEGORY_FORM_DATA;
    if (base.branchId && Number(base.branchId) > 0) {
      return base;
    }

    if (selectedBranchId && selectedBranchId > 0) {
      return { ...base, branchId: selectedBranchId };
    }

    return base;
  }, [initialData, selectedBranchId]);

  const [formData, setFormData] = useState<SubCategoryFormData>(() =>
    buildSubCategoryFormData(safeInitialData)
  );
  const [errors, setErrors] = useState<
    Partial<Record<keyof SubCategoryFormData | 'imageFile', string>>
  >({});
  const [categories, setCategories] = useState<CategoryOption[]>([]);
  const [isCategoriesLoading, setIsCategoriesLoading] = useState(false);
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [imagePreviewUrl, setImagePreviewUrl] = useState<string | null>(null);
  const [existingImage, setExistingImage] = useState<{ id: number; branchId: number } | null>(null);
  const [removeImage, setRemoveImage] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    void fetchBranches();
  }, [fetchBranches]);

  useEffect(() => {
    setFormData(buildSubCategoryFormData(safeInitialData));
    setErrors({});
    setImageFile(null);
    setRemoveImage(false);

    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }

    const subCategoryId = Number(safeInitialData?.id ?? 0);
    const branchId = Number(safeInitialData?.branchId ?? 0);
    const hasImage = Boolean(safeInitialData?.hasImage);

    if (subCategoryId > 0 && branchId > 0 && hasImage) {
      setExistingImage({ id: subCategoryId, branchId });
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

  useEffect(() => {
    const branchId = Number(formData.branchId ?? 0);
    if (branchId <= 0) {
      setCategories([]);
      return;
    }

    let isCancelled = false;
    const loadCategories = async () => {
      setIsCategoriesLoading(true);
      try {
        const response = await categoryService.getAll(branchId, 1, 1000);
        const rows = Array.isArray(response.data?.categories) ? response.data.categories : [];
        if (!isCancelled) {
          setCategories(
            rows.map((row: Record<string, unknown>) => ({
              id: Number(row.id ?? row.Id),
              name: String(row.name ?? row.Name ?? ''),
            }))
          );
        }
      } catch {
        if (!isCancelled) {
          setCategories([]);
        }
      } finally {
        if (!isCancelled) {
          setIsCategoriesLoading(false);
        }
      }
    };

    void loadCategories();
    return () => {
      isCancelled = true;
    };
  }, [formData.branchId]);

  const showStoredImage = !imagePreviewUrl && !removeImage && existingImage;
  const showPreview = Boolean(imagePreviewUrl || showStoredImage);

  const validateForm = () => {
    const nextErrors: Partial<Record<keyof SubCategoryFormData | 'imageFile', string>> = {};

    if (!formData.name.trim()) {
      nextErrors.name = 'Sub category name is required';
    }

    if (formData.branchId <= 0) {
      nextErrors.branchId = 'Branch selection is required';
    }

    if (formData.categoryId <= 0) {
      nextErrors.categoryId = 'Category selection is required';
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

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]:
        name === 'displayOrder' || name === 'categoryId' || name === 'branchId'
          ? Number(value || 0)
          : value,
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
    setFormData(buildSubCategoryFormData(safeInitialData));
    setErrors({});
    setImageFile(null);
    setRemoveImage(false);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }

    const subCategoryId = Number(safeInitialData?.id ?? 0);
    const branchId = Number(safeInitialData?.branchId ?? 0);
    const hasImage = Boolean(safeInitialData?.hasImage);
    setExistingImage(
      subCategoryId > 0 && branchId > 0 && hasImage ? { id: subCategoryId, branchId } : null
    );
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validateForm()) {
      onSubmit({
        ...formData,
        imageFile,
        removeImage,
      });
    }
  };

  return (
    <form onSubmit={handleSubmit} className="flex h-full min-h-0 flex-col">
      <div className="min-h-0 flex-1 overflow-y-auto px-6 py-4">
        <p className="mb-6 text-sm text-gray-600">Enter sub category details below.</p>

        <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
          {!lockBranch ? (
            <FormSelect
              label="Branch"
              name="branchId"
              value={String(formData.branchId || '')}
              onChange={handleChange}
              options={[
                { label: 'Select branch', value: '' },
                ...branches.map((branch) => ({
                  label: branch.name,
                  value: String(branch.id),
                })),
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

          <FormSelect
            label="Category"
            name="categoryId"
            value={String(formData.categoryId || '')}
            onChange={handleChange}
            options={[
              {
                label: isCategoriesLoading ? 'Loading categories...' : 'Select category',
                value: '',
              },
              ...categories.map((category) => ({
                label: category.name,
                value: String(category.id),
              })),
            ]}
            required
            error={errors.categoryId}
          />

          <FormInput
            label="Sub Category Name"
            name="name"
            value={formData.name}
            onChange={handleChange}
            placeholder="Enter sub category name"
            required
            error={errors.name}
          />

          <CodeFieldWithGenerate
            label="Code"
            name="code"
            value={formData.code}
            onChange={(code) => setFormData((prev) => ({ ...prev, code }))}
            module={CODE_MODULES.SubCategory}
            branchId={formData.branchId}
            placeholder="Auto-generated if empty"
          />

          <FormInput
            label="Display Order"
            name="displayOrder"
            type="number"
            value={String(formData.displayOrder)}
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

          <FormInput
            label="Icon"
            name="icon"
            value={formData.icon}
            onChange={handleChange}
            placeholder="fa-solid fa-layer-group"
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
            <label className="mb-2 block text-sm font-medium text-gray-800">
              Image <span className="font-normal text-gray-500">(optional)</span>
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

          {showPreview && (
            <div className="md:col-span-2">
              <label className="mb-2 block text-sm font-medium text-gray-800">Image Preview</label>
              <div className="flex items-center gap-4">
                {imagePreviewUrl ? (
                  <img
                    src={imagePreviewUrl}
                    alt="Sub category preview"
                    className="h-20 w-20 rounded-lg border border-gray-200 bg-white object-cover"
                  />
                ) : showStoredImage && existingImage ? (
                  <AuthenticatedImage
                    endpoint={subCategoryService.getImageEndpoint(existingImage.id)}
                    params={{ branchId: existingImage.branchId }}
                    alt="Sub category preview"
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

export default SubCategoryForm;
