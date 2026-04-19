import React, { useMemo, useEffect, useState } from 'react';
import { useFormModal } from '../contexts/FormModalContext';
import { BranchForm, UserForm, MenuForm, InventoryForm } from './forms';
import { BranchService, UserService, MenuService, InventoryService } from '../services/apiService';
import { getApiErrorMessage } from '../services/api';

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

const FormModal: React.FC = () => {
  const { isOpen, formType, editingData, closeForm } = useFormModal();
  const isEditMode = editingData?.id != null;
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [menuCategories, setMenuCategories] = useState<MenuCategoryOption[]>([]);
  const [isMenuCategoriesLoading, setIsMenuCategoriesLoading] = useState(false);
  const [menuCategoriesError, setMenuCategoriesError] = useState<string | null>(null);

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
    try {
      if (isEditMode) {
        // Update
        await BranchService.update(editingData.id, data);
      } else {
        // Create
        await BranchService.create(data);
      }
      closeWithSuccess(isEditMode ? 'Branch updated successfully.' : 'Branch created successfully.');
    } catch (err: any) {
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

  if (!isOpen) return null;

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
      default:
        return null;
    }
  };

  return (
    <>
      {/* Modal Backdrop */}
      <div
        className="fixed inset-0 bg-black bg-opacity-50 z-40 transition-opacity"
        onClick={closeForm}
      />

      {/* Modal Container */}
      <div className="fixed inset-0 z-50 overflow-y-auto flex items-center justify-center">
        <div
          className="bg-white rounded-lg shadow-xl max-w-2xl w-full mx-4 max-h-[90vh] overflow-y-auto"
          onClick={(e) => e.stopPropagation()}
        >
          {/* Modal Header */}
          <div className="sticky top-0 bg-white border-b border-gray-200 px-6 py-4 flex justify-between items-center">
            <h2 className="text-xl font-semibold text-gray-900">
              {isEditMode ? 'Edit' : 'Create'} {formType?.charAt(0).toUpperCase() + formType?.slice(1)}
            </h2>
            <button
              onClick={closeForm}
              className="text-gray-400 hover:text-gray-600 transition-colors"
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

          {/* Error Message */}
          {error && (
            <div className="border-b border-red-200 bg-red-50 px-6 py-4">
              <p className="text-sm text-red-700">{error}</p>
            </div>
          )}

          {successMessage && (
            <div className="border-b border-green-200 bg-green-50 px-6 py-4">
              <p className="text-sm text-green-700">{successMessage}</p>
            </div>
          )}

          {/* Modal Body */}
          <div className="p-6">
            {getFormComponent()}
          </div>
        </div>
      </div>
    </>
  );
};

export default FormModal;
