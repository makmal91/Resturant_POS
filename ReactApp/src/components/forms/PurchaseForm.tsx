import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { FormButton, FormInput, FormSelect, FormTextarea } from './index';
import CodeFieldWithGenerate from './CodeFieldWithGenerate';
import { CODE_MODULES } from '../../services/codeGeneratorService';
import { safeString } from '../../utils/safeValues';
import apiClient from '../../services/api';
import { useFormBranchId } from '../../hooks/useFormBranchId';
import { useHasFeature } from '../../hooks/useFeature';
import { FEATURE_KEYS } from '../../types/featurePermissions';

export interface PurchaseItemFormData {
  productId: number;
  variantId?: number | null;
  unitId: number;
  quantity: number;
  conversionFactor: number;
  costPrice: number;
}

export interface PurchaseFormData {
  invoiceNo: string;
  supplierId: number;
  warehouseId: number;
  purchaseDate: string;
  notes: string;
  isCreditPurchase: boolean;
  branchId: number;
  items: PurchaseItemFormData[];
}

interface LookupOption {
  id: number;
  name: string;
}

interface ProductOption {
  id: number;
  productName: string;
  productCode: string;
  isVariantEnabled: boolean;
}

interface VariantOption {
  id: number;
  variantName: string;
  sku: string;
  costPriceOverride?: number | null;
}

interface UnitOption {
  id: number;
  unitName: string;
  conversionFactor: number;
  isBaseUnit: boolean;
  costPrice?: number | null;
}

interface ItemRow {
  key: string;
  productId: number;
  productName: string;
  productCode: string;
  variantId: number | null;
  unitId: number;
  unitName: string;
  quantity: number;
  conversionFactor: number;
  costPrice: number;
  totalCost: number;
  variants: VariantOption[];
  units: UnitOption[];
  isVariantEnabled: boolean;
  productCostPrice: number;
}

const mapProductUnits = (d: Record<string, unknown>): UnitOption[] =>
  ((d.units ?? d.Units) as Record<string, unknown>[] | undefined ?? []).map((u) => ({
    id: Number(u.id ?? u.Id ?? 0),
    unitName: String(u.unitName ?? u.UnitName ?? ''),
    conversionFactor: Number(u.conversionFactor ?? u.ConversionFactor ?? 1),
    isBaseUnit: Boolean(u.isBaseUnit ?? u.IsBaseUnit ?? false),
    costPrice:
      u.costPrice != null || u.CostPrice != null
        ? Number(u.costPrice ?? u.CostPrice ?? 0)
        : null,
  }));

const mapProductVariants = (d: Record<string, unknown>): VariantOption[] =>
  ((d.variants ?? d.Variants) as Record<string, unknown>[] | undefined ?? []).map((v) => ({
    id: Number(v.id ?? v.Id ?? 0),
    variantName: String(v.variantName ?? v.VariantName ?? ''),
    sku: String(v.sku ?? v.SKU ?? ''),
    costPriceOverride:
      v.costPriceOverride != null || v.CostPriceOverride != null
        ? Number(v.costPriceOverride ?? v.CostPriceOverride ?? 0)
        : null,
  }));

const resolvePurchaseCostPrice = (
  productCostPrice: number,
  units: UnitOption[],
  unitId: number,
  variantId: number | null,
  variants: VariantOption[]
): number => {
  const unit = units.find((u) => u.id === unitId);
  const variant = variantId != null ? variants.find((v) => v.id === variantId) : undefined;

  if (unit?.costPrice != null) {
    return unit.costPrice;
  }

  const baseCost = variant?.costPriceOverride ?? productCostPrice;
  const factor = unit?.conversionFactor ?? 1;
  return baseCost * factor;
};

export type PurchaseSubmitMode = 'draft' | 'post';

interface PurchaseFormProps {
  initialData?: Partial<
    PurchaseFormData & {
      id?: number;
      status?: string;
      isCreditPurchase?: boolean;
      items?: Array<{
        productId: number;
        productName?: string;
        variantId?: number | null;
        unitId: number;
        unitName?: string;
        quantity: number;
        conversionFactor: number;
        costPrice: number;
      }>;
    }
  > | null;
  suppliers?: LookupOption[];
  warehouses?: LookupOption[];
  onSubmit: (data: PurchaseFormData, mode: PurchaseSubmitMode) => void;
  isLoading?: boolean;
}

