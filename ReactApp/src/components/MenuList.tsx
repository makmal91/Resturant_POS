import React, { useState, useEffect } from 'react';
import DataTable, { Column, Action } from './DataTable';
import Badge from './Badge';
import { useFormModal } from '../contexts/FormModalContext';
import { useConfirmDialog } from '../contexts/ConfirmDialogContext';
import { MenuService } from '../services/apiService';

interface MenuItem {
  id: number;
  name: string;
  category: string;
  price: number;
  variants: number;
  status: string;
}

const EMPTY_MENU_FORM_DATA = {
  name: '',
  price: 0,
  description: '',
  categoryId: null,
  category: '',
  variants: [],
};

const MenuList: React.FC = () => {
  const [items, setItems] = useState<MenuItem[]>([]);
  const [loading, setLoading] = useState(true);
  const { openForm } = useFormModal();
  const { showConfirm } = useConfirmDialog();

  useEffect(() => {
    setTimeout(() => {
      setItems([
        {
          id: 1,
          name: 'Grilled Salmon',
          category: 'Main Course',
          price: 24.99,
          variants: 3,
          status: 'Available'
        },
        {
          id: 2,
          name: 'Caesar Salad',
          category: 'Appetizer',
          price: 8.99,
          variants: 2,
          status: 'Available'
        },
        {
          id: 3,
          name: 'Chocolate Cake',
          category: 'Dessert',
          price: 6.99,
          variants: 1,
          status: 'Unavailable'
        },
      ]);
      setLoading(false);
    }, 1000);
  }, []);

  const columns: Column<MenuItem>[] = [
    {
      key: 'name',
      header: 'Item Name',
      sortable: true,
    },
    {
      key: 'category',
      header: 'Category',
      render: (value) => (
        <Badge variant="secondary" size="sm">{value}</Badge>
      ),
    },
    {
      key: 'price',
      header: 'Price',
      render: (value) => `$${(value || 0).toFixed(2)}`,
      sortable: true,
    },
    {
      key: 'variants',
      header: 'Variants',
      render: (value) => (
        <Badge variant="info" size="sm" rounded>{value}</Badge>
      ),
    },
    {
      key: 'status',
      header: 'Status',
      render: (value) => (
        <Badge 
          variant={value === 'Available' ? 'success' : 'danger'} 
          size="sm" 
          dot
        >
          {value}
        </Badge>
      ),
    },
  ];

  const handleAddItem = () => {
    console.log('[MenuList] Add Item clicked');
    openForm('menu', EMPTY_MENU_FORM_DATA);
  };

  const handleEditItem = (item: MenuItem) => {
    openForm('menu', item);
  };

  const handleDeleteItem = (item: MenuItem) => {
    showConfirm({
      title: 'Delete Menu Item',
      message: `Are you sure you want to delete "${item.name}"? This action cannot be undone.`,
      confirmLabel: 'Delete',
      cancelLabel: 'Cancel',
      onConfirm: async () => {
        try {
          await MenuService.delete(item.id);
          setItems(items.filter(i => i.id !== item.id));
        } catch (error) {
          console.error('Failed to delete menu item:', error);
          alert('Failed to delete menu item. Please try again.');
        }
      }
    });
  };

  const actions: Action<MenuItem>[] = [
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
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Menu Items</h1>
        <p className="text-gray-600">Manage your restaurant menu and items</p>
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

export default MenuList;