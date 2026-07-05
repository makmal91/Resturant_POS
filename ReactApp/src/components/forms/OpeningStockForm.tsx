import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { FormButton, FormInput, FormSelect, FormTextarea } from './index';
import CodeFieldWithGenerate from './CodeFieldWithGenerate';
import { CODE_MODULES } from '../../services/codeGeneratorService';
import { useFormBranchId } from '../../hooks/useFormBranchId';
import { useBusinessCurrency } from '../../hooks/useBusinessCurrency';
import { useHasFeature } from '../../hooks/useFeature';
import { FEATURE_KEYS } from '../../types/featurePermissions';
import apiClient from '../../services/api';
import { safeString } from '../../utils/safeValues';
import { toBaseQuantity } from '../../modules/product/unitPricing';

export interface OpeningStockLineFormData {
  productId: number;
  productName: string;
  productCode: string;
  baseUnitName: string;
  variantId?: number | null;
  variantName?: string;
  unitId: number;
  unitName?: string;
  quantity: number;
  conversionFactor?: number;
  convertedQuantity?: number;
  costPrice: number;
}

export interface OpeningStockFormData {
  voucherNo: string;
  voucherDate: string;
  description: string;
  warehouseId: number;
  branchId: number;
  id?: number;
  lines: OpeningStockLineFormData[];
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

interface UnitOption {
  id: number;
  unitName: string;
  conversionFactor: number;
  isBaseUnit: boolean;
  costPrice?: number | null;
}

interface VariantOption {
  id: number;
  variantName: string;
  costPriceOverride?: number | null;
}

interface ItemRow {
  key: string;
  productId: number;
  productName: string;
  productCode: string;
  baseUnitName: string;
  variantId: number | null;
  unitId: number;
  unitName: string;
  quantity: number;
  conversionFactor: number;
  costPrice: number;
  totalAmount: number;
  units: UnitOption[];
  variants: VariantOption[];
  isVariantEnabled: boolean;
  productCostPrice: number;
  metaLoading: boolean;
}

interface OpeningStockFormProps {
  initialData?: Partial<
    OpeningStockFormData & {
      readOnly?: boolean;
      lines?: OpeningStockLineFormData[];
    }
  > | null;
  warehouses?: LookupOption[];
  onSubmit: (data: OpeningStockFormData) => void;
  isLoading?: boolean;
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
    costPriceOverride:
      v.costPriceOverride != null || v.CostPriceOverride != null
        ? Number(v.costPriceOverride ?? v.CostPriceOverride ?? 0)
        : null,
  }));

const resolveUnitCostPrice = (
  productCostPrice: number,
  units: UnitOption[],
  unitId: number,
  variantId: number | null,
  variants: VariantOption[],
): number => {
  const unit = units.find((u) => u.id === unitId);
  const variant = variantId != null ? variants.find((v) => v.id === variantId) : undefined;

  if (unit?.costPrice != null) return unit.costPrice;

  const baseCost = variant?.costPriceOverride ?? productCostPrice;
  const factor = unit?.isBaseUnit ? 1 : unit?.conversionFactor ?? 1;
  return baseCost * factor;
};

const formatQty = (value: number) => (value % 1 === 0 ? value.toFixed(0) : value.toFixed(3));

const unitOptionLabel = (unit: UnitOption, baseUnitName: string) => {
  if (unit.isBaseUnit) return `${unit.unitName} (Base)`;
  if (baseUnitName) return `${unit.unitName} (${formatQty(unit.conversionFactor)} ${baseUnitName})`;
  return `${unit.unitName} (×${formatQty(unit.conversionFactor)})`;
};

const emptyRow = (): ItemRow => ({
  key: crypto.randomUUID(),
  productId: 0,
  productName: '',
  productCode: '',
  baseUnitName: '',
  variantId: null,
  unitId: 0,
  unitName: '',
  quantity: 0,
  conversionFactor: 1,
  costPrice: 0,
  totalAmount: 0,
  units: [],
  variants: [],
  isVariantEnabled: false,
  productCostPrice: 0,
  metaLoading: false,
});

