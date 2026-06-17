import React, { useCallback, useEffect, useMemo, useState } from 'react';
import DataTable, { Action, Column } from '../../components/DataTable';
import Badge from '../../components/Badge';
import AuthenticatedImage from '../../components/AuthenticatedImage';
import PermissionGate from '../../components/PermissionGate';
import { useFormModal } from '../../contexts/FormModalContext';
import { useConfirmDialog } from '../../contexts/ConfirmDialogContext';
import { usePermission } from '../../hooks/usePermission';
import { useBranchWriteAccess } from '../../hooks/useBranchWriteAccess';
import { hasBranchContext } from '../../types/permissions';
import { getApiErrorMessage } from '../../services/api';
import { safeString } from '../../utils/safeValues';
import { categoryService } from '../category/categoryService';
import { subCategoryService } from './subcategoryService';

interface CategoryOption {
  id: number;
  name: string;
}

interface SubCategoryItem {
  id: number;
  name: string;
  code: string;
  description: string;
  displayOrder: number;
  status: boolean;
  icon: string;
  hasImage: boolean;
  categoryId: number;
  categoryName: string;
  branchId: number;
  branchName: string;
}

const SubCategoryPage: React.FC = () => {
  const { openForm, isOpen } = useFormModal();
  const { showConfirm } = useConfirmDialog();

  const [categories, setCategories] = useState<CategoryOption[]>([]);
  const [items, setItems] = useState<SubCategoryItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [searchTerm, setSearchTerm] = useState('');
  const [sortColumn, setSortColumn] = useState<string>('name');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [selectedCategoryId, setSelectedCategoryId] = useState<number | null>(null);
  const [statusFilter, setStatusFilter] = useState<boolean | null>(null);
  const [notification, setNotification] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  const { canCreate, canEdit, canDelete } = usePermission('SubCategories');
  const {
    selectedBranchId,
    isMasterUser,
    isGlobalMode,
    canWriteInView,
    resolveEntityBranchId,
    getWriteBlockMessage,
  } = useBranchWriteAccess();

  const hasBranchSelection = hasBranchContext(selectedBranchId);
  const canAdd = canWriteInView && (isMasterUser || canCreate);
  const canModify = canWriteInView && (isMasterUser || canEdit);
  const canRemove = canWriteInView && (isMasterUser || canDelete);

  const showNotification = useCallback((type: 'success' | 'error', message: string) => {
    setNotification({ type, message });
    setTimeout(() => setNotification(null), 4000);
  }, []);

  const normalizeSubCategory = useCallback(
    (row: unknown, fallbackBranchId: number): SubCategoryItem | null => {
      const item = row as Record<string, unknown>;
      const id = Number(item?.id ?? item?.Id ?? 0);
      if (id <= 0) {
        return null;
      }

      const branchId = Number(item?.branchId ?? item?.BranchId ?? fallbackBranchId);

      return {
        id,
        name: safeString(item?.name ?? item?.Name),
        code: safeString(item?.code ?? item?.Code),
        description: safeString(item?.description ?? item?.Description),
        displayOrder: Number(item?.displayOrder ?? item?.DisplayOrder ?? 0),
        status: Boolean(item?.status ?? item?.Status ?? true),
        icon: safeString(item?.icon ?? item?.Icon),
        hasImage: Boolean(item?.hasImage ?? item?.HasImage ?? false),
        categoryId: Number(item?.categoryId ?? item?.CategoryId ?? 0),
        categoryName: safeString(item?.categoryName ?? item?.CategoryName),
        branchId,
        branchName: safeString(item?.branchName ?? item?.BranchName),
      };
    },
    []
  );

  const loadCategories = useCallback(async (branchId: number) => {
    try {
      const response = await categoryService.getAll(branchId, 1, 1000);
      const rows = Array.isArray(response.data?.categories) ? response.data.categories : [];
      setCategories(
        rows.map((row: Record<string, unknown>) => ({
          id: Number(row.id ?? row.Id),
          name: String(row.name ?? row.Name ?? ''),
        }))
      );
    } catch {
      setCategories([]);
    }
  }, []);

  const fetchSubCategories = useCallback(async () => {
    if (!hasBranchSelection || selectedBranchId === null) {
      setItems([]);
      setTotalRecords(0);
      setTotalPages(0);
      return;
    }

    setLoading(true);
    try {
      const response = await subCategoryService.getAll(
        selectedBranchId,
        currentPage,
        pageSize,
        searchTerm.trim() || undefined,
        selectedCategoryId ?? undefined,
        statusFilter,
        sortColumn,
        sortDirection,
      );

      const rows = Array.isArray(response.data?.subCategories) ? response.data.subCategories : [];
      setItems(
        rows
          .map((row) => normalizeSubCategory(row, selectedBranchId))
          .filter((item): item is SubCategoryItem => item !== null)
      );
      setTotalRecords(Number(response.data?.totalRecords ?? 0));
      setTotalPages(Number(response.data?.totalPages ?? 0));
    } catch (error) {
      console.error('Failed to fetch subcategories:', error);
      setItems([]);
      setTotalRecords(0);
      setTotalPages(0);
      showNotification('error', getApiErrorMessage(error, 'Failed to load sub categories.'));
    } finally {
      setLoading(false);
    }
  }, [
    hasBranchSelection,
    selectedBranchId,
    currentPage,
    pageSize,
    searchTerm,
    selectedCategoryId,
    statusFilter,
    sortColumn,
    sortDirection,
    normalizeSubCategory,
    showNotification,
  ]);

  useEffect(() => {
    if (!hasBranchSelection || selectedBranchId === null) {
      setCategories([]);
      return;
    }

    void loadCategories(selectedBranchId);
  }, [hasBranchSelection, selectedBranchId, loadCategories]);

  useEffect(() => {
    const timer = setTimeout(() => {
      void fetchSubCategories();
    }, searchTerm ? 300 : 0);

    return () => clearTimeout(timer);
  }, [fetchSubCategories, searchTerm]);

  useEffect(() => {
    if (!isOpen) {
      return undefined;
    }

    return () => {
      void fetchSubCategories();
    };
  }, [isOpen, fetchSubCategories]);

  useEffect(() => {
    setCurrentPage(1);
  }, [selectedBranchId, selectedCategoryId, statusFilter, pageSize]);

  const handleAddSubCategory = () => {
    const blockMessage = getWriteBlockMessage();
    if (!canAdd || blockMessage) {
      showNotification('error', blockMessage ?? 'You do not have permission to create sub categories.');
      return;
    }

    openForm('subcategory', isGlobalMode ? {} : { branchId: selectedBranchId });
  };

  const handleEdit = async (item: SubCategoryItem) => {
    const blockMessage = getWriteBlockMessage();
    if (!canModify || blockMessage) {
      showNotification('error', blockMessage ?? 'You do not have permission to edit sub categories.');
      return;
    }

    const branchId = resolveEntityBranchId(item.branchId);
    if (branchId <= 0) {
      showNotification('error', 'Unable to determine the branch for this sub category.');
      return;
    }

    try {
      const response = await subCategoryService.getById(item.id, branchId);
      const data = response.data as Record<string, unknown>;
      openForm('subcategory', {
        id: Number(data.id ?? data.Id ?? item.id),
        name: safeString(data.name ?? data.Name ?? item.name),
        code: safeString(data.code ?? data.Code ?? item.code),
        description: safeString(data.description ?? data.Description ?? item.description),
        displayOrder: Number(data.displayOrder ?? data.DisplayOrder ?? item.displayOrder),
        icon: safeString(data.icon ?? data.Icon ?? item.icon),
        status: Boolean(data.status ?? data.Status ?? item.status) ? 'Active' : 'Inactive',
        hasImage: Boolean(data.hasImage ?? data.HasImage ?? item.hasImage),
        categoryId: Number(data.categoryId ?? data.CategoryId ?? item.categoryId),
        branchId: Number(data.branchId ?? data.BranchId ?? item.branchId),
      });
    } catch (error) {
      console.error('Failed to load sub category details:', error);
      openForm('subcategory', {
        ...item,
        status: item.status ? 'Active' : 'Inactive',
      });
    }
  };

  const handleDelete = (item: SubCategoryItem) => {
    const blockMessage = getWriteBlockMessage();
    if (!canRemove || blockMessage) {
      showNotification('error', blockMessage ?? 'You do not have permission to delete sub categories.');
      return;
    }

    const branchId = resolveEntityBranchId(item.branchId);
    if (branchId <= 0) {
      showNotification('error', 'Unable to determine the branch for this sub category.');
      return;
    }

    showConfirm({
      title: 'Delete Sub Category?',
      message:
        'This sub category will be removed from the system. If it is used in products, deletion will be blocked.',
      highlightText: item.name,
      variant: 'danger',
      confirmLabel: 'Yes, Delete',
      cancelLabel: 'Keep Sub Category',
      onConfirm: async () => {
        try {
          await subCategoryService.delete(item.id, branchId);
          await fetchSubCategories();
          showNotification('success', `Sub category "${item.name}" deleted successfully.`);
        } catch (error) {
          console.error('Failed to delete sub category:', error);
          showNotification('error', getApiErrorMessage(error, 'Failed to delete sub category. Please try again.'));
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

  const columns: Column<SubCategoryItem>[] = useMemo(() => {
    const baseColumns: Column<SubCategoryItem>[] = [
      {
        key: 'hasImage',
        header: 'Image',
        render: (_value, item) => {
          const fallback = (
            <div className="flex h-10 w-10 items-center justify-center rounded-md border border-gray-200 bg-gray-100 text-xs font-semibold text-gray-600">
              {item.name.slice(0, 1).toUpperCase()}
            </div>
          );

          if (!item.hasImage) {
            return fallback;
          }

          return (
            <AuthenticatedImage
              endpoint={subCategoryService.getImageEndpoint(item.id)}
              params={{ branchId: item.branchId }}
              alt={`${item.name} image`}
              className="h-10 w-10 rounded-md border border-gray-200 bg-white object-cover"
              fallback={fallback}
            />
          );
        },
      },
      {
        key: 'name',
        header: 'Sub Category Name',
        sortable: true,
      },
      {
        key: 'code',
        header: 'Code',
        sortable: true,
        render: (value) => safeString(value) || '-',
      },
      {
        key: 'categoryName',
        header: 'Category',
        sortable: true,
        render: (value, item) =>
          safeString(value) || categories.find((category) => category.id === item.categoryId)?.name || '-',
      },
    ];

    if (isGlobalMode) {
      baseColumns.push({
        key: 'branchName',
        header: 'Branch',
        sortable: true,
        render: (value) => safeString(value) || '-',
      });
    }

    baseColumns.push(
      {
        key: 'displayOrder',
        header: 'Display Order',
        sortable: true,
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
      }
    );

    return baseColumns;
  }, [categories, isGlobalMode]);

  const actions: Action<SubCategoryItem>[] = [];
  if (canModify) {
    actions.push({
      label: '',
      onClick: (item) => {
        void handleEdit(item);
      },
      icon: (
        <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" title="Edit">
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
        <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" title="Delete">
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

  const emptyMessage = !hasBranchSelection
    ? 'Select a branch to load sub categories.'
    : searchTerm
      ? 'No sub categories match your search.'
      : 'No sub categories found';

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
        <h1 className="mb-2 text-3xl font-bold text-gray-900">Sub Categories</h1>
        <p className="text-gray-600">Manage sub categories by branch and active status</p>
      </div>

      {isGlobalMode && isMasterUser && (
        <div className="mb-6 rounded-md border border-blue-200 bg-blue-50 px-4 py-3 text-sm text-blue-800">
          Global view is active. Choose a target branch in the form when creating records.
        </div>
      )}

      {!hasBranchSelection && (
        <div className="mb-6 rounded-md border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-700">
          Select a branch from the header to load sub categories.
        </div>
      )}

      <div className="mb-6 flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Category Filter</label>
            <select
              value={selectedCategoryId ?? ''}
              onChange={(event) => {
                const value = event.target.value;
                setSelectedCategoryId(value ? Number(value) : null);
              }}
              disabled={!hasBranchSelection}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none disabled:cursor-not-allowed disabled:bg-gray-100"
            >
              <option value="">All Categories</option>
              {categories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Status Filter</label>
            <select
              value={statusFilter === null ? '' : statusFilter ? 'active' : 'inactive'}
              onChange={(event) => {
                const value = event.target.value;
                setStatusFilter(value === '' ? null : value === 'active');
              }}
              disabled={!hasBranchSelection}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none disabled:cursor-not-allowed disabled:bg-gray-100"
            >
              <option value="">All Statuses</option>
              <option value="active">Active</option>
              <option value="inactive">Inactive</option>
            </select>
          </div>
        </div>

        <PermissionGate module="SubCategories" action="create">
          <button
            onClick={handleAddSubCategory}
            disabled={!canAdd || !hasBranchSelection}
            className="inline-flex items-center rounded-md border border-transparent bg-blue-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-60"
          >
            <svg className="-ml-1 mr-2 h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
            </svg>
            Add Sub Category
          </button>
        </PermissionGate>
      </div>

      <DataTable
        data={items}
        columns={columns}
        actions={actions}
        loading={loading}
        searchable
        searchPlaceholder="Search by name, code, or description..."
        pagination
        pageSize={pageSize}
        pageSizeOptions={[5, 10, 25, 50]}
        onPageSizeChange={handlePageSizeChange}
        emptyMessage={emptyMessage}
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

export default SubCategoryPage;
