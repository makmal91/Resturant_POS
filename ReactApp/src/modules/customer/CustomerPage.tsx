import React, { useCallback, useEffect, useState } from 'react';
import DataTable, { Action, Column } from '../../components/DataTable';
import Badge from '../../components/Badge';
import { useFormModal } from '../../contexts/FormModalContext';
import { useConfirmDialog } from '../../contexts/ConfirmDialogContext';
import { customerService, type CustomerListItem } from './customerService';
import { getApiErrorMessage } from '../../services/api';
import { useBranchStore } from '../../stores/useBranchStore';

const TYPE_BADGE: Record<string, 'info' | 'warning' | 'success'> = {
  Retail:    'info',
  Wholesale: 'warning',
  VIP:       'success',
};

const CustomerPage: React.FC = () => {
  const selectedBranchId = useBranchStore((state) => state.selectedBranchId);
  const [customers, setCustomers]       = useState<CustomerListItem[]>([]);
  const [loading, setLoading]           = useState(true);
  const [currentPage, setCurrentPage]   = useState(1);
  const [pageSize, setPageSize]         = useState(25);
  const [searchTerm, setSearchTerm]     = useState('');
  const [sortColumn, setSortColumn]     = useState<string>('name');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages]     = useState(0);
  const [notification, setNotification] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  const { openForm, isOpen } = useFormModal();
  const { showConfirm } = useConfirmDialog();

  const branchId = selectedBranchId ?? 0;

  const showNotification = useCallback((type: 'success' | 'error', message: string) => {
    setNotification({ type, message });
    setTimeout(() => setNotification(null), 4000);
  }, []);

  const fetchCustomers = useCallback(async () => {
    if (!branchId) return;
    setLoading(true);
    try {
      const res = await customerService.getAll({
        branchId,
        page: currentPage,
        pageSize,
        search: searchTerm || undefined,
      });
      const d = res.data as any;
      setCustomers(d.customers ?? []);
      setTotalRecords(d.totalRecords ?? 0);
      setTotalPages(d.totalPages ?? 1);
    } catch (err) {
      setCustomers([]);
      showNotification('error', getApiErrorMessage(err, 'Failed to load customers.'));
    } finally {
      setLoading(false);
    }
  }, [branchId, currentPage, pageSize, searchTerm, showNotification]);

  useEffect(() => {
    const timer = setTimeout(() => { fetchCustomers(); }, searchTerm ? 300 : 0);
    return () => clearTimeout(timer);
  }, [fetchCustomers, searchTerm]);

  useEffect(() => {
    if (!isOpen) return undefined;
    return () => { fetchCustomers(); };
  }, [isOpen, fetchCustomers]);

  const handleAdd = () => openForm('customer');

  const handleEdit = async (c: CustomerListItem) => {
    try {
      const res = await customerService.getById(c.id, branchId);
      const d = res.data;
      const typeMap: Record<string, string> = { '1': 'Retail', '2': 'Wholesale', '3': 'VIP', Retail: 'Retail', Wholesale: 'Wholesale', VIP: 'VIP' };
      openForm('customer', {
        id:             d.id,
        name:           d.name,
        phone:          d.phone ?? '',
        email:          d.email ?? '',
        address:        d.address ?? '',
        countryId:      Number(d.countryId ?? 0),
        cityId:         Number(d.cityId ?? 0),
        cityName:       d.cityName ?? '',
        cnic:           d.cnic ?? '',
        customerType:   typeMap[String(d.customerType)] ?? 'Retail',
        creditLimit:    String(d.creditLimit ?? 0),
        openingBalance: String(d.openingBalance ?? 0),
        status:         d.status ? 'Active' : 'Inactive',
        branchId,
      });
    } catch {
      openForm('customer', { id: c.id, name: c.name, branchId, status: c.status ? 'Active' : 'Inactive' });
    }
  };

  const handleDelete = (c: CustomerListItem) => {
    if (c.isWalkIn) { showNotification('error', 'The walk-in customer cannot be deleted.'); return; }
    showConfirm({
      title: 'Delete Customer?',
      message: 'This customer will be removed from the system.',
      highlightText: c.name,
      variant: 'danger',
      confirmLabel: 'Yes, Delete',
      cancelLabel: 'Keep Customer',
      onConfirm: async () => {
        try {
          await customerService.delete(c.id, branchId);
          await fetchCustomers();
          showNotification('success', `Customer "${c.name}" deleted.`);
        } catch (err: any) {
          showNotification('error', err?.response?.data?.message ?? 'Failed to delete customer.');
        }
      },
    });
  };

  const handleSearchChange = (v: string) => { setSearchTerm(v); setCurrentPage(1); };
  const handleSortChange = (col: string, dir: 'asc' | 'desc') => { setSortColumn(col); setSortDirection(dir); setCurrentPage(1); };
  const handlePageSizeChange = (ps: number) => { setPageSize(ps); setCurrentPage(1); };

  const columns: Column<CustomerListItem>[] = [
    {
      key: 'customerCode',
      header: 'Code',
      render: (value, item) => (
        <span className="font-mono text-xs text-gray-500">
          {String(value)}
          {item.isWalkIn && (
            <span className="ml-1.5 rounded-full bg-green-100 px-1.5 py-0.5 text-[10px] font-semibold text-green-700">
              Walk-in
            </span>
          )}
        </span>
      ),
    },
    { key: 'name',  header: 'Full Name',  sortable: true },
    { key: 'phone', header: 'Phone',      sortable: true, render: (v) => v ? String(v) : <span className="text-gray-300">—</span> },
    { key: 'cityName', header: 'City', render: (v) => v ? String(v) : <span className="text-gray-300">—</span> },
    {
      key: 'customerType',
      header: 'Type',
      sortable: true,
      render: (value) => (
        <Badge variant={TYPE_BADGE[String(value)] ?? 'info'} size="sm">
          {String(value)}
        </Badge>
      ),
    },
    {
      key: 'creditLimit',
      header: 'Credit Limit',
      render: (v) => {
        const n = Number(v);
        return n > 0
          ? <span className="tabular-nums">{n.toLocaleString('en-US', { minimumFractionDigits: 2 })}</span>
          : <span className="text-gray-300">—</span>;
      },
    },
    {
      key: 'status',
      header: 'Status',
      sortable: true,
      render: (value) => (
        <Badge variant={value ? 'success' : 'danger'} size="sm" dot>
          {value ? 'Active' : 'Inactive'}
        </Badge>
      ),
    },
  ];

  const actions: Action<CustomerListItem>[] = [
    {
      label: '',
      onClick: handleEdit,
      icon: (
        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" title="Edit">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
            d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
        </svg>
      ),
      variant: 'secondary',
    },
    {
      label: '',
      onClick: handleDelete,
      icon: (
        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" title="Delete">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
            d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
        </svg>
      ),
      variant: 'danger',
    },
  ];

  return (
    <div>
      {/* Notification */}
      {notification && (
        <div className={`mb-6 p-4 rounded-md flex items-center ${
          notification.type === 'success' ? 'bg-green-50 text-green-800' : 'bg-red-50 text-red-800'
        }`}>
          {notification.type === 'success' ? (
            <svg className="w-5 h-5 mr-3" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
            </svg>
          ) : (
            <svg className="w-5 h-5 mr-3" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
            </svg>
          )}
          <span className="font-medium">{notification.message}</span>
        </div>
      )}

      {/* Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Customers</h1>
        <p className="text-gray-600">Manage customer records and account details</p>
      </div>

      {/* Add button */}
      <div className="mb-6 flex justify-between items-center">
        <div />
        <button
          onClick={handleAdd}
          className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors"
        >
          <svg className="-ml-1 mr-2 h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
          </svg>
          Add Customer
        </button>
      </div>

      <DataTable
        data={customers}
        columns={columns}
        actions={actions}
        loading={loading}
        searchable
        searchPlaceholder="Search by name, phone, code…"
        pagination
        pageSize={pageSize}
        pageSizeOptions={[10, 25, 50, 100]}
        onPageSizeChange={handlePageSizeChange}
        emptyMessage={searchTerm ? 'No customers match your search.' : 'No customers found'}
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

export default CustomerPage;