const buildRowsFromInitial = (lines?: OpeningStockLineFormData[]): ItemRow[] => {
  if (!lines?.length) return [emptyRow()];
  return lines.map((line) => {
    const conversionFactor = Number(line.conversionFactor ?? 1) || 1;
    const unitQty = Number(line.quantity ?? 0);
    const costPrice = Number(line.costPrice ?? 0);
    return {
      key: crypto.randomUUID(),
      productId: Number(line.productId ?? 0),
      productName: safeString(line.productName),
      productCode: safeString(line.productCode),
      baseUnitName: safeString(line.baseUnitName),
      variantId: line.variantId ?? null,
      unitId: Number(line.unitId ?? 0),
      unitName: safeString(line.unitName),
      quantity: unitQty,
      conversionFactor,
      costPrice,
      totalAmount: unitQty * costPrice,
      units: [],
      variants: [],
      isVariantEnabled: Boolean(line.variantId),
      productCostPrice: costPrice,
      metaLoading: false,
    };
  });
};

const rowsToFormLines = (rows: ItemRow[]): OpeningStockLineFormData[] =>
  rows
    .filter((row) => row.productId > 0 && row.unitId > 0)
    .map((row) => ({
      productId: row.productId,
      productName: row.productName,
      productCode: row.productCode,
      baseUnitName: row.baseUnitName,
      variantId: row.variantId,
      variantName: row.variants.find((v) => v.id === row.variantId)?.variantName ?? '',
      unitId: row.unitId,
      unitName: row.unitName,
      quantity: row.quantity,
      conversionFactor: row.conversionFactor,
      convertedQuantity: toBaseQuantity(row.quantity, row.conversionFactor),
      costPrice: row.costPrice,
    }));

const lineKey = (productId: number, variantId: number | null) => `${productId}:${variantId ?? 0}`;

