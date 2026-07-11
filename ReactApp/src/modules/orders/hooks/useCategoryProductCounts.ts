import { useEffect, useState } from 'react';
import { hasBranchContext } from '../../../types/permissions';
import { productService } from '../../product/productService';

export function useCategoryProductCounts(branchId: number, categoryIds: number[]) {
  const [counts, setCounts] = useState<Record<number, number>>({});

  useEffect(() => {
    if (!hasBranchContext(branchId) || categoryIds.length === 0) {
      setCounts({});
      return;
    }

    let cancelled = false;

    Promise.all(
      categoryIds.map(async (categoryId) => {
        try {
          const response = await productService.getAll(branchId, 1, 1, {
            categoryId,
            status: true,
          });
          return { categoryId, count: response.data?.totalRecords ?? 0 };
        } catch {
          return { categoryId, count: 0 };
        }
      }),
    ).then((results) => {
      if (cancelled) return;
      setCounts(Object.fromEntries(results.map((r) => [r.categoryId, r.count])));
    });

    return () => {
      cancelled = true;
    };
  }, [branchId, categoryIds.join(',')]);

  return counts;
}
