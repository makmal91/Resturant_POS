import React, { useMemo, useState, useEffect } from 'react';
import { FormInput, FormSelect, FormButton } from './index';

interface Variant {
  name: string;
  price: string;
}

const normalizeVariants = (variants: unknown): Variant[] => {
  if (!Array.isArray(variants)) {
    return [];
  }

  return variants.map((variant) => {
    const item = variant as { name?: unknown; price?: unknown };
    return {
      name: String(item?.name ?? ''),
      price: String(item?.price ?? ''),
    };
  });
};

interface MenuFormData {
  name: string;
  category: string;
  price: string;
  productType: string;
  variants: Variant[];
}

interface MenuFormProps {
  initialData?: Partial<MenuFormData> | null;
  onSubmit: (data: MenuFormData) => void | Promise<void>;
  categories?: { id: string; name: string }[];
  isCategoryLoading?: boolean;
  categoryError?: string | null;
  isLoading?: boolean;
  submitLabel?: string;
  isEditMode?: boolean;
}

const DEFAULT_MENU_FORM_DATA: MenuFormData = {
  name: '',
  category: '',
  price: '0',
  productType: 'FinishedGood',
  variants: [],
};

const normalizeCategory = (
  rawCategory: unknown,
  normalizedCategories: { id: string; name: string }[]
): string => {
  if (rawCategory == null) {
    return '';
  }

  const value = String(rawCategory).trim();
  if (!value) {
    return '';
  }

  const matched = normalizedCategories.find(
    (category) =>
      category.id === value ||
      category.name.toLowerCase() === value.toLowerCase()
  );

  return matched ? matched.id : value;
};

const buildInitialFormData = (
  initialData: Partial<MenuFormData> | null | undefined,
  normalizedCategories: { id: string; name: string }[]
): MenuFormData => ({
  name: String(initialData?.name ?? ''),
  category: normalizeCategory(initialData?.category, normalizedCategories),
  price: String(initialData?.price ?? ''),
  productType: String(initialData?.productType ?? 'FinishedGood'),
  variants: normalizeVariants(initialData?.variants),
});

