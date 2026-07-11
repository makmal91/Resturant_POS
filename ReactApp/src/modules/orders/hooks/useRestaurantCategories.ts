import { useCallback, useEffect, useState } from 'react';
import { hasBranchContext } from '../../../types/permissions';
import { categoryService } from '../../category/categoryService';

export interface RestaurantCategory {
  id: number;
  name: string;
  displayOrder: number;
}

export function useRestaurantCategories(branchId: number) {
  const [categories, setCategories] = useState<RestaurantCategory[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const loadCategories = useCallback(async () => {
    if (!hasBranchContext(branchId)) {
      setCategories([]);
      return;
    }

    setLoading(true);
    setError('');
    try {
      const response = await categoryService.getAll(branchId, 1, 200, {
        sortBy: 'displayOrder',
        sortDirection: 'asc',
      });
      const rows = Array.isArray(response.data?.categories) ? response.data.categories : [];
      const active = rows
        .filter((item: { status?: boolean; categoryType?: string }) => item.status && item.categoryType === 'Sale')
        .map((item: { id: number; name: string; displayOrder?: number }) => ({
          id: Number(item.id),
          name: String(item.name ?? ''),
          displayOrder: Number(item.displayOrder ?? 0),
        }))
        .filter((item: RestaurantCategory) => item.id > 0 && item.name);

      setCategories(active);
    } catch {
      setError('Failed to load categories.');
      setCategories([]);
    } finally {
      setLoading(false);
    }
  }, [branchId]);

  useEffect(() => {
    void loadCategories();
  }, [loadCategories]);

  return { categories, loading, error, reload: loadCategories };
}
