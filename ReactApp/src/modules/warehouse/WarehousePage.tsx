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
import { warehouseService, type WarehouseItem } from './warehouseService';

const formatDate = (value: string) => {
  if (!value) return '-';
  const d = new Date(value);
  return Number.isNaN(d.getTime()) ? '-' : d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
};

const WarehousePage: React.FC = () => {
  const { openForm, isOpen } = useFormModal();
  const { showConfirm } = useConfirmDialog();

  const [items, setItems] = useState<WarehouseItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [searchTerm, setSearchTerm] = useState('');
  const [sortColumn, setSortColumn] = useState<string>('name');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [statusFilter, setStatusFilter] = useState<boolean | null>(null);
  const [notification, setNotification] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  const { canCreate, canEdit, canDelete } = usePermission('Warehouses');
  const { selectedBranchId, isGlobalMode, isGlobalAdmin, canWriteInView, resolveEntityBranchId, getWriteBlockMessage } = useBranchWriteAccess();

  const hasBranchSelection = hasBranchContext(selectedBranchId);
  const canAdd = canWriteInView && (isGlobalAdmin || canCreate);
  const canModify = canWriteInView && (isGlobalAdmin || canEdit);
  const canRemove = canWriteInView && (isGlobalAdmin || canDelete);

  const showNotification = useCallback((type: 'success' | 'error', message: string) => {
    setNotification({ type, message });
    setTimeout(() => setNotification(null), 4000);
  }, []);

  const fetchWarehouses = useCallback(async () => {
    if (!hasBranchSelection || selectedBranchId === null) {
      setItems([]); setTotalRecords(0); setTotalPages(0);
      return;
    }
    setLoading(true);
    try {
      const response = await warehouseService.getAll(
        selectedBranchId, currentPage, pageSize, searchTerm.trim() || undefined, statusFilter
      );
      const rows = Array.isArray(response.data?.warehouses) ? response.data.warehouses : [];
      setItems(
        rows
          .map((r: unknown) => {
            const row = r as Record<string, unknown>;
            return {
              id: Number(row.id ?? row.Id ?? 0),
              name: safeString(row.name ?? row.Name),
              code: safeString(row.code ?? row.Code),
              address: safeString(row.address ?? row.Address),
              isActive: Boolean(row.isActive ?? row.IsActive ?? true),
              branchId: Number(row.branchId ?? row.BranchId ?? selectedBranchId),
              branchName: safeString(row.branchName ?? row.BranchName),
              createdDate: safeString(row.createdDate ?? row.CreatedDate),
            } as WarehouseItem;
          })
          .filter((item: WarehouseItem) => item.id > 0)
      );
      setTotalRecords(Number(response.data?.totalRecords ?? 0));
      setTotalPages(Number(response.data?.totalPages ?? 0));
    } catch (err) {
      showNotification('error', getApiErrorMessage(err, 'Failed to load warehouses.'));
    } finally {
      setLoading(false);
    }
  }, [hasBranchSelection, selectedBranchId, currentPage, pageSize, searchTerm, statusFilter, showNotification]);

  useEffect(() => {
    const timer = setTimeout(() => { void fetchWarehouses(); }, searchTerm ? 300 : 0);
    return () => clearTimeout(timer);
  }, [fetchWarehouses, searchTerm]);

  useEffect(() => {
    if (!isOpen) return undefined;
    return () => { void fetchWarehouses(); };
  }, [isOpen, fetchWarehouses]);

  useEffect(() => { setCurrentPage(1); }, [selectedBranchId, statusFilter, pageSize]);

  const handleAdd = () => {
    const msg = getWriteBlockMessage();
    if (!canAdd || msg) { showNotification('error', msg ?? 'No permission to create warehouses.'); return; }
    openForm('warehouse', isGlobalMode ? {} : { branchId: selectedBranchId });
  };

  const handleEdit = (item: WarehouseItem) => {
    const msg = getWriteBlockMessage();
    if (!canModify || msg) { showNotification('error', msg ?? 'No permission to edit warehouses.'); return; }
    openForm('warehouse', {
      id: item.id,
      name: item.name,
      code: item.code,
      address: item.address,
      status: item.isActive ? 'Active' : 'Inactive',
      isActive: item.isActive,
      branchId: item.branchId,
    });
  };

  const handleDelete = (item: WarehouseItem) => {
    const msg = getWriteBlockMessage();
    if (!canRemove || msg) { showNotification('error', msg ?? 'No permission to delete warehouses.'); return; }
    const branchId = resolveEntityBranchId(item.branchId);
    if (branchId <= 0) { showNotification('error', 'Cannot determine branch for this warehouse.'); return; }
    showConfirm({
      title: 'Delete Warehouse?',
      message: 'This warehouse will be soft-deleted.',
      highlightText: item.name,
      variant: 'danger',
      confirmLabel: 'Yes, Delete',
      cancelLabel: 'Keep',
      onConfirm: async () => {
        try {
          await warehouseService.delete(item.id, branchId);
          await fetchWarehouses();
          showNotification('success', `Warehouse "${item.name}" deleted.`);
        } catch (err) {
          showNotification('error', getApiErrorMessage(err, 'Failed to delete warehouse.'));
        }
      },
    });
  };

  const columns: Column<WarehouseItem>[] = useMemo(() => [
    { key: 'name', header: 'Name', sortable: true },
    { key: 'code', header: 'Code', sortable: true, render: (v) => safeString(v) || '-' },
    { key: 'address', header: 'Address', render: (v) => safeString(v) || '-' },
    {
      key: 'isActive', header: 'Status', sortable: true,
      render: (v) => <Badge variant={v ? 'success' : 'danger'} size="sm" dot>{v ? 'Active' : 'Inactive'}</Badge>,
    },
    { key: 'createdDate', header: 'Created', sortable: true, render: (v) => formatDate(safeString(v)) },
  ], []);

  const actions: Action<WarehouseItem>[] = [];
  if (canModify) {
    actions.push({
      label: '',
      onClick: handleEdit,
      icon: <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>,
      variant: 'secondary',
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
        <h1 className="mb-2 text-3xl font-bold text-gray-900">Warehouses</h1>
        <p className="text-gray-600">Manage warehouses for stock management per branch</p>
      </div>

      {!hasBranchSelection && (
        <div className="mb-6 rounded-md border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-700">
          Select a branch to load warehouses.
        </div>
      )}

      <div className="mb-6 flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">Status Filter</label>
          <select
            value={statusFilter === null ? '' : statusFilter ? 'active' : 'inactive'}
            onChange={(e) => { const v = e.target.value; setStatusFilter(v === '' ? null : v === 'active'); }}
            disabled={!hasBranchSelection}
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none disabled:cursor-not-allowed disabled:bg-gray-100 sm:w-48"
          >
            <option value="">All Statuses</option>
            <option value="active">Active</option>
            <option value="inactive">Inactive</option>
          </select>
        </div>

        <PermissionGate module="Warehouses" action="create">
          <button
            onClick={handleAdd}
            disabled={!canAdd || !hasBranchSelection}
            className="inline-flex items-center rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
          >
            <svg className="-ml-1 mr-2 h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
            </svg>
            Add Warehouse
          </button>
        </PermissionGate>
      </div>

      <DataTable
        data={items}
        columns={columns}
        actions={actions}
        loading={loading}
        searchable
        searchPlaceholder="Search by name or code…"
        pagination
        pageSize={pageSize}
        pageSizeOptions={[5, 10, 25, 50]}
        onPageSizeChange={(n) => { setPageSize(n); setCurrentPage(1); }}
        emptyMessage={!hasBranchSelection ? 'Select a branch to load warehouses.' : searchTerm ? 'No warehouses match your search.' : 'No warehouses found.'}
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

export default WarehousePage;