const emptyRow = (): ItemRow => ({
  key: crypto.randomUUID(),
  productId: 0,
  productName: '',
  productCode: '',
  variantId: null,
  unitId: 0,
  unitName: '',
  quantity: 1,
  conversionFactor: 1,
  costPrice: 0,
  totalCost: 0,
  variants: [],
  units: [],
  isVariantEnabled: false,
  productCostPrice: 0,
});

const buildRowsFromInitial = (
  items?: Array<{
    productId: number;
    productName?: string;
    variantId?: number | null;
    unitId: number;
    unitName?: string;
    quantity: number;
    conversionFactor: number;
    costPrice: number;
  }>
): ItemRow[] => {
  if (!Array.isArray(items) || items.length === 0) return [emptyRow()];
  return items.map((item) => ({
    key: crypto.randomUUID(),
    productId: Number(item.productId ?? 0),
    productName: safeString(item.productName),
    productCode: '',
    variantId: item.variantId ?? null,
    unitId: Number(item.unitId ?? 0),
    unitName: safeString(item.unitName),
    quantity: Number(item.quantity ?? 1),
    conversionFactor: Number(item.conversionFactor ?? 1),
    costPrice: Number(item.costPrice ?? 0),
    totalCost: Number(item.quantity ?? 0) * Number(item.costPrice ?? 0),
    variants: [],
    units: [],
    isVariantEnabled: Boolean(item.variantId),
    productCostPrice: Number(item.costPrice ?? 0),
  }));
};

const inputCls =
  'w-full rounded-md border border-gray-300 bg-white px-3 py-2 text-sm text-gray-900 placeholder-gray-400 focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500';

