# POS Forms - Integration Examples

This file contains real-world integration examples for the POS forms.

---

## 🎯 Example 1: Admin Dashboard with All Forms

```tsx
// src/pages/AdminDashboard.tsx
import React, { useState } from 'react';
import {
  BranchForm,
  UserForm,
  MenuForm,
  InventoryForm,
} from '@/components/forms';
import axios from 'axios';

type TabType = 'branches' | 'users' | 'menu' | 'inventory';

function AdminDashboard() {
  const [activeTab, setActiveTab] = useState<TabType>('branches');
  const [isLoading, setIsLoading] = useState(false);
  const [branches, setBranches] = useState([]);
  const [categories, setCategories] = useState([]);

  // Fetch initial data
  React.useEffect(() => {
    const fetchData = async () => {
      try {
        const branchRes = await axios.get('/api/branches');
        setBranches(branchRes.data);

        const catRes = await axios.get('/api/categories');
        setCategories(catRes.data);
      } catch (error) {
        console.error('Error fetching data:', error);
      }
    };
    fetchData();
  }, []);

  const handleCreateBranch = async (data) => {
    setIsLoading(true);
    try {
      const response = await axios.post('/api/branches', data);
      setBranches([...branches, response.data]);
      alert('Branch created successfully!');
    } catch (error) {
      alert('Error creating branch');
    } finally {
      setIsLoading(false);
    }
  };

  const handleCreateUser = async (data) => {
    setIsLoading(true);
    try {
      await axios.post('/api/users', data);
      alert('User created successfully!');
    } catch (error) {
      alert('Error creating user');
    } finally {
      setIsLoading(false);
    }
  };

  const handleCreateMenuItem = async (data) => {
    setIsLoading(true);
    try {
      await axios.post('/api/menu-items', data);
      alert('Menu item created successfully!');
    } catch (error) {
      alert('Error creating menu item');
    } finally {
      setIsLoading(false);
    }
  };

  const handleAddInventory = async (data) => {
    setIsLoading(true);
    try {
      await axios.post('/api/inventory', data);
      alert('Inventory item added successfully!');
    } catch (error) {
      alert('Error adding inventory');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-100">
      {/* Navigation */}
      <nav className="bg-white shadow-md">
        <div className="max-w-7xl mx-auto px-6 py-4">
          <h1 className="text-3xl font-bold text-gray-800">Admin Dashboard</h1>
        </div>
      </nav>

      {/* Tabs */}
      <div className="max-w-7xl mx-auto px-6 py-6">
        <div className="flex gap-4 mb-6">
          <button
            onClick={() => setActiveTab('branches')}
            className={`px-6 py-2 rounded-lg font-medium transition ${
              activeTab === 'branches'
                ? 'bg-blue-600 text-white'
                : 'bg-white text-gray-800'
            }`}
          >
            Branches
          </button>
          <button
            onClick={() => setActiveTab('users')}
            className={`px-6 py-2 rounded-lg font-medium transition ${
              activeTab === 'users'
                ? 'bg-blue-600 text-white'
                : 'bg-white text-gray-800'
            }`}
          >
            Users
          </button>
          <button
            onClick={() => setActiveTab('menu')}
            className={`px-6 py-2 rounded-lg font-medium transition ${
              activeTab === 'menu'
                ? 'bg-blue-600 text-white'
                : 'bg-white text-gray-800'
            }`}
          >
            Menu
          </button>
          <button
            onClick={() => setActiveTab('inventory')}
            className={`px-6 py-2 rounded-lg font-medium transition ${
              activeTab === 'inventory'
                ? 'bg-blue-600 text-white'
                : 'bg-white text-gray-800'
            }`}
          >
            Inventory
          </button>
        </div>

        {/* Content */}
        <div className="bg-white rounded-lg shadow-md p-6">
          {activeTab === 'branches' && (
            <BranchForm onSubmit={handleCreateBranch} isLoading={isLoading} />
          )}
          {activeTab === 'users' && (
            <UserForm
              onSubmit={handleCreateUser}
              branches={branches}
              isLoading={isLoading}
            />
          )}
          {activeTab === 'menu' && (
            <MenuForm
              onSubmit={handleCreateMenuItem}
              categories={categories}
              isLoading={isLoading}
            />
          )}
          {activeTab === 'inventory' && (
            <InventoryForm onSubmit={handleAddInventory} isLoading={isLoading} />
          )}
        </div>
      </div>
    </div>
  );
}

export default AdminDashboard;
```

---

## 🎯 Example 2: Order Taking POS Screen

