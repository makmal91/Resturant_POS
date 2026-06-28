import React, { useCallback, useEffect, useState } from 'react';
import DataTable, { type Action, type Column } from '../../components/DataTable';
import Badge from '../../components/Badge';
import PermissionGate from '../../components/PermissionGate';
import { useFormModal } from '../../contexts/FormModalContext';
import { useConfirmDialog } from '../../contexts/ConfirmDialogContext';
import { useModuleCrudAccess } from '../../hooks/useModuleCrudAccess';
import { hasBranchContext } from '../../types/permissions';
import { getApiErrorMessage } from '../../services/api';
import { safeString } from '../../utils/safeValues';
import { supplierService, type SupplierItem } from './supplierService';

const SupplierPage: React.FC = () => {
  const { openForm, isOpen } = useFormModal();
  const { showConfirm } = useConfirmDialog();

  const [suppliers, setSuppliers] = useState<SupplierItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(5);
  const [searchTerm, setSearchTerm] = useState('');
  const [sortColumn, setSortColumn] = useState<string>('name');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [notification, setNotification] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  const {
    canAdd,
    canModify,
    canRemove,
    selectedBranchId,
    isGlobalMode,
    resolveEntityBranchId,
    getWriteBlockMessage,
  } = useModuleCrudAccess('Suppliers');

  const hasBranchSelection = hasBranchContext(selectedBranchId);

  const normalizeSupplier = (row: unknown, fallbackBranchId: number): SupplierItem | null => {
    const item = row as Record<string, unknown>;
    const id = Number(item?.id ?? item?.Id ?? 0);
    if (id <= 0) return null;

    return {
      id,
      supplierCode: safeString(item?.supplierCode ?? item?.SupplierCode),
      name: safeString(item?.name ?? item?.Name),
      contactPerson: safeString(item?.contactPerson ?? item?.ContactPerson),
      phone: safeString(item?.phone ?? item?.Phone),
      email: safeString(item?.email ?? item?.Email),
      address: safeString(item?.address ?? item?.Address),
      taxNumber: safeString(item?.taxNumber ?? item?.TaxNumber),
      isActive: Boolean(item?.isActive ?? item?.IsActive ?? true),
      branchId: Number(item?.branchId ?? item?.BranchId ?? fallbackBranchId),
      branchName: safeString(item?.branchName ?? item?.BranchName),
      createdDate: safeString(item?.createdDate ?? item?.CreatedDate),
    };
  };

  const showNotification = useCallback((type: 'success' | 'error', message: string) => {
    setNotification({ type, message });
    setTimeout(() => setNotification(null), 4000);
  }, []);

  const fetchSuppliers = useCallback(async () => {
    if (!hasBranchSelection || selectedBranchId === null) {
      setSuppliers([]);
      setTotalRecords(0);
      setTotalPages(0);
      setLoading(false);
      return;
    }

    setLoading(true);
    try {
      const response = await supplierService.getAll(
        selectedBranchId,
        currentPage,
        pageSize,
        searchTerm.trim() || undefined
      );
      const rows = Array.isArray(response.data?.suppliers) ? response.data.suppliers : [];
      setSuppliers(
        rows
          .map((row: unknown) => normalizeSupplier(row, selectedBranchId))
          .filter((item: SupplierItem | null): item is SupplierItem => item !== null)
      );
      setTotalRecords(Number(response.data?.totalRecords ?? 0));
      setTotalPages(Number(response.data?.totalPages ?? 0));
    } catch (error) {
      console.error('Failed to fetch suppliers:', error);
      setSuppliers([]);
      setTotalRecords(0);
      setTotalPages(0);
      showNotification('error', getApiErrorMessage(error, 'Failed to load suppliers.'));
    } finally {
      setLoading(false);
    }
  }, [
    hasBranchSelection,
    selectedBranchId,
    currentPage,
    pageSize,
    searchTerm,
    showNotification,
  ]);

  useEffect(() => {
    const timer = setTimeout(() => {
      void fetchSuppliers();
    }, searchTerm ? 300 : 0);

    return () => clearTimeout(timer);
  }, [fetchSuppliers, searchTerm]);

  useEffect(() => {
    if (!isOpen) return undefined;
    return () => {
      void fetchSuppliers();
    };
  }, [isOpen, fetchSuppliers]);

  useEffect(() => {
    setCurrentPage(1);
  }, [selectedBranchId, pageSize]);

  const handleAddSupplier = () => {
    const blockMessage = getWriteBlockMessage();
    if (!canAdd || blockMessage) {
      showNotification('error', blockMessage ?? 'You do not have permission to create suppliers.');
      return;
    }
    openForm('supplier', isGlobalMode ? {} : { branchId: selectedBranchId });
  };

  const handleEdit = async (supplier: SupplierItem) => {
    const blockMessage = getWriteBlockMessage();
    if (!canModify || blockMessage) {
      showNotification('error', blockMessage ?? 'You do not have permission to edit suppliers.');
      return;
    }

    const branchId = resolveEntityBranchId(supplier.branchId);
    if (branchId <= 0) {
      showNotification('error', 'Unable to determine the branch for this supplier.');
      return;
    }

    try {
      const response = await supplierService.getById(supplier.id, branchId);
      const detail = (response.data ?? supplier) as Record<string, unknown>;
      openForm('supplier', {
        id: Number(detail.id ?? detail.Id ?? supplier.id),
        name: safeString(detail.name ?? detail.Name ?? supplier.name),
        contactPerson: safeString(detail.contactPerson ?? detail.ContactPerson ?? supplier.contactPerson),
        phone: safeString(detail.phone ?? detail.Phone ?? supplier.phone),
        email: safeString(detail.email ?? detail.Email ?? supplier.email),
        address: safeString(detail.address ?? detail.Address ?? supplier.address),
        taxNumber: safeString(detail.taxNumber ?? detail.TaxNumber ?? supplier.taxNumber),
        status: Boolean(detail.isActive ?? detail.IsActive ?? supplier.isActive) ? 'Active' : 'Inactive',
        isActive: Boolean(detail.isActive ?? detail.IsActive ?? supplier.isActive),
        branchId: Number(detail.branchId ?? detail.BranchId ?? supplier.branchId),
      });
    } catch (error) {
      console.error('Failed to load supplier details:', error);
      openForm('supplier', {
        ...supplier,
        status: supplier.isActive ? 'Active' : 'Inactive',
      });
    }
  };

  const handleDelete = (supplier: SupplierItem) => {
    const blockMessage = getWriteBlockMessage();
    if (!canRemove || blockMessage) {
      showNotification('error', blockMessage ?? 'You do not have permission to delete suppliers.');
      return;
    }

    const branchId = resolveEntityBranchId(supplier.branchId);
    if (branchId <= 0) {
      showNotification('error', 'Unable to determine the branch for this supplier.');
      return;
    }

    showConfirm({
      title: 'Delete Supplier?',
      message: 'This supplier will be soft-deleted and removed from listings.',
      highlightText: supplier.name,
      variant: 'danger',
      confirmLabel: 'Yes, Delete',
      cancelLabel: 'Keep Supplier',
      onConfirm: async () => {
        try {
          await supplierService.delete(supplier.id, branchId);
          await fetchSuppliers();
          showNotification('success', `Supplier "${supplier.name}" deleted successfully.`);
        } catch (error) {
          console.error('Failed to delete supplier:', error);
          showNotification('error', getApiErrorMessage(error, 'Failed to delete supplier. Please try again.'));
        }
      },
    });
  };

  const handleSearchChange = (value: string) => {
    setSearchTerm(value);
    setCurrentPage(1);
  };

  const handleSortChange = (column: string, direction: 'asc' | 'desc') => {
    setSortColumn(column);
    setSortDirection(direction);
    setCurrentPage(1);
  };

  const handlePageSizeChange = (nextPageSize: number) => {
    setPageSize(nextPageSize);
    setCurrentPage(1);
  };

  const columns: Column<SupplierItem>[] = [
    {
      key: 'avatar',
      header: 'Avatar',
      render: (_value, item) => (
        <div className="flex h-10 w-10 items-center justify-center rounded-md border border-gray-200 bg-gray-100 text-xs font-semibold text-gray-600">
          {item.name.slice(0, 1).toUpperCase()}
        </div>
      ),
    },
    {
      key: 'name',
      header: 'Supplier Name',
      sortable: true,
    },
    {
      key: 'contactPerson',
      header: 'Contact Person',
      sortable: true,
      render: (value) => safeString(value) || '-',
    },
    {
      key: 'email',
      header: 'Email',
      sortable: true,
      render: (value) => safeString(value) || '-',
    },
    {
      key: 'phone',
      header: 'Phone',
      sortable: true,
      render: (value) => safeString(value) || '-',
    },
    {
      key: 'isActive',
      header: 'Status',
      sortable: true,
      render: (value) => (
        <Badge variant={value ? 'success' : 'danger'} size="sm" dot>
          {value ? 'Active' : 'Inactive'}
        </Badge>
      ),
    },
  ];

  const actions: Action<SupplierItem>[] = [];
  if (canModify) {
    actions.push({
      label: '',
      onClick: (item) => {
        void handleEdit(item);
      },
      icon: (
        <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
          />
        </svg>
      ),
      variant: 'secondary',
    });
  }
  if (canRemove) {
    actions.push({
      label: '',
      onClick: handleDelete,
      icon: (
        <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
          />
        </svg>
      ),
      variant: 'danger',
    });
  }

  return (
    <div>
      {notification && (
        <div
          className={`mb-6 flex items-center rounded-md p-4 ${
            notification.type === 'success' ? 'bg-green-50 text-green-800' : 'bg-red-50 text-red-800'
          }`}
        >
          {notification.type === 'success' ? (
            <svg className="mr-3 h-5 w-5" fill="currentColor" viewBox="0 0 20 20">
              <path
                fillRule="evenodd"
                d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                clipRule="evenodd"
              />
            </svg>
          ) : (
            <svg className="mr-3 h-5 w-5" fill="currentColor" viewBox="0 0 20 20">
              <path
                fillRule="evenodd"
                d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
                clipRule="evenodd"
              />
            </svg>
          )}
          <span className="font-medium">{notification.message}</span>
        </div>
      )}

      <div className="mb-8">
        <h1 className="mb-2 text-3xl font-bold text-gray-900">Suppliers</h1>
        <p className="text-gray-600">Manage supplier records and active status</p>
      </div>

      {!hasBranchSelection && (
        <div className="mb-6 rounded-md border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-700">
          Select a branch from the header to load suppliers.
        </div>
      )}

      <div className="mb-6 flex items-center justify-between">
        <div />
        <PermissionGate module="Suppliers" action="create">
          <button
            onClick={handleAddSupplier}
            disabled={!canAdd || !hasBranchSelection}
            className="inline-flex items-center rounded-md border border-transparent bg-blue-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-60"
          >
            <svg className="-ml-1 mr-2 h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
            </svg>
            Add Supplier
          </button>
        </PermissionGate>
      </div>

      <DataTable
        data={suppliers}
        columns={columns}
        actions={actions}
        loading={loading}
        searchable
        searchPlaceholder="Search by name, contact person, email, or phone..."
        pagination
        pageSize={pageSize}
        pageSizeOptions={[5, 10, 25, 50]}
        onPageSizeChange={handlePageSizeChange}
        emptyMessage={
          !hasBranchSelection
            ? 'Select a branch to load suppliers.'
            : searchTerm
              ? 'No suppliers match your search.'
              : 'No suppliers found'
        }
        serverSide
        totalRecords={totalRecords}
        totalPages={totalPages}
        currentPage={currentPage}
        onPageChange={setCurrentPage}
        searchTerm={searchTerm}
        onSearchChange={handleSearchChange}
        sortColumn={sortColumn}
        sortDirection={sortDirection}
        onSortChange={handleSortChange}
      />
    </div>
  );
};

export default SupplierPage;