const MenuForm: React.FC<MenuFormProps> = ({
  initialData,
  onSubmit,
  categories = [],
  isCategoryLoading = false,
  categoryError = null,
  isLoading = false,
  submitLabel = 'Create Menu Item',
  isEditMode = false,
}) => {
  const safeMenu = useMemo(
    () => initialData ?? DEFAULT_MENU_FORM_DATA,
    [initialData]
  );

  const normalizedCategories = useMemo(
    () =>
      (Array.isArray(categories) ? categories : [])
        .map((category) => ({
          id: String(category?.id ?? ''),
          name: String(category?.name ?? ''),
        }))
        .filter((category) => category.id && category.name),
    [categories]
  );

  const [formData, setFormData] = useState<MenuFormData>(() =>
    buildInitialFormData(safeMenu, normalizedCategories)
  );

  const [errors, setErrors] = useState<Partial<MenuFormData>>({});
  const [formError, setFormError] = useState<string | null>(null);

  useEffect(() => {
    setFormData(buildInitialFormData(safeMenu, normalizedCategories));
    setErrors({});
    setFormError(null);
  }, [safeMenu, normalizedCategories]);

  const validateForm = (): boolean => {
    const newErrors: Partial<MenuFormData> = {};
    const priceValue = Number(formData.price);

    if (!formData.name.trim()) newErrors.name = 'Item name is required';
    if (normalizedCategories.length === 0) {
      newErrors.category = 'No categories found. Please create a menu category first.';
    } else if (!formData.category) {
      newErrors.category = 'Category is required';
    }
    if (!formData.price || !Number.isFinite(priceValue)) {
      newErrors.price = 'Price is required';
    } else if (priceValue < 0) {
      newErrors.price = 'Price must be positive';
    }

    if (!formData.productType) {
      newErrors.productType = 'Product type is required';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    setErrors((prev) => ({ ...prev, [name]: '' }));
  };

  const handleAddVariant = () => {
    const newVariant: Variant = { name: '', price: '' };
    setFormData((prev) => ({ ...prev, variants: [...prev.variants, newVariant] }));
    setFormError(null);
  };

  const handleVariantChange = (
    index: number,
    field: keyof Variant,
    value: string
  ) => {
    const updatedVariants = [...formData.variants];
    updatedVariants[index] = { ...updatedVariants[index], [field]: value };
    setFormData((prev) => ({ ...prev, variants: updatedVariants }));
  };

  const handleRemoveVariant = (index: number) => {
    setFormData((prev) => ({
      ...prev,
      variants: prev.variants.filter((_, i) => i !== index),
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);

    if (validateForm()) {
      try {
        await Promise.resolve(onSubmit(formData));
      } catch (err) {
        console.error('Menu form submit failed:', err);
        setFormError('Failed to submit form. Please try again.');
      }
    }
  };

  const handleReset = () => {
    setFormData(buildInitialFormData(safeMenu, normalizedCategories));
    setErrors({});
    setFormError(null);
  };

  return (
    <div className="max-w-4xl mx-auto">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Menu Management</h1>
        <p className="text-gray-600">Create and manage menu items</p>
      </div>

      <div className="bg-white rounded-lg shadow-sm border border-gray-200">
        <div className="px-6 py-4 border-b border-gray-200">
          <h2 className="text-xl font-semibold text-gray-900">
            {isEditMode ? 'Edit Menu Item' : 'Add New Menu Item'}
          </h2>
          <p className="text-sm text-gray-600 mt-1">
            {isEditMode ? 'Update menu item details' : 'Create a new menu item with variants and pricing'}
          </p>
        </div>

        <form onSubmit={handleSubmit} className="p-6">
          {isCategoryLoading && (
            <div className="mb-4 rounded-md border border-blue-200 bg-blue-50 px-4 py-3 text-sm text-blue-700">
              Loading menu categories...
            </div>
          )}

          {categoryError && (
            <div className="mb-4 rounded-md border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
              {categoryError}
            </div>
          )}

          {!isCategoryLoading && normalizedCategories.length === 0 && (
            <div className="mb-4 rounded-md border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-700">
              No menu categories available yet. Please create a category before adding items.
            </div>
          )}

          {formError && (
            <div className="mb-4 rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
              {formError}
            </div>
          )}

          {/* Basic Information */}
          <div className="mb-8">
            <h3 className="text-base font-semibold text-gray-900 mb-4">Basic Information</h3>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              <FormInput
                label="Item Name"
                name="name"
                value={formData.name}
                onChange={handleChange}
                placeholder="e.g., Grilled Salmon"
                required
                error={errors.name}
              />

              <FormSelect
                label="Category"
                name="category"
                value={formData.category}
                onChange={handleChange}
                options={normalizedCategories.map((cat) => ({ label: cat.name, value: cat.id }))}
                placeholder="Select category"
                required
                error={errors.category}
                disabled={isCategoryLoading || normalizedCategories.length === 0}
              />

              <FormSelect
                label="Product Type"
                name="productType"
                value={formData.productType}
                onChange={handleChange}
                options={[
                  { label: 'Finished Good', value: 'FinishedGood' },
                  { label: 'Raw Material', value: 'RawMaterial' },
                  { label: 'Semi Finished', value: 'SemiFinished' },
                  { label: 'Service', value: 'Service' },
                ]}
                required
                error={errors.productType}
              />
            </div>
          </div>

          {/* Pricing */}
          <div className="mb-8">
            <h3 className="text-base font-semibold text-gray-900 mb-4">Pricing</h3>
            <FormInput
              label="Base Price"
              name="price"
              type="number"
              value={formData.price}
              onChange={handleChange}
              placeholder="0.00"
              required
              min="0"
              step="0.01"
              error={errors.price}
            />
          </div>

          {/* Variants Section */}
          {formData.productType === 'FinishedGood' && (
          <div>
            <div className="flex justify-between items-center mb-4">
              <div>
                <h3 className="text-base font-semibold text-gray-900">Variants</h3>
                <p className="text-sm text-gray-600 mt-1">Add different sizes or options for this item</p>
              </div>
              <button
                type="button"
                onClick={handleAddVariant}
                className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
              >
                <svg className="-ml-1 mr-2 h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                </svg>
                Add Variant
              </button>
            </div>

            {formData.variants.length > 0 ? (
              <div className="space-y-3">
                {formData.variants.map((variant, index) => (
                  <div key={index} className="flex gap-3 items-end bg-gray-50 p-4 rounded-lg border border-gray-200">
                    <div className="flex-1">
                      <FormInput
                        label={index === 0 ? 'Variant Name' : undefined}
                        name={`variant-name-${index}`}
                        value={variant.name}
                        onChange={(e) => handleVariantChange(index, 'name', e.target.value)}
                        placeholder="e.g., Small, Medium, Large"
                      />
                    </div>
                    <div className="w-32">
                      <FormInput
                        label={index === 0 ? 'Price Adjustment' : undefined}
                        name={`variant-price-${index}`}
                        type="number"
                        value={variant.price}
                        onChange={(e) => handleVariantChange(index, 'price', e.target.value)}
                        placeholder="0.00"
                        step="0.01"
                      />
                    </div>
                    <button
                      type="button"
                      onClick={() => handleRemoveVariant(index)}
                      className="p-3 text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                      title="Remove variant"
                    >
                      <svg className="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                      </svg>
                    </button>
                  </div>
                ))}
              </div>
            ) : (
              <div className="p-6 text-center border-2 border-dashed border-gray-300 rounded-lg">
                <svg className="mx-auto h-12 w-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" />
                </svg>
                <p className="mt-2 text-sm text-gray-600">No variants added. Click "Add Variant" to create one.</p>
              </div>
            )}
          </div>
          )}

          {/* Action Buttons */}
          <div className="mt-8 flex justify-end space-x-3">
            <FormButton type="button" label="Clear" variant="secondary" onClick={handleReset} />
            <FormButton
              type="submit"
              label={submitLabel}
              loading={isLoading}
              disabled={isCategoryLoading || normalizedCategories.length === 0}
              variant="primary"
            />
          </div>
        </form>
      </div>
    </div>
  );
};

export default MenuForm;
