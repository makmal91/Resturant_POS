import apiClient from '../../services/api';
import { ManagementFormValues } from '../shared/types';

const DEFAULT_BRANCH_ID = 1;

export const productService = {
  getAll: () => apiClient.get('/products', { params: { branchId: DEFAULT_BRANCH_ID } }),
  getById: (id: number) => apiClient.get(`/products/${id}`, { params: { branchId: DEFAULT_BRANCH_ID } }),
  create: (data: ManagementFormValues) =>
    apiClient.post('/products', {
      name: data.name,
      description: data.description,
      price: Number(data.price ?? 0),
      tax: Number(data.tax ?? 0),
      preparationTime: Number(data.preparationTime ?? 0),
      menuCategoryId: Number(data.menuCategoryId ?? 0),
      branchId: Number(data.branchId ?? DEFAULT_BRANCH_ID),
      productType: data.productType ?? 'FinishedGood',
      isSaleable: data.isSaleable,
      isInventoryItem: data.isInventoryItem,
      isRecipeItem: data.isRecipeItem,
      isPurchasable: data.isPurchasable,
      variants: data.variants ?? [],
      addons: data.addons ?? [],
    }),
  update: (id: number, data: ManagementFormValues) =>
    apiClient.put(`/products/${id}`, {
      name: data.name,
      description: data.description,
      price: Number(data.price ?? 0),
      tax: Number(data.tax ?? 0),
      preparationTime: Number(data.preparationTime ?? 0),
      menuCategoryId: Number(data.menuCategoryId ?? 0),
      branchId: Number(data.branchId ?? DEFAULT_BRANCH_ID),
      productType: data.productType ?? 'FinishedGood',
      isSaleable: data.isSaleable,
      isInventoryItem: data.isInventoryItem,
      isRecipeItem: data.isRecipeItem,
      isPurchasable: data.isPurchasable,
    }),
  delete: (id: number) => apiClient.delete(`/products/${id}`, { params: { branchId: DEFAULT_BRANCH_ID } }),
};
