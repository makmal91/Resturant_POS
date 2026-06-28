import React, { useCallback, useEffect, useState } from 'react';
import DataTable, { Action, Column } from './DataTable';
import Badge from './Badge';
import AuthenticatedImage from './AuthenticatedImage';
import PermissionGate from './PermissionGate';
import { useFormModal } from '../contexts/FormModalContext';
import { useConfirmDialog } from '../contexts/ConfirmDialogContext';
import { BusinessService } from '../services/apiService';
import { getApiErrorMessage } from '../services/api';
import { safeString } from '../utils/safeValues';
import { useModuleCrudAccess } from '../hooks/useModuleCrudAccess';
import { getPermissionDeniedMessage } from '../utils/permissionUtils';

interface Business {
  id: number;
  name: string;
  legalName: string;
  phone: string;
  email: string;
  timeZone: string;
  isActive: boolean;
  hasLogo: boolean;
}

interface PagedBusinessResponse {
  data: Business[];
  totalRecords: number;
  totalPages: number;
  currentPage: number;
}

const BusinessesList: React.FC = () => {
  const [businesses, setBusinesses] = useState<Business[]>([]);
  const [loading, setLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(5);
  const [searchTerm, setSearchTerm] = useState('');
  const [sortColumn, setSortColumn] = useState<string>('name');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [notification, setNotification] = useState<{ type: 'success' | 'error'; message: string } | null>(null);
  const { openForm, isOpen } = useFormModal();
  const { showConfirm } = useConfirmDialog();
  const { canAdd, canModify, canRemove } = useModuleCrudAccess('Businesses', {
    requireBranchWrite: false,
  });

  const normalizeBusiness = (row: unknown): Business | null => {
    const item = row as Partial<Business>;
    const id = Number(item?.id ?? 0);
    if (id <= 0) {
      return null;
    }

    return {
      id,
      name: safeString(item?.name),
      legalName: safeString(item?.legalName),
      phone: safeString(item?.phone),
      email: safeString(item?.email),
      timeZone: safeString(item?.timeZone, 'UTC') || 'UTC',
      isActive: Boolean(item?.isActive ?? true),
      hasLogo: Boolean(item?.hasLogo ?? false),
    };
  };

  const showNotification = useCallback((type: 'success' | 'error', message: string) => {
    setNotification({ type, message });
    setTimeout(() => {
      setNotification(null);
    }, 4000);
  }, []);

  const fetchBusinesses = useCallback(async () => {
    setLoading(true);
    try {
      const response = await BusinessService.getAll({
        page: currentPage,
        pageSize,
        search: searchTerm,
        sortBy: sortColumn,
        sortDirection,
      });
      const payload = response?.data as PagedBusinessResponse | Business[];

      const rows = Array.isArray(payload)
        ? payload
        : Array.isArray(payload?.data)
          ? payload.data
          : [];

      setBusinesses(rows.map(normalizeBusiness).filter((business): business is Business => business !== null));

      if (Array.isArray(payload)) {
        setTotalRecords(rows.length);
        setTotalPages(Math.max(1, Math.ceil(rows.length / pageSize)));
      } else {
        setTotalRecords(Number(payload?.totalRecords ?? rows.length));
        setTotalPages(Number(payload?.totalPages ?? 1));
      }
    } catch (error) {
      console.error('Failed to fetch businesses:', error);
      setBusinesses([]);
      setTotalRecords(0);
      setTotalPages(0);
      showNotification('error', getApiErrorMessage(error, 'Failed to load businesses.'));
    } finally {
      setLoading(false);
    }
  }, [currentPage, pageSize, searchTerm, sortColumn, sortDirection, showNotification]);

  useEffect(() => {
    const timer = setTimeout(() => {
      fetchBusinesses();
    }, searchTerm ? 300 : 0);

    return () => clearTimeout(timer);
  }, [fetchBusinesses, searchTerm]);

  useEffect(() => {
    if (!isOpen) {
      return undefined;
    }

    return () => {
      fetchBusinesses();
    };
  }, [isOpen, fetchBusinesses]);

  const handleAddBusiness = () => {
    if (!canAdd) {
      showNotification('error', getPermissionDeniedMessage('create', 'Businesses'));
      return;
    }
    openForm('business');
  };

  const handleEdit = async (business: Business) => {
    if (!canModify) {
      showNotification('error', getPermissionDeniedMessage('edit', 'Businesses'));
      return;
    }
    try {
      const response = await BusinessService.getById(business.id);
      const detail = response?.data ?? business;
      openForm('business', {
        ...detail,
        status: detail?.isActive ? 'Active' : 'Inactive',
      });
    } catch (error) {
      console.error('Failed to load business details:', error);
      openForm('business', {
        ...business,
        status: business.isActive ? 'Active' : 'Inactive',
      });
    }
  };

  const handleDelete = (business: Business) => {
    if (!canRemove) {
      showNotification('error', getPermissionDeniedMessage('delete', 'Businesses'));
      return;
    }
    showConfirm({
      title: 'Delete Business?',
      message: 'All business data will be permanently removed from the system. If this business has branches, deletion will be blocked.',
      highlightText: business.name,
      variant: 'danger',
      confirmLabel: 'Yes, Delete',
      cancelLabel: 'Keep Business',
      onConfirm: async () => {
        try {
          await BusinessService.delete(business.id);
          await fetchBusinesses();
          showNotification('success', `Business "${business.name}" deleted successfully.`);
        } catch (error: any) {
          console.error('Failed to delete business:', error);
          const errorMessage = error?.response?.data?.message || 'Failed to delete business. Please try again.';
          showNotification('error', errorMessage);
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

  const columns: Column<Business>[] = [
    {
      key: 'hasLogo',
      header: 'Logo',
      render: (_value, item) => {
        const fallback = (
          <div className="h-10 w-10 rounded-md border border-gray-200 bg-gray-100 flex items-center justify-center text-xs font-semibold text-gray-600">
            {item.name.slice(0, 1).toUpperCase()}
          </div>
        );

        if (!item.hasLogo) {
          return fallback;
        }

        return (
          <AuthenticatedImage
            endpoint={`/businesses/${item.id}/logo`}
            alt={`${item.name} logo`}
            className="h-10 w-10 rounded-md border border-gray-200 object-contain bg-white"
            fallback={fallback}
          />
        );
      },
    },
    {
      key: 'name',
      header: 'Business Name',
      sortable: true,
    },
    {
      key: 'legalName',
      header: 'Legal Name',
      sortable: true,
    },
    {
      key: 'email',
      header: 'Email',
      sortable: true,
    },
    {
      key: 'phone',
      header: 'Phone',
      sortable: true,
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

  const actions: Action<Business>[] = [
    ...(canModify
      ? [{
          label: '',
          onClick: handleEdit,
          icon: (
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" title="Edit">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
            </svg>
          ),
          variant: 'secondary' as const,
        }]
      : []),
    ...(canRemove
      ? [{
          label: '',
          onClick: handleDelete,
          icon: (
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" title="Delete">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
            </svg>
          ),
          variant: 'danger' as const,
        }]
      : []),
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
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Businesses</h1>
        <p className="text-gray-600">Manage company records and active status</p>
      </div>

      <div className="mb-6 flex justify-between items-center">
        <div></div>
        <PermissionGate module="Businesses" action="create">
          <button
            onClick={handleAddBusiness}
            disabled={!canAdd}
            className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors disabled:cursor-not-allowed disabled:opacity-60"
          >
            <svg className="-ml-1 mr-2 h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
            </svg>
            Add Business
          </button>
        </PermissionGate>
      </div>

      <DataTable
        data={businesses}
        columns={columns}
        actions={actions}
        loading={loading}
        searchable={true}
        searchPlaceholder="Search by name, legal name, email, or phone..."
        pagination={true}
        pageSize={pageSize}
        pageSizeOptions={[5, 10, 25, 50]}
        onPageSizeChange={handlePageSizeChange}
        emptyMessage={searchTerm ? 'No businesses match your search.' : 'No businesses found'}
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

export default BusinessesList;
