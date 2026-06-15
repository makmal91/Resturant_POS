import React, { useCallback, useEffect, useMemo, useState } from 'react';
import Badge from '../../components/Badge';
import AuthenticatedImage from '../../components/AuthenticatedImage';
import { getApiErrorMessage } from '../../services/api';
import apiClient from '../../services/api';
import { useBranchWriteAccess } from '../../hooks/useBranchWriteAccess';
import ProductForm from './ProductForm';
import { ProductDetail, ProductListItem, ProductPayload, productService } from './productService';

const fallbackUnits = [
  { id: 1, name: 'Piece', conversionFactor: 1 },
  { id: 2, name: 'Box', conversionFactor: 1 },
  { id: 3, name: 'Pack', conversionFactor: 1 },
  { id: 4, name: 'Kg', conversionFactor: 1 },
  { id: 5, name: 'Gram', conversionFactor: 0.001 },
  { id: 6, name: 'Liter', conversionFactor: 1 },
  { id: 7, name: 'Meter', conversionFactor: 1 },
];

const ProductPage: React.FC = () => {
  const { selectedBranchId, canWriteInView, getWriteBlockMessage } = useBranchWriteAccess();
  const [products, setProducts] = useState<ProductListItem[]>([]);
  const [selectedProduct, setSelectedProduct] = useState<ProductDetail | null>(null);
  const [editingProduct, setEditingProduct] = useState<ProductDetail | null>(null);
  const [categories, setCategories] = useState<Array<{ id: number; name: string }>>([]);
  const [subCategories, setSubCategories] = useState<Array<{ id: number; name: string; categoryId: number }>>([]);
  const [brands, setBrands] = useState<Array<{ id: number; name: string }>>([]);
  const [units, setUnits] = useState<Array<{ id: number; name: string; conversionFactor?: number }>>(fallbackUnits);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [totalRecords, setTotalRecords] = useState(0);
  const [search, setSearch] = useState('');
  const [categoryFilter, setCategoryFilter] = useState<number | null>(null);
  const [subCategoryFilter, setSubCategoryFilter] = useState<number | null>(null);
  const [brandFilter, setBrandFilter] = useState<number | null>(null);
  const [statusFilter, setStatusFilter] = useState<boolean | null>(null);
  const [notification, setNotification] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  const branchId = selectedBranchId && selectedBranchId > 0 ? selectedBranchId : 0;
  const pageSize = 10;

  const showNotification = useCallback((type: 'success' | 'error', message: string) => {
    setNotification({ type, message });
    window.setTimeout(() => setNotification(null), 4000);
  }, []);

  const loadMasters = useCallback(async () => {
    if (branchId <= 0) {
      setCategories([]);
      setSubCategories([]);
      setBrands([]);
      setUnits(fallbackUnits);
      return;
    }

    try {
      const [categoryResponse, subCategoryResponse, brandResponse, unitResponse] = await Promise.all([
        apiClient.get('/categories', { params: { branchId, pageSize: 100 }, headers: { 'X-Branch-Id': String(branchId) } }),
        apiClient.get('/subcategories', { params: { branchId, pageSize: 100 }, headers: { 'X-Branch-Id': String(branchId) } }),
        apiClient.get('/brands', { params: { branchId, pageSize: 100 }, headers: { 'X-Branch-Id': String(branchId) } }),
        apiClient.get('/units', { params: { branchId }, headers: { 'X-Branch-Id': String(branchId) } }).catch(() => null),
      ]);

      setCategories(
        (Array.isArray(categoryResponse.data?.categories) ? categoryResponse.data.categories : [])
          .map((item: any) => ({ id: Number(item.id ?? 0), name: String(item.name ?? '') }))
          .filter((item: { id: number; name: string }) => item.id > 0)
      );
      setSubCategories(
        (Array.isArray(subCategoryResponse.data?.subCategories) ? subCategoryResponse.data.subCategories : [])
          .map((item: any) => ({ id: Number(item.id ?? 0), name: String(item.name ?? ''), categoryId: Number(item.categoryId ?? 0) }))
          .filter((item: { id: number; name: string; categoryId: number }) => item.id > 0)
      );
      setBrands(
        (Array.isArray(brandResponse.data?.brands) ? brandResponse.data.brands : [])
          .map((item: any) => ({ id: Number(item.id ?? 0), name: String(item.name ?? '') }))
          .filter((item: { id: number; name: string }) => item.id > 0)
      );
      const rawUnits = unitResponse?.data
        ? Array.isArray(unitResponse.data)
          ? unitResponse.data
          : Array.isArray(unitResponse.data.units)
            ? unitResponse.data.units
            : Array.isArray(unitResponse.data.items)
              ? unitResponse.data.items
              : []
        : [];
      const normalizedUnits = rawUnits
        .map((item: any) => ({
          id: Number(item.id ?? item.Id ?? 0),
          name: String(item.name ?? item.Name ?? item.unitName ?? item.UnitName ?? ''),
          conversionFactor: Number(item.conversionFactor ?? item.ConversionFactor ?? 1),
        }))
        .filter((item: { id: number; name: string; conversionFactor: number }) => item.id > 0 && item.name);
      setUnits(normalizedUnits.length > 0 ? normalizedUnits : fallbackUnits);
    } catch (error) {
      showNotification('error', getApiErrorMessage(error, 'Failed to load product master data.'));
      setUnits(fallbackUnits);
    }
  }, [branchId, showNotification]);

  const loadProducts = useCallback(async () => {
    if (branchId <= 0) {
      setProducts([]);
      setTotalPages(0);
      setTotalRecords(0);
      return;
    }

    setLoading(true);
    try {
      const response = await productService.getAll(branchId, page, pageSize, {
        search: search.trim() || undefined,
        categoryId: categoryFilter,
        subCategoryId: subCategoryFilter,
        brandId: brandFilter,
        status: statusFilter,
      });
      setProducts(Array.isArray(response.data.products) ? response.data.products : []);
      setTotalPages(Number(response.data.totalPages ?? 0));
      setTotalRecords(Number(response.data.totalRecords ?? 0));
    } catch (error) {
      setProducts([]);
      showNotification('error', getApiErrorMessage(error, 'Failed to load products.'));
    } finally {
      setLoading(false);
    }
  }, [branchId, page, search, categoryFilter, subCategoryFilter, brandFilter, statusFilter, showNotification]);

  useEffect(() => {
    void loadMasters();
  }, [loadMasters]);

  useEffect(() => {
    const timer = window.setTimeout(() => void loadProducts(), search ? 300 : 0);
    return () => window.clearTimeout(timer);
  }, [loadProducts, search]);

  useEffect(() => {
    setPage(1);
  }, [branchId, categoryFilter, subCategoryFilter, brandFilter, statusFilter]);

  const openCreate = () => {
    const blockMessage = getWriteBlockMessage();
    if (branchId <= 0) {
      showNotification('error', 'Select a branch to create products.');
      return;
    }
    if (!canWriteInView || blockMessage) {
      showNotification('error', blockMessage ?? 'You do not have permission to create products.');
      return;
    }
    setEditingProduct(null);
    setIsFormOpen(true);
  };

  const openEdit = async (item: ProductListItem) => {
    const blockMessage = getWriteBlockMessage();
    if (!canWriteInView || blockMessage) {
      showNotification('error', blockMessage ?? 'You do not have permission to edit products.');
      return;
    }
    try {
      const response = await productService.getById(item.id, item.branchId);
      setEditingProduct(response.data);
      setIsFormOpen(true);
    } catch (error) {
      showNotification('error', getApiErrorMessage(error, 'Failed to load product details.'));
    }
  };

  const openDetail = async (item: ProductListItem) => {
    try {
      const response = await productService.getById(item.id, item.branchId);
      setSelectedProduct(response.data);
    } catch (error) {
      showNotification('error', getApiErrorMessage(error, 'Failed to load product details.'));
    }
  };

  const handleSubmit = async (payload: ProductPayload, primaryImageFile: File | null, imageFiles: File[]) => {
    setSubmitting(true);
    try {
      const response = editingProduct
        ? await productService.update(editingProduct.id, payload, branchId)
        : await productService.create(payload, branchId);

      if (primaryImageFile) {
        await productService.uploadImages(response.data.id, branchId, [primaryImageFile], true);
      }

      if (imageFiles.length > 0) {
        await productService.uploadImages(
          response.data.id,
          branchId,
          imageFiles,
          !primaryImageFile && response.data.images.length === 0
        );
      }

      setIsFormOpen(false);
      setEditingProduct(null);
      await loadProducts();
      showNotification('success', editingProduct ? 'Product updated successfully.' : 'Product created successfully.');
    } catch (error) {
      showNotification('error', getApiErrorMessage(error, 'Failed to save product.'));
      throw error;
    } finally {
      setSubmitting(false);
    }
  };

  const filteredSubCategoryOptions = useMemo(
    () => (categoryFilter ? subCategories.filter((item) => item.categoryId === categoryFilter) : subCategories),
    [categoryFilter, subCategories]
  );

  return (
    <div>
      {notification && (
        <div className={`mb-6 rounded-md px-4 py-3 text-sm font-medium ${notification.type === 'success' ? 'bg-green-50 text-green-800' : 'bg-red-50 text-red-800'}`}>
          {notification.message}
        </div>
      )}

      <div className="mb-8 flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
        <div>
          <h1 className="mb-2 text-3xl font-bold text-gray-900">Products</h1>
          <p className="text-gray-600">Manage product catalog, units, variants, barcodes, pricing, discounts, and images.</p>
        </div>
        <button onClick={openCreate} className="rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700">
          Add Product
        </button>
      </div>

      {branchId <= 0 && (
        <div className="mb-6 rounded-md border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-700">
          Select a branch from the header to load products.
        </div>
      )}

      <div className="mb-6 grid grid-cols-1 gap-3 rounded-lg border border-gray-200 bg-white p-4 md:grid-cols-5">
        <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search name, code, SKU, barcode" className="rounded-lg border border-gray-300 px-3 py-2 text-sm md:col-span-2" />
        <select value={categoryFilter ?? 0} onChange={(event) => { setCategoryFilter(Number(event.target.value) || null); setSubCategoryFilter(null); }} className="rounded-lg border border-gray-300 px-3 py-2 text-sm">
          <option value={0}>All Categories</option>
          {categories.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
        </select>
        <select value={subCategoryFilter ?? 0} onChange={(event) => setSubCategoryFilter(Number(event.target.value) || null)} className="rounded-lg border border-gray-300 px-3 py-2 text-sm">
          <option value={0}>All SubCategories</option>
          {filteredSubCategoryOptions.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
        </select>
        <select value={brandFilter ?? 0} onChange={(event) => setBrandFilter(Number(event.target.value) || null)} className="rounded-lg border border-gray-300 px-3 py-2 text-sm">
          <option value={0}>All Brands</option>
          {brands.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
        </select>
        <select value={statusFilter === null ? '' : statusFilter ? 'active' : 'inactive'} onChange={(event) => setStatusFilter(event.target.value === '' ? null : event.target.value === 'active')} className="rounded-lg border border-gray-300 px-3 py-2 text-sm">
          <option value="">All Statuses</option>
          <option value="active">Active</option>
          <option value="inactive">Inactive</option>
        </select>
      </div>

      <div className="overflow-hidden rounded-lg border border-gray-200 bg-white">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                {['ProductName', 'Category', 'Brand', 'SellingPrice', 'Status', 'Actions'].map((header) => (
                  <th key={header} className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500">{header}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100 bg-white">
              {loading ? (
                <tr><td colSpan={6} className="px-4 py-8 text-center text-sm text-gray-500">Loading products...</td></tr>
              ) : products.length === 0 ? (
                <tr><td colSpan={6} className="px-4 py-8 text-center text-sm text-gray-500">No products found.</td></tr>
              ) : products.map((product) => (
                <tr key={product.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3">
                    <div className="font-medium text-gray-900">{product.productName}</div>
                    <div className="text-xs text-gray-500">{product.productCode}{product.sku ? ` | ${product.sku}` : ''}</div>
                  </td>
                  <td className="px-4 py-3 text-sm text-gray-700">{product.categoryName || '-'}</td>
                  <td className="px-4 py-3 text-sm text-gray-700">{product.brandName || '-'}</td>
                  <td className="px-4 py-3 text-sm text-gray-700">{Number(product.sellingPrice ?? 0).toFixed(2)}</td>
                  <td className="px-4 py-3"><Badge variant={product.status ? 'success' : 'danger'} size="sm" dot>{product.status ? 'Active' : 'Inactive'}</Badge></td>
                  <td className="px-4 py-3">
                    <div className="flex gap-2">
                      <button onClick={() => void openDetail(product)} className="rounded border px-3 py-1 text-xs font-medium text-gray-700 hover:bg-gray-50">View</button>
                      <button onClick={() => void openEdit(product)} className="rounded border px-3 py-1 text-xs font-medium text-blue-700 hover:bg-blue-50">Edit</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div className="flex items-center justify-between border-t border-gray-200 px-4 py-3 text-sm text-gray-600">
          <span>{totalRecords} product(s)</span>
          <div className="flex items-center gap-2">
            <button disabled={page <= 1} onClick={() => setPage((prev) => Math.max(1, prev - 1))} className="rounded border px-3 py-1 disabled:opacity-50">Previous</button>
            <span>Page {page} of {totalPages || 1}</span>
            <button disabled={totalPages === 0 || page >= totalPages} onClick={() => setPage((prev) => prev + 1)} className="rounded border px-3 py-1 disabled:opacity-50">Next</button>
          </div>
        </div>
      </div>

      {isFormOpen && (
        <SlideOver title={editingProduct ? 'Edit Product' : 'Create Product'} onClose={() => setIsFormOpen(false)}>
          <ProductForm
            initialData={editingProduct}
            branchId={branchId}
            categories={categories}
            subCategories={subCategories}
            brands={brands}
            unitOptions={units}
            isSubmitting={submitting}
            onCancel={() => setIsFormOpen(false)}
            onSubmit={handleSubmit}
          />
        </SlideOver>
      )}

      {selectedProduct && (
        <SlideOver title="Product Detail" onClose={() => setSelectedProduct(null)}>
          <ProductDetailView product={selectedProduct} />
        </SlideOver>
      )}
    </div>
  );
};

const SlideOver: React.FC<{ title: string; onClose: () => void; children: React.ReactNode }> = ({ title, onClose, children }) => (
  <>
    <div className="fixed inset-0 z-40 bg-black/50" onClick={onClose} />
    <div className="fixed inset-y-0 right-0 z-50 flex h-full w-full max-w-5xl flex-col overflow-hidden border-l border-gray-200 bg-white shadow-2xl">
      <div className="flex items-center justify-between border-b border-gray-200 px-6 py-4">
        <h2 className="text-xl font-semibold text-gray-900">{title}</h2>
        <button onClick={onClose} className="rounded-lg p-2 text-gray-400 hover:bg-gray-100 hover:text-gray-600">Close</button>
      </div>
      <div className="min-h-0 flex-1 overflow-hidden">{children}</div>
    </div>
  </>
);

const ProductDetailView: React.FC<{ product: ProductDetail }> = ({ product }) => (
  <div className="h-full space-y-6 overflow-y-auto px-6 py-5">
    <section>
      <h3 className="mb-3 text-sm font-semibold uppercase tracking-wide text-gray-500">Basic Info</h3>
      <div className="grid grid-cols-1 gap-3 text-sm md:grid-cols-3">
        <Info label="Name" value={product.productName} />
        <Info label="Code" value={product.productCode} />
        <Info label="SKU" value={product.sku || '-'} />
        <Info label="Category" value={product.categoryName} />
        <Info label="SubCategory" value={product.subCategoryName || '-'} />
        <Info label="Brand" value={product.brandName || '-'} />
      </div>
      <p className="mt-3 text-sm text-gray-600">{product.description || 'No description provided.'}</p>
    </section>

    <section className="grid grid-cols-1 gap-3 md:grid-cols-3">
      <Info label="Cost Price" value={product.costPrice.toFixed(2)} />
      <Info label="Selling Price" value={product.sellingPrice.toFixed(2)} />
      <Info label="Wholesale Price" value={product.wholesalePrice.toFixed(2)} />
      <Info label="Discount" value={product.isDiscountAllowed ? `${product.discountType} ${product.discountValue}` : 'Not allowed'} />
      <Info label="Variants" value={product.isVariantEnabled ? 'Enabled' : 'Disabled'} />
      <Info label="Status" value={product.status ? 'Active' : 'Inactive'} />
    </section>

    <DetailList title="Units" rows={product.units.map((unit) => `${unit.unitName} | factor ${unit.conversionFactor}${unit.isBaseUnit ? ' | Base' : ''}`)} />
    <DetailList title="Variants" rows={product.variants.map((variant) => `${variant.variantName}${variant.size ? ` | ${variant.size}` : ''}${variant.color ? ` | ${variant.color}` : ''} | +${variant.additionalPrice}`)} />
    <DetailList title="Barcodes" rows={product.barcodes.map((barcode) => `${barcode.barcodeValue}${barcode.isPrimary ? ' | Primary' : ''}`)} />

    <section>
      <h3 className="mb-3 text-sm font-semibold uppercase tracking-wide text-gray-500">Images</h3>
      {product.images.length === 0 ? (
        <p className="text-sm text-gray-500">No images uploaded.</p>
      ) : (
        <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
          {product.images.map((image) => (
            <AuthenticatedImage
              key={image.id}
              endpoint={productService.getImageEndpoint(product.id, image.id)}
              params={{ branchId: product.branchId }}
              alt={image.fileName}
              className="h-32 w-full rounded-lg border border-gray-200 object-cover"
              fallback={<div className="flex h-32 items-center justify-center rounded-lg bg-gray-100 text-xs text-gray-500">Image</div>}
            />
          ))}
        </div>
      )}
    </section>
  </div>
);

const Info: React.FC<{ label: string; value: string }> = ({ label, value }) => (
  <div className="rounded-lg border border-gray-200 bg-gray-50 px-3 py-2">
    <div className="text-xs font-medium uppercase tracking-wide text-gray-500">{label}</div>
    <div className="mt-1 text-sm font-medium text-gray-900">{value}</div>
  </div>
);

const DetailList: React.FC<{ title: string; rows: string[] }> = ({ title, rows }) => (
  <section>
    <h3 className="mb-3 text-sm font-semibold uppercase tracking-wide text-gray-500">{title}</h3>
    {rows.length === 0 ? (
      <p className="text-sm text-gray-500">No {title.toLowerCase()} added.</p>
    ) : (
      <ul className="space-y-2 text-sm text-gray-700">
        {rows.map((row, index) => <li key={`${title}-${index}`} className="rounded-lg border border-gray-200 px-3 py-2">{row}</li>)}
      </ul>
    )}
  </section>
);

export default ProductPage;