```tsx
// src/pages/POSOrderScreen.tsx
import React, { useState, useEffect } from 'react';
import { OrderScreen } from '@/components/forms';
import axios from 'axios';

function POSOrderScreen() {
  const [menuItems, setMenuItems] = useState([]);
  const [isLoading, setIsLoading] = useState(false);

  // Fetch menu items on component mount
  useEffect(() => {
    const fetchMenuItems = async () => {
      try {
        const response = await axios.get('/api/menu-items');
        setMenuItems(response.data);
      } catch (error) {
        console.error('Error fetching menu items:', error);
      }
    };
    fetchMenuItems();
  }, []);

  const handleCompleteOrder = async (orderData) => {
    setIsLoading(true);
    try {
      const response = await axios.post('/api/orders', {
        customerName: orderData.customerName,
        tableNumber: orderData.tableNumber,
        items: orderData.items.map((item) => ({
          menuItemId: item.menuItemId,
          quantity: item.quantity,
          variantId: item.variantId,
          notes: item.variantName,
        })),
        notes: orderData.notes,
        subtotal: orderData.subtotal,
        tax: orderData.tax,
        total: orderData.total,
      });

      alert('Order placed successfully! Order ID: ' + response.data.id);

      // Print receipt (optional)
      // printReceipt(response.data);
    } catch (error) {
      alert('Error placing order');
      console.error(error);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <OrderScreen
      menuItems={menuItems}
      onCompleteOrder={handleCompleteOrder}
      isLoading={isLoading}
    />
  );
}

export default POSOrderScreen;
```

---

## 🎯 Example 3: Modal Form Implementation

```tsx
// src/components/BranchModal.tsx
import React, { useState } from 'react';
import { BranchForm } from '@/components/forms';

interface BranchModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (data: any) => void;
}

function BranchModal({ isOpen, onClose, onSubmit }: BranchModalProps) {
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (data) => {
    setIsLoading(true);
    try {
      await onSubmit(data);
      onClose();
    } finally {
      setIsLoading(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl max-w-md w-full mx-4">
        <div className="flex justify-between items-center border-b p-6">
          <h2 className="text-2xl font-bold">Create Branch</h2>
          <button
            onClick={onClose}
            className="text-gray-500 hover:text-gray-700 text-2xl"
          >
            ×
          </button>
        </div>

        <div className="p-6">
          <BranchForm
            onSubmit={handleSubmit}
            isLoading={isLoading}
            submitLabel="Create"
          />
        </div>
      </div>
    </div>
  );
}

export default BranchModal;
```

---

## 🎯 Example 4: Edit Form with Initial Data

```tsx
// src/components/BranchEditor.tsx
import React, { useState, useEffect } from 'react';
import { BranchForm } from '@/components/forms';
import axios from 'axios';

interface BranchEditorProps {
  branchId: string;
  onSuccess?: () => void;
}

function BranchEditor({ branchId, onSuccess }: BranchEditorProps) {
  const [branch, setBranch] = useState(null);
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    const fetchBranch = async () => {
      try {
        const response = await axios.get(`/api/branches/${branchId}`);
        setBranch(response.data);
      } catch (error) {
        console.error('Error fetching branch:', error);
      }
    };
    fetchBranch();
  }, [branchId]);

  const handleSubmit = async (data) => {
    setIsLoading(true);
    try {
      await axios.put(`/api/branches/${branchId}`, data);
      alert('Branch updated successfully!');
      onSuccess?.();
    } catch (error) {
      alert('Error updating branch');
    } finally {
      setIsLoading(false);
    }
  };

  if (!branch) return <div>Loading...</div>;

  return (
    <BranchForm
      initialData={branch}
      onSubmit={handleSubmit}
      isLoading={isLoading}
      submitLabel="Update Branch"
    />
  );
}

export default BranchEditor;
```

---

## 🎯 Example 5: Multi-Step Form Wizard

```tsx
// src/components/SetupWizard.tsx
import React, { useState } from 'react';
import { BranchForm, UserForm } from '@/components/forms';
import axios from 'axios';

function SetupWizard() {
  const [step, setStep] = useState<'branch' | 'user' | 'complete'>('branch');
  const [createdBranch, setCreatedBranch] = useState(null);
  const [isLoading, setIsLoading] = useState(false);

  const handleCreateBranch = async (data) => {
    setIsLoading(true);
    try {
      const response = await axios.post('/api/branches', data);
      setCreatedBranch(response.data);
      setStep('user');
    } catch (error) {
      alert('Error creating branch');
    } finally {
      setIsLoading(false);
    }
  };

  const handleCreateUser = async (data) => {
    setIsLoading(true);
    try {
      await axios.post('/api/users', { ...data, branchId: createdBranch.id });
      setStep('complete');
    } catch (error) {
      alert('Error creating user');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="max-w-2xl mx-auto py-12">
      {/* Progress Bar */}
      <div className="mb-12">
        <div className="flex justify-between mb-2">
          <div
            className={`flex-1 h-2 rounded-lg mr-2 ${
              ['branch', 'user', 'complete'].includes(step)
                ? 'bg-blue-600'
                : 'bg-gray-300'
            }`}
          />
          <div
            className={`flex-1 h-2 rounded-lg mr-2 ${
              ['user', 'complete'].includes(step) ? 'bg-blue-600' : 'bg-gray-300'
            }`}
          />
          <div
            className={`flex-1 h-2 rounded-lg ${
              step === 'complete' ? 'bg-blue-600' : 'bg-gray-300'
            }`}
          />
        </div>
        <div className="flex justify-between text-sm text-gray-600">
          <span>Step 1: Branch</span>
          <span>Step 2: Manager</span>
          <span>Step 3: Complete</span>
        </div>
      </div>

      {/* Forms */}
      {step === 'branch' && (
        <BranchForm
          onSubmit={handleCreateBranch}
          isLoading={isLoading}
          submitLabel="Continue to Manager Setup"
        />
      )}

      {step === 'user' && createdBranch && (
        <UserForm
          onSubmit={handleCreateUser}
          branches={[createdBranch]}
          isLoading={isLoading}
          submitLabel="Complete Setup"
        />
      )}

      {step === 'complete' && (
        <div className="bg-green-50 border border-green-200 rounded-lg p-12 text-center">
          <div className="text-6xl mb-4">✅</div>
          <h2 className="text-2xl font-bold text-green-800 mb-2">Setup Complete!</h2>
          <p className="text-green-700 mb-6">
            Your restaurant POS system is ready to use.
          </p>
          <button className="bg-green-600 text-white px-6 py-2 rounded-lg hover:bg-green-700">
            Go to Dashboard
          </button>
        </div>
      )}
    </div>
  );
}

export default SetupWizard;
```

