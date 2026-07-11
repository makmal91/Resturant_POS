import React from 'react';
import type { RestaurantCategory } from '../hooks/useRestaurantCategories';
import CategoryButton from './CategoryButton';
import SearchInput from './SearchInput';
import EmptyState from './EmptyState';

export interface CategoriesSidebarProps {
  categories: RestaurantCategory[];
  filteredCategories: RestaurantCategory[];
  categoryCounts: Record<number, number>;
  selectedCategoryId: number | null;
  categorySearch: string;
  loading: boolean;
  onCategorySearchChange: (value: string) => void;
  onSelectCategory: (id: number) => void;
}

const CategoriesSidebar: React.FC<CategoriesSidebarProps> = React.memo(
  ({
    categories,
    filteredCategories,
    categoryCounts,
    selectedCategoryId,
    categorySearch,
    loading,
    onCategorySearchChange,
    onSelectCategory,
  }) => (
    <aside className="w-48 sm:w-56 lg:w-64 flex-shrink-0 flex flex-col min-h-0 rounded-lg border border-gray-200 bg-white overflow-hidden">
      <div className="p-4 border-b border-gray-200">
        <h2 className="text-sm font-semibold text-gray-900 mb-3">Categories</h2>
        <SearchInput
          value={categorySearch}
          onChange={onCategorySearchChange}
          placeholder="Filter categories…"
        />
      </div>

      <div className="flex-1 overflow-y-auto p-3 space-y-2">
        {loading && <p className="text-sm text-gray-500 text-center py-8">Loading…</p>}

        {!loading && categories.length === 0 && (
          <EmptyState title="No categories" subtitle="Add sale categories in settings" />
        )}

        {!loading && categories.length > 0 && filteredCategories.length === 0 && (
          <EmptyState title="No match" subtitle="Try a different search term" />
        )}

        {filteredCategories.map((category) => (
          <CategoryButton
            key={category.id}
            name={category.name}
            count={categoryCounts[category.id] ?? 0}
            selected={selectedCategoryId === category.id}
            onClick={() => onSelectCategory(category.id)}
          />
        ))}
      </div>
    </aside>
  ),
);

CategoriesSidebar.displayName = 'CategoriesSidebar';

export default CategoriesSidebar;
