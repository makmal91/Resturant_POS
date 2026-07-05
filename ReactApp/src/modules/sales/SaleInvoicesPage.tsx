import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import DataTable, { type Action, type Column } from '../../components/DataTable';
import Badge from '../../components/Badge';
import { useConfirmDialog } from '../../contexts/ConfirmDialogContext';
import { useModuleCrudAccess } from '../../hooks/useModuleCrudAccess';
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
import { ReceiptPrintModal } from '../../components/receipt';

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
  const { canModify, selectedBranchId, canWriteInView, resolveEntityBranchId } = useModuleCrudAccess('Sales');
  const hasBranchSelection = hasBranchContext(selectedBranchId);

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

  // Show success toast when returning from EditInvoicePage; open receipt when linked from party ledger
  useEffect(() => {
    const state = location.state as { success?: string; viewInvoiceId?: number; branchId?: number } | null;
    if (state?.success) {
      showNotification('success', state.success);
    }

    if (state?.viewInvoiceId && state.viewInvoiceId > 0) {
      const branchId = state.branchId && state.branchId > 0
        ? state.branchId
        : resolveEntityBranchId(selectedBranchId);

      if (branchId <= 0) {
        showNotification('error', 'Cannot load invoice: branch is unknown.');
      } else {
        setLoadingDetail(state.viewInvoiceId);
        void salesService.getById(state.viewInvoiceId, branchId)
          .then((res) => setReceiptInvoice(res.data))
          .catch((err) => showNotification('error', getApiErrorMessage(err, 'Failed to load invoice.')))
          .finally(() => setLoadingDetail(null));
      }
    }

    if (state?.success || state?.viewInvoiceId) {
      window.history.replaceState({}, '');
    }
  }, [location.state, showNotification, resolveEntityBranchId, selectedBranchId]);

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
          id:            Number(row.id ?? row.Id ?? 0),
          invoiceNo:     safeString(row.invoiceNo ?? row.InvoiceNo),
          customerName:  row.customerName != null ? safeString(row.customerName ?? row.CustomerName) : null,
          customerPhone: row.customerPhone != null ? safeString(row.customerPhone ?? row.CustomerPhone) : null,
          warehouseName: safeString(row.warehouseName ?? row.WarehouseName),
          saleDate:      safeString(row.saleDate ?? row.SaleDate),
          grandTotal:    Number(row.grandTotal ?? row.GrandTotal ?? 0),
          paidAmount:    Number(row.paidAmount ?? row.PaidAmount ?? 0),
          paymentMethod: safeString(row.paymentMethod ?? row.PaymentMethod),
          status:        safeString(row.status ?? row.Status) as SaleInvoiceStatus,
          cashierName:   row.cashierName != null ? safeString(row.cashierName ?? row.CashierName) : null,
          itemCount:     Number(row.itemCount ?? row.ItemCount ?? 0),
          createdDate:   safeString(row.createdDate ?? row.createdAt ?? row.CreatedAt),
          voidedAt:      row.voidedAt != null ? safeString(row.voidedAt ?? row.VoidedAt) : null,
          branchId:      Number(row.branchId ?? row.BranchId ?? 0),
          warehouseId:   Number(row.warehouseId ?? row.WarehouseId ?? 0),
          customerId:    row.customerId != null || row.CustomerId != null
            ? Number(row.customerId ?? row.CustomerId ?? 0)
            : null,
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

  const resolveInvoiceBranchId = useCallback((item: SaleInvoiceListDto): number => {
    if (item.branchId > 0) return item.branchId;
    return resolveEntityBranchId(selectedBranchId);
  }, [resolveEntityBranchId, selectedBranchId]);

  // ── View Receipt ──
  const handleViewReceipt = async (item: SaleInvoiceListDto) => {
    const branchId = resolveInvoiceBranchId(item);
    if (branchId <= 0) {
      showNotification('error', 'Cannot load receipt: invoice branch is unknown. Select a specific branch.');
      return;
    }
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
    const branchId = resolveInvoiceBranchId(item);
    if (branchId <= 0) {
      showNotification('error', 'Cannot load ledger: invoice branch is unknown. Select a specific branch.');
      return;
    }
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
    const branchId = resolveInvoiceBranchId(item);
    if (branchId <= 0) {
      showNotification('error', 'Cannot edit: invoice branch is unknown. Select a specific branch.');
      return;
    }
    navigate(`/sales-invoices/edit/${item.id}`, { state: { branchId } });
  };

  // ── Void Invoice ──
  const handleVoid = (item: SaleInvoiceListDto) => {
    if (item.status !== 'Completed') {
      showNotification('error', 'Only completed invoices can be voided.');
      return;
    }
    if (!canModify) { showNotification('error', 'No permission to void invoices.'); return; }
    const branchId = resolveInvoiceBranchId(item);
    if (branchId <= 0) {
      showNotification('error', 'Cannot void: invoice branch is unknown. Select a specific branch.');
      return;
    }
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
        tooltip: 'Edit invoice items, quantities, discounts and payment',
        iconOnly: true,
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
      tooltip: 'View and print sales receipt',
      iconOnly: true,
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
      tooltip: 'View stock ledger entries for this sale only',
      iconOnly: true,
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
        tooltip: 'Void invoice and return sold stock to warehouse',
        iconOnly: true,
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
        <ReceiptPrintModal
          invoice={receiptInvoice}
          onClose={() => setReceiptInvoice(null)}
        />
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
