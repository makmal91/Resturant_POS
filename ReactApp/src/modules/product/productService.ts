import apiClient from '../../services/api';

export type DiscountType = 'Percentage' | 'Fixed';

export interface ProductUnitPayload {
  id?: number;
  unitName: string;
  conversionFactor: number;
  isBaseUnit: boolean;
  costPrice?: number | null;
  sellingPrice?: number | null;
  wholesalePrice?: number | null;
}

export interface ProductVariantPayload {
  id?: number;
  variantName: string;
  size?: string;
  color?: string;
  sku?: string;
  additionalPrice: number;
  costPriceOverride?: number | null;
  sellingPriceOverride?: number | null;
  status: boolean;
}

export interface ProductBarcodePayload {
  id?: number;
  barcodeValue: string;
  unitId?: number | null;
  variantId?: number | null;
  unitName?: string | null;
  variantName?: string | null;
  isPrimary: boolean;
}

export interface ProductImageInfo {
  id: number;
  fileName: string;
  contentType: string;
  isPrimary: boolean;
  sortOrder: number;
}

export interface ProductOpeningStockLine {
  variantName: string;
  variantId?: number | null;
  quantity: number;
  unitPrice?: number;
  totalAmount?: number;
}

export interface ProductPayload {
  id?: number;
  productName: string;
  productCode?: string;
  sku?: string;
  categoryId: number;
  subCategoryId?: number | null;
  brandId?: number | null;
  description?: string;
  status: boolean;
  costPrice: number;
  sellingPrice: number;
  wholesalePrice: number;
  isVariantEnabled: boolean;
  isDiscountAllowed: boolean;
  discountType?: DiscountType | null;
  discountValue: number;
  branchId: number;
  units: ProductUnitPayload[];
  variants: ProductVariantPayload[];
  barcodes: ProductBarcodePayload[];
  allowNegativeStock?: boolean;
  enableLowStockAlert?: boolean;
  lowStockAlertLevel?: number | null;
  openingStock?: number;
  openingStockWarehouseId?: number | null;
  openingStockVariantWise?: boolean;
  openingStockByVariant?: ProductOpeningStockLine[];
}

export interface ProductListItem {
  id: number;
  productName: string;
  productCode: string;
  sku: string;
  categoryId: number;
  categoryName: string;
  subCategoryId?: number | null;
  subCategoryName: string;
  brandId?: number | null;
  brandName: string;
  sellingPrice: number;
  status: boolean;
  hasImage: boolean;
  branchId: number;
  branchName: string;
  allowNegativeStock?: boolean;
  enableLowStockAlert?: boolean;
  lowStockAlertLevel?: number | null;
}

export interface ProductDetail extends ProductListItem {
  description: string;
  costPrice: number;
  wholesalePrice: number;
  isVariantEnabled: boolean;
  isDiscountAllowed: boolean;
  discountType?: DiscountType | null;
  discountValue: number;
  units: ProductUnitPayload[];
  variants: ProductVariantPayload[];
  barcodes: ProductBarcodePayload[];
  images: ProductImageInfo[];
  allowNegativeStock?: boolean;
  enableLowStockAlert?: boolean;
  lowStockAlertLevel?: number | null;
  openingStock?: number;
  hasOpeningStockApplied?: boolean;
  openingStockVariantWise?: boolean;
  openingStockByVariant?: ProductOpeningStockLine[];
}

export interface ProductListResponse {
  products: ProductListItem[];
  totalRecords: number;
  totalPages: number;
  currentPage: number;
  pageSize: number;
}

/** Derives per-unit prices from base prices and each unit's conversion factor. */
export const recalculateUnitPrices = (
  units: ProductUnitPayload[],
  baseCostPrice: number,
  baseSellingPrice: number,
  baseWholesalePrice: number,
): ProductUnitPayload[] =>
  units.map((unit) => {
    const factor = unit.conversionFactor > 0 ? unit.conversionFactor : 1;
    return {
      ...unit,
      costPrice: baseCostPrice * factor,
      sellingPrice: baseSellingPrice * factor,
      wholesalePrice: baseWholesalePrice * factor,
    };
  });

const branchRequestConfig = (branchId: number) => ({
  headers: { 'X-Branch-Id': String(branchId) },
});

