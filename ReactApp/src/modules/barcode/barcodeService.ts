import apiClient from '../../services/api';
import { ProductBarcodePayload, ProductUnitPayload, ProductVariantPayload } from '../product/productService';

export interface BarcodePrintProduct {
  productId: number;
  productName: string;
  sku: string;
  primaryBarcode?: string | null;
  sellingPrice: number;
  stockQty: number;
  hasMultipleUnits: boolean;
  hasVariants: boolean;
  categoryId: number;
  categoryName: string;
  subCategoryId?: number | null;
  subCategoryName: string;
  brandId?: number | null;
  brandName: string;
}

export interface BarcodePrintItemsResponse {
  items: BarcodePrintProduct[];
  totalRecords: number;
  totalPages: number;
  currentPage: number;
  pageSize: number;
}

export interface ProductPrintDetails {
  productId: number;
  name: string;
  sku: string;
  sellingPrice: number;
  hasMultipleUnits: boolean;
  hasVariants: boolean;
  units: ProductUnitPayload[];
  variants: ProductVariantPayload[];
  barcodes: ProductBarcodePayload[];
}

export interface BarcodePrintFilters {
  search?: string;
  categoryId?: number | null;
  subCategoryId?: number | null;
  brandId?: number | null;
  inStock?: boolean;
  page?: number;
  pageSize?: number;
}

const branchRequestConfig = (branchId: number) => ({
  headers: { 'X-Branch-Id': String(branchId) },
});

export const barcodeService = {
  getItems: (branchId: number, filters: BarcodePrintFilters = {}) =>
    apiClient.get<BarcodePrintItemsResponse>('/barcode/items', {
      params: {
        branchId,
        page: filters.page ?? 1,
        pageSize: filters.pageSize ?? 50,
        ...(filters.search?.trim() ? { search: filters.search.trim() } : {}),
        ...(filters.categoryId ? { categoryId: filters.categoryId } : {}),
        ...(filters.subCategoryId ? { subCategoryId: filters.subCategoryId } : {}),
        ...(filters.brandId ? { brandId: filters.brandId } : {}),
        ...(filters.inStock ? { inStock: true } : {}),
      },
      ...branchRequestConfig(branchId),
    }),

  getProductDetails: (productId: number, branchId: number) =>
    apiClient.get<ProductPrintDetails>(`/products/${productId}/details`, {
      params: { branchId },
      ...branchRequestConfig(branchId),
    }),
};
