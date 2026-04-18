import React, { useState } from 'react';
import {
  BranchForm,
  UserForm,
  MenuForm,
  InventoryForm,
  OrderScreen,
} from './forms';

type FormType = 'branch' | 'user' | 'menu' | 'inventory' | 'order' | null;

// Mock data
const mockBranches = [
  { id: '1', name: 'Downtown' },
  { id: '2', name: 'Uptown' },
];

const mockCategories = [
  { id: '1', name: 'Appetizers' },
  { id: '2', name: 'Main Course' },
  { id: '3', name: 'Desserts' },
  { id: '4', name: 'Beverages' },
];

const mockMenuItems = [
  {
    id: '1',
    name: 'Burger',
    price: 8.99,
    category: 'Main Course',
    variants: [
      { id: 'v1', name: 'Small', priceAdjustment: -1.0 },
      { id: 'v2', name: 'Large', priceAdjustment: 1.0 },
    ],
  },
  {
    id: '2',
    name: 'Pizza',
    price: 12.99,
    category: 'Main Course',
    variants: [
      { id: 'v3', name: 'Thin Crust', priceAdjustment: 0 },
      { id: 'v4', name: 'Thick Crust', priceAdjustment: 1.5 },
    ],
  },
  {
    id: '3',
    name: 'Caesar Salad',
    price: 7.99,
    category: 'Appetizers',
    variants: [],
  },
  {
    id: '4',
    name: 'Ice Cream',
    price: 4.99,
    category: 'Desserts',
    variants: [
      { id: 'v5', name: 'Vanilla', priceAdjustment: 0 },
      { id: 'v6', name: 'Chocolate', priceAdjustment: 0 },
      { id: 'v7', name: 'Strawberry', priceAdjustment: 0 },
    ],
  },
  {
    id: '5',
    name: 'Soft Drink',
    price: 2.99,
    category: 'Beverages',
    variants: [
      { id: 'v8', name: 'Small (12oz)', priceAdjustment: 0 },
      { id: 'v9', name: 'Large (20oz)', priceAdjustment: 1.0 },
    ],
  },
];

