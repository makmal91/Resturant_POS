import React, { useMemo, useEffect, useState } from 'react';
import { useFormModal } from '../contexts/FormModalContext';
import { BranchForm, BusinessForm, UserForm, MenuForm, InventoryForm, CategoryForm, SubCategoryForm, BrandForm, WarehouseForm, SupplierForm, PurchaseForm, CustomerForm } from './forms';
import { BranchService, BusinessService, MenuService, InventoryService } from '../services/apiService';
import { getApiErrorMessage } from '../services/api';
import { categoryService } from '../modules/category/categoryService';
import { subCategoryService } from '../modules/subcategory/subcategoryService';
import { brandService } from '../modules/brand/brandService';
import { warehouseService } from '../modules/warehouse/warehouseService';
import { supplierService } from '../modules/supplier/supplierService';
import { purchaseService } from '../modules/purchase/purchaseService';
import { customerService as apiCustomerService } from '../modules/customer/customerService';
import { userService, RoleListItem } from '../modules/user/userService';
import { useBranchStore } from '../stores/useBranchStore';
import { useIsGlobalAdmin, useIsMasterUser } from '../hooks/usePermission';
import { isProtectedRole } from '../types/permissions';

interface MenuCategoryOption {
  id: string;
  name: string;
}

interface LookupOption {
  id: number;
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
  const [purchaseSuppliers, setPurchaseSuppliers] = useState<LookupOption[]>([]);
  const [purchaseWarehouses, setPurchaseWarehouses] = useState<LookupOption[]>([]);
  const [isPurchaseMetaLoading, setIsPurchaseMetaLoading] = useState(false);
  const [userRoles, setUserRoles] = useState<RoleListItem[]>([]);
  const [isUserMetaLoading, setIsUserMetaLoading] = useState(false);
  const branches = useBranchStore((state) => state.branches);
  const selectedBranchId = useBranchStore((state) => state.selectedBranchId);
  const fetchBranches = useBranchStore((state) => state.fetchBranches);
  const isMasterUser = useIsMasterUser();
  const isGlobalAdmin = useIsGlobalAdmin();
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

  useEffect(() => {
    let isCancelled = false;

    const loadUserMeta = async () => {
      if (!isOpen || formType !== 'user') {
        return;
      }

      setIsUserMetaLoading(true);
      try {
        await fetchBranches();
        const roles = await userService.getRoles();
        if (!isCancelled) {
          const availableRoles = roles.filter((role) => {
            if (!role.isActive) {
              return false;
            }

            if (isMasterUser) {
              return true;
            }

            if (isGlobalAdmin) {
              return !isProtectedRole(role.name);
            }

            return true;
          });
          setUserRoles(availableRoles);
        }
      } catch (err) {
        if (!isCancelled) {
          setUserRoles([]);
          setError(getApiErrorMessage(err, 'Failed to load user form data.'));
        }
      } finally {
        if (!isCancelled) {
          setIsUserMetaLoading(false);
        }
      }
    };

    loadUserMeta();

    return () => {
      isCancelled = true;
    };
  }, [isOpen, formType, fetchBranches, isGlobalAdmin, isMasterUser]);

  useEffect(() => {
    let isCancelled = false;

    const loadPurchaseMeta = async () => {
      if (!isOpen || formType !== 'purchase') {
        return;
      }

      const branchId = Number(editingData?.branchId ?? selectedBranchId ?? 0);
      if (branchId <= 0) {
        setPurchaseSuppliers([]);
        setPurchaseWarehouses([]);
        return;
      }

      setIsPurchaseMetaLoading(true);
      try {
        const [suppliersRes, warehousesRes] = await Promise.all([
          supplierService.getAllActive(branchId),
          warehouseService.getAllActive(branchId),
        ]);

        if (!isCancelled) {
          setPurchaseSuppliers(
            (Array.isArray(suppliersRes.data) ? suppliersRes.data : []).map((item) => ({
              id: Number((item as { id?: number }).id ?? 0),
              name: String((item as { name?: string }).name ?? ''),
            })).filter((item) => item.id > 0)
          );
          setPurchaseWarehouses(
            (Array.isArray(warehousesRes.data) ? warehousesRes.data : []).map((item) => ({
              id: Number((item as { id?: number }).id ?? 0),
              name: String((item as { name?: string }).name ?? ''),
            })).filter((item) => item.id > 0)
          );
        }
      } catch (err) {
        if (!isCancelled) {
          setPurchaseSuppliers([]);
          setPurchaseWarehouses([]);
          setError(getApiErrorMessage(err, 'Failed to load purchase form data.'));
        }
      } finally {
        if (!isCancelled) {
          setIsPurchaseMetaLoading(false);
        }
      }
    };

    void loadPurchaseMeta();

    return () => {
      isCancelled = true;
    };
  }, [isOpen, formType, editingData?.branchId, selectedBranchId]);

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

