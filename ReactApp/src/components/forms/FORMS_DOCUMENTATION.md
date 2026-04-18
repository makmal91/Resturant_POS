# POS System Forms - Documentation

A comprehensive set of reusable React form components built with Tailwind CSS for a restaurant POS system.

## 📦 Components Structure

```
src/components/forms/
├── FormInput.tsx          # Reusable input field component
├── FormSelect.tsx         # Reusable select dropdown component
├── FormButton.tsx         # Reusable button component
├── FormTextarea.tsx       # Reusable textarea component
├── BranchForm.tsx         # Branch management form
├── UserForm.tsx           # User/Staff management form
├── MenuForm.tsx           # Menu item management form
├── InventoryForm.tsx      # Inventory management form
├── OrderScreen.tsx        # POS order taking screen
└── index.ts              # Barrel export file
```

## 🎯 Features

- ✅ **Fully Responsive** - Works on mobile, tablet, and desktop
- ✅ **Form Validation** - Built-in validation with error messages
- ✅ **Tailwind UI** - Clean, modern design with Tailwind CSS
- ✅ **Type-Safe** - Full TypeScript support
- ✅ **Reusable Components** - Compose forms easily
- ✅ **Accessible** - Proper labels and error handling
- ✅ **Loading States** - Built-in loading indicators

---

## 📋 Base Form Components

### FormInput

A reusable text input component with validation.

```tsx
import { FormInput } from '@/components/forms';

<FormInput
  label="Customer Name"
  name="customerName"
  type="text"
  value={name}
  onChange={handleChange}
  placeholder="Enter name"
  required
  error={errors.name}
  disabled={false}
/>
```

**Props:**
- `label` (string) - Field label
- `name` (string) - Input name/id
- `type` (string, optional) - Input type (text, email, number, etc.) - default: "text"
- `value` (string | number) - Input value
- `onChange` (function) - Change handler
- `placeholder` (string, optional) - Placeholder text
- `required` (boolean, optional) - Mark as required
- `error` (string, optional) - Error message
- `disabled` (boolean, optional) - Disable input
- `min`, `max`, `step` (optional) - For number inputs

---

### FormSelect

A reusable dropdown select component.

```tsx
import { FormSelect } from '@/components/forms';

<FormSelect
  label="Category"
  name="category"
  value={selectedCategory}
  onChange={handleChange}
  options={[
    { label: 'Main Course', value: 'main' },
    { label: 'Appetizer', value: 'app' },
  ]}
  placeholder="Select category"
  required
  error={errors.category}
/>
```

**Props:**
- `label` (string) - Field label
- `name` (string) - Select name/id
- `value` (string | number) - Selected value
- `onChange` (function) - Change handler
- `options` (array) - Array of {label, value} options
- `placeholder` (string, optional) - Placeholder option
- `required` (boolean, optional) - Mark as required
- `error` (string, optional) - Error message
- `disabled` (boolean, optional) - Disable select

---

### FormButton

A reusable button component with multiple variants.

```tsx
import { FormButton } from '@/components/forms';

<FormButton
  type="submit"
  label="Create Branch"
  variant="primary"
  loading={isLoading}
  disabled={false}
  onClick={handleClick}
/>
```

**Props:**
- `label` (string) - Button text
- `type` (string, optional) - Button type (submit, button, reset) - default: "submit"
- `onClick` (function, optional) - Click handler
- `disabled` (boolean, optional) - Disable button
- `loading` (boolean, optional) - Show loading state
- `variant` (string, optional) - Style variant (primary, secondary, danger) - default: "primary"
- `className` (string, optional) - Additional CSS classes

**Variants:**
- `primary` - Blue background (default)
- `secondary` - Gray background
- `danger` - Red background

---

### FormTextarea

A reusable textarea component.

```tsx
import { FormTextarea } from '@/components/forms';

<FormTextarea
  label="Special Notes"
  name="notes"
  value={notes}
  onChange={handleChange}
  placeholder="Enter special requests"
  rows={4}
  error={errors.notes}
/>
```