const OpeningStockForm: React.FC<OpeningStockFormProps> = ({
  initialData,
  warehouses = [],
  onSubmit,
  isLoading = false,
}) => {
  const isViewMode = Boolean(initialData?.readOnly);
  const isEditMode = Boolean(initialData?.id) && !isViewMode;
  const { branchId: resolvedBranchId, branchError } = useFormBranchId(initialData?.branchId);
  const { symbol } = useBusinessCurrency();
  const variantFeatureEnabled = useHasFeature(FEATURE_KEYS.VARIANT);
  const unitFeatureEnabled = useHasFeature(FEATURE_KEYS.UNIT);

  const lineGridColumns = useMemo(() => {
    const parts = ['minmax(180px,2fr)'];
    if (variantFeatureEnabled) parts.push('minmax(100px,1.1fr)');
    if (unitFeatureEnabled) parts.push('minmax(110px,1.2fr)');
    parts.push('88px', '72px', '88px');
    if (isViewMode) parts.push('80px', '88px');
    else parts.push('44px');
    return parts.join(' ');
  }, [variantFeatureEnabled, unitFeatureEnabled, isViewMode]);

  const [voucherNo, setVoucherNo] = useState(safeString(initialData?.voucherNo));
  const [voucherDate, setVoucherDate] = useState(
    safeString(initialData?.voucherDate) || new Date().toISOString().slice(0, 10),
  );
  const [description, setDescription] = useState(safeString(initialData?.description));
  const [warehouseId, setWarehouseId] = useState(Number(initialData?.warehouseId ?? 0));
  const [rows, setRows] = useState<ItemRow[]>(() => buildRowsFromInitial(initialData?.lines));
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [rowErrors, setRowErrors] = useState<Record<string, string>>({});

  const [searchRowKey, setSearchRowKey] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [searchResults, setSearchResults] = useState<ProductOption[]>([]);
  const [searchLoading, setSearchLoading] = useState(false);

  const searchInputRef = useRef<HTMLInputElement>(null);
  const searchDropdownRef = useRef<HTMLDivElement>(null);
  const variantSelectRefs = useRef<Map<string, HTMLSelectElement>>(new Map());
  const unitSelectRefs = useRef<Map<string, HTMLSelectElement>>(new Map());
  const qtyInputRefs = useRef<Map<string, HTMLInputElement>>(new Map());

  const closeSearch = useCallback(() => {
    setSearchRowKey(null);
    setSearchTerm('');
    setSearchResults([]);
  }, []);

  useEffect(() => {
    setRows(buildRowsFromInitial(initialData?.lines));
    setVoucherNo(safeString(initialData?.voucherNo));
    setVoucherDate(safeString(initialData?.voucherDate) || new Date().toISOString().slice(0, 10));
    setDescription(safeString(initialData?.description));
    setWarehouseId(Number(initialData?.warehouseId ?? 0));
    setErrors({});
    setRowErrors({});
    closeSearch();
  }, [initialData, closeSearch]);

  useEffect(() => {
    if (!searchTerm.trim() || resolvedBranchId <= 0 || isViewMode) {
      setSearchResults([]);
      return;
    }

    const timer = window.setTimeout(() => {
      setSearchLoading(true);
      void (async () => {
        try {
          const res = await apiClient.get('/products', {
            params: { branchId: resolvedBranchId, search: searchTerm.trim(), pageSize: 20, status: true },
            headers: { 'X-Branch-Id': String(resolvedBranchId) },
          });
          const data = (res.data as { products?: unknown[] })?.products ?? [];
          setSearchResults(
            (data as Record<string, unknown>[]).map((p) => ({
              id: Number(p.id ?? p.Id ?? 0),
              productName: String(p.productName ?? p.ProductName ?? ''),
              productCode: String(p.productCode ?? p.ProductCode ?? ''),
              isVariantEnabled: Boolean(p.isVariantEnabled ?? p.IsVariantEnabled ?? false),
            })),
          );
        } catch {
          setSearchResults([]);
        } finally {
          setSearchLoading(false);
        }
      })();
    }, 250);

    return () => window.clearTimeout(timer);
  }, [searchTerm, resolvedBranchId, isViewMode]);

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (searchDropdownRef.current && !searchDropdownRef.current.contains(e.target as Node)) {
        closeSearch();
      }
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [closeSearch]);

  useEffect(() => {
    if (resolvedBranchId <= 0 || !initialData?.lines?.length || isViewMode) return;

    const productIds = [...new Set(initialData.lines.map((l) => Number(l.productId)).filter((id) => id > 0))];
    if (productIds.length === 0) return;

    let cancelled = false;
    void (async () => {
      const productMap = new Map<
        number,
        { units: UnitOption[]; variants: VariantOption[]; productCostPrice: number; isVariantEnabled: boolean; baseUnitName: string }
      >();

      await Promise.all(
        productIds.map(async (productId) => {
          try {
            const res = await apiClient.get(`/products/${productId}`, {
              params: { branchId: resolvedBranchId },
              headers: { 'X-Branch-Id': String(resolvedBranchId) },
            });
            const d = res.data as Record<string, unknown>;
            const units = mapProductUnits(d);
            const variants = mapProductVariants(d);
            const baseUnit = units.find((u) => u.isBaseUnit) ?? units[0];
            productMap.set(productId, {
              units,
              variants,
              productCostPrice: Number(d.costPrice ?? d.CostPrice ?? 0),
              isVariantEnabled:
                Boolean(d.isVariantEnabled ?? d.IsVariantEnabled ?? false) || variants.length > 0,
              baseUnitName: baseUnit?.unitName ?? '',
            });
          } catch {
            /* keep row as-is */
          }
        }),
      );

      if (cancelled) return;

      setRows((prev) =>
        prev.map((row) => {
          if (row.productId <= 0) return row;
          const meta = productMap.get(row.productId);
          if (!meta) return row;
          const costPrice = resolveUnitCostPrice(
            meta.productCostPrice,
            meta.units,
            row.unitId || meta.units.find((u) => u.isBaseUnit)?.id || 0,
            row.variantId,
            meta.variants,
          );
          return {
            ...row,
            units: meta.units,
            variants: meta.variants,
            isVariantEnabled: meta.isVariantEnabled,
            productCostPrice: meta.productCostPrice,
            baseUnitName: meta.baseUnitName || row.baseUnitName,
            costPrice,
            totalAmount: row.quantity * costPrice,
            unitName: meta.units.find((u) => u.id === row.unitId)?.unitName ?? row.unitName,
          };
        }),
      );
    })();

    return () => {
      cancelled = true;
    };
  }, [initialData?.lines, resolvedBranchId, isViewMode]);

  const openSearch = useCallback((rowKey: string) => {
    setSearchRowKey(rowKey);
    setSearchTerm('');
    setSearchResults([]);
    window.setTimeout(() => searchInputRef.current?.focus(), 50);
  }, []);

  const focusNextField = (rowKey: string, row: ItemRow) => {
    window.setTimeout(() => {
      if (variantFeatureEnabled && row.isVariantEnabled && row.variants.length > 0) {
        variantSelectRefs.current.get(rowKey)?.focus();
        return;
      }
      if (unitFeatureEnabled && row.units.length > 0) {
        unitSelectRefs.current.get(rowKey)?.focus();
        return;
      }
      qtyInputRefs.current.get(rowKey)?.focus();
    }, 80);
  };

  const selectProduct = async (product: ProductOption) => {
    if (!searchRowKey || resolvedBranchId <= 0) return;
    const targetKey = searchRowKey;
    closeSearch();

    setRows((prev) =>
      prev.map((row) =>
        row.key === targetKey
          ? { ...row, productId: product.id, productName: product.productName, productCode: product.productCode, metaLoading: true }
          : row,
      ),
    );

    let units: UnitOption[] = [];
    let variants: VariantOption[] = [];
    let productCostPrice = 0;
    let isVariantEnabled = false;
    let baseUnitName = '';

    try {
      const res = await apiClient.get(`/products/${product.id}`, {
        params: { branchId: resolvedBranchId },
        headers: { 'X-Branch-Id': String(resolvedBranchId) },
      });
      const d = res.data as Record<string, unknown>;
      units = mapProductUnits(d);
      variants = mapProductVariants(d);
      productCostPrice = Number(d.costPrice ?? d.CostPrice ?? 0);
      isVariantEnabled =
        variantFeatureEnabled &&
        (Boolean(d.isVariantEnabled ?? d.IsVariantEnabled ?? false) || variants.length > 0);
      const baseUnit = units.find((u) => u.isBaseUnit) ?? units[0];
      baseUnitName = baseUnit?.unitName ?? '';
    } catch {
      /* partial row */
    }

    const selectedVariantId = isVariantEnabled && variants.length > 0 ? variants[0].id : null;
    const selectedUnit = units.find((u) => u.isBaseUnit) ?? units[0];
    const selectedUnitId = selectedUnit?.id ?? 0;
    const conversionFactor = unitFeatureEnabled ? selectedUnit?.conversionFactor ?? 1 : 1;
    const costPrice = resolveUnitCostPrice(
      productCostPrice,
      units,
      selectedUnitId,
      selectedVariantId,
      variants,
    );

    const nextRow: ItemRow = {
      key: targetKey,
      productId: product.id,
      productName: product.productName,
      productCode: product.productCode,
      baseUnitName,
      variantId: selectedVariantId,
      unitId: selectedUnitId,
      unitName: selectedUnit?.unitName ?? '',
      quantity: 0,
      conversionFactor,
      costPrice,
      totalAmount: 0,
      units,
      variants,
      isVariantEnabled,
      productCostPrice,
      metaLoading: false,
    };

    setRows((prev) => prev.map((row) => (row.key === targetKey ? nextRow : row)));
    setRowErrors((prev) => {
      const next = { ...prev };
      delete next[targetKey];
      return next;
    });
    focusNextField(targetKey, nextRow);
  };

  const updateRow = (key: string, field: Partial<ItemRow>) => {
    setRows((prev) =>
      prev.map((row) => {
        if (row.key !== key) return row;
        const updated = { ...row, ...field };

        if ('unitId' in field) {
          const unit = updated.units.find((u) => u.id === updated.unitId);
          if (unit) {
            updated.unitName = unit.unitName;
            updated.conversionFactor = unitFeatureEnabled ? unit.conversionFactor : 1;
          }
        }

        if ('unitId' in field || 'variantId' in field) {
          updated.costPrice = resolveUnitCostPrice(
            updated.productCostPrice,
            updated.units,
            updated.unitId,
            updated.variantId,
            updated.variants,
          );
        }

        updated.totalAmount = updated.quantity * updated.costPrice;
        return updated;
      }),
    );
    setRowErrors((prev) => {
      const next = { ...prev };
      delete next[key];
      return next;
    });
    setErrors((prev) => ({ ...prev, items: '' }));
  };

  const addRow = () => {
    const newRow = emptyRow();
    setRows((prev) => [...prev, newRow]);
    window.setTimeout(() => openSearch(newRow.key), 50);
  };

  const removeRow = (key: string) => {
    if (rows.length <= 1) {
      setRows([emptyRow()]);
    } else {
      setRows((prev) => prev.filter((row) => row.key !== key));
    }
    if (searchRowKey === key) closeSearch();
  };

  const filledCount = rows.filter((row) => row.productId > 0).length;
  const grandTotal = rows.reduce((sum, row) => sum + row.totalAmount, 0);

  const validate = (): boolean => {
    const nextErrors: Record<string, string> = {};
    const nextRowErrors: Record<string, string> = {};

    if (resolvedBranchId <= 0) {
      nextErrors.branchId = branchError || 'Select a branch before saving.';
    }
    if (!warehouseId) {
      nextErrors.warehouseId = 'Warehouse is required.';
    }

    const filledRows = rows.filter((row) => row.productId > 0);
    if (filledRows.length === 0) {
      nextErrors.items = 'Add at least one product.';
    }

    const seen = new Set<string>();
    rows.forEach((row) => {
      if (row.productId <= 0) return;

      const key = lineKey(row.productId, row.variantId);
      if (seen.has(key)) {
        nextRowErrors[row.key] = 'Duplicate product + variant';
      }
      seen.add(key);

      if (variantFeatureEnabled && row.isVariantEnabled && row.variants.length > 0 && !row.variantId) {
        nextRowErrors[row.key] = 'Variant is required';
      }
      if (unitFeatureEnabled && row.unitId <= 0) {
        nextRowErrors[row.key] = 'Unit is required';
      }
      if (Number(row.quantity) <= 0) {
        nextRowErrors[row.key] = 'Quantity must be greater than 0';
      }
      if (Number(row.costPrice) <= 0) {
        nextRowErrors[row.key] = 'Cost price missing — update product master';
      }
    });

    setErrors(nextErrors);
    setRowErrors(nextRowErrors);
    return Object.keys(nextErrors).length === 0 && Object.keys(nextRowErrors).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (isViewMode || !validate()) return;

    onSubmit({
      id: isEditMode ? Number(initialData?.id ?? 0) : undefined,
      voucherNo: voucherNo.trim(),
      voucherDate,
      description: description.trim(),
      warehouseId,
      branchId: resolvedBranchId,
      lines: rowsToFormLines(rows),
    });
  };

  const minGridWidth = isViewMode ? '920px' : '760px';

  return (
    <form onSubmit={handleSubmit} className="flex h-full min-h-0 flex-col">
      <div className="shrink-0 space-y-5 border-b border-gray-100 px-6 py-5">
        <p className="text-sm text-gray-500">
          Enter stock in any unit — the system converts and stores everything in the base unit.
        </p>

        <div className="grid grid-cols-1 gap-5 md:grid-cols-2">
          {errors.branchId && <p className="md:col-span-2 text-sm text-red-600">{errors.branchId}</p>}

          {!isViewMode && !isEditMode ? (
            <CodeFieldWithGenerate
              label="Voucher No"
              name="voucherNo"
              module={CODE_MODULES.OpeningStock}
              branchId={resolvedBranchId}
              value={voucherNo}
              onChange={setVoucherNo}
              isEditMode={false}
              required
            />
          ) : (
            <div className="mb-5">
              <label className="mb-2 block text-sm font-medium text-gray-800">Voucher No</label>
              <div className="rounded-lg border border-gray-200 bg-gray-50 px-4 py-3 text-sm font-semibold text-gray-900">
                {voucherNo}
              </div>
            </div>
          )}

          <FormInput
            label="Date"
            name="voucherDate"
            type="date"
            value={voucherDate}
            onChange={(e) => setVoucherDate(e.target.value)}
            disabled={isViewMode}
            required
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
            disabled={isViewMode}
            error={errors.warehouseId}
          />

          <div className="md:col-span-2">
            <FormTextarea
              label="Remarks"
              name="description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              disabled={isViewMode}
              rows={2}
              placeholder="Optional notes for this voucher"
            />
          </div>
        </div>
      </div>

      <div className="flex min-h-0 flex-1 flex-col px-6 py-4">
        <div className="mb-3 flex items-center justify-between">
          <div>
            <h3 className="text-sm font-semibold text-gray-800">Stock Lines</h3>
            {filledCount > 0 && (
              <p className="mt-0.5 text-xs text-gray-500">
                {filledCount} {filledCount === 1 ? 'product' : 'products'} added
              </p>
            )}
          </div>
          {!isViewMode && (
            <button
              type="button"
              onClick={addRow}
              className="inline-flex items-center gap-1.5 rounded-md border border-blue-200 bg-blue-50 px-3 py-1.5 text-xs font-medium text-blue-700 transition-colors hover:bg-blue-100"
            >
              <svg className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M12 4v16m8-8H4" />
              </svg>
              Add Row
            </button>
          )}
        </div>

        {errors.items && (
          <div className="mb-3 flex items-center gap-2 rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
            {errors.items}
          </div>
        )}

        <div className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-lg border border-gray-200 bg-white shadow-sm">
          <div
            className="shrink-0 grid gap-0 overflow-x-auto border-b border-gray-200 bg-gray-50 px-3 py-2.5 text-xs font-semibold uppercase tracking-wide text-gray-500"
            style={{ gridTemplateColumns: lineGridColumns, minWidth: minGridWidth }}
          >
            <div>Product</div>
            {variantFeatureEnabled && <div>Variant</div>}
            {unitFeatureEnabled && <div>Unit</div>}
            <div>Base Unit</div>
            <div className="text-right">Qty</div>
            <div className="text-right">Base Qty</div>
            {isViewMode ? (
              <>
                <div className="text-right">Cost</div>
                <div className="text-right">Amount</div>
              </>
            ) : (
              <div className="text-center">Act</div>
            )}
          </div>

          <div className="min-h-0 flex-1 divide-y divide-gray-100 overflow-y-auto overflow-x-auto">
            {rows.map((row, idx) => {
              const baseQty =
                row.conversionFactor > 0
                  ? toBaseQuantity(row.quantity, row.conversionFactor)
                  : row.quantity;
              const rowError = rowErrors[row.key];

              return (
                <div
                  key={row.key}
                  className={`grid items-center gap-0 px-3 py-2 transition-colors ${
                    row.productId > 0 ? 'bg-white' : 'bg-gray-50/60'
                  } ${rowError ? 'ring-1 ring-inset ring-red-200' : ''}`}
                  style={{ gridTemplateColumns: lineGridColumns, minWidth: minGridWidth }}
                >
                  {/* Product */}
                  <div className="pr-2">
                    {isViewMode ? (
                      <div className="min-w-0">
                        <p className="truncate text-sm font-medium text-gray-900">{row.productName}</p>
                        <p className="text-xs text-gray-400">{row.productCode}</p>
                      </div>
                    ) : searchRowKey === row.key ? (
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
                            placeholder="Search product…"
                            className="min-w-0 flex-1 bg-transparent text-sm outline-none"
                            onKeyDown={(e) => e.key === 'Escape' && closeSearch()}
                          />
                          {searchLoading && (
                            <svg className="h-3.5 w-3.5 animate-spin text-blue-500" fill="none" viewBox="0 0 24 24">
                              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
                            </svg>
                          )}
                          <button type="button" onClick={closeSearch} className="text-xs text-gray-500 hover:text-gray-700">
                            Cancel
                          </button>
                        </div>
                        {searchResults.length > 0 && (
                          <div className="absolute left-0 top-full z-[60] mt-1 max-h-56 w-80 overflow-auto rounded-lg border border-gray-200 bg-white shadow-xl">
                            {searchResults.map((product) => (
                              <button
                                key={product.id}
                                type="button"
                                onClick={() => void selectProduct(product)}
                                className="flex w-full items-center justify-between px-3 py-2.5 text-left text-sm hover:bg-blue-50"
                              >
                                <div>
                                  <p className="font-medium text-gray-900">{product.productName}</p>
                                  <p className="text-xs text-gray-400">{product.productCode}</p>
                                </div>
                                {product.isVariantEnabled && (
                                  <span className="rounded-full bg-purple-100 px-1.5 py-0.5 text-xs text-purple-700">Variants</span>
                                )}
                              </button>
                            ))}
                          </div>
                        )}
                      </div>
                    ) : row.productId > 0 ? (
                      <div className="min-w-0">
                        <p className="truncate text-sm font-medium text-gray-900">{row.productName}</p>
                        <p className="text-xs text-gray-400">{row.productCode}</p>
                        <button
                          type="button"
                          onClick={() => openSearch(row.key)}
                          className="text-xs font-medium text-blue-600 hover:underline"
                        >
                          Change
                        </button>
                      </div>
                    ) : (
                      <button
                        type="button"
                        onClick={() => openSearch(row.key)}
                        className="flex w-full items-center gap-1.5 rounded-md border border-dashed border-gray-300 px-2 py-1.5 text-xs text-gray-500 hover:border-blue-400 hover:bg-blue-50 hover:text-blue-600"
                      >
                        Row {idx + 1} — Select product
                      </button>
                    )}
                    {row.metaLoading && <p className="mt-0.5 text-xs text-gray-400">Loading units…</p>}
                    {rowError && !isViewMode && <p className="mt-0.5 text-xs text-red-600">{rowError}</p>}
                  </div>

                  {/* Variant */}
                  {variantFeatureEnabled && (
                    <div className="px-1">
                      {isViewMode ? (
                        <span className="text-sm text-gray-700">
                          {row.variants.find((v) => v.id === row.variantId)?.variantName || '—'}
                        </span>
                      ) : row.isVariantEnabled && row.variants.length > 0 ? (
                        <select
                          ref={(el) => {
                            if (el) variantSelectRefs.current.set(row.key, el);
                            else variantSelectRefs.current.delete(row.key);
                          }}
                          value={row.variantId ?? ''}
                          onChange={(e) => {
                            updateRow(row.key, { variantId: e.target.value ? Number(e.target.value) : null });
                            window.setTimeout(() => {
                              if (unitFeatureEnabled) unitSelectRefs.current.get(row.key)?.focus();
                              else qtyInputRefs.current.get(row.key)?.focus();
                            }, 50);
                          }}
                          disabled={row.productId <= 0 || row.metaLoading}
                          className="w-full rounded-md border border-gray-300 bg-white px-2 py-1.5 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 disabled:bg-gray-50 disabled:text-gray-400"
                        >
                          <option value="">Select</option>
                          {row.variants.map((v) => (
                            <option key={v.id} value={v.id}>
                              {v.variantName}
                            </option>
                          ))}
                        </select>
                      ) : row.metaLoading ? (
                        <span className="text-xs italic text-amber-600">…</span>
                      ) : (
                        <span className="text-gray-300">—</span>
                      )}
                    </div>
                  )}

                  {/* Unit */}
                  {unitFeatureEnabled && (
                    <div className="px-1">
                      {isViewMode ? (
                        <span className="text-sm text-gray-700">{row.unitName || '—'}</span>
                      ) : row.units.length > 0 ? (
                        <select
                          ref={(el) => {
                            if (el) unitSelectRefs.current.set(row.key, el);
                            else unitSelectRefs.current.delete(row.key);
                          }}
                          value={row.unitId || ''}
                          onChange={(e) => {
                            updateRow(row.key, { unitId: Number(e.target.value) });
                            window.setTimeout(() => qtyInputRefs.current.get(row.key)?.focus(), 50);
                          }}
                          disabled={row.productId <= 0 || row.metaLoading}
                          className="w-full rounded-md border border-gray-300 bg-white px-2 py-1.5 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 disabled:bg-gray-50 disabled:text-gray-400"
                        >
                          {row.units.map((u) => (
                            <option key={u.id} value={u.id}>
                              {unitOptionLabel(u, row.baseUnitName)}
                            </option>
                          ))}
                        </select>
                      ) : (
                        <span className="text-sm text-gray-400">{row.unitName || '—'}</span>
                      )}
                    </div>
                  )}

                  {/* Base unit badge */}
                  <div className="px-1">
                    {row.baseUnitName ? (
                      <span className="inline-flex rounded-full bg-slate-100 px-2 py-0.5 text-xs font-semibold text-slate-700">
                        {row.baseUnitName}
                      </span>
                    ) : (
                      <span className="text-gray-300">—</span>
                    )}
                  </div>

                  {/* Qty in selected unit */}
                  <div className="px-1">
                    {isViewMode ? (
                      <p className="text-right text-sm">{formatQty(row.quantity)}</p>
                    ) : (
                      <input
                        ref={(el) => {
                          if (el) qtyInputRefs.current.set(row.key, el);
                          else qtyInputRefs.current.delete(row.key);
                        }}
                        type="number"
                        min={0.0001}
                        step="any"
                        value={row.quantity || ''}
                        placeholder="Qty"
                        disabled={row.productId <= 0}
                        onChange={(e) => updateRow(row.key, { quantity: parseFloat(e.target.value) || 0 })}
                        className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-right text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 disabled:bg-gray-50 disabled:text-gray-400"
                      />
                    )}
                  </div>

                  {/* Converted base qty */}
                  <div className="px-1 text-right">
                    <span
                      className={`inline-block min-w-[3rem] rounded-md px-2 py-1 text-sm font-semibold ${
                        baseQty > 0 ? 'bg-emerald-50 text-emerald-800' : 'text-gray-300'
                      }`}
                      title={row.baseUnitName ? `${formatQty(baseQty)} ${row.baseUnitName}` : undefined}
                    >
                      {row.productId > 0 && baseQty > 0 ? formatQty(baseQty) : '—'}
                    </span>
                  </div>

                  {isViewMode ? (
                    <>
                      <div className="px-1 text-right text-sm text-gray-700">
                        {symbol}
                        {row.costPrice.toFixed(2)}
                      </div>
                      <div className="px-1 text-right text-sm font-semibold">
                        {symbol}
                        {row.totalAmount.toFixed(2)}
                      </div>
                    </>
                  ) : (
                    <div className="flex justify-center px-1">
                      {row.productId > 0 || rows.length > 1 ? (
                        <button
                          type="button"
                          onClick={() => removeRow(row.key)}
                          className="rounded p-1 text-gray-400 hover:bg-red-50 hover:text-red-500"
                          title="Remove row"
                        >
                          <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                          </svg>
                        </button>
                      ) : (
                        <span className="text-gray-200">—</span>
                      )}
                    </div>
                  )}
                </div>
              );
            })}
          </div>

          <div className="flex shrink-0 items-center justify-between rounded-b-lg border-t border-gray-200 bg-gray-50 px-4 py-3">
            <span className="text-sm font-medium text-gray-500">
              {filledCount} of {rows.length} {rows.length === 1 ? 'row' : 'rows'} filled
            </span>
            <div className="flex items-center gap-3">
              <span className="text-sm font-medium text-gray-600">Estimated Total</span>
              <span className="min-w-[100px] rounded-md bg-blue-600 px-4 py-1.5 text-right text-sm font-bold text-white">
                {symbol}
                {grandTotal.toFixed(2)}
              </span>
            </div>
          </div>
        </div>
      </div>

      {!isViewMode && (
        <div className="flex shrink-0 items-center justify-end border-t border-gray-200 bg-white px-6 py-4">
          <FormButton
            type="submit"
            label={isEditMode ? 'Update Voucher' : 'Save Voucher'}
            variant="primary"
            loading={isLoading}
          />
        </div>
      )}
    </form>
  );
};

export default OpeningStockForm;