export const productService = {
  getAll: (
    branchId: number,
    page = 1,
    pageSize = 25,
    filters: {
      search?: string;
      categoryId?: number | null;
      subCategoryId?: number | null;
      brandId?: number | null;
      status?: boolean | null;
      sortBy?: string;
      sortDirection?: 'asc' | 'desc';
    } = {}
  ) =>
    apiClient.get<ProductListResponse>('/products', {
      params: {
        branchId,
        page,
        pageSize,
        ...(filters.search ? { search: filters.search } : {}),
        ...(filters.categoryId ? { categoryId: filters.categoryId } : {}),
        ...(filters.subCategoryId ? { subCategoryId: filters.subCategoryId } : {}),
        ...(filters.brandId ? { brandId: filters.brandId } : {}),
        ...(filters.status !== null && filters.status !== undefined ? { status: filters.status } : {}),
        ...(filters.sortBy ? { sortBy: filters.sortBy } : {}),
        ...(filters.sortDirection ? { sortDirection: filters.sortDirection } : {}),
      },
      ...branchRequestConfig(branchId),
    }),

  getById: (id: number, branchId: number) =>
    apiClient.get<ProductDetail>(`/products/${id}`, {
      params: { branchId },
      ...branchRequestConfig(branchId),
    }),

  create: (data: ProductPayload, branchId: number) =>
    apiClient.post<ProductDetail>('/products', normalizePayload(data, branchId), branchRequestConfig(branchId)),

  update: (id: number, data: ProductPayload, branchId: number) =>
    apiClient.put<ProductDetail>(`/products/${id}`, normalizePayload(data, branchId), branchRequestConfig(branchId)),

  updateUnits: (id: number, branchId: number, items: ProductUnitPayload[]) =>
    apiClient.put<ProductDetail>(`/products/${id}/units`, { branchId, items }, branchRequestConfig(branchId)),

  updateVariants: (id: number, branchId: number, items: ProductVariantPayload[]) =>
    apiClient.put<ProductDetail>(`/products/${id}/variants`, { branchId, items }, branchRequestConfig(branchId)),

  updateBarcodes: (id: number, branchId: number, items: ProductBarcodePayload[]) =>
    apiClient.put<ProductDetail>(`/products/${id}/barcodes`, { branchId, items }, branchRequestConfig(branchId)),

  uploadImages: (id: number, branchId: number, files: File[], isPrimary = false) => {
    const formData = new FormData();
    formData.append('branchId', String(branchId));
    formData.append('isPrimary', String(isPrimary));
    files.forEach((file) => formData.append('images', file));
    return apiClient.post<ProductDetail>(`/products/${id}/images`, formData, branchRequestConfig(branchId));
  },

  getImageEndpoint: (productId: number, imageId: number) => `/products/${productId}/images/${imageId}`,

  deleteImage: (productId: number, imageId: number, branchId: number) =>
    apiClient.delete(`/products/${productId}/images/${imageId}`, {
      params: { branchId },
      ...branchRequestConfig(branchId),
    }),
};

const normalizePayload = (data: ProductPayload, branchId: number) => ({
  productName: data.productName,
  productCode: data.productCode ?? '',
  sku: data.sku ?? '',
  categoryId: Number(data.categoryId ?? 0),
  subCategoryId: data.subCategoryId ? Number(data.subCategoryId) : null,
  brandId: data.brandId ? Number(data.brandId) : null,
  description: data.description ?? '',
  status: data.status,
  costPrice: Number(data.costPrice ?? 0),
  sellingPrice: Number(data.sellingPrice ?? 0),
  wholesalePrice: Number(data.wholesalePrice ?? 0),
  isVariantEnabled: data.isVariantEnabled,
  isDiscountAllowed: data.isDiscountAllowed,
  discountType: data.isDiscountAllowed ? data.discountType : null,
  discountValue: data.isDiscountAllowed ? Number(data.discountValue ?? 0) : 0,
  allowNegativeStock: Boolean(data.allowNegativeStock),
  enableLowStockAlert: Boolean(data.enableLowStockAlert),
  lowStockAlertLevel: data.enableLowStockAlert && data.lowStockAlertLevel != null
    ? Number(data.lowStockAlertLevel)
    : null,
  openingStock: Number(data.openingStock ?? 0),
  openingStockWarehouseId: data.openingStockWarehouseId ? Number(data.openingStockWarehouseId) : null,
  openingStockVariantWise: Boolean(data.openingStockVariantWise && data.isVariantEnabled),
  openingStockByVariant: data.openingStockVariantWise && data.isVariantEnabled
    ? (data.openingStockByVariant ?? []).map((line) => ({
        variantName: line.variantName,
        variantId: line.variantId ?? null,
        quantity: Number(line.quantity ?? 0),
      }))
    : [],
  branchId,
  units: data.units ?? [],
  variants: data.isVariantEnabled ? data.variants ?? [] : [],
  barcodes: data.barcodes ?? [],
});
