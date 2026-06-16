import React, { useCallback, useEffect, useMemo, useState } from 'react';
import DataTable, { type Action, type Column } from '../../components/DataTable';
import Badge from '../../components/Badge';
import PermissionGate from '../../components/PermissionGate';
import { useFormModal } from '../../contexts/FormModalContext';
import { useConfirmDialog } from '../../contexts/ConfirmDialogContext';
import { usePermission } from '../../hooks/usePermission';
import { useBranchWriteAccess } from '../../hooks/useBranchWriteAccess';
import { hasBranchContext } from '../../types/permissions';
import { getApiErrorMessage } from '../../services/api';
import { safeString } from '../../utils/safeValues';
import { purchaseService, type PurchaseDto, type PurchaseStatus } from './purchaseService';

const formatDate = (value: string) => {
  if (!value) return '-';
  const d = new Date(value);
  return Number.isNaN(d.getTime()) ? '-' : d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
};

const formatCurrency = (value: number) =>
  new Intl.NumberFormat(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value);

const statusVariant = (s: PurchaseStatus) => {
  if (s === 'Posted') return 'success' as const;
  if (s === 'Cancelled') return 'danger' as const;
  return 'warning' as const;
};

const PurchasePage: React.FC = () => {
  const { openForm, isOpen } = useFormModal();
  const { showConfirm } = useConfirmDialog();

  const [items, setItems] = useState<PurchaseDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [searchTerm, setSearchTerm] = useState('');
  const [sortColumn, setSortColumn] = useState<string>('purchaseDate');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('desc');
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [statusFilter, setStatusFilter] = useState<PurchaseStatus | null>(null);
  const [notification, setNotification] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  const { canCreate, canEdit, canDelete } = usePermission('Purchase');
  const { selectedBranchId, isGlobalMode, isGlobalAdmin, canWriteInView, resolveEntityBranchId, getWriteBlockMessage } = useBranchWriteAccess();

  const hasBranchSelection = hasBranchContext(selectedBranchId);
  const canAdd = canWriteInView && (isGlobalAdmin || canCreate);
  const canModify = canWriteInView && (isGlobalAdmin || canEdit);
  const canRemove = canWriteInView && (isGlobalAdmin || canDelete);

  const showNotification = useCallback((type: 'success' | 'error', message: string) => {
    setNotification({ type, message });
    setTimeout(() => setNotification(null), 4000);
  }, []);

  const fetchPurchases = useCallback(async () => {
    if (!hasBranchSelection || selectedBranchId === null) {
      setItems([]); setTotalRecords(0); setTotalPages(0); return;
    }
    setLoading(true);
    try {
      const response = await purchaseService.getAll(selectedBranchId, currentPage, pageSize, searchTerm.trim() || undefined, statusFilter);
      const rows = Array.isArray(response.data?.purchases) ? response.data.purchases : [];
      setItems(
        rows
          .map((r: unknown) => {
            const row = r as Record<string, unknown>;
            return {
              id: Number(row.id ?? row.Id ?? 0),
              invoiceNo: safeString(row.invoiceNo ?? row.InvoiceNo),
              supplierId: Number(row.supplierId ?? row.SupplierId ?? 0),
              supplierName: safeString(row.supplierName ?? row.SupplierName),
              warehouseId: Number(row.warehouseId ?? row.WarehouseId ?? 0),
              warehouseName: safeString(row.warehouseName ?? row.WarehouseName),
              branchId: Number(row.branchId ?? row.BranchId ?? selectedBranchId),
              branchName: safeString(row.branchName ?? row.BranchName),
              purchaseDate: safeString(row.purchaseDate ?? row.PurchaseDate),
              totalAmount: Number(row.totalAmount ?? row.TotalAmount ?? 0),
              status: safeString(row.status ?? row.Status) as PurchaseStatus,
              notes: safeString(row.notes ?? row.Notes),
              itemCount: Number(row.itemCount ?? row.ItemCount ?? 0),
              createdDate: safeString(row.createdDate ?? row.CreatedDate),
            } as PurchaseDto;
          })
          .filter((item: PurchaseDto) => item.id > 0)
      );
      setTotalRecords(Number(response.data?.totalRecords ?? 0));
      setTotalPages(Number(response.data?.totalPages ?? 0));
    } catch (err) {
      showNotification('error', getApiErrorMessage(err, 'Failed to load purchases.'));
    } finally { setLoading(false); }
  }, [hasBranchSelection, selectedBranchId, currentPage, pageSize, searchTerm, statusFilter, showNotification]);

  useEffect(() => {
    const t = setTimeout(() => { void fetchPurchases(); }, searchTerm ? 300 : 0);
    return () => clearTimeout(t);
  }, [fetchPurchases, searchTerm]);

  useEffect(() => {
    if (!isOpen) return undefined;
    return () => { void fetchPurchases(); };
  }, [isOpen, fetchPurchases]);

  useEffect(() => { setCurrentPage(1); }, [selectedBranchId, statusFilter, pageSize]);

  const handleAdd = () => {
    const msg = getWriteBlockMessage();
    if (!canAdd || msg) { showNotification('error', msg ?? 'No permission to create purchases.'); return; }
    openForm('purchase', isGlobalMode ? {} : { branchId: selectedBranchId });
  };

  const handleEdit = async (item: PurchaseDto) => {
    if (item.status === 'Cancelled') { showNotification('error', 'Cannot edit a voided/cancelled purchase.'); return; }
    const msg = getWriteBlockMessage();
    if (!canModify || msg) { showNotification('error', msg ?? 'No permission to edit purchases.'); return; }
    try {
      const branchId = resolveEntityBranchId(item.branchId);
      const res = await purchaseService.getById(item.id, branchId);
      const data = res.data;
      openForm('purchase', {
        id: data.id,
        invoiceNo: data.invoiceNo,
        supplierId: data.supplierId,
        warehouseId: data.warehouseId,
        purchaseDate: data.purchaseDate,
        notes: data.notes,
        branchId: data.branchId,
        items: data.items ?? [],
      });
    } catch (err) {
      showNotification('error', getApiErrorMessage(err, 'Failed to load purchase details.'));
    }
  };

  const handlePost = (item: PurchaseDto) => {
    if (item.status === 'Posted') { showNotification('error', 'Already posted.'); return; }
    const branchId = resolveEntityBranchId(item.branchId);
    showConfirm({
      title: 'Post Purchase?',
      message: 'This will update stock ledger. Action cannot be undone.',
      highlightText: item.invoiceNo,
      variant: 'danger',
      confirmLabel: 'Post Now',
      cancelLabel: 'Cancel',
      onConfirm: async () => {
        try {
          await purchaseService.post(item.id, branchId);
          await fetchPurchases();
          showNotification('success', `Purchase "${item.invoiceNo}" posted successfully.`);
        } catch (err) { showNotification('error', getApiErrorMessage(err, 'Failed to post purchase.')); }
      },
    });
  };

  const handleVoid = (item: PurchaseDto) => {
    if (item.status !== 'Posted') { showNotification('error', 'Only posted purchases can be voided.'); return; }
    const msg = getWriteBlockMessage();
    if (!canModify || msg) { showNotification('error', msg ?? 'No permission to void purchases.'); return; }
    const branchId = resolveEntityBranchId(item.branchId);
    showConfirm({
      title: 'Void Purchase?',
      message: '⚠ This will create reversal entries in the Stock Ledger and remove all stock added by this purchase. The invoice will be marked as Cancelled.',
      highlightText: item.invoiceNo,
      variant: 'danger',
      confirmLabel: 'Yes, Void Purchase',
      cancelLabel: 'Keep',
      onConfirm: async () => {
        try {
          await purchaseService.void(item.id, { businessId: 0, branchId, reason: 'Voided from Purchase Page' });
          await fetchPurchases();
          showNotification('success', `Purchase "${item.invoiceNo}" voided. Stock reversed.`);
        } catch (err) { showNotification('error', getApiErrorMessage(err, 'Failed to void purchase.')); }
      },
    });
  };

  const handleDelete = (item: PurchaseDto) => {
    if (item.status === 'Posted') { showNotification('error', 'Cannot delete a posted purchase.'); return; }
    const msg = getWriteBlockMessage();
    if (!canRemove || msg) { showNotification('error', msg ?? 'No permission to delete purchases.'); return; }
    const branchId = resolveEntityBranchId(item.branchId);
    showConfirm({
      title: 'Delete Purchase?', message: 'This draft purchase will be deleted.', highlightText: item.invoiceNo,
      variant: 'danger', confirmLabel: 'Yes, Delete', cancelLabel: 'Keep',
      onConfirm: async () => {
        try {
          await purchaseService.delete(item.id, branchId);
          await fetchPurchases();
          showNotification('success', `Purchase "${item.invoiceNo}" deleted.`);
        } catch (err) { showNotification('error', getApiErrorMessage(err, 'Failed to delete purchase.')); }
      },
    });
  };

  const columns: Column<PurchaseDto>[] = useMemo(() => [
    { key: 'invoiceNo', header: 'Invoice No', sortable: true },
    { key: 'supplierName', header: 'Supplier', sortable: true },
    { key: 'warehouseName', header: 'Warehouse', sortable: true },
    { key: 'purchaseDate', header: 'Date', sortable: true, render: (v) => formatDate(safeString(v)) },
    { key: 'itemCount', header: 'Items', render: (v) => String(v) },
    {
      key: 'totalAmount', header: 'Total', sortable: true,
      render: (v) => <span className="font-semibold">{formatCurrency(Number(v))}</span>,
    },
    {
      key: 'status', header: 'Status', sortable: true,
      render: (v) => {
        const s = v as PurchaseStatus;
        return <Badge variant={statusVariant(s)} size="sm" dot>{s}</Badge>;
      },
    },
  ], []);

  const actions: Action<PurchaseDto>[] = [];
  if (canModify) {
    actions.push({
      label: '',
      title: 'Edit',
      onClick: (item) => { void handleEdit(item); },
      icon: <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>,
      variant: 'secondary',
    });
    actions.push({
      label: '',
      title: 'Post',
      onClick: handlePost,
      icon: <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>,
      variant: 'primary',
    });
    actions.push({
      label: '',
      title: 'Void (reverse stock)',
      onClick: handleVoid,
      icon: <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" /></svg>,
      variant: 'danger',
    });
  }
  if (canRemove) {
    actions.push({
      label: '',
      onClick: handleDelete,
      icon: <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>,
      variant: 'danger',
    });
  }

  return (
    <div>
      {notification && (
        <div className={`mb-6 flex items-center rounded-md p-4 ${notification.type === 'success' ? 'bg-green-50 text-green-800' : 'bg-red-50 text-red-800'}`}>
          <span className="font-medium">{notification.message}</span>
        </div>
      )}

      <div className="mb-8">
        <h1 className="mb-2 text-3xl font-bold text-gray-900">Purchase Orders</h1>
        <p className="text-gray-600">Create and post purchase orders to update warehouse stock</p>
      </div>

      {!hasBranchSelection && (
        <div className="mb-6 rounded-md border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-700">
          Select a branch to load purchases.
        </div>
      )}

      <div className="mb-6 flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">Status Filter</label>
          <select
            value={statusFilter ?? ''}
            onChange={(e) => { const v = e.target.value; setStatusFilter(v ? (v as PurchaseStatus) : null); }}
            disabled={!hasBranchSelection}
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none disabled:cursor-not-allowed disabled:bg-gray-100 sm:w-48"
          >
            <option value="">All Statuses</option>
            <option value="Draft">Draft</option>
            <option value="Posted">Posted</option>
            <option value="Cancelled">Cancelled</option>
          </select>
        </div>

        <PermissionGate module="Purchase" action="create">
          <button
            onClick={handleAdd}
            disabled={!canAdd || !hasBranchSelection}
            className="inline-flex items-center rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
          >
            <svg className="-ml-1 mr-2 h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
            </svg>
            New Purchase
          </button>
        </PermissionGate>
      </div>

      <DataTable
        data={items}
        columns={columns}
        actions={actions}
        loading={loading}
        searchable
        searchPlaceholder="Search by invoice no or supplier…"
        pagination
        pageSize={pageSize}
        pageSizeOptions={[5, 10, 25, 50]}
        onPageSizeChange={(n) => { setPageSize(n); setCurrentPage(1); }}
        emptyMessage={!hasBranchSelection ? 'Select a branch to load purchases.' : searchTerm ? 'No purchases match your search.' : 'No purchases found.'}
        serverSide
        totalRecords={totalRecords}
        totalPages={totalPages}
        currentPage={currentPage}
        onPageChange={setCurrentPage}
        searchTerm={searchTerm}
        onSearchChange={(v) => { setSearchTerm(v); setCurrentPage(1); }}
        sortColumn={sortColumn}
        sortDirection={sortDirection}
        onSortChange={(col, dir) => { setSortColumn(col); setSortDirection(dir); setCurrentPage(1); }}
      />
    </div>
  );
};

export default PurchasePage;
