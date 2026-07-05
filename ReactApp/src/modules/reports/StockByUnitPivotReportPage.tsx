import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useBranchWriteAccess } from '../../hooks/useBranchWriteAccess';
import { getApiErrorMessage } from '../../services/api';
import { hasBranchContext } from '../../types/permissions';
import { brandService } from '../brand/brandService';
import { categoryService } from '../category/categoryService';
import { productService } from '../product/productService';
import { subCategoryService } from '../subcategory/subcategoryService';
import { warehouseService, type WarehouseItem } from '../warehouse/warehouseService';
import { fmtQty, monthStart, todayStr } from './reportFormatters';
import {
  reportService,
  type StockByUnitPivotColumn,
  type StockByUnitPivotRow,
} from './reportService';

interface LookupItem {
  id: number;
  name: string;
  categoryId?: number;
}

const parseLookupItems = (rows: unknown[], nameKey = 'name'): LookupItem[] =>
  rows
    .map((row) => {
      if (!row || typeof row !== 'object') return null;
      const item = row as Record<string, unknown>;
      const id = Number(item.id);
      const name = String(item[nameKey] ?? item.productName ?? '').trim();
      const categoryId = item.categoryId != null ? Number(item.categoryId) : undefined;
      if (!Number.isFinite(id) || !name) return null;
      return { id, name, categoryId };
    })
    .filter((item): item is LookupItem => item !== null);

const FilterSelect: React.FC<{
  label: string;
  value: number | '';
  onChange: (value: number | '') => void;
  options: LookupItem[];
  placeholder?: string;
}> = ({ label, value, onChange, options, placeholder = 'All' }) => (
  <div>
    <label className="mb-1 block text-sm font-medium text-gray-700">{label}</label>
    <select
      value={value}
      onChange={(e) => onChange(e.target.value === '' ? '' : Number(e.target.value))}
      className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
    >
      <option value="">{placeholder}</option>
      {options.map((option) => (
        <option key={option.id} value={option.id}>{option.name}</option>
      ))}
    </select>
  </div>
);

const formatCellValue = (value: unknown): string => {
  if (value == null) return '-';
  const num = Number(value);
  if (!Number.isFinite(num)) return '-';
  return fmtQty(num);
};

