import React from 'react';
import type { CategoryProduct } from '../hooks/useCategoryProducts';
import ProductCard from './ProductCard';
import SearchInput from './SearchInput';
import EmptyState from './EmptyState';

export interface ProductsGridProps {
  categoryName: string;
  products: CategoryProduct[];
  filteredProducts: CategoryProduct[];
  productSearch: string;
  loading: boolean;
  addingProductId: number | null;
  branchId: number;
  productSearchRef: React.RefObject<HTMLInputElement | null>;
  formatPrice: (price: number) => string;
  onProductSearchChange: (value: string) => void;
  onSelectProduct: (productId: number) => void;
}

const ProductsGrid: React.FC<ProductsGridProps> = React.memo(
  ({
    categoryName,
    products,
    filteredProducts,
    productSearch,
    loading,
    addingProductId,
    branchId,
    productSearchRef,
    formatPrice,
    onProductSearchChange,
    onSelectProduct,
  }) => (
    <main className="flex-1 min-w-0 flex flex-col min-h-0 rounded-lg border border-gray-200 bg-white overflow-hidden">
      <div className="p-4 border-b border-gray-200">
        <div className="mb-3">
          <h2 className="text-base font-semibold text-gray-900">{categoryName}</h2>
          <p className="text-xs text-gray-500 mt-0.5">
            {filteredProducts.length} of {products.length} products
          </p>
        </div>
        <SearchInput
          inputRef={productSearchRef}
          value={productSearch}
          onChange={onProductSearchChange}
          placeholder="Search products…"
          shortcutHint="/"
        />
      </div>

      <div className="flex-1 overflow-y-auto p-4">
        {loading && <p className="text-sm text-gray-500 text-center py-16">Loading products…</p>}

        {!loading && products.length === 0 && (
          <EmptyState title="No products in this category" subtitle="Add products or pick another category" />
        )}

        {!loading && products.length > 0 && filteredProducts.length === 0 && (
          <EmptyState title="No products found" subtitle={`No matches for "${productSearch.trim()}"`} />
        )}

        {!loading && filteredProducts.length > 0 && (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 xl:grid-cols-5 gap-3">
            {filteredProducts.map((product) => (
              <ProductCard
                key={product.id}
                productId={product.id}
                name={product.name}
                priceLabel={formatPrice(product.price)}
                branchId={branchId}
                hasImage={product.hasImage}
                disabled={addingProductId === product.id}
                onSelect={onSelectProduct}
              />
            ))}
          </div>
        )}
      </div>
    </main>
  ),
);

ProductsGrid.displayName = 'ProductsGrid';

export default ProductsGrid;
