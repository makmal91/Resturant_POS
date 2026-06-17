import React, { useCallback, useEffect, useState } from 'react';
import DataTable, { Column, Action } from './DataTable';
import Badge from './Badge';
import { useFormModal } from '../contexts/FormModalContext';
import { useConfirmDialog } from '../contexts/ConfirmDialogContext';
import { InventoryService } from '../services/apiService';
import { useBranchStore } from '../stores/useBranchStore';
import { getApiErrorMessage } from '../services/api';

interface InventoryItem {
  id: number;
  itemName: string;
  unit: string;
  stock: number;
  minLevel: number;
  status: string;
  productType: string;
}

const InventoryList: React.FC = () => {
  const selectedBranchId = useBranchStore((state) => state.selectedBranchId);
  const branchId = selectedBranchId !== null && selectedBranchId > 0 ? selectedBranchId : 0;

  const [items, setItems] = useState<InventoryItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [searchTerm, setSearchTerm] = useState('');
  const [sortColumn, setSortColumn] = useState('itemName');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const { openForm } = useFormModal();
  const { showConfirm } = useConfirmDialog();

  const getStockStatus = (stock: number, minLevel: number): 'In Stock' | 'Low Stock' | 'Critical' => {
    if (stock < minLevel / 2) return 'Critical';
    if (stock < minLevel) return 'Low Stock';
    return 'In Stock';
  };

  const normalizeInventoryItems = (payload: unknown): InventoryItem[] => {
    const source = payload as { items?: unknown[] } | undefined;
    const rawItems = Array.isArray(source?.items) ? source.items : [];

    return rawItems
      .map((raw): InventoryItem => {
        const stock = Number((raw as Record<string, unknown>)?.currentStock ?? 0);
        const minLevel = Number((raw as Record<string, unknown>)?.minStockLevel ?? 0);

        return {
          id: Number((raw as Record<string, unknown>)?.id ?? 0),
          itemName: String((raw as Record<string, unknown>)?.name ?? ''),
          unit: String((raw as Record<string, unknown>)?.unit ?? ''),
          stock,
          minLevel,
          status: getStockStatus(stock, minLevel),
          productType: String((raw as Record<string, unknown>)?.productType ?? ''),
        };
      })
      .filter((item) => item.id > 0)
      .filter((item) => item.productType === 'RawMaterial' || item.productType === 'SemiFinished');
  };

  const loadInventory = useCallback(async () => {
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
      const response = await InventoryService.getAll(branchId, {
        page: currentPage,
        pageSize,
        search: searchTerm,
        sortBy: sortColumn === 'itemName' ? 'name' : sortColumn,
        sortDirection,
      });
      setItems(normalizeInventoryItems(response?.data));
      setTotalRecords(Number(response?.data?.totalRecords ?? 0));
      setTotalPages(Number(response?.data?.totalPages ?? 0));
    } catch (err) {
      console.error('Failed to fetch inventory items:', err);
      setItems([]);
      setTotalRecords(0);
      setTotalPages(0);
      setError(getApiErrorMessage(err, 'Failed to load inventory items.'));
    } finally {
      setLoading(false);
    }
  }, [branchId, currentPage, pageSize, searchTerm, sortColumn, sortDirection]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void loadInventory();
    }, searchTerm ? 300 : 0);
    return () => window.clearTimeout(timer);
  }, [loadInventory, searchTerm]);

  useEffect(() => {
    setCurrentPage(1);
  }, [branchId, pageSize]);

  const columns: Column<InventoryItem>[] = [
    {
      key: 'itemName',
      header: 'Item Name',
      sortable: true,
    },
    {
      key: 'unit',
      header: 'Unit',
      sortable: true,
    },
    {
      key: 'stock',
      header: 'Current Stock',
      sortable: true,
    },
    {
      key: 'minLevel',
      header: 'Min Level',
      sortable: true,
    },
    {
      key: 'status',
      header: 'Status',
      render: (value) => (
        <Badge
          variant={value === 'In Stock' ? 'success' : value === 'Low Stock' ? 'warning' : 'danger'}
          size="sm"
          dot
        >
          {String(value)}
        </Badge>
      ),
    },
    {
      key: 'productType',
      header: 'Type',
      sortable: true,
    },
  ];

  const actions: Action<InventoryItem>[] = [
    {
      label: 'Adjust',
      onClick: (item) => {
        openForm('inventory', {
          id: item.id,
          itemName: item.itemName,
          branchId,
        });
      },
      variant: 'primary',
    },
    {
      label: 'Purchase',
      onClick: (item) => {
        openForm('inventoryPurchase', {
          itemId: item.id,
          itemName: item.itemName,
          branchId,
        });
      },
      variant: 'secondary',
    },
  ];

  return (
    <div>
      <div className="mb-8">
        <h1 className="mb-2 text-3xl font-bold text-gray-900">Inventory</h1>
        <p className="text-gray-600">Track raw materials and semi-finished goods</p>
      </div>

      {branchId <= 0 && (
        <div className="mb-4 rounded-lg border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-700">
          Select a branch from the header to load inventory.
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
        searchPlaceholder="Search inventory items..."
        pagination={branchId > 0}
        pageSize={pageSize}
        pageSizeOptions={[5, 10, 25, 50]}
        onPageSizeChange={(size) => {
          setPageSize(size);
          setCurrentPage(1);
        }}
        emptyMessage={branchId <= 0 ? 'Select a branch to load inventory.' : 'No inventory items found.'}
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

export default InventoryList;
