import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import apiClient, { getApiErrorMessage } from '../../services/api';
import { useAuth } from '../../contexts/AuthContext';
import { useBranchWriteAccess } from '../../hooks/useBranchWriteAccess';
import { useBranchStore } from '../../stores/useBranchStore';
import { salesService } from './salesService';
import type { SaleInvoiceDto, SaleInvoiceItemResult } from '../pos/posService';

// ─── types ────────────────────────────────────────────────────────────────────

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
}

interface UnitOption {
  id: number;
  unitName: string;
  conversionFactor: number;
  isBaseUnit: boolean;
}

interface ItemRow {
  key: string;
  productId: number;
  productName: string;
  productCode: string;
  variantId: number | null;
  variantName: string;
  unitId: number;
  unitName: string;
  conversionFactor: number;
  quantity: number;
  unitPrice: number;
  discountPercent: number;
  discountAmount: number;
  taxPercent: number;
  variants: VariantOption[];
  units: UnitOption[];
  isVariantEnabled: boolean;
}

// ─── helpers ──────────────────────────────────────────────────────────────────

const fmt = (n: number) =>
  new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(n);

const emptyRow = (): ItemRow => ({
  key: crypto.randomUUID(),
  productId: 0,
  productName: '',
  productCode: '',
  variantId: null,
  variantName: '',
  unitId: 0,
  unitName: '',
  conversionFactor: 1,
  quantity: 1,
  unitPrice: 0,
  discountPercent: 0,
  discountAmount: 0,
  taxPercent: 0,
  variants: [],
  units: [],
  isVariantEnabled: false,
});

const rowFromItem = (item: SaleInvoiceItemResult): ItemRow => ({
  key: crypto.randomUUID(),
  productId: item.productId,
  productName: item.productName,
  productCode: item.productCode,
  variantId: item.variantId,
  variantName: item.variantName ?? '',
  unitId: item.unitId,
  unitName: item.unitName,
  conversionFactor: item.conversionFactor,
  quantity: item.quantity,
  unitPrice: item.unitPrice,
  discountPercent: item.discountPercent,
  discountAmount: item.discountAmount,
  taxPercent: item.taxPercent,
  variants: [],
  units: [],
  isVariantEnabled: !!item.variantId,
});

const inputCls =
  'w-full rounded-md border border-gray-300 bg-white px-2 py-1.5 text-sm text-gray-900 focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500';

// ─── Component ────────────────────────────────────────────────────────────────