const StockByUnitPivotReportPage: React.FC = () => {
  const { selectedBranchId } = useBranchWriteAccess();
  const branchId = hasBranchContext(selectedBranchId) && selectedBranchId !== null ? selectedBranchId : 0;

  const [fromDate, setFromDate] = useState(monthStart);
  const [toDate, setToDate] = useState(todayStr);
  const [warehouseId, setWarehouseId] = useState<number | ''>('');
  const [categoryId, setCategoryId] = useState<number | ''>('');
  const [subCategoryId, setSubCategoryId] = useState<number | ''>('');
  const [brandId, setBrandId] = useState<number | ''>('');
  const [productId, setProductId] = useState<number | ''>('');
  const [warehouses, setWarehouses] = useState<WarehouseItem[]>([]);
  const [categories, setCategories] = useState<LookupItem[]>([]);
  const [subCategories, setSubCategories] = useState<LookupItem[]>([]);
  const [brands, setBrands] = useState<LookupItem[]>([]);
  const [products, setProducts] = useState<LookupItem[]>([]);
  const [columns, setColumns] = useState<StockByUnitPivotColumn[]>([]);
  const [rows, setRows] = useState<StockByUnitPivotRow[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [search, setSearch] = useState('');
  const [sortColumn, setSortColumn] = useState('productName');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages] = useState(0);

  useEffect(() => {
    if (branchId <= 0) {
      setWarehouses([]);
      setCategories([]);
      setSubCategories([]);
      setBrands([]);
      setProducts([]);
      return;
    }

    void warehouseService.getAll(branchId, 1, 100)
      .then((res) => setWarehouses(Array.isArray(res.data?.warehouses) ? res.data.warehouses : []))
      .catch(() => setWarehouses([]));

    void categoryService.getAll(branchId, 1, 500, { sortBy: 'name', sortDirection: 'asc' })
      .then((res) => setCategories(parseLookupItems(Array.isArray(res.data?.categories) ? res.data.categories : [])))
      .catch(() => setCategories([]));

    void brandService.getAll(branchId, 1, 500, undefined, true)
      .then((res) => setBrands(parseLookupItems(Array.isArray(res.data?.brands) ? res.data.brands : [])))
      .catch(() => setBrands([]));
  }, [branchId]);

  useEffect(() => {
    if (branchId <= 0) {
      setSubCategories([]);
      return;
    }

    void subCategoryService.getAll(
      branchId,
      1,
      500,
      undefined,
      categoryId === '' ? undefined : Number(categoryId),
      true,
    )
      .then((res) => setSubCategories(parseLookupItems(Array.isArray(res.data?.subCategories) ? res.data.subCategories : [])))
      .catch(() => setSubCategories([]));
  }, [branchId, categoryId]);

  useEffect(() => {
    if (branchId <= 0) {
      setProducts([]);
      return;
    }

    void productService.getAll(branchId, 1, 500, {
      categoryId: categoryId === '' ? undefined : Number(categoryId),
      subCategoryId: subCategoryId === '' ? undefined : Number(subCategoryId),
      brandId: brandId === '' ? undefined : Number(brandId),
      sortBy: 'productName',
      sortDirection: 'asc',
    })
      .then((res) => setProducts(parseLookupItems(Array.isArray(res.data?.products) ? res.data.products : [], 'productName')))
      .catch(() => setProducts([]));
  }, [branchId, categoryId, subCategoryId, brandId]);

  const load = useCallback(async () => {
    if (branchId <= 0) return;
    setLoading(true);
    setError(null);
    try {
      const res = await reportService.getStockByUnitPivot(branchId, {
        fromDate,
        toDate,
        warehouseId: warehouseId === '' ? undefined : Number(warehouseId),
        productId: productId === '' ? undefined : Number(productId),
        categoryId: categoryId === '' ? undefined : Number(categoryId),
        subCategoryId: subCategoryId === '' ? undefined : Number(subCategoryId),
        brandId: brandId === '' ? undefined : Number(brandId),
        page: pageNumber,
        pageSize,
        search: search.trim() || undefined,
        sortBy: sortColumn,
        sortDirection,
      });
      const raw = res.data;
      setColumns(Array.isArray(raw?.columns) ? raw.columns : []);
      setRows(Array.isArray(raw?.rows) ? raw.rows : []);
      setTotalRecords(raw?.totalRecords ?? 0);
      setTotalPages(raw?.totalPages ?? 0);
    } catch (err) {
      setColumns([]);
      setRows([]);
      setError(getApiErrorMessage(err, 'Failed to load stock by unit report.'));
    } finally {
      setLoading(false);
    }
  }, [
    branchId,
    fromDate,
    toDate,
    warehouseId,
    productId,
    categoryId,
    subCategoryId,
    brandId,
    pageNumber,
    pageSize,
    search,
    sortColumn,
    sortDirection,
  ]);

  useEffect(() => {
    const timer = setTimeout(() => { void load(); }, search ? 300 : 0);
    return () => clearTimeout(timer);
  }, [load, search]);

  useEffect(() => {
    setPageNumber(1);
  }, [branchId, fromDate, toDate, warehouseId, productId, categoryId, subCategoryId, brandId, pageSize]);

  useEffect(() => {
    setSubCategoryId('');
    setProductId('');
  }, [categoryId]);

  useEffect(() => {
    setProductId('');
  }, [subCategoryId, brandId]);

  const unitColumns = useMemo(
    () => columns.filter((col) => col.key !== 'productName'),
    [columns],
  );

  const handleSort = (columnKey: string) => {
    if (sortColumn === columnKey) {
      setSortDirection((prev) => (prev === 'asc' ? 'desc' : 'asc'));
    } else {
      setSortColumn(columnKey);
      setSortDirection(columnKey === 'productName' ? 'asc' : 'desc');
    }
  };

  const sortIndicator = (columnKey: string) => {
    if (sortColumn !== columnKey) return null;
    return sortDirection === 'asc' ? ' ↑' : ' ↓';
  };

  if (branchId <= 0) {
    return (
      <div className="rounded-md border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-700">
        Select a branch from the header to view this report.
      </div>
    );
  }

  return (
    <div className="print-area">
      {error && (
        <div className="mb-6 rounded-md bg-red-50 p-4 text-red-800">
          <span className="font-medium">{error}</span>
        </div>
      )}

      <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h1 className="mb-2 text-3xl font-bold text-gray-900">Stock By Unit Report</h1>
          <p className="text-gray-600">
            Pivot view: one row per product with dynamic unit columns (stock = base stock ÷ conversion factor).
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2 self-start print:hidden">
          <button
            type="button"
            onClick={() => void load()}
            disabled={loading}
            className="inline-flex items-center rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 shadow-sm hover:bg-gray-50 disabled:opacity-60"
          >
            {loading ? 'Loading…' : 'Refresh'}
          </button>
        </div>
      </div>

      <div className="mb-6 grid grid-cols-1 gap-4 rounded-xl border border-gray-100 bg-white p-5 shadow-sm print:hidden sm:grid-cols-2 lg:grid-cols-4">
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">From Date</label>
          <input
            type="date"
            value={fromDate}
            onChange={(e) => setFromDate(e.target.value)}
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
          />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">To Date</label>
          <input
            type="date"
            value={toDate}
            onChange={(e) => setToDate(e.target.value)}
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
          />
        </div>
        <FilterSelect
          label="Warehouse"
          value={warehouseId}
          onChange={setWarehouseId}
          options={warehouses.map((w) => ({ id: w.id, name: w.name }))}
          placeholder="All Warehouses"
        />
        <FilterSelect label="Category" value={categoryId} onChange={setCategoryId} options={categories} />
        <FilterSelect label="Sub Category" value={subCategoryId} onChange={setSubCategoryId} options={subCategories} />
        <FilterSelect label="Brand" value={brandId} onChange={setBrandId} options={brands} />
        <FilterSelect label="Product" value={productId} onChange={setProductId} options={products} />
      </div>

      <div className="mb-4 print:hidden">
        <input
          type="search"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search products…"
          className="w-full max-w-md rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
        />
      </div>

      <div className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
        <div className="overflow-x-auto">
          <table className="min-w-full border-collapse text-sm">
            <thead>
              <tr className="border-b border-gray-200 bg-gray-50">
                <th
                  className="sticky left-0 z-20 min-w-[200px] border-r border-gray-200 bg-gray-50 px-4 py-3 text-left font-semibold text-gray-700 shadow-[2px_0_4px_-2px_rgba(0,0,0,0.1)]"
                >
                  <button
                    type="button"
                    onClick={() => handleSort('productName')}
                    className="inline-flex items-center font-semibold text-gray-700 hover:text-blue-600"
                  >
                    Product Name{sortIndicator('productName')}
                  </button>
                </th>
                {unitColumns.map((col) => (
                  <th
                    key={col.key}
                    className="min-w-[100px] whitespace-nowrap px-4 py-3 text-right font-semibold text-gray-700"
                  >
                    <button
                      type="button"
                      onClick={() => handleSort(col.key)}
                      className="inline-flex w-full items-center justify-end font-semibold text-gray-700 hover:text-blue-600"
                    >
                      {col.label}{sortIndicator(col.key)}
                    </button>
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {loading && rows.length === 0 ? (
                <tr>
                  <td
                    colSpan={Math.max(unitColumns.length + 1, 2)}
                    className="px-4 py-8 text-center text-gray-500"
                  >
                    Loading…
                  </td>
                </tr>
              ) : rows.length === 0 ? (
                <tr>
                  <td
                    colSpan={Math.max(unitColumns.length + 1, 2)}
                    className="px-4 py-8 text-center text-gray-500"
                  >
                    No products found for the selected filters.
                  </td>
                </tr>
              ) : (
                rows.map((row) => (
                  <tr key={row.productId} className="border-b border-gray-100 hover:bg-gray-50/80">
                    <td className="sticky left-0 z-10 min-w-[200px] border-r border-gray-100 bg-white px-4 py-2.5 font-medium text-gray-900 shadow-[2px_0_4px_-2px_rgba(0,0,0,0.06)]">
                      {row.productName}
                    </td>
                    {unitColumns.map((col) => {
                      const value = row[col.key];
                      const isMissing = value == null;
                      return (
                        <td
                          key={col.key}
                          className={`min-w-[100px] whitespace-nowrap px-4 py-2.5 text-right tabular-nums ${
                            isMissing ? 'text-gray-400' : 'text-gray-900'
                          }`}
                        >
                          {formatCellValue(value)}
                        </td>
                      );
                    })}
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        <div className="flex flex-col gap-3 border-t border-gray-200 px-4 py-3 text-sm text-gray-600 sm:flex-row sm:items-center sm:justify-between print:hidden">
          <div>
            {totalRecords > 0
              ? `Showing ${(pageNumber - 1) * pageSize + 1}–${Math.min(pageNumber * pageSize, totalRecords)} of ${totalRecords}`
              : 'No records'}
          </div>
          <div className="flex flex-wrap items-center gap-3">
            <label className="flex items-center gap-2">
              <span>Rows</span>
              <select
                value={pageSize}
                onChange={(e) => setPageSize(Number(e.target.value))}
                className="rounded border border-gray-300 px-2 py-1"
              >
                {[10, 25, 50, 100].map((size) => (
                  <option key={size} value={size}>{size}</option>
                ))}
              </select>
            </label>
            <div className="flex items-center gap-1">
              <button
                type="button"
                disabled={pageNumber <= 1 || loading}
                onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
                className="rounded border border-gray-300 px-3 py-1 disabled:opacity-50"
              >
                Prev
              </button>
              <span className="px-2">
                Page {pageNumber} of {Math.max(totalPages, 1)}
              </span>
              <button
                type="button"
                disabled={pageNumber >= totalPages || loading}
                onClick={() => setPageNumber((p) => p + 1)}
                className="rounded border border-gray-300 px-3 py-1 disabled:opacity-50"
              >
                Next
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default StockByUnitPivotReportPage;