**Props:**
- `label` (string) - Field label
- `name` (string) - Textarea name/id
- `value` (string) - Textarea value
- `onChange` (function) - Change handler
- `placeholder` (string, optional) - Placeholder text
- `required` (boolean, optional) - Mark as required
- `error` (string, optional) - Error message
- `disabled` (boolean, optional) - Disable textarea
- `rows` (number, optional) - Number of rows - default: 4

---

## 📝 Entity Forms

### 1. BranchForm

Manage branch information with name, address, city, phone, tax rate, and status.

```tsx
import { BranchForm } from '@/components/forms';

<BranchForm
  initialData={{
    name: "Downtown Branch",
    address: "123 Main St",
    city: "New York",
    phone: "555-0123",
    taxRate: "10",
    status: "Active"
  }}
  onSubmit={(data) => {
    console.log('Branch Data:', data);
    // Call API to save
  }}
  isLoading={false}
  submitLabel="Create Branch"
/>
```

**Form Fields:**
- Name (required)
- Address (required)
- City (required)
- Phone (required)
- Tax Rate % (0-100, required)
- Status (Active/Inactive)

**Data Structure:**
```ts
interface BranchFormData {
  name: string;
  address: string;
  city: string;
  phone: string;
  taxRate: string;
  status: string;
}
```

---

### 2. UserForm

Create and manage staff members with role, branch, salary, and shift information.

```tsx
import { UserForm } from '@/components/forms';

<UserForm
  initialData={{
    name: "John Doe",
    role: "Manager",
    branch: "1",
    salary: "2500",
    shift: "Morning"
  }}
  branches={[
    { id: "1", name: "Downtown" },
    { id: "2", name: "Uptown" }
  ]}
  onSubmit={(data) => {
    // Call API to save
  }}
  isLoading={false}
  submitLabel="Create User"
/>
```

**Form Fields:**
- Full Name (required)
- Role (required) - Manager, Cashier, Chef, Waiter, Admin
- Assigned Branch (required)
- Monthly Salary (required)
- Shift - Morning, Afternoon, Evening, Night

**Data Structure:**
```ts
interface UserFormData {
  name: string;
  role: string;
  branch: string;
  salary: string;
  shift: string;
}
```

---

### 3. MenuForm

Create menu items with pricing, categories, and variants (sizes, styles, etc.).

```tsx
import { MenuForm } from '@/components/forms';

<MenuForm
  initialData={{
    name: "Burger",
    category: "1",
    price: "8.99",
    variants: [
      { name: "Small", price: "-1.00" },
      { name: "Large", price: "1.00" }
    ]
  }}
  categories={[
    { id: "1", name: "Main Course" },
    { id: "2", name: "Appetizers" }
  ]}
  onSubmit={(data) => {
    // Call API to save
  }}
  isLoading={false}
  submitLabel="Create Item"
/>
```

**Form Fields:**
- Item Name (required)
- Category (required)
- Base Price (required)
- Variants (optional) - Add multiple variants with price adjustments

**Data Structure:**
```ts
interface MenuFormData {
  name: string;
  category: string;
  price: string;
  variants: Array<{
    name: string;
    price: string;
  }>;
}
```

---

### 4. InventoryForm

Manage inventory items with stock tracking and minimum level alerts.

```tsx
import { InventoryForm } from '@/components/forms';

<InventoryForm
  initialData={{
    itemName: "Tomatoes",
    unit: "kg",
    stock: "50",
    minLevel: "10"
  }}
  onSubmit={(data) => {
    // Call API to save
  }}
  isLoading={false}
  submitLabel="Add Item"
/>
```

**Form Fields:**
- Item Name (required)
- Unit of Measurement (required) - Piece, kg, g, L, ml, Box, Dozen, Bundle
- Current Stock (required)
- Minimum Level (required)

**Data Structure:**
```ts
interface InventoryFormData {
  itemName: string;
  unit: string;
  stock: string;
  minLevel: string;
}
```

---

## 🛒 OrderScreen (POS)

Complete point-of-sale order taking interface with menu browsing, item selection, and order summary.