---

## 🎯 Example 6: Custom Form with Validation

```tsx
// src/components/CustomMenuForm.tsx
import React, { useState } from 'react';
import { FormInput, FormSelect, FormButton } from '@/components/forms';

function CustomMenuForm() {
  const [formData, setFormData] = useState({
    name: '',
    price: '',
    description: '',
  });
  const [errors, setErrors] = useState({});

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    setErrors((prev) => ({ ...prev, [name]: '' }));
  };

  const validateForm = () => {
    const newErrors = {};

    if (!formData.name.trim()) {
      newErrors.name = 'Name is required';
    } else if (formData.name.length < 3) {
      newErrors.name = 'Name must be at least 3 characters';
    }

    if (!formData.price) {
      newErrors.price = 'Price is required';
    } else if (parseFloat(formData.price) <= 0) {
      newErrors.price = 'Price must be greater than 0';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    if (validateForm()) {
      console.log('Form submitted:', formData);
      // Submit to API
    }
  };

  return (
    <form onSubmit={handleSubmit} className="max-w-md mx-auto space-y-4">
      <FormInput
        label="Item Name"
        name="name"
        value={formData.name}
        onChange={handleChange}
        error={errors.name}
        required
      />

      <FormInput
        label="Price"
        name="price"
        type="number"
        value={formData.price}
        onChange={handleChange}
        step="0.01"
        error={errors.price}
        required
      />

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Description
        </label>
        <textarea
          name="description"
          value={formData.description}
          onChange={handleChange}
          placeholder="Enter item description"
          rows={4}
          className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>

      <FormButton type="submit" label="Add Item" variant="primary" />
    </form>
  );
}

export default CustomMenuForm;
```

---

## 🎯 Example 7: Form Handling with State Management (Zustand)

```tsx
// src/stores/formStore.ts
import { create } from 'zustand';

interface FormStore {
  branches: any[];
  users: any[];
  menuItems: any[];
  
  addBranch: (branch: any) => void;
  addUser: (user: any) => void;
  addMenuItem: (item: any) => void;
  updateBranch: (id: string, data: any) => void;
}

export const useFormStore = create<FormStore>((set) => ({
  branches: [],
  users: [],
  menuItems: [],
  
  addBranch: (branch) =>
    set((state) => ({
      branches: [...state.branches, { ...branch, id: Date.now().toString() }],
    })),
  
  addUser: (user) =>
    set((state) => ({
      users: [...state.users, { ...user, id: Date.now().toString() }],
    })),
  
  addMenuItem: (item) =>
    set((state) => ({
      menuItems: [...state.menuItems, { ...item, id: Date.now().toString() }],
    })),
  
  updateBranch: (id, data) =>
    set((state) => ({
      branches: state.branches.map((b) => (b.id === id ? { ...b, ...data } : b)),
    })),
}));

// Usage in component
import { useFormStore } from '@/stores/formStore';

function BranchFormWithStore() {
  const { addBranch } = useFormStore();

  const handleSubmit = (data) => {
    addBranch(data);
    alert('Branch added!');
  };

  return <BranchForm onSubmit={handleSubmit} />;
}
```

---

## 💡 Key Patterns

1. **Loading States**: Use `isLoading` prop to show loading indicator
2. **Error Handling**: Catch API errors and show user-friendly messages
3. **Initial Data**: Pre-fill forms with `initialData` prop
4. **List Props**: Pass dynamic lists like branches and categories to forms
5. **Submit Handlers**: Use `onSubmit` callbacks for data processing

---

For more details, see the main documentation in `FORMS_DOCUMENTATION.md`.