const EditInvoicePage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const location = useLocation();
  const { user } = useAuth();
  const { resolveEntityBranchId } = useBranchWriteAccess();
  const selectedBranchId = useBranchStore((s) => s.selectedBranchId);

  // branchId passed via navigation state from Invoice History page
  const navBranchId: number =
    (location.state as { branchId?: number } | null)?.branchId ??
    selectedBranchId ??
    1;

  // ── invoice header ──
  const [invoice, setInvoice]         = useState<SaleInvoiceDto | null>(null);
  const [pageLoading, setPageLoading] = useState(true);
  const [pageError, setPageError]     = useState('');

  // ── editable header ──
  const [billDiscount, setBillDiscount]   = useState(0);
  const [paidAmount, setPaidAmount]       = useState(0);
  const [paymentMethod, setPaymentMethod] = useState<'Cash' | 'Card' | 'Mixed'>('Cash');
  const [notes, setNotes]                 = useState('');

  // ── line items ──
  const [rows, setRows] = useState<ItemRow[]>([]);

  // ── product search ──
  const [searchRowKey, setSearchRowKey]   = useState<string | null>(null);
  const [searchTerm, setSearchTerm]       = useState('');
  const [searchResults, setSearchResults] = useState<ProductOption[]>([]);
  const [searchLoading, setSearchLoading] = useState(false);
  const searchDropdownRef = useRef<HTMLDivElement>(null);
  const searchInputRef    = useRef<HTMLInputElement>(null);
  const searchDebounce    = useRef<ReturnType<typeof setTimeout> | null>(null);

  // ── save ──
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState('');

  // ── computed totals ──
  const subTotal  = rows.reduce((s, r) => s + r.quantity * r.unitPrice, 0);
  const lineDisc  = rows.reduce((s, r) => {
    const gross = r.quantity * r.unitPrice;
    return s + (r.discountAmount > 0 ? r.discountAmount : gross * r.discountPercent / 100);
  }, 0);
  const totalDisc = lineDisc + billDiscount;
  const totalTax  = rows.reduce((s, r) => {
    const gross = r.quantity * r.unitPrice;
    const disc  = r.discountAmount > 0 ? r.discountAmount : gross * r.discountPercent / 100;
    return s + (r.taxPercent > 0 ? (gross - disc) * r.taxPercent / 100 : 0);
  }, 0);
  const grandTotal  = subTotal - totalDisc + totalTax;
  const changeAmt   = paidAmount > grandTotal ? paidAmount - grandTotal : 0;
  const validRows   = rows.filter((r) => r.productId > 0 && r.unitId > 0);

  // ── load invoice ──────────────────────────────────────────────────────────
  useEffect(() => {
    if (!id) { setPageError('Invalid invoice ID.'); setPageLoading(false); return; }
    const invoiceId = parseInt(id, 10);
    if (isNaN(invoiceId)) { setPageError('Invalid invoice ID.'); setPageLoading(false); return; }

    void (async () => {
      try {
        const res = await salesService.getById(invoiceId, navBranchId);
        const inv = res.data;
        setInvoice(inv);
        setRows(inv.items.map(rowFromItem));
        // derive bill-level discount (total minus sum of line discounts)
        const lineDiscSum = inv.items.reduce((s, i) => s + i.discountAmount, 0);
        setBillDiscount(Math.max(0, inv.discountAmount - lineDiscSum));
        setPaidAmount(inv.paidAmount);
        setPaymentMethod(inv.paymentMethod);
        setNotes(inv.notes ?? '');
      } catch (err) {
        setPageError(getApiErrorMessage(err, 'Failed to load invoice.'));
      } finally {
        setPageLoading(false);
      }
    })();
  }, [id]);

  // ── product search ────────────────────────────────────────────────────────
  const openSearch = (rowKey: string) => {
    setSearchRowKey(rowKey);
    setSearchTerm('');
    setSearchResults([]);
    setTimeout(() => searchInputRef.current?.focus(), 50);
  };

  const closeSearch = () => {
    setSearchRowKey(null);
    setSearchTerm('');
    setSearchResults([]);
  };

  useEffect(() => {
    if (searchDebounce.current) clearTimeout(searchDebounce.current);
    if (!searchTerm || searchTerm.length < 2 || !invoice) {
      setSearchResults([]);
      return;
    }
    searchDebounce.current = setTimeout(() => {
      setSearchLoading(true);
      const branchId = resolveEntityBranchId(invoice.branchId);
      apiClient
        .get<ProductOption[]>(`/products/search`, { params: { term: searchTerm, branchId } })
        .then((r) => setSearchResults(r.data))
        .catch(() => setSearchResults([]))
        .finally(() => setSearchLoading(false));
    }, 300);
    return () => { if (searchDebounce.current) clearTimeout(searchDebounce.current); };
  }, [searchTerm, invoice, resolveEntityBranchId]);

  // Close search on outside click
  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (searchDropdownRef.current && !searchDropdownRef.current.contains(e.target as Node)) {
        closeSearch();
      }
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, []);

  const selectProduct = useCallback(async (product: ProductOption) => {
    if (!searchRowKey || !invoice) return;
    closeSearch();

    const branchId = resolveEntityBranchId(invoice.branchId);

    const [unitsRes, variantsRes] = await Promise.allSettled([
      apiClient.get<UnitOption[]>(`/products/${product.id}/units`, { params: { branchId } }),
      product.isVariantEnabled
        ? apiClient.get<VariantOption[]>(`/products/${product.id}/variants`, { params: { branchId } })
        : Promise.resolve({ data: [] as VariantOption[] }),
    ]);

    const units    = unitsRes.status === 'fulfilled' ? unitsRes.value.data : [];
    const variants = variantsRes.status === 'fulfilled' ? variantsRes.value.data : [];
    const baseUnit = units.find((u) => u.isBaseUnit) ?? units[0];

    setRows((prev) =>
      prev.map((r) =>
        r.key === searchRowKey
          ? {
              ...r,
              productId: product.id,
              productName: product.productName,
              productCode: product.productCode,
              variantId: null,
              variantName: '',
              unitId: baseUnit?.id ?? 0,
              unitName: baseUnit?.unitName ?? '',
              conversionFactor: baseUnit?.conversionFactor ?? 1,
              variants,
              units,
              isVariantEnabled: product.isVariantEnabled,
            }
          : r
      )
    );
  }, [searchRowKey, invoice, resolveEntityBranchId]);

  const updateRow = (key: string, patch: Partial<ItemRow>) => {
    setRows((prev) => prev.map((r) => (r.key === key ? { ...r, ...patch } : r)));
  };

  const removeRow = (key: string) => {
    setRows((prev) => prev.filter((r) => r.key !== key));
  };

  // ── save ──────────────────────────────────────────────────────────────────
  const handleSave = async () => {
    if (!invoice) return;
    if (validRows.length === 0) { setSaveError('At least one item is required.'); return; }

    setSaving(true);
    setSaveError('');

    const branchId   = resolveEntityBranchId(invoice.branchId);
    const businessId = (user as { businessId?: number })?.businessId ?? 1;

    const cash = paymentMethod === 'Cash' ? paidAmount : paymentMethod === 'Card' ? 0 : paidAmount / 2;
    const card = paymentMethod === 'Card' ? paidAmount : paymentMethod === 'Cash' ? 0 : paidAmount / 2;

    try {
      await salesService.updateInvoice(invoice.id, {
        customerId:    invoice.customerId,
        warehouseId:   invoice.warehouseId,
        pricingType:   invoice.pricingType,
        paymentMethod,
        paidAmount,
        cashAmount:    cash,
        cardAmount:    card,
        discountAmount: billDiscount,
        notes:         notes.trim() || undefined,
        cashierName:   (user as { fullName?: string; username?: string })?.fullName
                         ?? (user as { fullName?: string; username?: string })?.username,
        businessId,
        branchId,
        items: validRows.map((r) => ({
          productId:        r.productId,
          variantId:        r.variantId,
          unitId:           r.unitId,
          quantity:         r.quantity,
          conversionFactor: r.conversionFactor,
          unitPrice:        r.unitPrice,
          discountPercent:  r.discountPercent,
          discountAmount:   r.discountAmount,
          taxPercent:       r.taxPercent,
          itemNote:         null,
        })),
      });
      navigate('/sales-invoices', { replace: true, state: { success: `Invoice ${invoice.invoiceNo} updated. Stock recalculated.` } });
    } catch (err) {
      setSaveError(getApiErrorMessage(err, 'Failed to save. Please try again.'));
    } finally {
      setSaving(false);
    }
  };

  // ── loading / error states ────────────────────────────────────────────────
  if (pageLoading) {
    return (
      <div className="flex h-full items-center justify-center">
        <svg className="h-8 w-8 animate-spin text-blue-500" fill="none" viewBox="0 0 24 24">
          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
          <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" />
        </svg>
      </div>
    );
  }

  if (pageError || !invoice) {
    return (
      <div className="flex h-full flex-col items-center justify-center gap-4 text-center">
        <p className="text-red-600">{pageError || 'Invoice not found.'}</p>
        <button
          onClick={() => navigate('/sales-invoices')}
          className="rounded-lg bg-blue-600 px-5 py-2 text-sm font-semibold text-white hover:bg-blue-700"
        >
          Back to Invoice History
        </button>
      </div>
    );
  }

  // ── render ────────────────────────────────────────────────────────────────
  return (
    <div className="flex h-full min-h-0 flex-col bg-gray-50">

      {/* ── Top bar ── */}
      <div className="flex items-center gap-3 border-b border-gray-200 bg-white px-6 py-4 shadow-sm">
        <button
          onClick={() => navigate('/sales-invoices')}
          className="flex items-center gap-1.5 rounded-lg border border-gray-200 bg-white px-3 py-1.5 text-sm text-gray-600 transition hover:bg-gray-50"
        >
          <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
          </svg>
          Back
        </button>
        <div className="flex-1">
          <h1 className="text-lg font-bold text-gray-900">Edit Invoice</h1>
          <p className="text-xs text-gray-500">
            {invoice.invoiceNo} · {invoice.warehouseName} · {invoice.branchName}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => navigate('/sales-invoices')}
            className="rounded-lg border border-gray-200 px-4 py-2 text-sm font-semibold text-gray-600 hover:bg-gray-50 transition"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={() => void handleSave()}
            disabled={saving || validRows.length === 0}
            className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-5 py-2 text-sm font-bold text-white hover:bg-blue-700 disabled:opacity-50 transition"
          >
            {saving && (
              <svg className="h-4 w-4 animate-spin" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" />
              </svg>
            )}
            {saving ? 'Saving…' : 'Save Changes'}
          </button>
        </div>
      </div>

      {/* ── Body ── */}
      <div className="flex-1 min-h-0 overflow-y-auto px-6 py-5 space-y-5">

        {/* Warning */}
        <div className="flex items-start gap-2 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
          <svg className="mt-0.5 h-4 w-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" />
          </svg>
          <span>
            Saving will <strong>reverse</strong> existing stock entries and create new ones based on updated quantities.
            Invoice status will remain <strong>Completed</strong>.
          </span>
        </div>

        {saveError && (
          <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            {saveError}
          </div>
        )}

        {/* ── Invoice Info (read-only) ── */}
        <div className="rounded-xl border border-gray-200 bg-white px-6 py-4 shadow-sm">
          <h2 className="mb-4 text-sm font-semibold text-gray-700 uppercase tracking-wide">Invoice Details</h2>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
            <div>
              <p className="text-xs text-gray-400 mb-1">Invoice No</p>
              <p className="font-semibold text-gray-800">{invoice.invoiceNo}</p>
            </div>
            <div>
              <p className="text-xs text-gray-400 mb-1">Sale Date</p>
              <p className="font-semibold text-gray-800">{new Date(invoice.saleDate).toLocaleDateString()}</p>
            </div>
            <div>
              <p className="text-xs text-gray-400 mb-1">Warehouse</p>
              <p className="font-semibold text-gray-800">{invoice.warehouseName}</p>
            </div>
            <div>
              <p className="text-xs text-gray-400 mb-1">Customer</p>
              <p className="font-semibold text-gray-800">{invoice.customerName ?? '— Walk-in —'}</p>
            </div>
          </div>
        </div>

        {/* ── Line Items ── */}
        <div className="rounded-xl border border-gray-200 bg-white shadow-sm">
          <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
            <div>
              <h2 className="text-sm font-semibold text-gray-700 uppercase tracking-wide">Line Items</h2>
              <p className="mt-0.5 text-xs text-gray-400">
                {validRows.length} {validRows.length === 1 ? 'product' : 'products'}
              </p>
            </div>
            <button
              type="button"
              onClick={() => setRows((prev) => [...prev, emptyRow()])}
              className="inline-flex items-center gap-1.5 rounded-lg border border-blue-200 bg-blue-50 px-3 py-1.5 text-xs font-semibold text-blue-700 hover:bg-blue-100 transition"
            >
              <svg className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M12 4v16m8-8H4" />
              </svg>
              Add Row
            </button>
          </div>

          {/* Table header */}
          <div className="grid grid-cols-[2.5fr_1.2fr_80px_90px_80px_80px_85px_32px] gap-0 border-b border-gray-100 bg-gray-50 px-4 py-2.5 text-xs font-semibold uppercase tracking-wide text-gray-500">
            <div>Product</div>
            <div>Unit</div>
            <div className="text-right">Qty</div>
            <div className="text-right">Unit Price</div>
            <div className="text-right">Disc %</div>
            <div className="text-right">Disc Amt</div>
            <div className="text-right">Total</div>
            <div />
          </div>

          {/* Rows */}
          <div className="divide-y divide-gray-100">
            {rows.map((row, idx) => {
              const gross    = row.quantity * row.unitPrice;
              const disc     = row.discountAmount > 0 ? row.discountAmount : gross * row.discountPercent / 100;
              const lineTotal = gross - disc + (row.taxPercent > 0 ? (gross - disc) * row.taxPercent / 100 : 0);
              const isCurrent = searchRowKey === row.key;

              return (
                <div
                  key={row.key}
                  className="grid grid-cols-[2.5fr_1.2fr_80px_90px_80px_80px_85px_32px] items-center gap-0 px-4 py-2 hover:bg-gray-50/60 transition-colors"
                >
                  {/* Product */}
                  <div className="pr-2">
                    {row.productId > 0 ? (
                      <div className="flex items-start gap-1.5">
                        <div className="min-w-0 flex-1">
                          <p className="truncate text-sm font-medium text-gray-900">{row.productName}</p>
                          {row.variantName && (
                            <p className="text-xs text-gray-400">{row.variantName}</p>
                          )}
                        </div>
                        <button
                          type="button"
                          onClick={() => openSearch(row.key)}
                          title="Change product"
                          className="mt-0.5 shrink-0 rounded p-0.5 text-gray-400 hover:bg-gray-100 hover:text-gray-600"
                        >
                          <svg className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
                          </svg>
                        </button>
                      </div>
                    ) : isCurrent ? (
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
                            className="min-w-0 flex-1 bg-transparent text-sm text-gray-900 placeholder-gray-400 outline-none"
                            onKeyDown={(e) => e.key === 'Escape' && closeSearch()}
                          />
                          {searchLoading && (
                            <svg className="h-3.5 w-3.5 animate-spin text-blue-500" fill="none" viewBox="0 0 24 24">
                              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" />
                            </svg>
                          )}
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
                                {p.isVariantEnabled && (
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
                    ) : (
                      <button
                        type="button"
                        onClick={() => openSearch(row.key)}
                        className="flex items-center gap-1.5 rounded-md border border-dashed border-gray-300 px-2 py-1.5 text-xs text-gray-400 transition-colors hover:border-blue-400 hover:bg-blue-50 hover:text-blue-600"
                      >
                        <svg className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                        </svg>
                        Row {idx + 1} — Select product
                      </button>
                    )}
                  </div>

                  {/* Unit */}
                  <div className="px-1">
                    {row.units.length > 0 ? (
                      <select
                        value={row.unitId}
                        onChange={(e) => {
                          const u = row.units.find((x) => x.id === Number(e.target.value));
                          updateRow(row.key, {
                            unitId: Number(e.target.value),
                            unitName: u?.unitName ?? '',
                            conversionFactor: u?.conversionFactor ?? 1,
                          });
                        }}
                        className={inputCls}
                      >
                        {row.units.map((u) => (
                          <option key={u.id} value={u.id}>{u.unitName}</option>
                        ))}
                      </select>
                    ) : (
                      <span className="text-sm text-gray-500">{row.unitName || '—'}</span>
                    )}
                  </div>

                  {/* Qty */}
                  <div className="px-1">
                    <input
                      type="number" min="0.0001" step="any"
                      value={row.quantity}
                      onChange={(e) => updateRow(row.key, { quantity: parseFloat(e.target.value) || 0 })}
                      className={`${inputCls} text-right`}
                    />
                  </div>

                  {/* Unit Price */}
                  <div className="px-1">
                    <input
                      type="number" min="0" step="0.01"
                      value={row.unitPrice}
                      onChange={(e) => updateRow(row.key, { unitPrice: parseFloat(e.target.value) || 0 })}
                      className={`${inputCls} text-right`}
                    />
                  </div>

                  {/* Disc % */}
                  <div className="px-1">
                    <input
                      type="number" min="0" max="100" step="0.01"
                      value={row.discountPercent}
                      onChange={(e) => updateRow(row.key, { discountPercent: parseFloat(e.target.value) || 0, discountAmount: 0 })}
                      className={`${inputCls} text-right`}
                    />
                  </div>

                  {/* Disc Amt */}
                  <div className="px-1">
                    <input
                      type="number" min="0" step="0.01"
                      value={row.discountAmount}
                      onChange={(e) => updateRow(row.key, { discountAmount: parseFloat(e.target.value) || 0, discountPercent: 0 })}
                      className={`${inputCls} text-right`}
                    />
                  </div>

                  {/* Line Total */}
                  <div className="px-1 text-right">
                    <span className="text-sm font-semibold text-gray-800">{fmt(lineTotal)}</span>
                    {row.taxPercent > 0 && (
                      <p className="text-xs text-gray-400">Tax {row.taxPercent}%</p>
                    )}
                  </div>

                  {/* Remove */}
                  <div className="flex justify-center">
                    <button
                      type="button"
                      onClick={() => removeRow(row.key)}
                      className="flex h-6 w-6 items-center justify-center rounded text-gray-400 hover:bg-red-50 hover:text-red-500 transition"
                      title="Remove row"
                    >
                      <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                      </svg>
                    </button>
                  </div>
                </div>
              );
            })}

            {rows.length === 0 && (
              <div className="px-4 py-8 text-center text-sm text-gray-400">
                No items. Click <strong>Add Row</strong> to add a product.
              </div>
            )}
          </div>
        </div>

        {/* ── Payment + Totals ── */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-5">

          {/* Payment */}
          <div className="rounded-xl border border-gray-200 bg-white px-6 py-4 shadow-sm space-y-4">
            <h2 className="text-sm font-semibold text-gray-700 uppercase tracking-wide">Payment</h2>

            <div>
              <label className="mb-1.5 block text-sm font-medium text-gray-700">Payment Method</label>
              <select
                value={paymentMethod}
                onChange={(e) => setPaymentMethod(e.target.value as 'Cash' | 'Card' | 'Mixed')}
                className={inputCls}
              >
                <option value="Cash">Cash</option>
                <option value="Card">Card</option>
                <option value="Mixed">Mixed (Cash + Card)</option>
              </select>
            </div>

            <div>
              <label className="mb-1.5 block text-sm font-medium text-gray-700">Bill Discount</label>
              <input
                type="number" min="0" step="0.01"
                value={billDiscount}
                onChange={(e) => setBillDiscount(parseFloat(e.target.value) || 0)}
                className={inputCls}
              />
            </div>

            <div>
              <label className="mb-1.5 block text-sm font-medium text-gray-700">Paid Amount</label>
              <input
                type="number" min="0" step="0.01"
                value={paidAmount}
                onChange={(e) => setPaidAmount(parseFloat(e.target.value) || 0)}
                className={inputCls}
              />
            </div>

            <div>
              <label className="mb-1.5 block text-sm font-medium text-gray-700">Notes</label>
              <textarea
                rows={2}
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                placeholder="Optional notes…"
                className={`${inputCls} resize-none`}
              />
            </div>
          </div>

          {/* Totals */}
          <div className="rounded-xl border border-gray-200 bg-white px-6 py-4 shadow-sm flex flex-col">
            <h2 className="mb-4 text-sm font-semibold text-gray-700 uppercase tracking-wide">Summary</h2>
            <div className="flex-1 space-y-2 text-sm">
              <div className="flex justify-between text-gray-600">
                <span>Subtotal</span>
                <span>{fmt(subTotal)}</span>
              </div>
              {totalDisc > 0 && (
                <div className="flex justify-between text-red-500">
                  <span>Discount</span>
                  <span>−{fmt(totalDisc)}</span>
                </div>
              )}
              {totalTax > 0 && (
                <div className="flex justify-between text-gray-600">
                  <span>Tax</span>
                  <span>+{fmt(totalTax)}</span>
                </div>
              )}
              <div className="flex justify-between border-t border-gray-200 pt-2 font-bold text-gray-900 text-base">
                <span>Grand Total</span>
                <span>{fmt(grandTotal)}</span>
              </div>
              <div className="flex justify-between text-gray-600">
                <span>Paid</span>
                <span>{fmt(paidAmount)}</span>
              </div>
              {changeAmt > 0 && (
                <div className="flex justify-between font-semibold text-green-600">
                  <span>Change</span>
                  <span>{fmt(changeAmt)}</span>
                </div>
              )}
              {paidAmount < grandTotal && (
                <div className="flex justify-between font-semibold text-red-500">
                  <span>Balance Due</span>
                  <span>{fmt(grandTotal - paidAmount)}</span>
                </div>
              )}
            </div>

            <div className="mt-6 pt-4 border-t border-gray-100">
              <button
                type="button"
                onClick={() => void handleSave()}
                disabled={saving || validRows.length === 0}
                className="w-full rounded-xl bg-blue-600 py-3 text-sm font-bold text-white hover:bg-blue-700 disabled:opacity-50 transition inline-flex items-center justify-center gap-2"
              >
                {saving && (
                  <svg className="h-4 w-4 animate-spin" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" />
                  </svg>
                )}
                {saving ? 'Saving…' : 'Save Changes'}
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default EditInvoicePage;