    const payload = {
      fullName: String(data.fullName ?? '').trim(),
      username: String(data.username ?? '').trim(),
      email: String(data.email ?? '').trim(),
      phone: String(data.phone ?? '').trim(),
      password: String(data.password ?? '').trim() || undefined,
      roleId: Number(data.roleId),
      isActive: String(data.status) !== 'Inactive',
      branchIds: Array.isArray(data.branchIds) ? data.branchIds.map(Number).filter((id: number) => id > 0) : [],
    };

    if (payload.branchIds.length === 0 && selectedBranchId && selectedBranchId > 0) {
      payload.branchIds = [selectedBranchId];
    }

    const branchId =
      selectedBranchId && selectedBranchId > 0
        ? selectedBranchId
        : payload.branchIds[0] ?? 0;

    if (branchId <= 0 && payload.branchIds.length === 0) {
      setError('At least one branch must be selected.');
      setIsSubmitting(false);
      return;
    }

    if (!payload.fullName || !payload.username || !payload.email || !payload.roleId) {
      setError('Full name, username, email, and role are required.');
      setIsSubmitting(false);
      return;
    }

    if (!isEditMode && !payload.password) {
      setError('Password is required for new users.');
      setIsSubmitting(false);
      return;
    }

    if (payload.branchIds.length === 0) {
      setError('At least one branch must be selected.');
      setIsSubmitting(false);
      return;
    }

