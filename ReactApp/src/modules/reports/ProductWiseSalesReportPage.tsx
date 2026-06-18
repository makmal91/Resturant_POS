import React, { useCallback, useEffect, useMemo, useState } from 'react';
import type { Column } from '../../components/DataTable';
import { useBranchWriteAccess } from '../../hooks/useBranchWriteAccess';
import { getApiErrorMessage } from '../../services/api';
import { hasBranchContext } from '../../types/permissions';
import { safeString } from '../../utils/safeValues';
import { brandService } from '../brand/brandService';
import { categoryService } from '../category/categoryService';
import { productService } from '../product/productService';
import { subCategoryService } from '../subcategory/subcategoryService';
import ReportPageShell from './ReportPageShell';
import ProductWiseSalesAttractiveSummary from './ProductWiseSalesAttractiveSummary';
import { productWiseSalesExportColumns } from './reportExportColumns';
import { fmt, fmtQty, monthStart, todayStr } from './reportFormatters';
import {
  reportService,
  type ProductWiseSalesReportRow,
  type ProductWiseSalesReportSummary,
} from './reportService';
import { useReportExport } from './useReportExport';

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

const ProductWiseSalesReportPage: React.FC = () => {
  const { selectedBranchId } = useBranchWriteAccess();
  const branchId = hasBranchContext(selectedBranchId) && selectedBranchId !== null ? selectedBranchId : 0;
  const [fromDate, setFromDate] = useState(monthStart);
  const [toDate, setToDate] = useState(todayStr);
  const [categoryId, setCategoryId] = useState<number | ''>('');
  const [subCategoryId, setSubCategoryId] = useState<number | ''>('');
  const [brandId, setBrandId] = useState<number | ''>('');
  const [productId, setProductId] = useState<number | ''>('');
  const [categories, setCategories] = useState<LookupItem[]>([]);
  const [subCategories, setSubCategories] = useState<LookupItem[]>([]);
  const [brands, setBrands] = useState<LookupItem[]>([]);
  const [products, setProducts] = useState<LookupItem[]>([]);
  const [rows, setRows] = useState<ProductWiseSalesReportRow[]>([]);
  const [summary, setSummary] = useState<ProductWiseSalesReportSummary | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [search, setSearch] = useState('');
  const [sortColumn, setSortColumn] = useState('totalAmount');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('desc');
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages] = useState(0);

  useEffect(() => {
    if (branchId <= 0) {
      setCategories([]);
      setSubCategories([]);
      setBrands([]);
      setProducts([]);
      return;
    }

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
      status: true,
      sortBy: 'productName',
      sortDirection: 'asc',
    })
      .then((res) => setProducts(parseLookupItems(Array.isArray(res.data?.products) ? res.data.products : [], 'productName')))
      .catch(() => setProducts([]));
  }, [branchId, categoryId, subCategoryId, brandId]);

  useEffect(() => {
    setSubCategoryId('');
    setProductId('');
  }, [categoryId]);

  useEffect(() => {
    setProductId('');
  }, [subCategoryId, brandId]);

  const load = useCallback(async () => {
    if (branchId <= 0) return;
    setLoading(true);
    setError(null);
    try {
      const res = await reportService.getProductWiseSalesReport(branchId, {
        fromDate,
        toDate,
        pageNumber,
        pageSize,
        search: search.trim() || undefined,
        sortColumn,
        sortDirection,
        categoryId: categoryId === '' ? undefined : Number(categoryId),
        subCategoryId: subCategoryId === '' ? undefined : Number(subCategoryId),
        brandId: brandId === '' ? undefined : Number(brandId),
        productId: productId === '' ? undefined : Number(productId),
      });
      const payload = res.data;
      setRows(Array.isArray(payload?.data) ? payload.data : []);
      setTotalRecords(payload?.totalRecords ?? 0);
      setTotalPages(payload?.totalPages ?? 0);
      setSummary(payload?.summary ?? null);
    } catch (err) {
      setRows([]);
      setSummary(null);
      setError(getApiErrorMessage(err, 'Failed to load product-wise sales report.'));
    } finally {
      setLoading(false);
    }
  }, [
    branchId,
    fromDate,
    toDate,
    pageNumber,
    pageSize,
    search,
    sortColumn,
    sortDirection,
    categoryId,
    subCategoryId,
    brandId,
    productId,
  ]);

  const fetchExportPage = useCallback(async (pageNumber: number, pageSize: number) => {
    const res = await reportService.getProductWiseSalesReport(branchId, {
      fromDate,
      toDate,
      pageNumber,
      pageSize,
      search: search.trim() || undefined,
      sortColumn,
      sortDirection,
      categoryId: categoryId === '' ? undefined : Number(categoryId),
      subCategoryId: subCategoryId === '' ? undefined : Number(subCategoryId),
      brandId: brandId === '' ? undefined : Number(brandId),
      productId: productId === '' ? undefined : Number(productId),
    });
    return { data: res.data.data, totalRecords: res.data.totalRecords };
  }, [branchId, fromDate, toDate, search, sortColumn, sortDirection, categoryId, subCategoryId, brandId, productId]);

  const { exporting, onExport } = useReportExport(
    `product-wise-sales-${fromDate}-${toDate}`,
    productWiseSalesExportColumns,
    fetchExportPage,
    branchId > 0,
  );

  useEffect(() => {
    const timer = setTimeout(() => { void load(); }, search ? 300 : 0);
    return () => clearTimeout(timer);
  }, [load, search]);

  useEffect(() => {
    setPageNumber(1);
  }, [branchId, fromDate, toDate, pageSize, categoryId, subCategoryId, brandId, productId]);

  const columns: Column<ProductWiseSalesReportRow>[] = useMemo(() => [
    { key: 'productCode', header: 'Code', sortable: true, render: (v) => <span className="font-mono text-xs">{safeString(v)}</span> },
    { key: 'productName', header: 'Product', sortable: true },
    { key: 'categoryName', header: 'Category', sortable: true },
    { key: 'subCategoryName', header: 'Sub Category', sortable: true, render: (v) => safeString(v) || '—' },
    { key: 'brandName', header: 'Brand', sortable: true, render: (v) => safeString(v) || '—' },
    { key: 'totalQuantity', header: 'Qty Sold', sortable: true, render: (v) => fmtQty(Number(v ?? 0)) },
    { key: 'totalAmount', header: 'Sales', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    { key: 'totalDiscount', header: 'Discount', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    { key: 'totalTax', header: 'Tax', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    { key: 'grossProfit', header: 'Gross Profit', sortable: true, render: (v) => fmt(Number(v ?? 0)) },
    { key: 'invoiceCount', header: 'Invoices', sortable: true },
  ], []);

  const footerRow = useMemo(() => {
    if (!summary) return undefined;
    return {
      label: 'Total',
      values: {
        productCode: 'Total',
        totalQuantity: fmtQty(summary.totalQuantity),
        totalAmount: fmt(summary.totalAmount),
        totalDiscount: fmt(summary.totalDiscount),
        totalTax: fmt(summary.totalTax),
        grossProfit: <span className="font-bold text-emerald-700">{fmt(summary.grossProfit)}</span>,
        invoiceCount: String(summary.totalInvoices),
      },
    };
  }, [summary]);

  if (branchId <= 0) {
    return (
      <div className="rounded-md border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-700">
        Select a branch from the header to view this report.
      </div>
    );
  }

  return (
    <ReportPageShell
      title="Product Wise Sales Report"
      description="Aggregated sales by product with category, brand, and date filters."
      fromDate={fromDate}
      toDate={toDate}
      onFromDateChange={setFromDate}
      onToDateChange={setToDate}
      extraFilters={(
        <>
          <FilterSelect label="Category" value={categoryId} onChange={setCategoryId} options={categories} />
          <FilterSelect label="Sub Category" value={subCategoryId} onChange={setSubCategoryId} options={subCategories} />
          <FilterSelect label="Brand" value={brandId} onChange={setBrandId} options={brands} />
          <FilterSelect label="Product" value={productId} onChange={setProductId} options={products} />
        </>
      )}
      error={error}
      loading={loading}
      onRefresh={load}
      onExport={onExport}
      exporting={exporting}
      columns={columns}
      rows={rows}
      searchPlaceholder="Search product, code, category, brand..."
      emptyMessage="No product sales found for the selected filters."
      pageNumber={pageNumber}
      pageSize={pageSize}
      totalRecords={totalRecords}
      totalPages={totalPages}
      search={search}
      sortColumn={sortColumn}
      sortDirection={sortDirection}
      onPageChange={setPageNumber}
      onPageSizeChange={(size) => { setPageSize(size); setPageNumber(1); }}
      onSearchChange={(value) => { setSearch(value); setPageNumber(1); }}
      onSortChange={(column, direction) => { setSortColumn(column); setSortDirection(direction); setPageNumber(1); }}
      footerRow={footerRow}
      summary={(
        <ProductWiseSalesAttractiveSummary summary={summary} loading={loading} />
      )}
    />
  );
};

export default ProductWiseSalesReportPage;