const PurchaseForm: React.FC<PurchaseFormProps> = ({
  initialData,
  suppliers = [],
  warehouses = [],
  onSubmit,
  isLoading = false,
}) => {
  const isPosted = initialData?.status === 'Posted';
  const isEditMode = Number(initialData?.id ?? 0) > 0;
  const { branchId, branchError } = useFormBranchId(initialData?.branchId);
  const variantFeatureEnabled = useHasFeature(FEATURE_KEYS.VARIANT);
  const unitFeatureEnabled = useHasFeature(FEATURE_KEYS.UNIT);

  const lineItemGridColumns = useMemo(() => {
    const parts = ['2fr'];
    if (variantFeatureEnabled) parts.push('1.5fr');
    if (unitFeatureEnabled) parts.push('1.2fr');
    parts.push('80px', '80px', '90px', '90px', '72px');
    return parts.join(' ');
  }, [variantFeatureEnabled, unitFeatureEnabled]);

  const [invoiceNo, setInvoiceNo] = useState(safeString(initialData?.invoiceNo));
  const [invoiceCodeResetKey, setInvoiceCodeResetKey] = useState(0);
  const [supplierId, setSupplierId] = useState(Number(initialData?.supplierId ?? 0));
  const [warehouseId, setWarehouseId] = useState(Number(initialData?.warehouseId ?? 0));
  const [purchaseDate, setPurchaseDate] = useState(
    initialData?.purchaseDate
      ? String(initialData.purchaseDate).slice(0, 10)
      : new Date().toISOString().slice(0, 10)
  );
  const [notes, setNotes] = useState(safeString(initialData?.notes));
  const [isCreditPurchase, setIsCreditPurchase] = useState(Boolean(initialData?.isCreditPurchase));
  const [rows, setRows] = useState<ItemRow[]>(() => buildRowsFromInitial(initialData?.items));
  const [errors, setErrors] = useState<Record<string, string>>({});

  // Per-row product search state
  const [searchRowKey, setSearchRowKey] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [searchResults, setSearchResults] = useState<ProductOption[]>([]);
  const [searchLoading, setSearchLoading] = useState(false);
  const searchInputRef = useRef<HTMLInputElement>(null);
  const searchDropdownRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    setInvoiceNo(safeString(initialData?.invoiceNo));
    setSupplierId(Number(initialData?.supplierId ?? 0));
    setWarehouseId(Number(initialData?.warehouseId ?? 0));
    setPurchaseDate(
      initialData?.purchaseDate
        ? String(initialData.purchaseDate).slice(0, 10)
        : new Date().toISOString().slice(0, 10)
    );
    setNotes(safeString(initialData?.notes));
    setIsCreditPurchase(Boolean(initialData?.isCreditPurchase));
    setRows(buildRowsFromInitial(initialData?.items));
    setErrors({});
  }, [initialData]);

  // Hydrate product units/variants when editing an existing purchase
  useEffect(() => {
    if (branchId <= 0 || !initialData?.items?.length) return;

    const productIds = [
      ...new Set(
        initialData.items
          .map((item) => Number(item.productId ?? 0))
          .filter((id) => id > 0)
      ),
    ];
    if (productIds.length === 0) return;

    let cancelled = false;

    void (async () => {
      const productMap = new Map<
        number,
        { units: UnitOption[]; variants: VariantOption[]; productCostPrice: number; isVariantEnabled: boolean }
      >();

      await Promise.all(
        productIds.map(async (productId) => {
          try {
            const res = await apiClient.get(`/products/${productId}`, {
              params: { branchId },
              headers: { 'X-Branch-Id': String(branchId) },
            });
            const d = res.data as Record<string, unknown>;
            const variants = mapProductVariants(d);
            productMap.set(productId, {
              units: mapProductUnits(d),
              variants,
              productCostPrice: Number(d.costPrice ?? d.CostPrice ?? 0),
              isVariantEnabled:
                Boolean(d.isVariantEnabled ?? d.IsVariantEnabled ?? false) || variants.length > 0,
            });
          } catch {
            /* keep row as-is */
          }
        })
      );

      if (cancelled) return;

      setRows((prev) =>
        prev.map((row) => {
          if (row.productId <= 0) return row;
          const meta = productMap.get(row.productId);
          if (!meta) return row;

          const costPrice = resolvePurchaseCostPrice(
            meta.productCostPrice,
            meta.units,
            row.unitId,
            row.variantId,
            meta.variants
          );

          return {
            ...row,
            units: meta.units,
            variants: meta.variants,
            isVariantEnabled: meta.isVariantEnabled,
            productCostPrice: meta.productCostPrice,
            costPrice,
            totalCost: row.quantity * costPrice,
            unitName:
              meta.units.find((u) => u.id === row.unitId)?.unitName ?? row.unitName,
          };
        })
      );
    })();

    return () => {
      cancelled = true;
    };
  }, [initialData?.items, branchId]);

  // Search products whenever term changes
  useEffect(() => {
    if (!searchTerm.trim() || branchId <= 0) {
      setSearchResults([]);
      return;
    }

    const timer = setTimeout(() => {
      setSearchLoading(true);
      void (async () => {
        try {
          const res = await apiClient.get('/products', {
            params: { branchId, search: searchTerm, pageSize: 20 },
            headers: { 'X-Branch-Id': String(branchId) },
          });
          const data = (res.data as { products?: unknown[] })?.products ?? [];
          setSearchResults(
            (data as Record<string, unknown>[]).map((p) => ({
              id: Number(p.id ?? p.Id ?? 0),
              productName: String(p.productName ?? p.ProductName ?? ''),
              productCode: String(p.productCode ?? p.ProductCode ?? ''),
              // isVariantEnabled is now included in the list DTO (backend fix applied)
              isVariantEnabled: Boolean(p.isVariantEnabled ?? p.IsVariantEnabled ?? false),
            }))
          );
        } catch {
          setSearchResults([]);
        } finally {
          setSearchLoading(false);
        }
      })();
    }, 250);

    return () => clearTimeout(timer);
  }, [searchTerm, branchId]);

  // Close dropdown when clicking outside
  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (
        searchDropdownRef.current &&
        !searchDropdownRef.current.contains(e.target as Node)
      ) {
        closeSearch();
      }
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, []);

  const openSearch = useCallback((rowKey: string) => {
    setSearchRowKey(rowKey);
    setSearchTerm('');
    setSearchResults([]);
    setTimeout(() => searchInputRef.current?.focus(), 50);
  }, []);

  const closeSearch = () => {
    setSearchRowKey(null);
    setSearchTerm('');
    setSearchResults([]);
  };

  const selectProduct = async (product: ProductOption) => {
    if (!searchRowKey) return;

    const targetRowKey = searchRowKey;
    closeSearch();

    let units: UnitOption[] = [];
    let variants: VariantOption[] = [];
    let productCostPrice = 0;

    // isVariantEnabled from the detail call is the source of truth
    let isVariantEnabledFromDetail = false;

    try {
      const res = await apiClient.get(`/products/${product.id}`, {
        params: { branchId },
        headers: { 'X-Branch-Id': String(branchId) },
      });
      const d = res.data as Record<string, unknown>;

      // Read isVariantEnabled from the detail response (more reliable than the list response)
      isVariantEnabledFromDetail = Boolean(d.isVariantEnabled ?? d.IsVariantEnabled ?? false);
      productCostPrice = Number(d.costPrice ?? d.CostPrice ?? 0);

      units = mapProductUnits(d);
      variants = mapProductVariants(d);
    } catch {
      /* keep empty */
    }

    // Treat the product as variant-enabled if either the flag is set or variants were actually returned
    const hasVariants = variantFeatureEnabled && (isVariantEnabledFromDetail || variants.length > 0);
    const baseUnit = units.find((u) => u.isBaseUnit) ?? units[0];
    const selectedVariantId = hasVariants && variants.length > 0 ? variants[0].id : null;
    const selectedUnitId = baseUnit?.id ?? 0;
    const costPrice = resolvePurchaseCostPrice(
      productCostPrice,
      units,
      selectedUnitId,
      selectedVariantId,
      variants
    );

    setRows((prev) =>
      prev.map((r) =>
        r.key === targetRowKey
          ? {
              ...r,
              productId: product.id,
              productName: product.productName,
              productCode: product.productCode,
              isVariantEnabled: hasVariants,
              // Auto-select first variant so the field is never blank
              variantId: selectedVariantId,
              unitId: selectedUnitId,
              unitName: baseUnit?.unitName ?? '',
              conversionFactor: baseUnit?.conversionFactor ?? 1,
              costPrice,
              units,
              variants,
              productCostPrice,
              totalCost: r.quantity * costPrice,
            }
          : r
      )
    );
  };

  const updateRow = (key: string, field: Partial<ItemRow>) => {
    setRows((prev) =>
      prev.map((r) => {
        if (r.key !== key) return r;
        const updated = { ...r, ...field };
        updated.totalCost = updated.quantity * updated.costPrice;
        if ('unitId' in field) {
          const unit = updated.units.find((u) => u.id === updated.unitId);
          if (unit) {
            updated.unitName = unit.unitName;
            updated.conversionFactor = unit.conversionFactor;
          }
        }
        if ('unitId' in field || 'variantId' in field) {
          updated.costPrice = resolvePurchaseCostPrice(
            updated.productCostPrice,
            updated.units,
            updated.unitId,
            updated.variantId,
            updated.variants
          );
          updated.totalCost = updated.quantity * updated.costPrice;
        }
        if ('unitId' in field && updated.variantId == null && updated.isVariantEnabled && updated.variants.length > 0) {
          updated.variantId = updated.variants[0].id;
        }
        return updated;
      })
    );
  };

  const resetRow = (key: string) => {
    setRows((prev) =>
      prev.map((r) => (r.key === key ? { ...emptyRow(), key: r.key } : r))
    );
  };

  const removeRow = (key: string) => {
    if (rows.length <= 1) {
      resetRow(key);
    } else {
      setRows((prev) => prev.filter((r) => r.key !== key));
    }
    if (searchRowKey === key) closeSearch();
  };

  const grandTotal = rows.reduce((sum, r) => sum + r.totalCost, 0);
  const itemCount = rows.filter((r) => r.productId > 0).length;

  const validateForm = () => {
    const nextErrors: Record<string, string> = {};
    if (supplierId <= 0) nextErrors.supplierId = 'Supplier is required';
    if (warehouseId <= 0) nextErrors.warehouseId = 'Warehouse is required';
    if (branchId <= 0) nextErrors.branchId = branchError ?? 'Branch is required';

    const invalidRows = rows.filter((r) => r.productId > 0 && (r.unitId <= 0 || r.quantity <= 0));
    const missingVariantRows = variantFeatureEnabled
      ? rows.filter(
          (r) =>
            r.productId > 0 &&
            r.isVariantEnabled &&
            r.variants.length > 0 &&
            (r.variantId == null || r.variantId <= 0),
        )
      : [];
    const emptyRows = rows.filter((r) => r.productId <= 0);

    if (rows.every((r) => r.productId <= 0)) {
      nextErrors.items = 'At least one product is required';
    } else if (missingVariantRows.length > 0) {
      nextErrors.items = 'All variant-enabled products must have a variant selected';
    } else if (invalidRows.length > 0) {
      nextErrors.items = 'All added items must have a unit and quantity greater than zero';
    } else if (emptyRows.length > 0 && rows.length > 1) {
      // Allow empty trailing rows — just warn but don't block
    }

    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const handleReset = () => {
    setInvoiceNo(safeString(initialData?.invoiceNo));
    if (!isEditMode) {
      setInvoiceCodeResetKey((key) => key + 1);
    }
    setSupplierId(Number(initialData?.supplierId ?? 0));
    setWarehouseId(Number(initialData?.warehouseId ?? 0));
    setPurchaseDate(
      initialData?.purchaseDate
        ? String(initialData.purchaseDate).slice(0, 10)
        : new Date().toISOString().slice(0, 10)
    );
    setNotes(safeString(initialData?.notes));
    setIsCreditPurchase(Boolean(initialData?.isCreditPurchase));
    setRows(buildRowsFromInitial(initialData?.items));
    setErrors({});
    closeSearch();
  };

  const submitModeRef = useRef<PurchaseSubmitMode>('draft');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!validateForm()) return;
    onSubmit(
      {
        invoiceNo: invoiceNo.trim(),
        supplierId,
        warehouseId,
        purchaseDate,
        notes: notes.trim(),
        isCreditPurchase,
        branchId,
        items: rows
          .filter((r) => r.productId > 0 && r.unitId > 0)
          .map((r) => ({
            productId: r.productId,
            variantId: r.variantId,
            unitId: r.unitId,
            quantity: r.quantity,
            conversionFactor: r.conversionFactor,
            costPrice: r.costPrice,
          })),
      },
      submitModeRef.current
    );
  };

  return (
    <form onSubmit={handleSubmit} className="flex h-full min-h-0 flex-col">
      {/* Header fields — fixed, no scroll */}
      <div className="shrink-0 space-y-5 border-b border-gray-100 px-6 py-5">
        <p className="text-sm text-gray-500">Fill in the purchase order details and add line items below.</p>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
          {errors.branchId && (
            <p className="md:col-span-2 text-sm text-red-600">{errors.branchId}</p>
          )}

          <CodeFieldWithGenerate
            label="Invoice No"
            name="invoiceNo"
            value={invoiceNo}
            onChange={(value) => {
              setInvoiceNo(value);
              setErrors((prev) => ({ ...prev, invoiceNo: '' }));
            }}
            module={CODE_MODULES.Purchase}
            branchId={branchId}
            error={errors.invoiceNo}
            isEditMode={isEditMode}
            resetKey={invoiceCodeResetKey}
          />

          <FormInput
            label="Purchase Date"
            name="purchaseDate"
            type="date"
            value={purchaseDate}
            onChange={(e) => setPurchaseDate(e.target.value)}
            required
          />

          <FormSelect
            label="Supplier"
            name="supplierId"
            value={String(supplierId || '')}
            onChange={(e) => {
              setSupplierId(Number(e.target.value || 0));
              setErrors((prev) => ({ ...prev, supplierId: '' }));
            }}
            options={[
              { label: 'Select supplier', value: '' },
              ...suppliers.map((s) => ({ label: s.name, value: String(s.id) })),
            ]}
            required
            error={errors.supplierId}
          />

          <FormSelect
            label="Warehouse"
            name="warehouseId"
            value={String(warehouseId || '')}
            onChange={(e) => {
              setWarehouseId(Number(e.target.value || 0));
              setErrors((prev) => ({ ...prev, warehouseId: '' }));
            }}
            options={[
              { label: 'Select warehouse', value: '' },
              ...warehouses.map((w) => ({ label: w.name, value: String(w.id) })),
            ]}
            required
            error={errors.warehouseId}
          />

          <div className="md:col-span-2">
            <FormTextarea
              label="Notes"
              name="notes"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder="Optional notes about this purchase order"
              rows={2}
            />
          </div>

          <label className="md:col-span-2 flex items-center gap-3 rounded-lg border border-gray-200 bg-gray-50 px-4 py-3 cursor-pointer">
            <input
              type="checkbox"
              checked={isCreditPurchase}
              onChange={(e) => setIsCreditPurchase(e.target.checked)}
              disabled={isPosted}
              className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
            />
            <span>
              <span className="block text-sm font-semibold text-gray-800">Credit Purchase</span>
              <span className="block text-xs text-gray-500">Record as supplier payable — no cash payment on post</span>
            </span>
          </label>
        </div>
      </div>

      {/* Line Items — only this section scrolls */}
      <div className="flex min-h-0 flex-1 flex-col px-6 py-4">
          <div className="mb-3 flex items-center justify-between">
            <div>
              <h3 className="text-sm font-semibold text-gray-800">Line Items</h3>
              {itemCount > 0 && (
                <p className="mt-0.5 text-xs text-gray-500">
                  {itemCount} {itemCount === 1 ? 'product' : 'products'} added
                </p>
              )}
            </div>
            <button
              type="button"
              onClick={() => setRows((prev) => [...prev, emptyRow()])}
              className="inline-flex items-center gap-1.5 rounded-md border border-blue-200 bg-blue-50 px-3 py-1.5 text-xs font-medium text-blue-700 transition-colors hover:bg-blue-100"
            >
              <svg className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M12 4v16m8-8H4" />
              </svg>
              Add Row
            </button>
          </div>

          {errors.items && (
            <div className="mb-3 flex items-center gap-2 rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
              <svg className="h-4 w-4 shrink-0" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
              </svg>
              {errors.items}
            </div>
          )}

          <div className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-lg border border-gray-200 bg-white shadow-sm">
            {/* Table header — sticky within line items panel */}
            <div
              className="shrink-0 grid gap-0 border-b border-gray-200 bg-gray-50 px-3 py-2.5 text-xs font-semibold uppercase tracking-wide text-gray-500"
              style={{ gridTemplateColumns: lineItemGridColumns }}
            >
              <div>Product</div>
              {variantFeatureEnabled && <div>Variant</div>}
              {unitFeatureEnabled && <div>Unit</div>}
              <div className="text-right">Qty</div>
              <div className="text-right">Base Qty</div>
              <div className="text-right">Cost</div>
              <div className="text-right">Total</div>
              <div className="text-center">Actions</div>
            </div>

            {/* Rows — scrollable */}
            <div className="min-h-0 flex-1 divide-y divide-gray-100 overflow-y-auto">
              {rows.map((row, idx) => (
                <div
                  key={row.key}
                  className={`grid items-center gap-0 px-3 py-2 transition-colors ${
                    row.productId > 0 ? 'bg-white' : 'bg-gray-50/50'
                  }`}
                  style={{ gridTemplateColumns: lineItemGridColumns }}
                >
                  {/* Product cell with per-row search */}
                  <div className="pr-2">
                    {searchRowKey === row.key ? (
                      <div className="relative" ref={searchDropdownRef}>
                        <div className="flex items-center gap-1 rounded-md border border-blue-400 bg-white px-2 py-1.5 ring-2 ring-blue-100">
                          <svg className="h-3.5 w-3.5 shrink-0 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                          </svg>
                          <input
                            ref={searchInputRef}
                            type="text"
                            value={searchTerm}
                            onChange={(e) => setSearchTerm(e.target.value)}
                            placeholder={row.productId > 0 ? 'Search to change product…' : 'Search product…'}
                            className="min-w-0 flex-1 bg-transparent text-sm text-gray-900 placeholder-gray-400 outline-none"
                            onKeyDown={(e) => e.key === 'Escape' && closeSearch()}
                          />
                          {searchLoading && (
                            <svg className="h-3.5 w-3.5 animate-spin text-blue-500" fill="none" viewBox="0 0 24 24">
                              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
                            </svg>
                          )}
                          <button
                            type="button"
                            onClick={closeSearch}
                            title="Cancel"
                            className="shrink-0 rounded px-1.5 py-0.5 text-xs font-medium text-gray-500 hover:bg-gray-100 hover:text-gray-700"
                          >
                            Cancel
                          </button>
                        </div>
                        {searchResults.length > 0 && (
                          <div className="absolute left-0 top-full z-50 mt-1 max-h-56 w-72 overflow-auto rounded-lg border border-gray-200 bg-white shadow-xl">
                            {searchResults.map((p) => (
                              <button
                                key={p.id}
                                type="button"
                                onClick={() => void selectProduct(p)}
                                className="flex w-full items-center justify-between px-3 py-2.5 text-left text-sm hover:bg-blue-50"
                              >
                                <div>
                                  <p className="font-medium text-gray-900">{p.productName}</p>
                                  <p className="text-xs text-gray-400">{p.productCode}</p>
                                </div>
                                {variantFeatureEnabled && p.isVariantEnabled && (
                                  <span className="ml-2 shrink-0 rounded-full bg-purple-100 px-1.5 py-0.5 text-xs font-medium text-purple-700">
                                    Variants
                                  </span>
                                )}
                              </button>
                            ))}
                          </div>
                        )}
                        {searchTerm.length > 1 && !searchLoading && searchResults.length === 0 && (
                          <div className="absolute left-0 top-full z-50 mt-1 w-72 rounded-lg border border-gray-200 bg-white px-3 py-3 text-sm text-gray-500 shadow-xl">
                            No products found for "{searchTerm}"
                          </div>
                        )}
                      </div>
                    ) : row.productId > 0 ? (
                      <div className="min-w-0">
                        <p className="truncate text-sm font-medium text-gray-900">{row.productName}</p>
                        {row.productCode && (
                          <p className="text-xs text-gray-400">{row.productCode}</p>
                        )}
                        {!isPosted && (
                          <button
                            type="button"
                            onClick={() => openSearch(row.key)}
                            className="mt-1 text-xs font-medium text-blue-600 hover:text-blue-800 hover:underline"
                          >
                            Change
                          </button>
                        )}
                      </div>
                    ) : (
                      <button
                        type="button"
                        onClick={() => openSearch(row.key)}
                        disabled={isPosted}
                        className="flex items-center gap-1.5 rounded-md border border-dashed border-gray-300 px-2 py-1.5 text-xs text-gray-400 transition-colors hover:border-blue-400 hover:bg-blue-50 hover:text-blue-600 disabled:cursor-not-allowed disabled:opacity-50"
                      >
                        <svg className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                        </svg>
                        {`Row ${idx + 1} — Select product`}
                      </button>
                    )}
                  </div>

                  {variantFeatureEnabled && (
                  <div className="px-1">
                    {row.isVariantEnabled && row.variants.length > 0 ? (
                      <select
                        value={row.variantId ?? ''}
                        onChange={(e) =>
                          updateRow(row.key, {
                            variantId: e.target.value ? Number(e.target.value) : null,
                          })
                        }
                        disabled={isPosted || row.productId <= 0}
                        className="w-full rounded-md border border-gray-300 bg-white px-2 py-1.5 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 disabled:bg-gray-50 disabled:text-gray-400"
                      >
                        <option value="">— Select —</option>
                        {row.variants.map((v) => (
                          <option key={v.id} value={v.id}>
                            {v.variantName}
                          </option>
                        ))}
                      </select>
                    ) : row.isVariantEnabled ? (
                      <span className="text-xs italic text-amber-600">Loading…</span>
                    ) : (
                      <span className="text-gray-300">—</span>
                    )}
                  </div>
                  )}

                  {unitFeatureEnabled && (
                  <div className="px-1">
                    {row.units.length > 1 ? (
                      <select
                        value={row.unitId}
                        onChange={(e) => updateRow(row.key, { unitId: Number(e.target.value) })}
                        disabled={isPosted || row.productId <= 0}
                        className="w-full rounded-md border border-gray-300 bg-white px-2 py-1.5 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 disabled:bg-gray-50 disabled:text-gray-400"
                      >
                        {row.units.map((u) => (
                          <option key={u.id} value={u.id}>
                            {u.unitName}
                          </option>
                        ))}
                      </select>
                    ) : (
                      <span className="text-sm text-gray-700">{row.unitName || '—'}</span>
                    )}
                  </div>
                  )}

                  {/* Qty */}
                  <div className="px-1">
                    <input
                      type="number"
                      min="0.0001"
                      step="any"
                      value={row.quantity}
                      onChange={(e) =>
                        updateRow(row.key, { quantity: parseFloat(e.target.value) || 0 })
                      }
                      disabled={isPosted || row.productId <= 0}
                      className="w-full rounded-md border border-gray-300 bg-white px-2 py-1.5 text-right text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 disabled:bg-gray-50 disabled:text-gray-400"
                    />
                  </div>

                  {/* Base Qty */}
                  <div className="px-1 text-right">
                    <span className="text-sm font-medium text-gray-600">
                      {(() => {
                        const baseQty = row.conversionFactor > 0
                          ? row.quantity / row.conversionFactor
                          : row.quantity;
                        return baseQty % 1 === 0 ? baseQty.toFixed(0) : baseQty.toFixed(3);
                      })()}
                    </span>
                  </div>

                  {/* Cost — auto from ProductUnit, not editable */}
                  <div className="px-1 text-right">
                    <span
                      className="inline-block w-full rounded-md border border-gray-200 bg-gray-50 px-2 py-1.5 text-sm text-gray-700"
                      title="Auto-filled from product unit purchase price"
                    >
                      {row.productId > 0 ? row.costPrice.toFixed(2) : '—'}
                    </span>
                  </div>

                  {/* Total */}
                  <div className="px-1 text-right">
                    <span className={`text-sm font-semibold ${row.totalCost > 0 ? 'text-gray-900' : 'text-gray-300'}`}>
                      {row.totalCost > 0 ? row.totalCost.toFixed(2) : '—'}
                    </span>
                  </div>

                  {/* Actions */}
                  <div className="flex justify-center px-1">
                    {!isPosted && (row.productId > 0 || rows.length > 1) ? (
                      <button
                        type="button"
                        onClick={() => removeRow(row.key)}
                        title="Remove line item"
                        className="rounded p-1 text-gray-400 transition-colors hover:bg-red-50 hover:text-red-500"
                      >
                        <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                        </svg>
                      </button>
                    ) : (
                      <span className="text-gray-200">—</span>
                    )}
                  </div>
                </div>
              ))}
            </div>

            {/* Grand total footer — fixed below scroll area */}
            <div className="flex shrink-0 items-center justify-between rounded-b-lg border-t border-gray-200 bg-gray-50 px-4 py-3">
              <span className="text-sm font-medium text-gray-500">
                {rows.filter((r) => r.productId > 0).length} of {rows.length}{' '}
                {rows.length === 1 ? 'row' : 'rows'} filled
              </span>
              <div className="flex items-center gap-3">
                <span className="text-sm font-medium text-gray-600">Grand Total</span>
                <span className="min-w-[100px] rounded-md bg-blue-600 px-4 py-1.5 text-right text-sm font-bold text-white">
                  {grandTotal.toFixed(2)}
                </span>
              </div>
            </div>
          </div>
      </div>

      {/* Footer */}
      <div className="shrink-0 border-t border-gray-200 bg-white px-6 py-4 flex items-center justify-between">
        <FormButton type="button" label="Reset" variant="secondary" onClick={handleReset} />
        {isPosted ? (
          <span className="inline-flex items-center gap-2 rounded-md bg-green-50 px-4 py-2 text-sm font-medium text-green-700">
            <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            Posted — read only
          </span>
        ) : (
          <div className="flex items-center gap-3">
            <button
              type="submit"
              disabled={isLoading}
              onClick={() => { submitModeRef.current = 'draft'; }}
              className="inline-flex items-center gap-2 rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 shadow-sm transition-colors hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-60"
            >
              <svg className="h-4 w-4 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7H5a2 2 0 00-2 2v9a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-3m-1 4l-3 3m0 0l-3-3m3 3V4" />
              </svg>
              Save as Draft
            </button>
            <button
              type="submit"
              disabled={isLoading}
              onClick={() => { submitModeRef.current = 'post'; }}
              className="inline-flex items-center gap-2 rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isLoading ? (
                <svg className="h-4 w-4 animate-spin" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
                </svg>
              ) : (
                <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              )}
              Post Purchase
            </button>
          </div>
        )}
      </div>
    </form>
  );
};

export default PurchaseForm;
