import React, { useState, useEffect } from 'react';
import DataTable, { Column, Action } from './DataTable';
import Badge from './Badge';
import { useFormModal } from '../contexts/FormModalContext';
import { useConfirmDialog } from '../contexts/ConfirmDialogContext';

interface InventoryItem {
  id: number;
  itemName: string;
  unit: string;
  stock: number;
  minLevel: number;
  status: string;
}

const InventoryList: React.FC = () => {
  const [items, setItems] = useState<InventoryItem[]>([]);
  const [loading, setLoading] = useState(true);
  const { openForm } = useFormModal();
  const { showConfirm } = useConfirmDialog();

  useEffect(() => {
    setTimeout(() => {
      setItems([
        {
          id: 1,
          itemName: 'Tomato',
          unit: 'kg',
          stock: 50,
          minLevel: 10,
          status: 'In Stock'
        },
        {
          id: 2,
          itemName: 'Chicken Breast',
          unit: 'kg',
          stock: 8,
          minLevel: 15,
          status: 'Low Stock'
        },
        {
          id: 3,
          itemName: 'Olive Oil',
          unit: 'L',
          stock: 2,
          minLevel: 5,
          status: 'Critical'
        },
      ]);
      setLoading(false);
    }, 1000);
  }, []);

  const getStockStatus = (stock: number, minLevel: number): 'In Stock' | 'Low Stock' | 'Critical' => {
    if (stock < minLevel / 2) return 'Critical';
    if (stock < minLevel) return 'Low Stock';
    return 'In Stock';
  };

  const columns: Column<InventoryItem>[] = [
    {
      key: 'itemName',
      header: 'Item Name',
      sortable: true,
    },
    {
      key: 'unit',
      header: 'Unit',
      render: (value) => (
        <Badge variant="secondary" size="sm">{value}</Badge>
      ),
    },
    {
      key: 'stock',
      header: 'Current Stock',
      render: (value) => (
        <span className="font-medium text-gray-900">{value}</span>
      ),
      sortable: true,
    },
    {
      key: 'minLevel',
      header: 'Min Level',
      render: (value) => (
        <span className="text-gray-600">{value}</span>
      ),
    },
    {
      key: 'status',
      header: 'Status',
      render: (value) => {
        let variant: 'success' | 'warning' | 'danger' = 'success';
        if (value === 'Low Stock') variant = 'warning';
        if (value === 'Critical') variant = 'danger';
        return (
          <Badge variant={variant} size="sm" dot>
            {value}
          </Badge>
        );
      },
    },
  ];

  const handleAddItem = () => {
    openForm('inventory');
  };

  const handleEditItem = (item: InventoryItem) => {
    openForm('inventory', item);
  };

  const handleDeleteItem = (item: InventoryItem) => {
    showConfirm({
      title: 'Delete Inventory Item',
      message: `Are you sure you want to delete "${item.itemName}"? This action cannot be undone.`,
      confirmLabel: 'Delete',
      cancelLabel: 'Cancel',
      onConfirm: async () => {
        try {
          await InventoryService.delete(item.id);
          setItems(items.filter(i => i.id !== item.id));
        } catch (error) {
          console.error('Failed to delete inventory item:', error);
          alert('Failed to delete inventory item. Please try again.');
        }
      }
    });
  };

  const actions: Action<InventoryItem>[] = [
    {
      label: 'Edit',
      onClick: handleEditItem,
      variant: 'primary',
    },
    {
      label: 'Delete',
      onClick: handleDeleteItem,
      variant: 'danger',
    },
  ];

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Inventory</h1>
        <p className="text-gray-600">Track and manage inventory levels</p>
      </div>

      <div className="mb-6 flex justify-end">
        <button
          onClick={handleAddItem}
          className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors"
        >
          <svg className="-ml-1 mr-2 h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
          </svg>
          Add Item
        </button>
      </div>

      <DataTable
        data={items}
        columns={columns}
        actions={actions}
        loading={loading}
        searchable={true}
        pagination={true}
        pageSize={10}
      />
    </div>
  );
};

export default InventoryList;