```tsx
import { OrderScreen } from '@/components/forms';

const menuItems = [
  {
    id: "1",
    name: "Burger",
    price: 8.99,
    category: "Main Course",
    variants: [
      { id: "v1", name: "Small", priceAdjustment: -1.00 },
      { id: "v2", name: "Large", priceAdjustment: 1.00 }
    ]
  }
];

<OrderScreen
  menuItems={menuItems}
  onCompleteOrder={(order) => {
    console.log('Order:', order);
    // Call API to save order
  }}
  isLoading={false}
/>
```

**Features:**
- ✅ Category filtering
- ✅ Variant selection
- ✅ Quantity management
- ✅ Item removal
- ✅ Real-time total calculation
- ✅ Tax calculation (10% default)
- ✅ Customer and table information
- ✅ Special notes

**Order Data Structure:**
```ts
interface OrderScreenData {
  items: Array<{
    menuItemId: string;
    itemName: string;
    quantity: number;
    unitPrice: number;
    variantId?: string;
    variantName?: string;
  }>;
  customerName: string;
  tableNumber: string;
  notes: string;
  subtotal: number;
  tax: number;
  total: number;
}
```

---

## 🚀 Usage Examples

### Complete Form Integration Example

```tsx
import React, { useState } from 'react';
import { BranchForm, UserForm, MenuForm, InventoryForm, OrderScreen } from '@/components/forms';
import axios from 'axios';

const AdminDashboard: React.FC = () => {
  const [isLoading, setIsLoading] = useState(false);

  const handleCreateBranch = async (data) => {
    setIsLoading(true);
    try {
      const response = await axios.post('/api/branches', data);
      console.log('Branch created:', response.data);
      alert('Branch created successfully!');
    } catch (error) {
      console.error('Error creating branch:', error);
      alert('Failed to create branch');
    } finally {
      setIsLoading(false);
    }
  };

  const handleCreateUser = async (data) => {
    setIsLoading(true);
    try {
      const response = await axios.post('/api/users', data);
      console.log('User created:', response.data);
      alert('User created successfully!');
    } catch (error) {
      console.error('Error creating user:', error);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div>
      <BranchForm
        onSubmit={handleCreateBranch}
        isLoading={isLoading}
        submitLabel="Create New Branch"
      />
      <UserForm
        onSubmit={handleCreateUser}
        branches={[]}
        isLoading={isLoading}
        submitLabel="Add Staff Member"
      />
    </div>
  );
};

export default AdminDashboard;
```

---

## 🎨 Tailwind Styling

All components use Tailwind CSS with consistent design patterns:

**Color Scheme:**
- Primary: Blue (`blue-600`)
- Secondary: Gray (`gray-300`)
- Danger: Red (`red-600`)
- Accent: Yellow (`yellow-50`)

**Responsive Breakpoints:**
- Mobile: Default styles
- Tablet: `md:` breakpoint
- Desktop: `lg:` breakpoint

---

## 📱 Responsive Design

All forms are fully responsive:

- **Mobile**: Single column layout, touch-friendly buttons
- **Tablet**: 2-column grid for some forms
- **Desktop**: Multi-column with optimal spacing

---

## ✔️ Validation

All forms include built-in validation:

- Required field checking
- Type validation (number, email, etc.)
- Range validation (min/max)
- Custom error messages
- Visual error indicators

---

## 🔧 Customization

### Custom Styling

```tsx
<FormButton
  label="Custom"
  variant="primary"
  className="px-8 py-3 rounded-full"
/>
```

### Custom Validation

```tsx
const validateForm = () => {
  const errors = {};
  if (!formData.name.trim()) {
    errors.name = 'Name is required';
  }
  if (formData.price < 0) {
    errors.price = 'Price must be positive';
  }
  return errors;
};
```

---

## 📦 Dependencies

- React 18+
- TypeScript
- Tailwind CSS 3+

---

## 🤝 Integration with API

```tsx
const handleSubmit = async (data) => {
  try {
    const response = await axios.post('/api/endpoint', data);
    // Handle success
  } catch (error) {
    // Handle error
  }
};
```

---

## 💡 Tips

1. **Use the FormDemo component** to see all forms in action
2. **Reuse FormInput, FormSelect, FormButton** to build custom forms
3. **Validate on the client** before API calls
4. **Show loading state** during API requests
5. **Handle errors gracefully** with user-friendly messages

---

## 📄 License

MIT
