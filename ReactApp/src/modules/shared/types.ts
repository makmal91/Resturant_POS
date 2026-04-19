export interface ManagementEntity {
  id: number;
  name: string;
  description: string;
  isActive: boolean;
  branchId?: number;
  categoryType?: 'Sale' | 'Inventory';
  menuCategoryId?: number;
  price?: number;
  tax?: number;
  preparationTime?: number;
  productType?: 'RawMaterial' | 'FinishedGood' | 'SemiFinished' | 'Service';
  isSaleable?: boolean;
  isInventoryItem?: boolean;
  isRecipeItem?: boolean;
  isPurchasable?: boolean;
  variants?: Array<{ name: string; price: number }>;
  addons?: Array<{ name: string; price: number }>;
}

export interface ManagementFormValues {
  name: string;
  description: string;
  isActive: boolean;
  branchId?: number;
  categoryType?: 'Sale' | 'Inventory';
  menuCategoryId?: number;
  price?: number;
  tax?: number;
  preparationTime?: number;
  productType?: 'RawMaterial' | 'FinishedGood' | 'SemiFinished' | 'Service';
  isSaleable?: boolean;
  isInventoryItem?: boolean;
  isRecipeItem?: boolean;
  isPurchasable?: boolean;
  variants?: Array<{ name: string; price: number }>;
  addons?: Array<{ name: string; price: number }>;
}

export const defaultManagementFormValues: ManagementFormValues = {
  name: '',
  description: '',
  isActive: true,
};

export interface CrudEntityService {
  getAll: () => Promise<{ data: unknown }>;
  getById: (id: number) => Promise<{ data: unknown }>;
  create: (data: ManagementFormValues) => Promise<unknown>;
  update: (id: number, data: ManagementFormValues) => Promise<unknown>;
  delete: (id: number) => Promise<unknown>;
}
