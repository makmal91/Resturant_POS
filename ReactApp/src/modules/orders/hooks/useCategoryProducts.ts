import { useCallback, useEffect, useRef, useState } from 'react';
import { hasBranchContext } from '../../../types/permissions';
import { productService, type ProductListItem } from '../../product/productService';
import { posService, type PosSearchGroup } from '../../pos/posService';

export interface CategoryProduct {
  id: number;
  name: string;
  code: string;
  price: number;
  categoryId: number;
  hasImage: boolean;
}

export function useCategoryProducts(
  branchId: number,
  categoryId: number | null,
  warehouseId: number,
) {
  const [products, setProducts] = useState<CategoryProduct[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const groupCacheRef = useRef<Map<number, PosSearchGroup>>(new Map());

  useEffect(() => {
    groupCacheRef.current.clear();
  }, [branchId, warehouseId]);

  const loadProducts = useCallback(async () => {
    if (!categoryId || !hasBranchContext(branchId)) {
      setProducts([]);
      return;
    }

    setLoading(true);
    setError('');
    try {
      const response = await productService.getAll(branchId, 1, 200, {
        categoryId,
        status: true,
        sortBy: 'productName',
        sortDirection: 'asc',
      });
      const rows: ProductListItem[] = Array.isArray(response.data?.products)
        ? response.data.products
        : [];
      setProducts(
        rows.map((item) => ({
          id: item.id,
          name: item.productName,
          code: item.productCode,
          price: item.sellingPrice,
          categoryId: item.categoryId,
          hasImage: item.hasImage,
        })),
      );
    } catch {
      setError('Failed to load products.');
      setProducts([]);
    } finally {
      setLoading(false);
    }
  }, [branchId, categoryId]);

  useEffect(() => {
    void loadProducts();
  }, [loadProducts]);

  const resolveProductGroup = useCallback(
    async (product: CategoryProduct): Promise<PosSearchGroup | null> => {
      const cached = groupCacheRef.current.get(product.id);
      if (cached) return cached;

      const searchTerm = product.code?.trim() || product.name.trim();
      if (!searchTerm) return null;

      const response = await posService.searchProductsGrouped(
        searchTerm,
        branchId,
        warehouseId || undefined,
      );
      const match =
        response.data.find((group) => group.productId === product.id) ?? response.data[0] ?? null;

      if (match) {
        groupCacheRef.current.set(product.id, match);
      }
      return match;
    },
    [branchId, warehouseId],
  );

  return { products, loading, error, resolveProductGroup, reload: loadProducts };
}
