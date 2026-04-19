import React, { useState, useEffect } from 'react';
import DataTable, { Column, Action } from './DataTable';
import Badge from './Badge';
import { useFormModal } from '../contexts/FormModalContext';
import { useConfirmDialog } from '../contexts/ConfirmDialogContext';
import { InventoryService } from '../services/apiService';

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
  const [items, setItems] = useState<InventoryItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [isProcessing, setIsProcessing] = useState(false);
  const { openForm } = useFormModal();
  const { showConfirm } = useConfirmDialog();

  const BRANCH_ID = 1;

  const normalizeInventoryItems = (payload: unknown): InventoryItem[] => {
    const source = payload as { items?: any[] } | undefined;
    const rawItems = Array.isArray(source?.items) ? source.items : [];

    return rawItems
      .map((raw): InventoryItem => {
        const stock = Number(raw?.currentStock ?? 0);
        const minLevel = Number(raw?.minStockLevel ?? 0);

        return {
          id: Number(raw?.id ?? 0),
          itemName: String(raw?.name ?? ''),
          unit: String(raw?.unit ?? ''),
          stock,
          minLevel,
          status: getStockStatus(stock, minLevel),
          productType: String(raw?.productType ?? ''),
        };
      })
      .filter((item) => item.id > 0)
      .filter((item) => item.productType === 'RawMaterial' || item.productType === 'SemiFinished');
  };

  const loadInventory = async () => {
    setLoading(true);
    setError('');

    try {
      const response = await InventoryService.getAll(BRANCH_ID);
      setItems(normalizeInventoryItems(response?.data));
    } catch (err) {
      console.error('Failed to fetch inventory items:', err);
      setItems([]);
      setError('Failed to load inventory items.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadInventory();
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
    {
      key: 'productType',
      header: 'Type',
      render: (value) => (
        <Badge variant="info" size="sm">{String(value ?? '')}</Badge>
      ),
    },
  ];

  const handleAddItem = () => {
    openForm('inventory');
  };

  const handleEditItem = (item: InventoryItem) => {
    openForm('inventory', item);
  };

  const handlePurchase = async (item: InventoryItem) => {
    if (item.productType !== 'RawMaterial') {
      alert('Purchase entry is allowed only for RawMaterial.');
      return;
    }

    const quantityText = window.prompt(`Enter purchase quantity for ${item.itemName}:`, '1');
    const quantity = Number(quantityText ?? '0');
    if (!Number.isFinite(quantity) || quantity <= 0) {
      return;
    }

    try {
      setIsProcessing(true);
      await InventoryService.purchase({ itemId: item.id, quantity, branchId: BRANCH_ID });
      await loadInventory();
    } catch (err) {
      console.error('Purchase entry failed:', err);
      alert('Failed to add purchase stock.');
    } finally {
      setIsProcessing(false);
    }
  };

  const handleAdjust = async (item: InventoryItem) => {
    const quantityText = window.prompt(`Enter adjustment quantity for ${item.itemName} (use negative to deduct):`, '1');
    const quantityDelta = Number(quantityText ?? '0');
    if (!Number.isFinite(quantityDelta) || quantityDelta === 0) {
      return;
    }

    try {
      setIsProcessing(true);
      await InventoryService.adjust({ itemId: item.id, quantityDelta, branchId: BRANCH_ID, reason: 'Manual adjustment' });
      await loadInventory();
    } catch (err) {
      console.error('Stock adjustment failed:', err);
      alert('Failed to adjust stock.');
    } finally {
      setIsProcessing(false);
    }
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
      label: 'Purchase',
      onClick: (item) => {
        void handlePurchase(item);
      },
      variant: 'secondary',
    },
    {
      label: 'Adjust',
      onClick: (item) => {
        void handleAdjust(item);
      },
      variant: 'secondary',
    },
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
        <p className="text-gray-600">Track raw materials and semi-finished inventory levels</p>
      </div>

      {error && (
        <div className="mb-4 rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </div>
      )}

      <div className="mb-6 flex justify-end">
        <button
          onClick={handleAddItem}
          disabled={isProcessing}
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