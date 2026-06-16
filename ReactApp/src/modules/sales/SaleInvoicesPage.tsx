import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import DataTable, { type Action, type Column } from '../../components/DataTable';
import Badge from '../../components/Badge';
import { useConfirmDialog } from '../../contexts/ConfirmDialogContext';
import { usePermission } from '../../hooks/usePermission';
import { useBranchWriteAccess } from '../../hooks/useBranchWriteAccess';
import { hasBranchContext } from '../../types/permissions';
import { getApiErrorMessage } from '../../services/api';
import { safeString } from '../../utils/safeValues';
import { useAuth } from '../../contexts/AuthContext';
import {
  salesService,
  type SaleInvoiceListDto,
  type SaleInvoiceStatus,
} from './salesService';
import type { SaleInvoiceDto, SaleLedgerEntry } from '../pos/posService';

// ─── helpers ─────────────────────────────────────────────────────────────────

const fmt = (n: number) =>
  n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const fmtDate = (v: string) => {
  if (!v) return '-';
  const d = new Date(v);
  return Number.isNaN(d.getTime()) ? '-' : d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
};

const fmtDateTime = (v: string | null | undefined) => {
  if (!v) return '-';
  const d = new Date(v);
  return Number.isNaN(d.getTime()) ? '-' : d.toLocaleString();
};

const statusVariant = (s: SaleInvoiceStatus) => {
  if (s === 'Completed') return 'success' as const;
  if (s === 'Voided' || s === 'Cancelled') return 'danger' as const;
  if (s === 'Held') return 'warning' as const;
  return 'secondary' as const;
};

const ledgerTypeVariant = (type: string) => {
  if (type === 'SaleEntry') return 'danger' as const;
  if (type === 'SaleReversal') return 'success' as const;
  return 'secondary' as const;
};

// ─── Receipt Modal ────────────────────────────────────────────────────────────

interface ReceiptModalProps {
  invoice: SaleInvoiceDto;
  onClose: () => void;
}

const ReceiptModal: React.FC<ReceiptModalProps> = ({ invoice, onClose }) => {
  const isVoided = invoice.status === 'Voided';
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-sm mx-4 overflow-hidden border border-gray-200 max-h-[90vh] flex flex-col">
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-lg font-bold text-gray-800">Receipt</h2>
              {isVoided && (
                <span className="text-xs font-bold text-red-600 bg-red-50 border border-red-200 px-2 py-0.5 rounded-full">
                  VOIDED
                </span>
              )}
            </div>
            <p className="text-xs text-gray-500">{invoice.invoiceNo}</p>
          </div>
          <button onClick={onClose} className="w-8 h-8 flex items-center justify-center rounded-lg hover:bg-gray-100 text-gray-500 text-xl transition">×</button>
        </div>

        <div className="overflow-y-auto flex-1 p-5 space-y-4 text-sm">
          <div className="text-center pb-3 border-b border-dashed border-gray-300">
            <p className="text-xs text-gray-500">{fmtDateTime(invoice.saleDate)}</p>
            {invoice.customerName && (
              <p className="text-gray-600 text-xs mt-1">Customer: <span className="font-medium">{invoice.customerName}</span></p>
            )}
            {invoice.warehouseName && (
              <p className="text-gray-600 text-xs">Warehouse: <span className="font-medium">{invoice.warehouseName}</span></p>
            )}
            {invoice.cashierName && (
              <p className="text-gray-600 text-xs">Cashier: <span className="font-medium">{invoice.cashierName}</span></p>
            )}
            {isVoided && invoice.voidedAt && (
              <p className="text-red-500 text-xs mt-1 font-medium">
                Voided: {fmtDateTime(invoice.voidedAt)}
                {invoice.voidedByName ? ` by ${invoice.voidedByName}` : ''}
              </p>
            )}
          </div>

          <table className="w-full text-xs">
            <tbody className="divide-y divide-gray-100">
              {invoice.items.map((item) => (
                <tr key={item.id} className={isVoided ? 'opacity-50' : ''}>
                  <td className="py-2 pr-2">
                    <p className="font-medium text-gray-800">{item.productName}</p>
                    {item.variantName && <p className="text-gray-400">{item.variantName}</p>}
                    <p className="text-gray-400">{item.unitName}</p>
                  </td>
                  <td className="py-2 text-right text-gray-600 whitespace-nowrap">{item.quantity} × {fmt(item.unitPrice)}</td>
                  <td className="py-2 pl-2 text-right font-semibold text-gray-800 whitespace-nowrap">{fmt(item.lineTotal)}</td>
                </tr>
              ))}
            </tbody>
          </table>

          <div className="border-t border-dashed border-gray-300 pt-3 space-y-1.5">
            <div className="flex justify-between text-gray-600"><span>Subtotal</span><span>{fmt(invoice.subTotal)}</span></div>
            {invoice.discountAmount > 0 && (
              <div className="flex justify-between text-red-500"><span>Discount</span><span>−{fmt(invoice.discountAmount)}</span></div>
            )}
            {invoice.taxAmount > 0 && (
              <div className="flex justify-between text-gray-600"><span>Tax</span><span>+{fmt(invoice.taxAmount)}</span></div>
            )}
            <div className="flex justify-between font-bold text-gray-900 text-base border-t border-gray-200 pt-2 mt-2">
              <span>Total</span><span>{fmt(invoice.grandTotal)}</span>
            </div>
            <div className="flex justify-between text-gray-600">
              <span>Paid ({invoice.paymentMethod})</span><span>{fmt(invoice.paidAmount)}</span>
            </div>
            {invoice.returnAmount > 0 && (
              <div className="flex justify-between font-semibold text-green-600">
                <span>Change</span><span>{fmt(invoice.returnAmount)}</span>
              </div>
            )}
          </div>

          <p className="text-center text-gray-400 text-xs pt-2 border-t border-dashed border-gray-300">
            {isVoided ? 'This invoice has been voided.' : 'Thank you for your purchase!'}
          </p>
        </div>

        <div className="px-5 pb-5 flex gap-3 border-t border-gray-100 pt-4">
          <button onClick={() => window.print()} className="flex-1 py-2.5 rounded-xl border border-gray-200 text-gray-600 font-semibold hover:bg-gray-50 transition text-sm">
            🖨️ Print
          </button>
          <button onClick={onClose} className="flex-1 py-2.5 rounded-xl bg-gray-800 hover:bg-gray-900 text-white font-bold transition text-sm">
            Close
          </button>
        </div>
      </div>
    </div>
  );
};

