import React, { useState, useEffect, useCallback } from 'react';
import DataTable, { Column, Action } from './DataTable';
import Badge from './Badge';
import { useFormModal } from '../contexts/FormModalContext';
import { useConfirmDialog } from '../contexts/ConfirmDialogContext';
import { BranchService } from '../services/apiService';
import { getApiErrorMessage } from '../services/api';
import { safeString } from '../utils/safeValues';

interface Branch {
  id: number;
  name: string;
  code: string;
  address: string;
  phone: string;
  email: string;
  businessId: number;
  businessName: string;
  countryId: number;
  countryName: string;
  cityId: number;
  cityName: string;
  status: string;
  createdAt: string;
}

const BranchesList: React.FC = () => {
  const [branches, setBranches] = useState<Branch[]>([]);
  const [loading, setLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(5);
  const [searchTerm, setSearchTerm] = useState('');
  const [sortColumn, setSortColumn] = useState('name');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [notification, setNotification] = useState<{ type: 'success' | 'error'; message: string } | null>(null);
  const { openForm, isOpen } = useFormModal();
  const { showConfirm } = useConfirmDialog();

  const showNotification = useCallback((type: 'success' | 'error', message: string) => {
    setNotification({ type, message });
    setTimeout(() => {
      setNotification(null);
    }, 4000);
  }, []);

  const normalizeBranches = (payload: unknown): Branch[] => {
    const rows = Array.isArray(payload) ? payload : [];

    return rows
      .map((row): Branch => {
        const item = row as Partial<Branch & { isActive?: boolean; createdDate?: string }>;
        const isActive = item?.isActive ?? String(item?.status ?? 'Active').toLowerCase() !== 'inactive';

        return {
          id: Number(item?.id ?? 0),
          name: safeString(item?.name),
          code: safeString(item?.code),
          address: safeString(item?.address),
          phone: safeString(item?.phone),
          email: safeString(item?.email),
          businessId: Number(item?.businessId ?? 0),
          businessName: safeString(item?.businessName),
          countryId: Number(item?.countryId ?? 0),
          countryName: safeString(item?.countryName),
          cityId: Number(item?.cityId ?? 0),
          cityName: safeString(item?.cityName),
          status: isActive ? 'Active' : 'Inactive',
          createdAt: safeString(item?.createdAt ?? item?.createdDate),
        };
      })
      .filter((branch) => branch.id > 0);
  };

  const fetchBranches = useCallback(async () => {
    setLoading(true);
    try {
      const response = await BranchService.getAll({
        page: currentPage,
        pageSize,
        search: searchTerm,
        sortBy: sortColumn,
        sortDirection,
      });
      const payload = response?.data as { branches?: unknown[]; totalRecords?: number; totalPages?: number } | unknown[];
      const rows = Array.isArray(payload)
        ? payload
        : Array.isArray(payload?.branches)
          ? payload.branches
          : [];

      setBranches(normalizeBranches(rows));
      if (Array.isArray(payload)) {
        setTotalRecords(rows.length);
        setTotalPages(Math.max(1, Math.ceil(rows.length / pageSize)));
      } else {
        setTotalRecords(Number(payload?.totalRecords ?? rows.length));
        setTotalPages(Number(payload?.totalPages ?? 1));
      }
    } catch (err) {
      console.error('Failed to load branches:', err);
      setBranches([]);
      setTotalRecords(0);
      setTotalPages(0);
      showNotification('error', getApiErrorMessage(err, 'Failed to load branches.'));
    } finally {
      setLoading(false);
    }
  }, [currentPage, pageSize, searchTerm, sortColumn, sortDirection, showNotification]);

  useEffect(() => {
    const timer = setTimeout(() => {
      void fetchBranches();
    }, searchTerm ? 300 : 0);
    return () => clearTimeout(timer);
  }, [fetchBranches, searchTerm]);

  useEffect(() => {
    setCurrentPage(1);
  }, [pageSize]);

  useEffect(() => {
    if (!isOpen) {
      fetchBranches();
    }
  }, [isOpen, fetchBranches]);

  const handleAddBranch = () => {
    openForm('branch');
  };

  const handleEditBranch = async (branch: Branch) => {
    try {
      const response = await BranchService.getById(branch.id, branch.businessId);
      const detail = response?.data ?? branch;
      openForm('branch', {
        ...detail,
        status: detail?.isActive ? 'Active' : 'Inactive',
      });
    } catch (error) {
      console.error('Failed to load branch details:', error);
      openForm('branch', {
        id: branch.id,
        name: branch.name,
        code: branch.code,
        address: branch.address,
        phone: branch.phone,
        email: branch.email,
        businessId: branch.businessId,
        countryId: branch.countryId,
        cityId: branch.cityId,
        status: branch.status,
        isActive: branch.status === 'Active',
      });
    }
  };

  const handleDeleteBranch = (branch: Branch) => {
    showConfirm({
      title: 'Delete Branch?',
      message: 'This branch will be removed from the system. If it is used by related records, deletion may be blocked.',
      highlightText: branch.name,
      variant: 'danger',
      confirmLabel: 'Yes, Delete',
      cancelLabel: 'Keep Branch',
      onConfirm: async () => {
        try {
          await BranchService.delete(branch.id, branch.businessId);
          await fetchBranches();
          showNotification('success', `Branch "${branch.name}" deleted successfully.`);
        } catch (error: any) {
          console.error('Failed to delete branch:', error);
          const errorMessage = error?.response?.data?.message || 'Failed to delete branch. Please try again.';
          showNotification('error', errorMessage);
        }
      },
    });
  };

  const columns: Column<Branch>[] = [
    {
      key: 'name',
      header: 'Branch Name',
      sortable: true,
    },
    {
      key: 'code',
      header: 'Code',
      sortable: true,
    },
    {
      key: 'businessName',
      header: 'Business',
      sortable: true,
    },
    {
      key: 'address',
      header: 'Address',
      sortable: true,
    },
    {
      key: 'cityName',
      header: 'City',
      sortable: true,
    },
    {
      key: 'countryName',
      header: 'Country',
      sortable: true,
    },
    {
      key: 'phone',
      header: 'Phone',
    },
    {
      key: 'status',
      header: 'Status',
      sortable: true,
      render: (value) => (
        <Badge variant={value === 'Active' ? 'success' : 'danger'} size="sm" dot>
          {safeString(value)}
        </Badge>
      ),
    },
  ];

  const actions: Action<Branch>[] = [
    {
      label: '',
      onClick: handleEditBranch,
      icon: (
        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" title="Edit">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
        </svg>
      ),
      variant: 'secondary',
    },
    {
      label: '',
      onClick: handleDeleteBranch,
      icon: (
        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" title="Delete">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
        </svg>
      ),
      variant: 'danger',
    },
  ];

  return (
    <div>
      {notification && (
        <div
          className={`mb-6 p-4 rounded-md flex items-center ${
            notification.type === 'success'
              ? 'bg-green-50 text-green-800'
              : 'bg-red-50 text-red-800'
          }`}
        >
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
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Branches</h1>
        <p className="text-gray-600">Manage branch locations, business links, and active status</p>
      </div>

      <div className="mb-6 flex justify-between items-center">
        <div></div>
        <button
          onClick={handleAddBranch}
          className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors"
        >
          <svg className="-ml-1 mr-2 h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
          </svg>
          Add Branch
        </button>
      </div>

      <DataTable
        data={branches}
        columns={columns}
        actions={actions}
        loading={loading}
        searchable={true}
        searchPlaceholder="Search by branch, code, business, city, country, or phone..."
        pagination={true}
        pageSize={pageSize}
        pageSizeOptions={[5, 10, 25, 50]}
        onPageSizeChange={(size) => {
          setPageSize(size);
          setCurrentPage(1);
        }}
        emptyMessage="No branches found"
        serverSide
        totalRecords={totalRecords}
        totalPages={totalPages}
        currentPage={currentPage}
        onPageChange={setCurrentPage}
        searchTerm={searchTerm}
        onSearchChange={(value) => {
          setSearchTerm(value);
          setCurrentPage(1);
        }}
        sortColumn={sortColumn}
        sortDirection={sortDirection}
        onSortChange={(column, direction) => {
          setSortColumn(column);
          setSortDirection(direction);
          setCurrentPage(1);
        }}
      />
    </div>
  );
};

export default BranchesList;
