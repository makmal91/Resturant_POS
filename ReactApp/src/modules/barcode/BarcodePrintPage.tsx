import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { getApiErrorMessage } from '../../services/api';
import { useBranchWriteAccess } from '../../hooks/useBranchWriteAccess';
import { useHasFeature } from '../../hooks/useFeature';
import { FEATURE_KEYS } from '../../types/featurePermissions';
import { categoryService } from '../category/categoryService';
import { subCategoryService } from '../subcategory/subcategoryService';
import { brandService } from '../brand/brandService';
import { barcodeService, BarcodePrintProduct, ProductPrintDetails } from './barcodeService';
import AddProductModal from './AddProductModal';
import { useBusinessCurrency } from '../../hooks/useBusinessCurrency';
import BarcodeLabelGrid from './BarcodeLabelGrid';
import LabelSizeSelector, { useLabelSizePreference } from './LabelSizeSelector';
import {
  buildLabelTitle,
  buildPrintQueueRow,
  formatLabelPrice,
  PrintQueueRow,
  productNeedsSelection,
  resolveDefaultUnit,
} from './barcodeUtils';
import { printBarcodeLabels } from './printBarcodeLabels';
import './barcodePrint.css';

type MasterOption = { id: number; name: string };

const parseNumberParam = (value: string | null): number | null => {
  if (!value) return null;
  const parsed = Number(value);
  return parsed > 0 ? parsed : null;
};