    try {
      if (isEditMode) {
        await userService.update(editingData.id, payload, branchId);
      } else {
        await userService.create(payload, branchId);
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

  const handleSubCategorySubmit = async (data: any) => {
    setIsSubmitting(true);
    setError(null);
    setSuccessMessage(null);

    const branchId = Number(data?.branchId ?? 0);
    const categoryId = Number(data?.categoryId ?? 0);
    const name = String(data?.name ?? '').trim();

    if (!name) {
      setError('Sub category name is required.');
      setIsSubmitting(false);
      return;
    }

    if (branchId <= 0) {
      setError('Branch selection is required.');
      setIsSubmitting(false);
      return;
    }

    if (categoryId <= 0) {
      setError('Category selection is required.');
      setIsSubmitting(false);
      return;
    }

    const formData = new FormData();
    formData.append('name', name);
    formData.append('code', String(data?.code ?? '').trim());
    formData.append('description', String(data?.description ?? '').trim());
    formData.append('displayOrder', String(Number(data?.displayOrder ?? 0)));
    formData.append('icon', String(data?.icon ?? '').trim());
    formData.append('status', String(data?.status ?? 'Active').toLowerCase() !== 'inactive' ? 'Active' : 'Inactive');
    formData.append('categoryId', String(categoryId));
    formData.append('branchId', String(branchId));

    const hasImageUpload = data?.imageFile instanceof File;
    const hasImageRemoval = Boolean(data?.removeImage);

    if (hasImageUpload) {
      formData.append('imageFile', data.imageFile);
    }

    if (hasImageRemoval) {
      formData.append('removeImage', 'true');
    }

    const jsonPayload = {
      name,
      code: String(data?.code ?? '').trim(),
      description: String(data?.description ?? '').trim(),
      displayOrder: Number(data?.displayOrder ?? 0),
      icon: String(data?.icon ?? '').trim(),
      status: String(data?.status ?? 'Active').toLowerCase() !== 'inactive',
      categoryId,
      branchId,
    };

    try {
      useBranchStore.getState().setSelectedBranchId(branchId);
      if (hasImageUpload || hasImageRemoval) {
        if (isEditMode) {
          await subCategoryService.update(Number(editingData.id), formData, branchId);
        } else {
          await subCategoryService.create(formData, branchId);
        }
      } else if (isEditMode) {
        await subCategoryService.updateJson(Number(editingData.id), jsonPayload, branchId);
      } else {
        await subCategoryService.createJson(jsonPayload, branchId);
      }
      closeWithSuccess(
        isEditMode ? 'Sub category updated successfully.' : 'Sub category created successfully.'
      );
    } catch (err: any) {
      setError(getApiErrorMessage(err, 'Failed to save sub category'));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleBrandSubmit = async (data: any) => {
    setIsSubmitting(true);
    setError(null);
    setSuccessMessage(null);

    const branchId = Number(data?.branchId ?? 0);
    const name = String(data?.name ?? '').trim();

    if (!name) {
      setError('Brand name is required.');
      setIsSubmitting(false);
      return;
    }

    if (branchId <= 0) {
      setError('Branch selection is required.');
      setIsSubmitting(false);
      return;
    }

    const formData = new FormData();
    formData.append('name', name);
    formData.append('description', String(data?.description ?? '').trim());
    formData.append('status', String(data?.status ?? 'Active').toLowerCase() !== 'inactive' ? 'Active' : 'Inactive');
    formData.append('branchId', String(branchId));

    const hasImageUpload = data?.imageFile instanceof File;
    const hasImageRemoval = Boolean(data?.removeImage);

    if (hasImageUpload) {
      formData.append('imageFile', data.imageFile);
    }

    if (hasImageRemoval) {
      formData.append('removeImage', 'true');
    }

    const jsonPayload = {
      name,
      description: String(data?.description ?? '').trim(),
      status: String(data?.status ?? 'Active').toLowerCase() !== 'inactive',
      branchId,
    };

    try {
      useBranchStore.getState().setSelectedBranchId(branchId);
      if (hasImageUpload || hasImageRemoval) {
        if (isEditMode) {
          await brandService.update(Number(editingData.id), formData, branchId);
        } else {
          await brandService.create(formData, branchId);
        }
      } else if (isEditMode) {
        await brandService.updateJson(Number(editingData.id), jsonPayload, branchId);
      } else {
        await brandService.createJson(jsonPayload, branchId);
      }
      closeWithSuccess(isEditMode ? 'Brand updated successfully.' : 'Brand created successfully.');
    } catch (err: any) {
      setError(getApiErrorMessage(err, 'Failed to save brand'));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleWarehouseSubmit = async (data: any) => {
    setIsSubmitting(true);
    setError(null);
    setSuccessMessage(null);

    const branchId = Number(data?.branchId ?? 0);
    const name = String(data?.name ?? '').trim();

    if (!name) {
      setError('Warehouse name is required.');
      setIsSubmitting(false);
      return;
    }

    if (branchId <= 0) {
      setError('Branch selection is required.');
      setIsSubmitting(false);
      return;
    }

    const payload = {
      name,
      code: String(data?.code ?? '').trim(),
      address: String(data?.address ?? '').trim(),
      isActive: String(data?.status ?? 'Active').toLowerCase() !== 'inactive',
      branchId,
    };

    try {
      useBranchStore.getState().setSelectedBranchId(branchId);
      if (isEditMode) {
        await warehouseService.update(Number(editingData.id), payload);
      } else {
        await warehouseService.create(payload);
      }
      closeWithSuccess(isEditMode ? 'Warehouse updated successfully.' : 'Warehouse created successfully.');
    } catch (err: any) {
      setError(getApiErrorMessage(err, 'Failed to save warehouse'));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleSupplierSubmit = async (data: any) => {
    setIsSubmitting(true);
    setError(null);
    setSuccessMessage(null);

    const branchId = Number(data?.branchId ?? 0);
    const name = String(data?.name ?? '').trim();

    if (!name) {
      setError('Supplier name is required.');
      setIsSubmitting(false);
      return;
    }

    if (branchId <= 0) {
      setError('Branch selection is required.');
      setIsSubmitting(false);
      return;
    }

    const payload = {
      supplierCode: String(data?.supplierCode ?? '').trim() || undefined,
      name,
      contactPerson: String(data?.contactPerson ?? '').trim(),
      phone: String(data?.phone ?? '').trim(),
      email: String(data?.email ?? '').trim(),
      address: String(data?.address ?? '').trim(),
      taxNumber: String(data?.taxNumber ?? '').trim(),
      isActive: String(data?.status ?? 'Active').toLowerCase() !== 'inactive',
      branchId,
    };

    try {
      useBranchStore.getState().setSelectedBranchId(branchId);
      if (isEditMode) {
        await supplierService.update(Number(editingData.id), payload);
      } else {
        await supplierService.create(payload);
      }
      closeWithSuccess(isEditMode ? 'Supplier updated successfully.' : 'Supplier created successfully.');
    } catch (err: any) {
      setError(getApiErrorMessage(err, 'Failed to save supplier'));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCustomerSubmit = async (data: any) => {
    setIsSubmitting(true);
    setError(null);
    setSuccessMessage(null);

    const branchId = Number(data?.branchId ?? 0);
    const name = String(data?.name ?? '').trim();

    if (!name) { setError('Customer name is required.'); setIsSubmitting(false); return; }
    if (branchId <= 0) { setError('Branch selection is required.'); setIsSubmitting(false); return; }

    const typeMap: Record<string, number> = { Retail: 1, Wholesale: 2, VIP: 3 };

    const payload = {
      customerCode: String(data?.customerCode ?? '').trim() || undefined,
      name,
      phone:           String(data?.phone ?? '').trim() || undefined,
      email:           String(data?.email ?? '').trim() || undefined,
      address:         String(data?.address ?? '').trim() || undefined,
      city:            String(data?.city ?? '').trim() || undefined,
      cnic:            String(data?.cnic ?? '').trim() || undefined,
      customerType:    typeMap[String(data?.customerType ?? 'Retail')] ?? 1,
      creditLimit:     parseFloat(String(data?.creditLimit ?? '0')) || 0,
      openingBalance:  parseFloat(String(data?.openingBalance ?? '0')) || 0,
      status:          String(data?.status ?? 'Active').toLowerCase() !== 'inactive',
      branchId,
    };

    try {
      useBranchStore.getState().setSelectedBranchId(branchId);
      if (isEditMode) {
        await apiCustomerService.update(Number(editingData.id), payload);
      } else {
        await apiCustomerService.create(payload);
      }
      closeWithSuccess(isEditMode ? 'Customer updated successfully.' : 'Customer created successfully.');
    } catch (err: any) {
      setError(getApiErrorMessage(err, 'Failed to save customer.'));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handlePurchaseSubmit = async (data: any, mode: 'draft' | 'post' = 'draft') => {
    setIsSubmitting(true);
    setError(null);
    setSuccessMessage(null);

    const branchId = Number(data?.branchId ?? 0);
    const invoiceNo = String(data?.invoiceNo ?? '').trim();
    const supplierId = Number(data?.supplierId ?? 0);
    const warehouseId = Number(data?.warehouseId ?? 0);

    if (branchId <= 0 || supplierId <= 0 || warehouseId <= 0) {
      setError('Branch, supplier, and warehouse are required.');
      setIsSubmitting(false);
      return;
    }

    const items = Array.isArray(data?.items) ? data.items : [];
    if (items.length === 0) {
      setError('At least one purchase item is required.');
      setIsSubmitting(false);
      return;
    }

    const payload = {
      invoiceNo,
      supplierId,
      warehouseId,
      purchaseDate: new Date(String(data?.purchaseDate ?? new Date().toISOString())).toISOString(),
      notes: String(data?.notes ?? '').trim(),
      branchId,
      items,
    };

    try {
      useBranchStore.getState().setSelectedBranchId(branchId);
      let savedId: number;
      if (isEditMode) {
        const res = await purchaseService.update(Number(editingData.id), payload);
        savedId = Number((res.data as any)?.id ?? editingData.id);
      } else {
        const res = await purchaseService.create(payload);
        savedId = Number((res.data as any)?.id ?? 0);
      }

      if (mode === 'post' && savedId > 0) {
        await purchaseService.post(savedId, branchId);
        closeWithSuccess(isEditMode ? 'Purchase updated and posted to stock.' : 'Purchase created and posted to stock.');
      } else {
        closeWithSuccess(isEditMode ? 'Purchase updated successfully.' : 'Purchase saved as draft.');
      }
    } catch (err: any) {
      setError(getApiErrorMessage(err, 'Failed to save purchase'));
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
            branches={branches}
            roles={userRoles}
            isLoading={isSubmitting || isUserMetaLoading}
            isEditMode={isEditMode}
            submitLabel={isEditMode ? 'Update User' : 'Create User'}
            lockToActiveBranch={!isGlobalAdmin && Boolean(selectedBranchId && selectedBranchId > 0)}
            activeBranchId={selectedBranchId && selectedBranchId > 0 ? selectedBranchId : 0}
            activeBranchName={
              branches.find((branch) => branch.id === selectedBranchId)?.name ?? ''
            }
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
      case 'subcategory':
        return (
          <SubCategoryForm
            initialData={editingData}
            onSubmit={handleSubCategorySubmit}
            isLoading={isSubmitting}
            submitLabel={isEditMode ? 'Update Sub Category' : 'Create Sub Category'}
            lockBranch={isEditMode || (!isGlobalAdmin && Boolean(selectedBranchId && selectedBranchId > 0))}
          />
        );
      case 'brand':
        return (
          <BrandForm
            initialData={editingData}
            onSubmit={handleBrandSubmit}
            isLoading={isSubmitting}
            submitLabel={isEditMode ? 'Update Brand' : 'Create Brand'}
            lockBranch={isEditMode || (!isGlobalAdmin && Boolean(selectedBranchId && selectedBranchId > 0))}
          />
        );
      case 'warehouse':
        return (
          <WarehouseForm
            initialData={editingData}
            onSubmit={handleWarehouseSubmit}
            isLoading={isSubmitting}
            submitLabel={isEditMode ? 'Update Warehouse' : 'Create Warehouse'}
            lockBranch={isEditMode || (!isGlobalAdmin && Boolean(selectedBranchId && selectedBranchId > 0))}
          />
        );
      case 'supplier':
        return (
          <SupplierForm
            initialData={editingData}
            onSubmit={handleSupplierSubmit}
            isLoading={isSubmitting}
            submitLabel={isEditMode ? 'Update Supplier' : 'Create Supplier'}
            lockBranch={isEditMode || (!isGlobalAdmin && Boolean(selectedBranchId && selectedBranchId > 0))}
          />
        );
      case 'purchase':
        return (
          <PurchaseForm
            initialData={editingData}
            suppliers={purchaseSuppliers}
            warehouses={purchaseWarehouses}
            onSubmit={handlePurchaseSubmit}
            isLoading={isSubmitting || isPurchaseMetaLoading}
            lockBranch={isEditMode || (!isGlobalAdmin && Boolean(selectedBranchId && selectedBranchId > 0))}
          />
        );
      case 'customer':
        return (
          <CustomerForm
            initialData={editingData}
            onSubmit={handleCustomerSubmit}
            isLoading={isSubmitting}
            submitLabel={isEditMode ? 'Update Customer' : 'Create Customer'}
            lockBranch={isEditMode || (!isGlobalAdmin && Boolean(selectedBranchId && selectedBranchId > 0))}
          />
        );
      default:
        return null;
    }
  };

  if (!isRendered) return null;

  const panelWidthClass =
    formType === 'business' ||
    formType === 'branch' ||
    formType === 'category' ||
    formType === 'subcategory' ||
    formType === 'brand' ||
    formType === 'warehouse' ||
    formType === 'supplier' ||
    formType === 'purchase' ||
    formType === 'customer' ||
    formType === 'user'
      ? 'max-w-4xl'
      : 'max-w-2xl';

  const formTypeLabel =
    formType === 'subcategory'
      ? 'Sub Category'
      : formType === 'brand'
        ? 'Brand'
        : formType === 'warehouse'
          ? 'Warehouse'
          : formType === 'supplier'
            ? 'Supplier'
            : formType === 'purchase'
            ? 'Purchase'
            : formType === 'customer'
              ? 'Customer'
              : formType
                ? formType.charAt(0).toUpperCase() + formType.slice(1)
                : '';

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
              {isEditMode ? 'Edit' : 'Create'} {formTypeLabel}
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