const FormDemo: React.FC = () => {
  const [activeForm, setActiveForm] = useState<FormType>(null);
  const [isLoading, setIsLoading] = useState(false);

  const handleBranchSubmit = async (data: any) => {
    setIsLoading(true);
    try {
      await new Promise((resolve) => setTimeout(resolve, 1000));
      console.log('Branch Form Data:', data);
      alert('Branch created successfully!');
      setActiveForm(null);
    } finally {
      setIsLoading(false);
    }
  };

  const handleUserSubmit = async (data: any) => {
    setIsLoading(true);
    try {
      await new Promise((resolve) => setTimeout(resolve, 1000));
      console.log('User Form Data:', data);
      alert('User created successfully!');
      setActiveForm(null);
    } finally {
      setIsLoading(false);
    }
  };

  const handleMenuSubmit = async (data: any) => {
    setIsLoading(true);
    try {
      await new Promise((resolve) => setTimeout(resolve, 1000));
      console.log('Menu Form Data:', data);
      alert('Menu item created successfully!');
      setActiveForm(null);
    } finally {
      setIsLoading(false);
    }
  };

  const handleInventorySubmit = async (data: any) => {
    setIsLoading(true);
    try {
      await new Promise((resolve) => setTimeout(resolve, 1000));
      console.log('Inventory Form Data:', data);
      alert('Inventory item added successfully!');
      setActiveForm(null);
    } finally {
      setIsLoading(false);
    }
  };

  const handleOrderSubmit = async (data: any) => {
    setIsLoading(true);
    try {
      await new Promise((resolve) => setTimeout(resolve, 1000));
      console.log('Order Data:', data);
      alert('Order placed successfully!');
      setActiveForm(null);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 p-6">
      <div className="max-w-6xl mx-auto">
        {/* Header */}
        <div className="text-center mb-8">
          <h1 className="text-4xl font-bold text-gray-800 mb-2">POS System Forms</h1>
          <p className="text-gray-600 text-lg">
            Reusable Tailwind UI components for restaurant management
          </p>
        </div>

        {/* Form Navigation */}
        {!activeForm && (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4 mb-8">
            <button
              onClick={() => setActiveForm('branch')}
              className="bg-white hover:bg-blue-50 border-2 border-gray-300 hover:border-blue-500 rounded-lg p-6 transition transform hover:scale-105 text-center"
            >
              <div className="text-3xl mb-2">🏢</div>
              <h3 className="font-bold text-gray-800">Branch Form</h3>
              <p className="text-sm text-gray-600 mt-1">Manage branch details</p>
            </button>

            <button
              onClick={() => setActiveForm('user')}
              className="bg-white hover:bg-blue-50 border-2 border-gray-300 hover:border-blue-500 rounded-lg p-6 transition transform hover:scale-105 text-center"
            >
              <div className="text-3xl mb-2">👤</div>
              <h3 className="font-bold text-gray-800">User Form</h3>
              <p className="text-sm text-gray-600 mt-1">Add staff members</p>
            </button>

            <button
              onClick={() => setActiveForm('menu')}
              className="bg-white hover:bg-blue-50 border-2 border-gray-300 hover:border-blue-500 rounded-lg p-6 transition transform hover:scale-105 text-center"
            >
              <div className="text-3xl mb-2">🍽️</div>
              <h3 className="font-bold text-gray-800">Menu Form</h3>
              <p className="text-sm text-gray-600 mt-1">Add menu items</p>
            </button>

            <button
              onClick={() => setActiveForm('inventory')}
              className="bg-white hover:bg-blue-50 border-2 border-gray-300 hover:border-blue-500 rounded-lg p-6 transition transform hover:scale-105 text-center"
            >
              <div className="text-3xl mb-2">📦</div>
              <h3 className="font-bold text-gray-800">Inventory Form</h3>
              <p className="text-sm text-gray-600 mt-1">Manage stock</p>
            </button>

            <button
              onClick={() => setActiveForm('order')}
              className="bg-white hover:bg-blue-50 border-2 border-gray-300 hover:border-blue-500 rounded-lg p-6 transition transform hover:scale-105 text-center"
            >
              <div className="text-3xl mb-2">🛒</div>
              <h3 className="font-bold text-gray-800">Order Screen</h3>
              <p className="text-sm text-gray-600 mt-1">Take orders</p>
            </button>
          </div>
        )}

        {/* Form Display Area */}
        {activeForm && (
          <div className="bg-white rounded-lg shadow-lg p-8 mb-6">
            <button
              onClick={() => setActiveForm(null)}
              className="mb-6 px-4 py-2 bg-gray-300 hover:bg-gray-400 text-gray-800 rounded-lg font-medium transition"
            >
              ← Back to Forms
            </button>

            {activeForm === 'branch' && (
              <div className="max-w-2xl mx-auto">
                <BranchForm
                  onSubmit={handleBranchSubmit}
                  isLoading={isLoading}
                  submitLabel="Create Branch"
                />
              </div>
            )}

            {activeForm === 'user' && (
              <div className="max-w-2xl mx-auto">
                <UserForm
                  onSubmit={handleUserSubmit}
                  branches={mockBranches}
                  isLoading={isLoading}
                  submitLabel="Create User"
                />
              </div>
            )}

            {activeForm === 'menu' && (
              <div className="max-w-2xl mx-auto">
                <MenuForm
                  onSubmit={handleMenuSubmit}
                  categories={mockCategories}
                  isLoading={isLoading}
                  submitLabel="Create Menu Item"
                />
              </div>
            )}

            {activeForm === 'inventory' && (
              <div className="max-w-2xl mx-auto">
                <InventoryForm
                  onSubmit={handleInventorySubmit}
                  isLoading={isLoading}
                  submitLabel="Add Inventory"
                />
              </div>
            )}

            {activeForm === 'order' && (
              <OrderScreen
                menuItems={mockMenuItems}
                onCompleteOrder={handleOrderSubmit}
                isLoading={isLoading}
              />
            )}
          </div>
        )}

        {/* Footer Info */}
        {!activeForm && (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mt-12">
            <div className="bg-white rounded-lg shadow p-6">
              <h3 className="text-lg font-bold text-gray-800 mb-2">✨ Features</h3>
              <ul className="text-sm text-gray-600 space-y-1">
                <li>✓ Form validation</li>
                <li>✓ Error handling</li>
                <li>✓ Responsive design</li>
                <li>✓ Tailwind CSS</li>
              </ul>
            </div>

            <div className="bg-white rounded-lg shadow p-6">
              <h3 className="text-lg font-bold text-gray-800 mb-2">🛠️ Components</h3>
              <ul className="text-sm text-gray-600 space-y-1">
                <li>• FormInput</li>
                <li>• FormSelect</li>
                <li>• FormButton</li>
                <li>• FormTextarea</li>
              </ul>
            </div>

            <div className="bg-white rounded-lg shadow p-6">
              <h3 className="text-lg font-bold text-gray-800 mb-2">📚 Forms</h3>
              <ul className="text-sm text-gray-600 space-y-1">
                <li>• Branch Management</li>
                <li>• Staff Management</li>
                <li>• Menu Management</li>
                <li>• Inventory Tracking</li>
              </ul>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default FormDemo;
