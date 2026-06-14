import React, { useCallback, useEffect, useMemo, useState } from 'react';
import DataTable, { Action, Column } from '../../components/DataTable';
import Badge from '../../components/Badge';
import BranchSelector from '../shared/BranchSelector';
import { useBranchStore } from '../../stores/useBranchStore';
import { useFormModal } from '../../contexts/FormModalContext';
import { useConfirmDialog } from '../../contexts/ConfirmDialogContext';
import { usePermission, useIsMasterUser } from '../../hooks/usePermission';
import PermissionGate from '../../components/PermissionGate';
import { getApiErrorMessage } from '../../services/api';
import { safeString } from '../../utils/safeValues';
import { categoryService } from './categoryService';

interface CategoryItem {
  id: number;
  name: string;
  code: string;
  description: string;
  displayOrder: number;
  imageUrl: string;
  hasImage: boolean;
  icon: string;
  color: string;
  status: boolean;
  categoryType: 'Sale' | 'Inventory';
  branchId: number;
  branchName: string;
}

const CategoryPage: React.FC = () => {
  const branches = useBranchStore((state) => state.branches);
  const selectedBranchId = useBranchStore((state) => state.selectedBranchId);
  const fetchBranches = useBranchStore((state) => state.fetchBranches);
  const { openForm, isOpen } = useFormModal();
  const { showConfirm } = useConfirmDialog();

  const [categories, setCategories] = useState<CategoryItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [pageSize, setPageSize] = useState(10);
  const [notification, setNotification] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  const { canCreate, canEdit, canDelete } = usePermission('Categories');
  const isMasterUser = useIsMasterUser();

  const hasBranchSelection = selectedBranchId !== null;
  const isGlobalMode = selectedBranchId === 0;
  const canWriteBranch = isMasterUser || (hasBranchSelection && !isGlobalMode);
  const canAdd = canWriteBranch && canCreate;
  const canModify = canWriteBranch && canEdit;
  const canRemove = canWriteBranch && canDelete;

  const showNotification = useCallback((type: 'success' | 'error', message: string) => {
    setNotification({ type, message });
    setTimeout(() => {
      setNotification(null);
    }, 4000);
  }, []);

  const normalizeCategory = useCallback(
    (row: unknown, fallbackBranchId: number): CategoryItem | null => {
      const item = row as Record<string, unknown>;
      const id = Number(item?.id ?? item?.Id ?? 0);
      if (id <= 0) {
        return null;
      }

      const branchId = Number(item?.branchId ?? item?.BranchId ?? fallbackBranchId);
      const branchName =
        safeString(item?.branchName ?? item?.BranchName) ||
        safeString(branches.find((branch) => branch.id === branchId)?.name);

      return {
        id,
        name: safeString(item?.name ?? item?.Name),
        code: safeString(item?.code ?? item?.Code),
        description: safeString(item?.description ?? item?.Description),
        displayOrder: Number(item?.displayOrder ?? item?.DisplayOrder ?? 0),
        imageUrl: safeString(item?.imageUrl ?? item?.ImageUrl),
        hasImage: Boolean(item?.hasImage ?? item?.HasImage ?? false),
        icon: safeString(item?.icon ?? item?.Icon),
        color: safeString(item?.color ?? item?.Color, '#2563eb') || '#2563eb',
        status: Boolean(item?.status ?? item?.Status ?? true),
        categoryType: (String(item?.categoryType ?? item?.CategoryType ?? 'Sale') === 'Inventory'
          ? 'Inventory'
          : 'Sale') as 'Sale' | 'Inventory',
        branchId,
        branchName,
      };
    },
    [branches]
  );

  const fetchCategories = useCallback(async () => {
    if (selectedBranchId === null) {
      setCategories([]);
      return;
    }

    setLoading(true);
    try {
      const response = await categoryService.getAll(selectedBranchId, 1, 1000);
      const rows = Array.isArray(response.data?.categories)
        ? response.data.categories
        : Array.isArray(response.data?.data)
          ? response.data.data
          : [];

      setCategories(
        rows
          .map((row) => normalizeCategory(row, selectedBranchId))
          .filter((category): category is CategoryItem => category !== null)
      );
    } catch (error) {
      console.error('Failed to fetch categories:', error);
      setCategories([]);
      showNotification('error', getApiErrorMessage(error, 'Failed to load categories.'));
    } finally {
      setLoading(false);
    }
  }, [selectedBranchId, normalizeCategory, showNotification]);

  useEffect(() => {
    void fetchBranches();
  }, [fetchBranches]);

  useEffect(() => {
    void fetchCategories();
  }, [fetchCategories]);

  useEffect(() => {
    if (!isOpen) {
      return undefined;
    }

    return () => {
      void fetchCategories();
    };
  }, [isOpen, fetchCategories]);

  const handleAddCategory = () => {
    if (!canAdd || selectedBranchId === null || selectedBranchId <= 0) {
      showNotification(
        'error',
        isGlobalMode
          ? 'Global mode is read-only. Select a branch to create categories.'
          : 'Please select a branch first.'
      );
      return;
    }

    openForm('category', { branchId: selectedBranchId });
  };

  const handleEdit = async (category: CategoryItem) => {
    if (!canModify || selectedBranchId === null || selectedBranchId <= 0) {
      showNotification(
        'error',
        isGlobalMode
          ? 'Global mode is read-only. Select a branch to edit categories.'
          : 'Please select a branch first.'
      );
      return;
    }

    try {
      const branchId = category.branchId > 0 ? category.branchId : selectedBranchId;
      const response = await categoryService.getById(category.id, branchId);
      const data = response.data as Record<string, unknown>;
      openForm('category', {
        id: Number(data.id ?? data.Id ?? category.id),
        name: safeString(data.name ?? data.Name ?? category.name),
        code: safeString(data.code ?? data.Code ?? category.code),
        description: safeString(data.description ?? data.Description ?? category.description),
        displayOrder: Number(data.displayOrder ?? data.DisplayOrder ?? category.displayOrder),
        imageUrl: safeString(data.imageUrl ?? data.ImageUrl ?? category.imageUrl),
        hasImage: Boolean(data.hasImage ?? data.HasImage ?? category.hasImage),
        icon: safeString(data.icon ?? data.Icon ?? category.icon),
        color: safeString(data.color ?? data.Color ?? category.color, '#2563eb') || '#2563eb',
        status: Boolean(data.status ?? data.Status ?? category.status) ? 'Active' : 'Inactive',
        categoryType: String(data.categoryType ?? data.CategoryType ?? category.categoryType),
        branchId: Number(data.branchId ?? data.BranchId ?? category.branchId),
      });
    } catch (error) {
      console.error('Failed to load category details:', error);
      showNotification('error', getApiErrorMessage(error, 'Failed to load category details.'));
    }
  };

  const handleDelete = (category: CategoryItem) => {
    if (!canRemove || selectedBranchId === null || selectedBranchId <= 0) {
      showNotification(
        'error',
        isGlobalMode
          ? 'Global mode is read-only. Select a branch to delete categories.'
          : 'Please select a branch first.'
      );
      return;
    }

    showConfirm({
      title: 'Delete Category?',
      message:
        'This category will be removed from the system. If it has products or subcategories, deletion will be blocked.',
      highlightText: category.name,
      variant: 'danger',
      confirmLabel: 'Yes, Delete',
      cancelLabel: 'Keep Category',
      onConfirm: async () => {
        try {
          const branchId = category.branchId > 0 ? category.branchId : selectedBranchId;
          await categoryService.delete(category.id, branchId);
          await fetchCategories();
          showNotification('success', `Category "${category.name}" deleted successfully.`);
        } catch (error: unknown) {
          console.error('Failed to delete category:', error);
          showNotification('error', getApiErrorMessage(error, 'Failed to delete category. Please try again.'));
        }
      },
    });
  };

  const columns: Column<CategoryItem>[] = useMemo(() => {
    const baseColumns: Column<CategoryItem>[] = [
      {
        key: 'imageUrl',
        header: 'Image',
        render: (_value, item) => {
          const externalImageUrl =
            item.imageUrl && !item.imageUrl.startsWith('data:') ? item.imageUrl : '';
          const imageSrc = item.hasImage
            ? categoryService.getImageUrl(item.id, item.branchId)
            : externalImageUrl;

          return imageSrc ? (
            <img
              src={imageSrc}
              alt={`${item.name} image`}
              className="h-10 w-10 rounded-md border border-gray-200 object-cover bg-white"
            />
          ) : (
            <div
              className="h-10 w-10 rounded-md border border-gray-200 flex items-center justify-center text-xs font-semibold text-white"
              style={{ backgroundColor: item.color }}
            >
              {item.name.slice(0, 1).toUpperCase()}
            </div>
          );
        },
      },
      {
        key: 'name',
        header: 'Category Name',
        sortable: true,
      },
      {
        key: 'code',
        header: 'Code',
        sortable: true,
        render: (value) => safeString(value) || '-',
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
        key: 'categoryType',
        header: 'Type',
        sortable: true,
        render: (value) => (
          <Badge variant={value === 'Inventory' ? 'warning' : 'info'} size="sm">
            {safeString(value)}
          </Badge>
        ),
      },
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
  }, [isGlobalMode]);

  const actions: Action<CategoryItem>[] = [];
  if (canModify) {
    actions.push({
      label: '',
      onClick: handleEdit,
      icon: (
        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" title="Edit">
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
        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" title="Delete">
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
    ? 'Select a branch to load categories.'
    : isGlobalMode
      ? 'No categories found across branches.'
      : 'No categories found';

  return (
    <div>
      {notification && (
        <div
          className={`mb-6 p-4 rounded-md flex items-center ${
            notification.type === 'success' ? 'bg-green-50 text-green-800' : 'bg-red-50 text-red-800'
          }`}
        >
          {notification.type === 'success' ? (
            <svg className="w-5 h-5 mr-3" fill="currentColor" viewBox="0 0 20 20">
              <path
                fillRule="evenodd"
                d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                clipRule="evenodd"
              />
            </svg>
          ) : (
            <svg className="w-5 h-5 mr-3" fill="currentColor" viewBox="0 0 20 20">
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
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Categories</h1>
        <p className="text-gray-600">Manage menu categories by branch and active status</p>
      </div>

      <div className="mb-6 max-w-md">
        <BranchSelector />
        {selectedBranchId === null && (
          <p className="mt-2 text-sm text-amber-700">Select a branch to load categories.</p>
        )}
        {isGlobalMode && (
          <p className="mt-2 text-sm text-blue-700">
            Global mode shows all branches and disables create, edit, and delete.
          </p>
        )}
      </div>

      <div className="mb-6 flex justify-between items-center">
        <div></div>
        <PermissionGate module="Categories" action="create">
          <button
            onClick={handleAddCategory}
            disabled={!canAdd}
            className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors disabled:cursor-not-allowed disabled:opacity-60"
          >
            <svg className="-ml-1 mr-2 h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
            </svg>
            Add Category
          </button>
        </PermissionGate>
      </div>

      <DataTable
        data={categories}
        columns={columns}
        actions={actions}
        loading={loading}
        searchable={hasBranchSelection}
        searchPlaceholder="Search by name, code, branch, or type..."
        pagination={hasBranchSelection}
        pageSize={pageSize}
        pageSizeOptions={[5, 10, 25, 50]}
        onPageSizeChange={setPageSize}
        emptyMessage={emptyMessage}
      />
    </div>
  );
};

export default CategoryPage;
