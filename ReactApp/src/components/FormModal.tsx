import React, { useMemo, useEffect, useState } from 'react';
import { useFormModal } from '../contexts/FormModalContext';
import { BranchForm, BusinessForm, UserForm, MenuForm, InventoryForm, CategoryForm } from './forms';
import { BranchService, BusinessService, UserService, MenuService, InventoryService } from '../services/apiService';
import { getApiErrorMessage } from '../services/api';
import { categoryService } from '../modules/category/categoryService';
import { useBranchStore } from '../stores/useBranchStore';

interface MenuCategoryOption {
  id: string;
  name: string;
}

const DEFAULT_BRANCH_ID = 1;
const EMPTY_MENU_FORM_DATA = {
  name: '',
  price: 0,
  description: '',
  categoryId: null,
  category: '',
  variants: [],
};

const PANEL_TRANSITION_MS = 300;

const FormModal: React.FC = () => {
  const { isOpen, formType, editingData, closeForm } = useFormModal();
  const isEditMode = editingData?.id != null;
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [menuCategories, setMenuCategories] = useState<MenuCategoryOption[]>([]);
  const [isMenuCategoriesLoading, setIsMenuCategoriesLoading] = useState(false);
  const [menuCategoriesError, setMenuCategoriesError] = useState<string | null>(null);
  const [isRendered, setIsRendered] = useState(false);
  const [isVisible, setIsVisible] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setIsRendered(true);
      const frame = window.requestAnimationFrame(() => {
        window.requestAnimationFrame(() => setIsVisible(true));
      });
      return () => window.cancelAnimationFrame(frame);
    }

    setIsVisible(false);
    const timer = window.setTimeout(() => setIsRendered(false), PANEL_TRANSITION_MS);
    return () => window.clearTimeout(timer);
  }, [isOpen]);

  useEffect(() => {
    if (!isRendered) {
      return undefined;
    }

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';

    return () => {
      document.body.style.overflow = previousOverflow;
    };
  }, [isRendered]);

  useEffect(() => {
    let isCancelled = false;

    const loadMenuCategories = async () => {
      if (!isOpen || formType !== 'menu') {
        return;
      }

      setIsMenuCategoriesLoading(true);
      setMenuCategoriesError(null);

      try {
        const response = await MenuService.getCategories(DEFAULT_BRANCH_ID, true);
        const categories = Array.isArray(response?.data?.categories)
          ? response.data.categories
          : [];

        const normalizedCategories = categories
          .map((category: { id?: unknown; name?: unknown }) => ({
            id: String(category?.id ?? ''),
            name: String(category?.name ?? ''),
          }))
          .filter((category: MenuCategoryOption) => category.id && category.name);

        if (!isCancelled) {
          setMenuCategories(normalizedCategories);
        }
      } catch (err) {
        if (!isCancelled) {
          setMenuCategories([]);
          setMenuCategoriesError(getApiErrorMessage(err, 'Failed to load menu categories.'));
        }
      } finally {
        if (!isCancelled) {
          setIsMenuCategoriesLoading(false);
        }
      }
    };

    loadMenuCategories();

    return () => {
      isCancelled = true;
    };
  }, [isOpen, formType]);

  const normalizedMenuInitialData = useMemo(() => {
    if (formType !== 'menu' || !editingData) {
      return editingData;
    }

    const rawCategory = editingData?.category;
    if (rawCategory == null) {
      return editingData;
    }

    const categoryValue = String(rawCategory);
    const matchedCategory = menuCategories.find(
      (category) =>
        category.id === categoryValue ||
        category.name.toLowerCase() === categoryValue.toLowerCase()
    );

    return {
      ...editingData,
      category: matchedCategory ? matchedCategory.id : categoryValue,
    };
  }, [formType, editingData, menuCategories]);

  const closeWithSuccess = (message: string) => {
    setSuccessMessage(message);
    setTimeout(() => {
      closeForm();
      setSuccessMessage(null);
    }, 900);
  };

  const handleBranchSubmit = async (data: any) => {
    setIsSubmitting(true);
    setError(null);
    setSuccessMessage(null);

    const selectedBusinessId = Number(data?.businessId ?? 0);
    const payload = {
      name: String(data?.name ?? '').trim(),
      code: String(data?.code ?? '').trim(),
      address: String(data?.address ?? '').trim(),
      phone: String(data?.phone ?? '').trim(),
      email: String(data?.email ?? '').trim(),
      businessId: Number.isFinite(selectedBusinessId) ? selectedBusinessId : 0,
      companyId: Number.isFinite(selectedBusinessId) ? selectedBusinessId : 0,
      countryId: Number(data?.countryId ?? 0),
      cityId: Number(data?.cityId ?? 0),
      status: String(data?.status ?? 'Active'),
      isActive: String(data?.status ?? 'Active').toLowerCase() !== 'inactive',
    };

    if (!payload.name || !payload.code) {
      alert('Name and Code are required');
      setIsSubmitting(false);
      return;
    }

    if (payload.businessId <= 0) {
      setError('Business is required.');
      setIsSubmitting(false);
      return;
    }

    if (payload.countryId <= 0 || payload.cityId <= 0) {
      setError('Country and City are required.');
      setIsSubmitting(false);
      return;
    }

    try {
      if (isEditMode) {
        // Update
        await BranchService.update(editingData.id, payload);
      } else {
        // Create
        await BranchService.create(payload);
      }
      closeWithSuccess(isEditMode ? 'Branch updated successfully.' : 'Branch created successfully.');
    } catch (err: any) {
      console.error('Branch API Error:', err?.response?.data || err?.message);
      setError(getApiErrorMessage(err, 'Failed to save branch'));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleUserSubmit = async (data: any) => {
    setIsSubmitting(true);
    setError(null);
    setSuccessMessage(null);
    try {
      if (isEditMode) {
        await UserService.update(editingData.id, data);
      } else {
        await UserService.create(data);
      }
      closeWithSuccess(isEditMode ? 'User updated successfully.' : 'User created successfully.');
    } catch (err: any) {
      setError(getApiErrorMessage(err, 'Failed to save user'));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleBusinessSubmit = async (data: any) => {
    setIsSubmitting(true);
    setError(null);
    setSuccessMessage(null);

    const name = String(data?.name ?? '').trim();
    const legalName = String(data?.legalName ?? '').trim();

    if (!name || !legalName) {
      setError('Business name and legal name are required.');
      setIsSubmitting(false);
      return;
    }

    const formData = new FormData();
    formData.append('name', name);
    formData.append('legalName', legalName);
    formData.append('phone', String(data?.phone ?? '').trim());
    formData.append('email', String(data?.email ?? '').trim());
    formData.append('address', String(data?.address ?? '').trim());
    formData.append('taxNumber', String(data?.taxNumber ?? '').trim());
    formData.append('currency', String(data?.currency ?? 'USD').trim().toUpperCase());
    formData.append('timeZone', String(data?.timeZone ?? 'UTC').trim());
    formData.append(
      'isActive',
      String(data?.status ?? 'Active').toLowerCase() !== 'inactive' ? 'true' : 'false'
    );

    if (data?.logoFile instanceof File) {
      formData.append('logo', data.logoFile);
    }

    if (data?.removeLogo) {
      formData.append('removeLogo', 'true');
    }

    try {
      if (isEditMode) {
        await BusinessService.update(editingData.id, formData);
      } else {
        await BusinessService.create(formData);
      }

      closeWithSuccess(isEditMode ? 'Business updated successfully.' : 'Business created successfully.');
    } catch (err: any) {
      setError(getApiErrorMessage(err, 'Failed to save business'));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleMenuSubmit = async (data: any) => {
    setIsSubmitting(true);
    setError(null);
    setSuccessMessage(null);

    const menuCategoryId = Number(data?.category);
    const price = Number(data?.price);
    const productType = String(data?.productType ?? 'FinishedGood');

    if (!data?.name?.trim()) {
      setError('Item name is required.');
      setIsSubmitting(false);
      return;
    }

    if (!Number.isFinite(menuCategoryId) || menuCategoryId <= 0) {
      setError('Please select a valid category.');
      setIsSubmitting(false);
      return;
    }

    if (!Number.isFinite(price) || price < 0) {
      setError('Please provide a valid item price.');
      setIsSubmitting(false);
      return;
    }

    try {
      const payload = {
        name: data.name.trim(),
        price,
        tax: Number(data?.tax ?? 0),
        preparationTime: Number(data?.preparationTime ?? 15),
        menuCategoryId,
        branchId: Number(data?.branchId ?? 1),
        productType,
        isSaleable: productType === 'FinishedGood' || productType === 'Service',
        isInventoryItem: productType === 'RawMaterial' || productType === 'SemiFinished',
        isRecipeItem: productType === 'RawMaterial' || productType === 'SemiFinished',
        isPurchasable: productType === 'RawMaterial',
        variants: Array.isArray(data?.variants)
          ? data.variants
              .filter((variant: any) => variant?.name?.trim())
              .map((variant: any) => ({
                name: variant.name.trim(),
                price: Number(variant.price ?? 0),
              }))
          : [],
        addons: [],
      };

      console.log('[Menu Submit] POST /api/menu/items', payload);

      if (isEditMode) {
        await MenuService.update(editingData.id, data);
      } else {
        await MenuService.create(payload);
      }

      closeWithSuccess(isEditMode ? 'Menu item updated successfully.' : 'Menu item created successfully.');
    } catch (err: any) {
      setError(getApiErrorMessage(err, 'Failed to save menu item'));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleInventorySubmit = async (data: any) => {
    setIsSubmitting(true);
    setError(null);
    setSuccessMessage(null);
    try {
      if (isEditMode) {
        await InventoryService.update(editingData.id, data);
      } else {
        await InventoryService.create(data);
      }
      closeWithSuccess(isEditMode ? 'Inventory item updated successfully.' : 'Inventory item created successfully.');
    } catch (err: any) {
      setError(getApiErrorMessage(err, 'Failed to save inventory item'));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCategorySubmit = async (data: any) => {
    setIsSubmitting(true);
    setError(null);
    setSuccessMessage(null);

    const branchId = Number(data?.branchId ?? 0);
    const name = String(data?.name ?? '').trim();

    if (!name) {
      setError('Category name is required.');
      setIsSubmitting(false);
      return;
    }

    if (branchId <= 0) {
      setError('Branch selection is required.');
      setIsSubmitting(false);
      return;
    }

    const rawImageUrl = String(data?.imageUrl ?? '').trim();
    const formData = new FormData();
    formData.append('name', name);
    formData.append('code', String(data?.code ?? '').trim());
    formData.append('description', String(data?.description ?? '').trim());
    formData.append('displayOrder', String(Number(data?.displayOrder ?? 0)));
    formData.append('imageUrl', rawImageUrl.startsWith('data:') ? '' : rawImageUrl.slice(0, 500));
    formData.append('icon', String(data?.icon ?? '').trim());
    formData.append('color', String(data?.color ?? '#2563eb').trim());
    formData.append('status', String(data?.status ?? 'Active').toLowerCase() !== 'inactive' ? 'Active' : 'Inactive');
    formData.append(
      'categoryType',
      String(data?.categoryType ?? 'Sale') === 'Inventory' ? 'Inventory' : 'Sale'
    );
    formData.append('branchId', String(branchId));

    if (data?.imageFile instanceof File) {
      formData.append('image', data.imageFile);
    }

    if (data?.removeImage) {
      formData.append('removeImage', 'true');
    }

    try {
      useBranchStore.getState().setSelectedBranchId(branchId);
      if (isEditMode) {
        await categoryService.update(Number(editingData.id), formData, branchId);
      } else {
        await categoryService.create(formData, branchId);
      }
      closeWithSuccess(isEditMode ? 'Category updated successfully.' : 'Category created successfully.');
    } catch (err: any) {
      setError(getApiErrorMessage(err, 'Failed to save category'));
    } finally {
      setIsSubmitting(false);
    }
  };

  const getFormComponent = () => {
    switch (formType) {
      case 'branch':
        return (
          <BranchForm
            initialData={editingData}
            onSubmit={handleBranchSubmit}
            isLoading={isSubmitting}
            submitLabel={isEditMode ? 'Update Branch' : 'Create Branch'}
          />
        );
      case 'user':
        return (
          <UserForm
            initialData={editingData}
            onSubmit={handleUserSubmit}
            isLoading={isSubmitting}
            submitLabel={isEditMode ? 'Update User' : 'Create User'}
          />
        );
      case 'business':
        return (
          <BusinessForm
            initialData={editingData}
            onSubmit={handleBusinessSubmit}
            isLoading={isSubmitting}
            submitLabel={isEditMode ? 'Update Business' : 'Create Business'}
          />
        );
      case 'menu':
        return (
          <MenuForm
            initialData={normalizedMenuInitialData ?? EMPTY_MENU_FORM_DATA}
            isEditMode={isEditMode}
            onSubmit={handleMenuSubmit}
            categories={menuCategories}
            isCategoryLoading={isMenuCategoriesLoading}
            categoryError={menuCategoriesError}
            isLoading={isSubmitting}
            submitLabel={isEditMode ? 'Update Item' : 'Create Item'}
          />
        );
      case 'inventory':
        return (
          <InventoryForm
            initialData={editingData}
            onSubmit={handleInventorySubmit}
            isLoading={isSubmitting}
            submitLabel={isEditMode ? 'Update Item' : 'Create Item'}
          />
        );
      case 'category':
        return (
          <CategoryForm
            initialData={editingData}
            onSubmit={handleCategorySubmit}
            isLoading={isSubmitting}
            submitLabel={isEditMode ? 'Update Category' : 'Create Category'}
            lockBranch={isEditMode || Number(editingData?.branchId ?? 0) > 0}
          />
        );
      default:
        return null;
    }
  };

  if (!isRendered) return null;

  const panelWidthClass =
    formType === 'business' || formType === 'branch' || formType === 'category' ? 'max-w-4xl' : 'max-w-2xl';

  return (
    <>
      <div
        className={`fixed inset-0 z-40 bg-black transition-opacity duration-300 ease-in-out ${
          isVisible ? 'bg-opacity-50' : 'bg-opacity-0'
        }`}
        onClick={closeForm}
        aria-hidden="true"
      />

      <div
        role="dialog"
        aria-modal="true"
        className={`fixed inset-y-0 right-0 z-50 h-full w-full ${panelWidthClass} bg-white shadow-2xl flex flex-col overflow-hidden border-l border-gray-200 transition-transform duration-300 ease-in-out ${
          isVisible ? 'translate-x-0' : 'translate-x-full'
        }`}
        onClick={(e) => e.stopPropagation()}
      >
        <div className="shrink-0 z-10 bg-white border-b border-gray-200 px-6 py-4 flex justify-between items-center">
          <div>
            <h2 className="text-xl font-semibold text-gray-900">
              {isEditMode ? 'Edit' : 'Create'} {formType?.charAt(0).toUpperCase() + formType?.slice(1)}
            </h2>
            <p className="text-sm text-gray-500 mt-0.5">Fill in the details below</p>
          </div>
          <button
            type="button"
            onClick={closeForm}
            className="rounded-lg p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 transition-colors"
            aria-label="Close panel"
          >
            <svg className="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M6 18L18 6M6 6l12 12"
              />
            </svg>
          </button>
        </div>

        {error && (
          <div className="shrink-0 border-b border-red-200 bg-red-50 px-6 py-4">
            <p className="text-sm text-red-700">{error}</p>
          </div>
        )}

        {successMessage && (
          <div className="shrink-0 border-b border-green-200 bg-green-50 px-6 py-4">
            <p className="text-sm text-green-700">{successMessage}</p>
          </div>
        )}

        <div className="flex-1 flex flex-col min-h-0 overflow-hidden">
          {getFormComponent()}
        </div>
      </div>
    </>
  );
};

export default FormModal;