const BarcodePrintPage: React.FC = () => {
  const { selectedBranchId } = useBranchWriteAccess();
  const unitFeatureEnabled = useHasFeature(FEATURE_KEYS.UNIT);
  const variantFeatureEnabled = useHasFeature(FEATURE_KEYS.VARIANT);
  const stockFeatureEnabled = useHasFeature(FEATURE_KEYS.STOCK);

  const branchId = selectedBranchId && selectedBranchId > 0 ? selectedBranchId : 0;
  const [searchParams, setSearchParams] = useSearchParams();

  const [searchInput, setSearchInput] = useState(searchParams.get('search') ?? '');
  const [debouncedSearch, setDebouncedSearch] = useState(searchInput);
  const [categoryId, setCategoryId] = useState<number | null>(parseNumberParam(searchParams.get('categoryId')));
  const [subCategoryId, setSubCategoryId] = useState<number | null>(parseNumberParam(searchParams.get('subCategoryId')));
  const [brandId, setBrandId] = useState<number | null>(parseNumberParam(searchParams.get('brandId')));
  const [inStockOnly, setInStockOnly] = useState(searchParams.get('inStock') === 'true');
  const [page, setPage] = useState(Math.max(1, Number(searchParams.get('page') ?? 1)));

  const [categories, setCategories] = useState<MasterOption[]>([]);
  const [subCategories, setSubCategories] = useState<MasterOption[]>([]);
  const [brands, setBrands] = useState<MasterOption[]>([]);
  const [items, setItems] = useState<BarcodePrintProduct[]>([]);
  const [totalPages, setTotalPages] = useState(0);
  const [totalRecords, setTotalRecords] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [queue, setQueue] = useState<PrintQueueRow[]>([]);
  const [addingProductId, setAddingProductId] = useState<number | null>(null);
  const [addModalDetails, setAddModalDetails] = useState<ProductPrintDetails | null>(null);
  const { labelSize, labelPreset, updateLabelSize } = useLabelSizePreference();
  const { symbol, currencyCode } = useBusinessCurrency();
  const [showPriceOnLabel, setShowPriceOnLabel] = useState(true);
  const [showVariantOnLabel, setShowVariantOnLabel] = useState(true);
  const [showUnitOnLabel, setShowUnitOnLabel] = useState(true);

  const formatPrice = useCallback(
    (value: number) => formatLabelPrice(value, symbol, currencyCode),
    [symbol, currencyCode],
  );

  const effectiveShowVariant = variantFeatureEnabled && showVariantOnLabel;
  const effectiveShowUnit = unitFeatureEnabled && showUnitOnLabel;

  const showVariantColumn = useMemo(
    () => variantFeatureEnabled && queue.some((row) => Boolean(row.variantName)),
    [queue, variantFeatureEnabled],
  );
  const showUnitColumn = useMemo(
    () => unitFeatureEnabled && queue.some((row) => Boolean(row.unitName)),
    [queue, unitFeatureEnabled],
  );

  useEffect(() => {
    const timer = window.setTimeout(() => setDebouncedSearch(searchInput.trim()), 300);
    return () => window.clearTimeout(timer);
  }, [searchInput]);

  useEffect(() => {
    const params = new URLSearchParams();
    if (debouncedSearch) params.set('search', debouncedSearch);
    if (categoryId) params.set('categoryId', String(categoryId));
    if (subCategoryId) params.set('subCategoryId', String(subCategoryId));
    if (brandId) params.set('brandId', String(brandId));
    if (inStockOnly) params.set('inStock', 'true');
    if (page > 1) params.set('page', String(page));
    setSearchParams(params, { replace: true });
  }, [debouncedSearch, categoryId, subCategoryId, brandId, inStockOnly, page, setSearchParams]);

  const loadMasters = useCallback(async () => {
    if (branchId <= 0) {
      setCategories([]);
      setSubCategories([]);
      setBrands([]);
      return;
    }

    try {
      const [categoryResponse, brandResponse] = await Promise.all([
        categoryService.getAll(branchId, 1, 200),
        brandService.getAll(branchId, 1, 200, undefined, true),
      ]);

      setCategories(
        (Array.isArray(categoryResponse.data?.categories) ? categoryResponse.data.categories : [])
          .map((item: { id?: number; name?: string; status?: boolean }) => ({
            id: Number(item.id ?? 0),
            name: String(item.name ?? ''),
          }))
          .filter((item) => item.id > 0 && item.name),
      );

      setBrands(
        (Array.isArray(brandResponse.data?.brands) ? brandResponse.data.brands : [])
          .map((item: { id?: number; name?: string }) => ({
            id: Number(item.id ?? 0),
            name: String(item.name ?? ''),
          }))
          .filter((item) => item.id > 0 && item.name),
      );
    } catch {
      setCategories([]);
      setBrands([]);
    }
  }, [branchId]);

  const loadSubCategories = useCallback(async () => {
    if (branchId <= 0 || !categoryId) {
      setSubCategories([]);
      return;
    }

    try {
      const response = await subCategoryService.getAll(branchId, 1, 200, undefined, categoryId, true);
      setSubCategories(
        (Array.isArray(response.data?.subCategories) ? response.data.subCategories : [])
          .map((item: { id?: number; name?: string }) => ({
            id: Number(item.id ?? 0),
            name: String(item.name ?? ''),
          }))
          .filter((item) => item.id > 0 && item.name),
      );
    } catch {
      setSubCategories([]);
    }
  }, [branchId, categoryId]);

  const loadItems = useCallback(async () => {
    if (branchId <= 0) {
      setItems([]);
      setTotalPages(0);
      setTotalRecords(0);
      return;
    }

    setLoading(true);
    setError('');
    try {
      const response = await barcodeService.getItems(branchId, {
        search: debouncedSearch || undefined,
        categoryId,
        subCategoryId,
        brandId,
        inStock: inStockOnly,
        page,
        pageSize: 50,
      });
      setItems(Array.isArray(response.data.items) ? response.data.items : []);
      setTotalPages(Number(response.data.totalPages ?? 0));
      setTotalRecords(Number(response.data.totalRecords ?? 0));
    } catch (err) {
      setItems([]);
      setError(getApiErrorMessage(err, 'Failed to load products.'));
    } finally {
      setLoading(false);
    }
  }, [branchId, debouncedSearch, categoryId, subCategoryId, brandId, inStockOnly, page]);

  useEffect(() => {
    void loadMasters();
  }, [loadMasters]);

  useEffect(() => {
    void loadSubCategories();
  }, [loadSubCategories]);

  useEffect(() => {
    void loadItems();
  }, [loadItems]);

  useEffect(() => {
    setPage(1);
  }, [branchId, debouncedSearch, categoryId, subCategoryId, brandId, inStockOnly]);

  const preselectedProductId = parseNumberParam(searchParams.get('productId'));

  useEffect(() => {
    if (!preselectedProductId || branchId <= 0) return;
    void handleAddProduct(preselectedProductId);
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [preselectedProductId, branchId]);

  const handleCategoryChange = (value: number | null) => {
    setCategoryId(value);
    setSubCategoryId(null);
  };

  const clearFilters = () => {
    setSearchInput('');
    setDebouncedSearch('');
    setCategoryId(null);
    setSubCategoryId(null);
    setBrandId(null);
    setInStockOnly(false);
    setPage(1);
  };

  const commitAddToQueue = useCallback((row: PrintQueueRow) => {
    setQueue((prev) => {
      const existing = prev.find((item) => item.key === row.key);
      if (existing) {
        return prev.map((item) => (item.key === row.key ? { ...item, qty: item.qty + row.qty } : item));
      }
      return [...prev, row];
    });
  }, []);

  const handleAddProduct = async (productId: number) => {
    if (branchId <= 0) return;
    setAddingProductId(productId);
    setError('');
    try {
      const response = await barcodeService.getProductDetails(productId, branchId);
      const details = response.data;
      const defaultUnit = resolveDefaultUnit(details.units);
      if (!defaultUnit?.id) {
        setError('Product has no units configured.');
        return;
      }

      if (productNeedsSelection(details, unitFeatureEnabled, variantFeatureEnabled)) {
        setAddModalDetails(details);
        return;
      }

      const needsVariant = variantFeatureEnabled && details.hasVariants;
      const defaultVariant = needsVariant ? details.variants.find((variant) => variant.status) ?? null : null;

      const row = buildPrintQueueRow(details, defaultUnit.id, defaultVariant?.id ?? null, 1);
      if (!row) {
        setError('Unable to build barcode row for this product.');
        return;
      }

      commitAddToQueue(row);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to load product details.'));
    } finally {
      setAddingProductId(null);
    }
  };

  const updateQueueQty = (key: string, qty: number) => {
    setQueue((prev) =>
      prev.map((row) => (row.key === key ? { ...row, qty: Math.max(1, qty) } : row)),
    );
  };

  const removeQueueRow = (key: string) => {
    setQueue((prev) => prev.filter((row) => row.key !== key));
  };

  const handlePrint = () => {
    if (queue.length === 0) {
      setError('Add at least one product with quantity greater than 0.');
      return;
    }
    if (queue.some((row) => row.qty <= 0)) {
      setError('All print quantities must be greater than 0.');
      return;
    }
    setError('');
    const ok = printBarcodeLabels(
      queue.map((row) => ({
        productName: row.productName,
        unitName: row.unitName,
        variantName: row.variantName,
        barcode: row.barcode,
        price: row.price,
        qty: row.qty,
      })),
      {
        labelSize,
        showPrice: showPriceOnLabel,
        showVariant: effectiveShowVariant,
        showUnit: effectiveShowUnit,
        currencySymbol: symbol,
        currencyCode,
      },
    );
    if (!ok) {
      setError('Pop-up blocked. Please allow pop-ups for this site to print labels.');
    }
  };

  const printLabels = queue.flatMap((row) =>
    Array.from({ length: row.qty }, (_, index) => ({
      ...row,
      printKey: `${row.key}:${index}`,
    })),
  );

  const hasPreview = printLabels.length > 0;

  return (
    <div>
      <div className="mb-6 flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Barcode Printing</h1>
          <p className="mt-1 text-gray-600">Search products, build a print queue, and print labels.</p>
        </div>
        <div className="flex gap-2">
          <button
            type="button"
            onClick={clearFilters}
            className="rounded-md border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
          >
            Clear Filters
          </button>
          <button
            type="button"
            onClick={handlePrint}
            disabled={queue.length === 0}
            className="rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
          >
            Print Labels
          </button>
        </div>
      </div>

      {branchId <= 0 && (
        <div className="mb-4 rounded-md border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-700">
          Select a branch from the header to load products.
        </div>
      )}

      {error && (
        <div className="mb-4 rounded-md bg-red-50 px-4 py-3 text-sm text-red-700">{error}</div>
      )}

      <section className="barcode-no-print mb-6 rounded-lg border border-gray-200 bg-white p-4">
        <div className="grid grid-cols-1 gap-3 md:grid-cols-6">
          <input
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
            placeholder="Search name or barcode"
            className="rounded-lg border border-gray-300 px-3 py-2 text-sm md:col-span-2"
          />
          <select
            value={categoryId ?? 0}
            onChange={(event) => handleCategoryChange(Number(event.target.value) || null)}
            className="rounded-lg border border-gray-300 px-3 py-2 text-sm"
          >
            <option value={0}>All Categories</option>
            {categories.map((item) => (
              <option key={item.id} value={item.id}>{item.name}</option>
            ))}
          </select>
          <select
            value={subCategoryId ?? 0}
            onChange={(event) => setSubCategoryId(Number(event.target.value) || null)}
            disabled={!categoryId}
            className="rounded-lg border border-gray-300 px-3 py-2 text-sm disabled:bg-gray-100 disabled:text-gray-400"
          >
            <option value={0}>{categoryId ? 'All SubCategories' : 'Select category first'}</option>
            {subCategories.map((item) => (
              <option key={item.id} value={item.id}>{item.name}</option>
            ))}
          </select>
          <select
            value={brandId ?? 0}
            onChange={(event) => setBrandId(Number(event.target.value) || null)}
            className="rounded-lg border border-gray-300 px-3 py-2 text-sm"
          >
            <option value={0}>All Brands</option>
            {brands.map((item) => (
              <option key={item.id} value={item.id}>{item.name}</option>
            ))}
          </select>
          {stockFeatureEnabled && (
            <label className="flex items-center gap-2 rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-700">
              <input
                type="checkbox"
                checked={inStockOnly}
                onChange={(event) => setInStockOnly(event.target.checked)}
              />
              In stock only
            </label>
          )}
        </div>
      </section>

      <section className="barcode-no-print mb-6 overflow-hidden rounded-lg border border-gray-200 bg-white">
        <div className="border-b border-gray-200 px-4 py-3 text-sm font-semibold text-gray-800">
          Products ({totalRecords})
        </div>
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200 text-sm">
            <thead className="bg-gray-50">
              <tr>
                {['Product', 'SKU', 'Category', 'Price', stockFeatureEnabled ? 'Stock' : null, 'Action']
                  .filter(Boolean)
                  .map((header) => (
                    <th key={String(header)} className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500">
                      {header}
                    </th>
                  ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {loading ? (
                <tr><td colSpan={6} className="px-4 py-8 text-center text-gray-500">Loading products...</td></tr>
              ) : items.length === 0 ? (
                <tr><td colSpan={6} className="px-4 py-8 text-center text-gray-500">No products match your filters.</td></tr>
              ) : items.map((item) => (
                <tr key={item.productId} className="hover:bg-gray-50">
                  <td className="px-4 py-3">
                    <div className="font-medium text-gray-900">{item.productName}</div>
                    {item.primaryBarcode && <div className="text-xs text-gray-500">{item.primaryBarcode}</div>}
                  </td>
                  <td className="px-4 py-3 text-gray-700">{item.sku || '-'}</td>
                  <td className="px-4 py-3 text-gray-700">
                    {item.categoryName}
                    {item.subCategoryName ? ` / ${item.subCategoryName}` : ''}
                  </td>
                  <td className="px-4 py-3 text-gray-700">{Number(item.sellingPrice).toFixed(2)}</td>
                  {stockFeatureEnabled && (
                    <td className="px-4 py-3 text-gray-700">{Number(item.stockQty).toFixed(2)}</td>
                  )}
                  <td className="px-4 py-3">
                    <button
                      type="button"
                      onClick={() => void handleAddProduct(item.productId)}
                      disabled={addingProductId === item.productId}
                      className="rounded border px-3 py-1 text-xs font-medium text-blue-700 hover:bg-blue-50 disabled:opacity-50"
                    >
                      {addingProductId === item.productId ? 'Adding…' : 'Add'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div className="flex items-center justify-between border-t border-gray-200 px-4 py-3 text-sm text-gray-600">
          <span>Page {page} of {totalPages || 1}</span>
          <div className="flex gap-2">
            <button
              type="button"
              disabled={page <= 1}
              onClick={() => setPage((prev) => Math.max(1, prev - 1))}
              className="rounded border px-3 py-1 disabled:opacity-50"
            >
              Previous
            </button>
            <button
              type="button"
              disabled={totalPages === 0 || page >= totalPages}
              onClick={() => setPage((prev) => prev + 1)}
              className="rounded border px-3 py-1 disabled:opacity-50"
            >
              Next
            </button>
          </div>
        </div>
      </section>

      <LabelSizeSelector
        value={labelSize}
        preset={labelPreset}
        onChange={updateLabelSize}
      />

      <section className="barcode-no-print mb-4 rounded-lg border border-gray-200 bg-white p-4">
        <p className="mb-3 text-sm font-semibold text-gray-800">Label display options</p>
        <div className="flex flex-wrap gap-x-6 gap-y-2">
          {variantFeatureEnabled && (
            <label className="inline-flex items-center gap-2 text-sm text-gray-700">
              <input
                type="checkbox"
                checked={showVariantOnLabel}
                onChange={(event) => setShowVariantOnLabel(event.target.checked)}
                className="rounded border-gray-300"
              />
              Show variant
            </label>
          )}
          {unitFeatureEnabled && (
            <label className="inline-flex items-center gap-2 text-sm text-gray-700">
              <input
                type="checkbox"
                checked={showUnitOnLabel}
                onChange={(event) => setShowUnitOnLabel(event.target.checked)}
                className="rounded border-gray-300"
              />
              Show unit
            </label>
          )}
          <label className="inline-flex items-center gap-2 text-sm text-gray-700">
            <input
              type="checkbox"
              checked={showPriceOnLabel}
              onChange={(event) => setShowPriceOnLabel(event.target.checked)}
              className="rounded border-gray-300"
            />
            Show price
          </label>
        </div>
      </section>

      <section className="barcode-no-print overflow-hidden rounded-lg border border-gray-200 bg-white">
        <div className="border-b border-gray-200 px-4 py-3 text-sm font-semibold text-gray-800">
          Print Queue ({queue.length})
        </div>
        {queue.length === 0 ? (
          <p className="px-4 py-8 text-sm text-gray-500">Add products from the list above to build your print queue.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200 text-sm">
              <thead className="bg-gray-50">
                <tr>
                  {['Product', 'SKU', showVariantColumn ? 'Variant' : null, showUnitColumn ? 'Unit' : null, 'Barcode', 'Price', 'Qty', '']
                    .filter(Boolean)
                    .map((header) => (
                      <th key={String(header)} className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500">
                        {header}
                      </th>
                    ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {queue.map((row) => (
                  <tr key={row.key}>
                    <td className="px-4 py-3 font-medium text-gray-900">
                      {buildLabelTitle(row.productName, row.unitName, row.variantName)}
                    </td>
                    <td className="px-4 py-3 text-gray-700">{row.sku || '-'}</td>
                    {showVariantColumn && <td className="px-4 py-3 text-gray-700">{row.variantName || '-'}</td>}
                    {showUnitColumn && <td className="px-4 py-3 text-gray-700">{row.unitName || '-'}</td>}
                    <td className="px-4 py-3 font-mono text-xs text-gray-700">{row.barcode}</td>
                    <td className="px-4 py-3 text-gray-700">{formatPrice(row.price)}</td>
                    <td className="px-4 py-3">
                      <input
                        type="number"
                        min={1}
                        value={row.qty}
                        onChange={(event) => updateQueueQty(row.key, Number(event.target.value))}
                        className="w-20 rounded border border-gray-300 px-2 py-1"
                      />
                    </td>
                    <td className="px-4 py-3">
                      <button
                        type="button"
                        onClick={() => removeQueueRow(row.key)}
                        className="text-xs font-medium text-red-600 hover:text-red-700"
                      >
                        Remove
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {hasPreview && (
        <section className="mt-6 overflow-hidden rounded-lg border border-gray-200 bg-white">
          <div className="border-b border-gray-200 px-4 py-3 text-sm font-semibold text-gray-800">
            Label Preview ({labelSize.labelWidth}mm × {labelSize.labelHeight}mm)
          </div>
          <BarcodeLabelGrid
            className="p-4"
            labels={printLabels.slice(0, 6).map((label) => ({
              key: label.printKey,
              productName: label.productName,
              variantName: label.variantName,
              unitName: label.unitName,
              barcode: label.barcode,
              price: label.price,
              showPrice: showPriceOnLabel,
              showVariant: effectiveShowVariant,
              showUnit: effectiveShowUnit,
              width: labelSize.labelWidth,
              height: labelSize.labelHeight,
              currencySymbol: symbol,
              currencyCode,
              showBorder: true,
            }))}
          />
          {printLabels.length > 6 && (
            <p className="px-4 pb-4 text-xs text-gray-500">+ {printLabels.length - 6} more label(s) will print</p>
          )}
        </section>
      )}

      {addModalDetails && (
        <AddProductModal
          details={addModalDetails}
          unitFeatureEnabled={unitFeatureEnabled}
          variantFeatureEnabled={variantFeatureEnabled}
          onClose={() => setAddModalDetails(null)}
          onAdd={commitAddToQueue}
        />
      )}
    </div>
  );
};

export default BarcodePrintPage;
