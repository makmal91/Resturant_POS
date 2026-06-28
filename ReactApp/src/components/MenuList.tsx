import React, { useCallback, useEffect, useState } from 'react';
import DataTable, { Column, Action } from './DataTable';
import Badge from './Badge';
import { useFormModal } from '../contexts/FormModalContext';
import { useModuleCrudAccess } from '../hooks/useModuleCrudAccess';
import PermissionGate from './PermissionGate';
import { getApiErrorMessage } from '../services/api';
import { productService } from '../modules/product/productService';

interface MenuItem {
  id: number;
  name: string;
  category: string;
  price: number;
  variants: number;
  status: string;
}

const MenuList: React.FC = () => {
  const { canAdd, canModify, selectedBranchId } = useModuleCrudAccess('Menu');
  const branchId = selectedBranchId !== null && selectedBranchId > 0 ? selectedBranchId : 0;

  const [items, setItems] = useState<MenuItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [searchTerm, setSearchTerm] = useState('');
  const [sortColumn, setSortColumn] = useState('productName');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [error, setError] = useState('');
  const { openForm } = useFormModal();

  const loadMenuItems = useCallback(async () => {
    if (branchId <= 0) {
      setItems([]);
      setTotalRecords(0);
      setTotalPages(0);
      setLoading(false);
      return;
    }

    setLoading(true);
    setError('');
    try {
      const response = await productService.getAll(branchId, currentPage, pageSize, {
        search: searchTerm.trim() || undefined,
        sortBy: sortColumn === 'name' ? 'productName' : sortColumn === 'price' ? 'sellingPrice' : sortColumn,
        sortDirection,
        status: true,
      });

      const rows = Array.isArray(response.data?.products) ? response.data.products : [];
      setItems(
        rows.map((row: Record<string, unknown>) => ({
          id: Number(row.id ?? 0),
          name: String(row.productName ?? row.name ?? ''),
          category: String(row.categoryName ?? row.category ?? '-'),
          price: Number(row.sellingPrice ?? row.price ?? 0),
          variants: Number(row.variantCount ?? row.variants ?? 0),
          status: Boolean(row.status ?? row.isActive ?? true) ? 'Available' : 'Unavailable',
        })).filter((item) => item.id > 0),
      );
      setTotalRecords(Number(response.data?.totalRecords ?? 0));
      setTotalPages(Number(response.data?.totalPages ?? 0));
    } catch (err) {
      setItems([]);
      setTotalRecords(0);
      setTotalPages(0);
      setError(getApiErrorMessage(err, 'Failed to load menu items.'));
    } finally {
      setLoading(false);
    }
  }, [branchId, currentPage, pageSize, searchTerm, sortColumn, sortDirection]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void loadMenuItems();
    }, searchTerm ? 300 : 0);
    return () => window.clearTimeout(timer);
  }, [loadMenuItems, searchTerm]);

  useEffect(() => {
    setCurrentPage(1);
  }, [branchId, pageSize]);

  const columns: Column<MenuItem>[] = [
    { key: 'name', header: 'Item Name', sortable: true },
    {
      key: 'category',
      header: 'Category',
      render: (value) => <Badge variant="secondary" size="sm">{String(value)}</Badge>,
    },
    {
      key: 'price',
      header: 'Price',
      sortable: true,
      render: (value) => `$${Number(value ?? 0).toFixed(2)}`,
    },
    { key: 'variants', header: 'Variants', sortable: true },
    {
      key: 'status',
      header: 'Status',
      sortable: true,
      render: (value) => (
        <Badge variant={value === 'Available' ? 'success' : 'danger'} size="sm" dot>
          {String(value)}
        </Badge>
      ),
    },
  ];

  const actions: Action<MenuItem>[] = canModify
    ? [{
        label: 'Edit',
        onClick: (item) => openForm('product', { id: item.id, branchId }),
        variant: 'primary',
      }]
    : [];

  return (
    <div>
      <div className="mb-8 flex items-start justify-between">
        <div>
          <h1 className="mb-2 text-3xl font-bold text-gray-900">Menu Items</h1>
          <p className="text-gray-600">Browse and manage saleable products</p>
        </div>
        <PermissionGate module="Menu" action="create">
          <button
            type="button"
            onClick={() => {
              if (!canAdd) return;
              openForm('product', { branchId });
            }}
            disabled={branchId <= 0 || !canAdd}
            className="inline-flex items-center rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
          >
            Add Item
          </button>
        </PermissionGate>
      </div>

      {branchId <= 0 && (
        <div className="mb-4 rounded-lg border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-700">
          Select a branch from the header to load menu items.
        </div>
      )}

      {error && (
        <div className="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </div>
      )}

      <DataTable
        data={items}
        columns={columns}
        actions={actions}
        loading={loading}
        searchable={branchId > 0}
        searchPlaceholder="Search menu items..."
        pagination={branchId > 0}
        pageSize={pageSize}
        pageSizeOptions={[5, 10, 25, 50]}
        onPageSizeChange={(size) => {
          setPageSize(size);
          setCurrentPage(1);
        }}
        emptyMessage={branchId <= 0 ? 'Select a branch to load menu items.' : 'No menu items found.'}
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

export default MenuList;
