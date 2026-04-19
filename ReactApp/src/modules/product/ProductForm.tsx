import React, { useEffect, useMemo, useState } from 'react';
import { EntityFormProps } from '../shared/ManagementPage';
import { ManagementFormValues } from '../shared/types';
import apiClient from '../../services/api';

type ProductType = 'RawMaterial' | 'FinishedGood' | 'SemiFinished' | 'Service';
type CategoryType = 'Sale' | 'Inventory';

interface CategoryOption {
  id: number;
  name: string;
  categoryType: CategoryType;
}

const resolveFlagsByType = (productType: ProductType) => {
  switch (productType) {
    case 'RawMaterial':
      return { isSaleable: false, isInventoryItem: true, isRecipeItem: true, isPurchasable: true };
    case 'FinishedGood':
      return { isSaleable: true, isInventoryItem: false, isRecipeItem: false, isPurchasable: false };
    case 'SemiFinished':
      return { isSaleable: false, isInventoryItem: true, isRecipeItem: true, isPurchasable: false };
    case 'Service':
      return { isSaleable: true, isInventoryItem: false, isRecipeItem: false, isPurchasable: false };
    default:
      return { isSaleable: true, isInventoryItem: false, isRecipeItem: false, isPurchasable: false };
  }
};

const ProductForm: React.FC<EntityFormProps> = (props) => {
  const { isOpen, isEditMode, initialData, isSubmitting, onCancel, onSubmit } = props;
  const [error, setError] = useState('');
  const [categories, setCategories] = useState<CategoryOption[]>([]);
  const [formData, setFormData] = useState<ManagementFormValues>({
    name: '',
    description: '',
    isActive: true,
    branchId: 1,
    menuCategoryId: 0,
    price: 0,
    tax: 0,
    preparationTime: 0,
    productType: 'FinishedGood',
    variants: [],
    addons: [],
  });

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    const loadCategories = async () => {
      try {
        const response = await apiClient.get('/categories', { params: { branchId: Number(initialData?.branchId ?? 1) } });
        const raw = Array.isArray(response.data?.categories) ? response.data.categories : [];
        setCategories(
          raw
            .map((item: any) => ({
              id: Number(item?.id ?? 0),
              name: String(item?.name ?? ''),
              categoryType: (item?.categoryType as CategoryType) ?? 'Sale',
            }))
            .filter((item: CategoryOption) => item.id > 0)
        );
      } catch {
        setCategories([]);
      }
    };

    void loadCategories();
  }, [initialData?.branchId, isOpen]);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    const initialType = (initialData?.productType as ProductType | undefined) ?? 'FinishedGood';
    const flags = resolveFlagsByType(initialType);

    setFormData({
      name: String(initialData?.name ?? ''),
      description: String(initialData?.description ?? ''),
      isActive: Boolean(initialData?.isActive ?? true),
      branchId: Number(initialData?.branchId ?? 1),
      menuCategoryId: Number(initialData?.menuCategoryId ?? 0),
      price: Number(initialData?.price ?? 0),
      tax: Number(initialData?.tax ?? 0),
      preparationTime: Number(initialData?.preparationTime ?? 0),
      productType: initialType,
      isSaleable: flags.isSaleable,
      isInventoryItem: flags.isInventoryItem,
      isRecipeItem: flags.isRecipeItem,
      isPurchasable: flags.isPurchasable,
      variants: Array.isArray(initialData?.variants) ? initialData?.variants : [],
      addons: Array.isArray(initialData?.addons) ? initialData?.addons : [],
    });
    setError('');
  }, [initialData, isOpen]);

  const filteredCategories = useMemo(() => {
    const type = (formData.productType ?? 'FinishedGood') as ProductType;
    if (type === 'FinishedGood') {
      return categories.filter((c) => c.categoryType === 'Sale');
    }

    if (type === 'RawMaterial' || type === 'SemiFinished') {
      return categories.filter((c) => c.categoryType === 'Inventory');
    }

    return categories.filter((c) => c.categoryType === 'Sale');
  }, [categories, formData.productType]);

  useEffect(() => {
    const selectedId = Number(formData.menuCategoryId ?? 0);
    if (selectedId > 0 && filteredCategories.some((category) => category.id === selectedId)) {
      return;
    }

    if (filteredCategories.length > 0) {
      setFormData((prev) => ({ ...prev, menuCategoryId: filteredCategories[0].id }));
    }
  }, [filteredCategories, formData.menuCategoryId]);

  if (!isOpen) {
    return null;
  }

  const productType = (formData.productType ?? 'FinishedGood') as ProductType;
  const flags = resolveFlagsByType(productType);
  const showInventoryFields = productType === 'RawMaterial' || productType === 'SemiFinished';
  const showVariantAddon = productType === 'FinishedGood';

  const handleProductTypeChange = (value: ProductType) => {
    const nextFlags = resolveFlagsByType(value);
    setFormData((prev) => ({
      ...prev,
      productType: value,
      isSaleable: nextFlags.isSaleable,
      isInventoryItem: nextFlags.isInventoryItem,
      isRecipeItem: nextFlags.isRecipeItem,
      isPurchasable: nextFlags.isPurchasable,
      variants: value === 'FinishedGood' ? prev.variants : [],
      addons: value === 'FinishedGood' ? prev.addons : [],
    }));
  };

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!String(formData.name ?? '').trim()) {
      setError('Product name is required.');
      return;
    }

    if (Number(formData.menuCategoryId ?? 0) <= 0) {
      setError('Please select a category.');
      return;
    }

    setError('');
    await onSubmit({
      ...formData,
      name: String(formData.name ?? '').trim(),
      description: String(formData.description ?? ''),
      branchId: Number(formData.branchId ?? 1),
      menuCategoryId: Number(formData.menuCategoryId ?? 0),
      price: Number(formData.price ?? 0),
      tax: Number(formData.tax ?? 0),
      preparationTime: Number(formData.preparationTime ?? 0),
      productType,
      isSaleable: flags.isSaleable,
      isInventoryItem: flags.isInventoryItem,
      isRecipeItem: flags.isRecipeItem,
      isPurchasable: flags.isPurchasable,
      variants: showVariantAddon ? (formData.variants ?? []) : [],
      addons: showVariantAddon ? (formData.addons ?? []) : [],
    });
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="w-full max-w-3xl rounded-xl bg-white shadow-xl">
        <div className="border-b border-gray-200 px-6 py-4">
          <h3 className="text-lg font-semibold text-gray-900">
            {isEditMode ? 'Edit Product' : 'Add Product'}
          </h3>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4 px-6 py-5">
          {error && (
            <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
              {error}
            </div>
          )}

          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">Name</label>
              <input
                type="text"
                value={String(formData.name ?? '')}
                onChange={(event) => setFormData((prev) => ({ ...prev, name: event.target.value }))}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
              />
            </div>

            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">Product Type</label>
              <select
                value={productType}
                onChange={(event) => handleProductTypeChange(event.target.value as ProductType)}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
              >
                <option value="RawMaterial">RawMaterial</option>
                <option value="FinishedGood">FinishedGood</option>
                <option value="SemiFinished">SemiFinished</option>
                <option value="Service">Service</option>
              </select>
            </div>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Description</label>
            <textarea
              value={String(formData.description ?? '')}
              onChange={(event) => setFormData((prev) => ({ ...prev, description: event.target.value }))}
              className="h-24 w-full resize-none rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
            />
          </div>

          <div className="grid grid-cols-1 gap-4 md:grid-cols-4">
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">Price</label>
              <input
                type="number"
                min={0}
                step="0.01"
                value={Number(formData.price ?? 0)}
                onChange={(event) => setFormData((prev) => ({ ...prev, price: Number(event.target.value || 0) }))}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
              />
            </div>

            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">Tax %</label>
              <input
                type="number"
                min={0}
                step="0.01"
                value={Number(formData.tax ?? 0)}
                onChange={(event) => setFormData((prev) => ({ ...prev, tax: Number(event.target.value || 0) }))}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
              />
            </div>

            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">Prep Time</label>
              <input
                type="number"
                min={0}
                value={Number(formData.preparationTime ?? 0)}
                onChange={(event) => setFormData((prev) => ({ ...prev, preparationTime: Number(event.target.value || 0) }))}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
              />
            </div>

            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">Branch ID</label>
              <input
                type="number"
                min={1}
                value={Number(formData.branchId ?? 1)}
                onChange={(event) => setFormData((prev) => ({ ...prev, branchId: Number(event.target.value || 1) }))}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
              />
            </div>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Category</label>
            <select
              value={Number(formData.menuCategoryId ?? 0)}
              onChange={(event) => setFormData((prev) => ({ ...prev, menuCategoryId: Number(event.target.value || 0) }))}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
            >
              {filteredCategories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name} ({category.categoryType})
                </option>
              ))}
            </select>
          </div>

          <div className="rounded-md border border-gray-200 bg-gray-50 p-3 text-xs text-gray-700">
            <div>IsSaleable: {String(flags.isSaleable)}</div>
            <div>IsInventoryItem: {String(flags.isInventoryItem)}</div>
            <div>IsRecipeItem: {String(flags.isRecipeItem)}</div>
            <div>IsPurchasable: {String(flags.isPurchasable)}</div>
          </div>

          {showVariantAddon && (
            <div className="rounded-md border border-blue-200 bg-blue-50 p-3 text-xs text-blue-700">
              FinishedGood supports variants and modifiers. Existing variant/addon management remains available via menu endpoints.
            </div>
          )}

          {showInventoryFields && (
            <div className="rounded-md border border-amber-200 bg-amber-50 p-3 text-xs text-amber-700">
              Inventory tracking is enabled for this product type. Use Inventory module for purchase entries and stock adjustments.
            </div>
          )}

          {productType === 'Service' && (
            <div className="rounded-md border border-green-200 bg-green-50 p-3 text-xs text-green-700">
              Service items are saleable but excluded from stock handling.
            </div>
          )}

          <div className="flex justify-end gap-3 border-t border-gray-200 pt-4">
            <button
              type="button"
              onClick={onCancel}
              disabled={isSubmitting}
              className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-60"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isSubmitting ? 'Saving...' : isEditMode ? 'Update' : 'Create'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default ProductForm;