// ─── Ledger History Modal ─────────────────────────────────────────────────────

interface LedgerModalProps {
  invoiceNo: string;
  entries: SaleLedgerEntry[];
  onClose: () => void;
}

const LedgerModal: React.FC<LedgerModalProps> = ({ invoiceNo, entries, onClose }) => (
  <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm">
    <div className="bg-white rounded-2xl shadow-2xl w-full max-w-2xl mx-4 overflow-hidden border border-gray-200 max-h-[85vh] flex flex-col">
      <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
        <div>
          <h2 className="text-lg font-bold text-gray-800">Stock Ledger History</h2>
          <p className="text-xs text-gray-500">Invoice: {invoiceNo}</p>
        </div>
        <button onClick={onClose} className="w-8 h-8 flex items-center justify-center rounded-lg hover:bg-gray-100 text-gray-500 text-xl transition">×</button>
      </div>

      <div className="overflow-y-auto flex-1 p-5">
        {entries.length === 0 ? (
          <p className="text-center text-gray-400 py-8">No ledger entries found for this invoice.</p>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-200 text-xs text-gray-500 uppercase">
                <th className="text-left py-2 pr-3">Type</th>
                <th className="text-left py-2 pr-3">Product</th>
                <th className="text-left py-2 pr-3">Warehouse</th>
                <th className="text-right py-2 pr-3">Qty</th>
                <th className="text-right py-2 pr-3">Date</th>
                <th className="text-left py-2">Remarks</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {entries.map((e) => (
                <tr key={e.id}>
                  <td className="py-2 pr-3">
                    <Badge variant={ledgerTypeVariant(e.type)} size="sm">{e.type}</Badge>
                  </td>
                  <td className="py-2 pr-3">
                    <p className="font-medium text-gray-800">{e.productName}</p>
                    {e.variantName && <p className="text-xs text-gray-400">{e.variantName}</p>}
                  </td>
                  <td className="py-2 pr-3 text-gray-600">{e.warehouseName}</td>
                  <td className={`py-2 pr-3 text-right font-semibold ${e.quantityInBaseUnit < 0 ? 'text-red-600' : 'text-green-600'}`}>
                    {e.quantityInBaseUnit > 0 ? '+' : ''}{e.quantityInBaseUnit}
                  </td>
                  <td className="py-2 pr-3 text-right text-gray-500 text-xs whitespace-nowrap">
                    {fmtDateTime(e.date)}
                  </td>
                  <td className="py-2 text-gray-500 text-xs">{e.remarks}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      <div className="px-5 pb-4 pt-3 border-t border-gray-100">
        <button onClick={onClose} className="w-full py-2.5 rounded-xl bg-gray-800 hover:bg-gray-900 text-white font-bold transition text-sm">
          Close
        </button>
      </div>
    </div>
  </div>
);

// ─── Main Page ────────────────────────────────────────────────────────────────

// EditInvoice is now a dedicated full page at /sales-invoices/edit/:id

const SaleInvoicesPage: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const { showConfirm } = useConfirmDialog();
  const { canEdit } = usePermission('Sales');
  const { selectedBranchId, isGlobalAdmin, canWriteInView, resolveEntityBranchId } = useBranchWriteAccess();
  const hasBranchSelection = hasBranchContext(selectedBranchId);
  const canModify = canWriteInView && (isGlobalAdmin || canEdit);

  const [items, setItems]               = useState<SaleInvoiceListDto[]>([]);
  const [loading, setLoading]           = useState(false);
  const [currentPage, setCurrentPage]   = useState(1);
  const [pageSize, setPageSize]         = useState(25);
  const [searchTerm, setSearchTerm]     = useState('');
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages]     = useState(0);
  const [statusFilter, setStatusFilter] = useState<SaleInvoiceStatus | null>(null);
  const [dateFrom, setDateFrom]         = useState('');
  const [dateTo, setDateTo]             = useState('');

  const [notification, setNotification] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  // modals
  const [receiptInvoice, setReceiptInvoice] = useState<SaleInvoiceDto | null>(null);
  const [ledgerEntries, setLedgerEntries]   = useState<SaleLedgerEntry[] | null>(null);
  const [ledgerInvoiceNo, setLedgerInvoiceNo] = useState('');
  const [loadingDetail, setLoadingDetail]   = useState<number | null>(null);
  const [loadingLedger, setLoadingLedger]   = useState<number | null>(null);
  const [voidingId, setVoidingId]           = useState<number | null>(null);

  const showNotification = useCallback((type: 'success' | 'error', message: string) => {
    setNotification({ type, message });
    setTimeout(() => setNotification(null), 5000);
  }, []);

  // Show success toast when returning from EditInvoicePage
  useEffect(() => {
    const state = location.state as { success?: string } | null;
    if (state?.success) {
      showNotification('success', state.success);
      window.history.replaceState({}, '');
    }
  }, [location.state, showNotification]);

  const fetchInvoices = useCallback(async () => {
    if (!hasBranchSelection || selectedBranchId === null) {
      setItems([]); setTotalRecords(0); setTotalPages(0); return;
    }
    setLoading(true);
    try {
      const res = await salesService.getAll(
        selectedBranchId, currentPage, pageSize,
        searchTerm.trim() || undefined,
        statusFilter,
        dateFrom || null,
        dateTo || null,
      );
      const data = res.data;
      const rows = Array.isArray(data?.invoices) ? data.invoices : [];
      setItems(rows.map((r: unknown) => {
        const row = r as Record<string, unknown>;
        return {
          id:            Number(row.id ?? 0),
          invoiceNo:     safeString(row.invoiceNo),
          customerName:  row.customerName != null ? safeString(row.customerName) : null,
          customerPhone: row.customerPhone != null ? safeString(row.customerPhone) : null,
          warehouseName: safeString(row.warehouseName),
          saleDate:      safeString(row.saleDate),
          grandTotal:    Number(row.grandTotal ?? 0),
          paidAmount:    Number(row.paidAmount ?? 0),
          paymentMethod: safeString(row.paymentMethod),
          status:        safeString(row.status) as SaleInvoiceStatus,
          cashierName:   row.cashierName != null ? safeString(row.cashierName) : null,
          itemCount:     Number(row.itemCount ?? 0),
          createdDate:   safeString(row.createdDate),
          voidedAt:      row.voidedAt != null ? safeString(row.voidedAt) : null,
        } as SaleInvoiceListDto;
      }).filter(r => r.id > 0));
      setTotalRecords(Number(data?.totalRecords ?? 0));
      setTotalPages(Number(data?.totalPages ?? 0));
    } catch (err) {
      showNotification('error', getApiErrorMessage(err, 'Failed to load invoices.'));
    } finally { setLoading(false); }
  }, [hasBranchSelection, selectedBranchId, currentPage, pageSize, searchTerm, statusFilter, dateFrom, dateTo, showNotification]);

  useEffect(() => {
    const t = setTimeout(() => { void fetchInvoices(); }, searchTerm ? 300 : 0);
    return () => clearTimeout(t);
  }, [fetchInvoices, searchTerm]);

  useEffect(() => { setCurrentPage(1); }, [selectedBranchId, statusFilter, pageSize, dateFrom, dateTo]);

  // ── View Receipt ──
  const handleViewReceipt = async (item: SaleInvoiceListDto) => {
    const branchId = resolveEntityBranchId(item.branchId ?? selectedBranchId ?? 1);
    setLoadingDetail(item.id);
    try {
      const res = await salesService.getById(item.id, branchId);
      setReceiptInvoice(res.data);
    } catch (err) {
      showNotification('error', getApiErrorMessage(err, 'Failed to load invoice.'));
    } finally { setLoadingDetail(null); }
  };

  // ── View Ledger History ──
  const handleViewLedger = async (item: SaleInvoiceListDto) => {
    const branchId = resolveEntityBranchId(item.branchId ?? selectedBranchId ?? 1);
    setLoadingLedger(item.id);
    try {
      const res = await salesService.getLedgerHistory(item.id, branchId);
      setLedgerEntries(res.data);
      setLedgerInvoiceNo(item.invoiceNo);
    } catch (err) {
      showNotification('error', getApiErrorMessage(err, 'Failed to load ledger history.'));
    } finally { setLoadingLedger(null); }
  };

  // ── Edit Invoice — navigate to full-page form ──
  const handleEdit = (item: SaleInvoiceListDto) => {
    if (item.status !== 'Completed') {
      showNotification('error', 'Only completed invoices can be edited.');
      return;
    }
    if (!canModify) { showNotification('error', 'No permission to edit invoices.'); return; }
    const branchId = resolveEntityBranchId(item.branchId ?? selectedBranchId ?? 1);
    navigate(`/sales-invoices/edit/${item.id}`, { state: { branchId } });
  };

  // ── Void Invoice ──
  const handleVoid = (item: SaleInvoiceListDto) => {
    if (item.status !== 'Completed') {
      showNotification('error', 'Only completed invoices can be voided.');
      return;
    }
    if (!canModify) { showNotification('error', 'No permission to void invoices.'); return; }
    const branchId = resolveEntityBranchId(item.branchId ?? selectedBranchId ?? 1);
    showConfirm({
      title: 'Void Invoice?',
      message: '⚠ Stock will be recalculated. All stock deducted by this sale will be returned to the warehouse. This cannot be undone.',
      highlightText: item.invoiceNo,
      variant: 'danger',
      confirmLabel: 'Yes, Void Invoice',
      cancelLabel: 'Keep',
      onConfirm: async () => {
        setVoidingId(item.id);
        try {
          await salesService.voidInvoice(item.id, {
            businessId: 0,
            branchId,
            voidedByName: (user as { fullName?: string; username?: string })?.fullName
              ?? (user as { fullName?: string; username?: string })?.username
              ?? undefined,
            reason: 'Voided from Invoice History',
          });
          await fetchInvoices();
          showNotification('success', `Invoice "${item.invoiceNo}" voided. Stock reversed.`);
        } catch (err) {
          showNotification('error', getApiErrorMessage(err, 'Failed to void invoice.'));
        } finally { setVoidingId(null); }
      },
    });
  };

  const columns: Column<SaleInvoiceListDto>[] = useMemo(() => [
    { key: 'invoiceNo', header: 'Invoice No', sortable: true },
    {
      key: 'saleDate', header: 'Date', sortable: true,
      render: (v) => fmtDate(safeString(v)),
    },
    {
      key: 'customerName', header: 'Customer',
      render: (v, row) => (
        <div>
          <p className="font-medium text-gray-800">{safeString(v) || '—'}</p>
          {row.customerPhone && <p className="text-xs text-gray-400">{row.customerPhone}</p>}
        </div>
      ),
    },
    { key: 'warehouseName', header: 'Warehouse' },
    { key: 'itemCount', header: 'Items', render: (v) => String(v) },
    {
      key: 'grandTotal', header: 'Total', sortable: true,
      render: (v) => <span className="font-semibold">{fmt(Number(v))}</span>,
    },
    { key: 'paymentMethod', header: 'Payment' },
    {
      key: 'status', header: 'Status', sortable: true,
      render: (v) => {
        const s = v as SaleInvoiceStatus;
        return <Badge variant={statusVariant(s)} size="sm" dot>{s}</Badge>;
      },
    },
    { key: 'cashierName', header: 'Cashier', render: (v) => safeString(v) || '—' },
  ], []);

  const actions: Action<SaleInvoiceListDto>[] = useMemo(() => {
    const list: Action<SaleInvoiceListDto>[] = [];

    if (canModify) {
      list.push({
        label: 'Edit',
        onClick: handleEdit,
        icon: (
          <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
          </svg>
        ),
        variant: 'primary',
      });
    }

    list.push({
      label: 'Receipt',
      onClick: (item) => { void handleViewReceipt(item); },
      icon: loadingDetail ? (
        <svg className="h-4 w-4 animate-spin" fill="none" viewBox="0 0 24 24">
          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
          <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" />
        </svg>
      ) : (
        <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
        </svg>
      ),
      variant: 'secondary',
    });

    list.push({
      label: 'Ledger',
      onClick: (item) => { void handleViewLedger(item); },
      icon: loadingLedger ? (
        <svg className="h-4 w-4 animate-spin" fill="none" viewBox="0 0 24 24">
          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
          <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" />
        </svg>
      ) : (
        <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
        </svg>
      ),
      variant: 'secondary',
    });

    if (canModify) {
      list.push({
        label: 'Void',
        onClick: handleVoid,
        icon: voidingId ? (
          <svg className="h-4 w-4 animate-spin" fill="none" viewBox="0 0 24 24">
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" />
          </svg>
        ) : (
          <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
          </svg>
        ),
        variant: 'danger',
      });
    }

    return list;
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [canModify, loadingDetail, loadingLedger, voidingId]);

  return (
    <div>
      {notification && (
        <div className={`mb-6 flex items-center rounded-md p-4 ${notification.type === 'success' ? 'bg-green-50 text-green-800' : 'bg-red-50 text-red-800'}`}>
          <span className="font-medium">{notification.message}</span>
        </div>
      )}

      <div className="mb-8">
        <h1 className="mb-2 text-3xl font-bold text-gray-900">Invoice History</h1>
        <p className="text-gray-600">View, reprint and void completed sale invoices</p>
      </div>

      {!hasBranchSelection && (
        <div className="mb-6 rounded-md border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-700">
          Select a branch to load invoices.
        </div>
      )}

      {/* Filters */}
      <div className="mb-6 flex flex-wrap gap-4 items-end">
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">Status</label>
          <select
            value={statusFilter ?? ''}
            onChange={(e) => { const v = e.target.value; setStatusFilter(v ? (v as SaleInvoiceStatus) : null); }}
            disabled={!hasBranchSelection}
            className="rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none disabled:cursor-not-allowed disabled:bg-gray-100 w-40"
          >
            <option value="">All</option>
            <option value="Completed">Completed</option>
            <option value="Voided">Voided</option>
            <option value="Held">Held</option>
            <option value="Cancelled">Cancelled</option>
          </select>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">Date From</label>
          <input
            type="date"
            value={dateFrom}
            onChange={(e) => setDateFrom(e.target.value)}
            disabled={!hasBranchSelection}
            className="rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none disabled:cursor-not-allowed disabled:bg-gray-100"
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">Date To</label>
          <input
            type="date"
            value={dateTo}
            onChange={(e) => setDateTo(e.target.value)}
            disabled={!hasBranchSelection}
            className="rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none disabled:cursor-not-allowed disabled:bg-gray-100"
          />
        </div>

        {(dateFrom || dateTo || statusFilter) && (
          <button
            onClick={() => { setDateFrom(''); setDateTo(''); setStatusFilter(null); }}
            className="px-3 py-2 text-sm text-gray-600 hover:text-gray-900 underline"
          >
            Clear filters
          </button>
        )}
      </div>

      <DataTable
        data={items}
        columns={columns}
        actions={actions}
        loading={loading}
        searchable
        searchPlaceholder="Search by invoice no, customer or cashier…"
        pagination
        pageSize={pageSize}
        pageSizeOptions={[10, 25, 50, 100]}
        onPageSizeChange={(n) => { setPageSize(n); setCurrentPage(1); }}
        emptyMessage={!hasBranchSelection ? 'Select a branch to load invoices.' : searchTerm ? 'No invoices match your search.' : 'No invoices found.'}
        serverSide
        totalRecords={totalRecords}
        totalPages={totalPages}
        currentPage={currentPage}
        onPageChange={setCurrentPage}
        onSearchChange={(v) => { setSearchTerm(v); setCurrentPage(1); }}
      />

      {/* Modals */}
      {receiptInvoice && (
        <ReceiptModal invoice={receiptInvoice} onClose={() => setReceiptInvoice(null)} />
      )}

      {ledgerEntries !== null && (
        <LedgerModal
          invoiceNo={ledgerInvoiceNo}
          entries={ledgerEntries}
          onClose={() => { setLedgerEntries(null); setLedgerInvoiceNo(''); }}
        />
      )}
    </div>
  );
};

export default SaleInvoicesPage;
