# 🚀 POS Forms - Quick Start Guide

## Installation & Setup

All components are already created and ready to use!

---

## 📂 File Structure

```
src/
├── components/
│   ├── forms/
│   │   ├── FormInput.tsx
│   │   ├── FormSelect.tsx
│   │   ├── FormButton.tsx
│   │   ├── FormTextarea.tsx
│   │   ├── BranchForm.tsx
│   │   ├── UserForm.tsx
│   │   ├── MenuForm.tsx
│   │   ├── InventoryForm.tsx
│   │   ├── OrderScreen.tsx
│   │   ├── index.ts
│   │   └── FORMS_DOCUMENTATION.md
│   ├── FormDemo.tsx          ← Demo with all forms
│   └── POS.tsx               ← Existing component
├── App.tsx
└── main.tsx
```

---

## 🎯 Option 1: Use the Demo Component (Easiest)

The `FormDemo.tsx` component showcases all forms in an interactive interface.

### Update `App.tsx`:

```tsx
import FormDemo from './components/FormDemo'

function App() {
  return <FormDemo />
}

export default App
```

Then run:
```bash
npm run dev
```

The demo shows:
- ✅ Interactive form navigation
- ✅ All 5 forms with mock data
- ✅ Form submission handling
- ✅ Responsive design showcase

---

## 🎯 Option 2: Use Individual Forms

Import and use forms directly in your components.

### Example: Create a Branch Manager Page

```tsx
import React, { useState } from 'react';
import { BranchForm } from '@/components/forms';
import axios from 'axios';

function BranchManager() {
  const [isLoading, setIsLoading] = useState(false);

  const handleCreateBranch = async (data) => {
    setIsLoading(true);
    try {
      await axios.post('/api/branches', data);
      alert('Branch created!');
    } catch (error) {
      alert('Error creating branch');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="p-6">
      <BranchForm
        onSubmit={handleCreateBranch}
        isLoading={isLoading}
        submitLabel="Create Branch"
      />
    </div>
  );
}

export default BranchManager;
```

---

## 🎯 Option 3: Custom Form Using Base Components

Create your own form by combining base components.

```tsx
import React, { useState } from 'react';
import { FormInput, FormSelect, FormButton } from '@/components/forms';

function CustomForm() {
  const [data, setData] = useState({ name: '', category: '' });

  const handleChange = (e) => {
    const { name, value } = e.target;
    setData(prev => ({ ...prev, [name]: value }));
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    console.log('Form Data:', data);
  };

  return (
    <form onSubmit={handleSubmit}>
      <FormInput
        label="Name"
        name="name"
        value={data.name}
        onChange={handleChange}
        required
      />

      <FormSelect
        label="Category"
        name="category"
        value={data.category}
        onChange={handleChange}
        options={[
          { label: 'Category 1', value: '1' },
          { label: 'Category 2', value: '2' },
        ]}
        required
      />

      <FormButton type="submit" label="Submit" />
    </form>
  );
}

export default CustomForm;
```

---

## 🎨 Styling Guide

All components use Tailwind CSS classes. Customize by modifying component files.

### Form Container Spacing
```tsx
<div className="bg-white p-6 rounded-lg shadow-md max-w-md mx-auto">
  {/* Form content */}
</div>
```

### Add Custom Classes
```tsx
<FormButton
  label="Click Me"
  className="w-full px-8 py-3"
  variant="primary"
/>
```

---

## 📋 Form Imports

All forms are exported from a single index file for easy importing:

```tsx
// Import specific forms
import { BranchForm, UserForm } from '@/components/forms';

// Import base components
import { FormInput, FormSelect, FormButton } from '@/components/forms';

// Import everything
import * as Forms from '@/components/forms';
```

---

## 🔌 API Integration

### Pattern 1: Direct API Call

```tsx
const handleSubmit = async (data) => {
  setIsLoading(true);
  try {
    const response = await axios.post('/api/branches', data);
    console.log('Success:', response.data);
  } catch (error) {
    console.error('Error:', error);
  } finally {
    setIsLoading(false);
  }
};

<BranchForm onSubmit={handleSubmit} isLoading={isLoading} />
```

### Pattern 2: API Service

```tsx
// api/branchService.ts
import axios from 'axios';

export const createBranch = (data) => {
  return axios.post('/api/branches', data);
};

// Component
import { createBranch } from '@/api/branchService';

const handleSubmit = async (data) => {
  setIsLoading(true);
  try {
    await createBranch(data);
  } finally {
    setIsLoading(false);
  }
};
```

---

## 🧪 Testing Forms Locally

### With Mock Data

All forms support `initialData` prop for pre-filling:

```tsx
<BranchForm
  initialData={{
    name: "Downtown",
    address: "123 Main St",
    city: "NYC",
    phone: "555-0123",
    taxRate: "10",
    status: "Active"
  }}
  onSubmit={handleSubmit}
/>
```

### With Mock Lists

```tsx
const mockBranches = [
  { id: "1", name: "Downtown" },
  { id: "2", name: "Uptown" }
];

<UserForm
  branches={mockBranches}
  onSubmit={handleSubmit}
/>
```

---

## ✅ Form Validation Guide

Each form has built-in validation. To customize:

1. Open the form file (e.g., `BranchForm.tsx`)
2. Modify the `validateForm()` function
3. Add your custom validation logic

```tsx
const validateForm = (): boolean => {
  const newErrors: Partial<BranchFormData> = {};

  if (!formData.name.trim()) {
    newErrors.name = 'Branch name is required';
  }

  // Add more validations...

  setErrors(newErrors);
  return Object.keys(newErrors).length === 0;
};
```

---

## 🎯 Common Tasks

### How to add a new form field?

1. Update the interface:
```tsx
interface BranchFormData {
  name: string;
  email: string; // Add new field
}
```

2. Add to state:
```tsx
const [formData, setFormData] = useState<BranchFormData>({
  name: '',
  email: '', // Add here
});
```

3. Add validation:
```tsx
if (!formData.email) newErrors.email = 'Email is required';
```

4. Add to form JSX:
```tsx
<FormInput
  label="Email"
  name="email"
  value={formData.email}
  onChange={handleChange}
  required
/>
```

---

### How to change form styling?

All forms use inline Tailwind classes. Modify them directly:

```tsx
// Change button colors
<FormButton
  variant="primary" // Change to 'secondary' or 'danger'
  className="px-8 py-4" // Add custom sizes
/>

// Change container styling
<form onSubmit={handleSubmit} className="bg-gray-100 p-8 rounded-2xl">
  {/* Form content */}
</form>
```

---

### How to use OrderScreen with real menu data?

```tsx
// Fetch from your API
const [menuItems, setMenuItems] = useState([]);

useEffect(() => {
  const fetchMenu = async () => {
    const response = await axios.get('/api/menu-items');
    setMenuItems(response.data);
  };
  fetchMenu();
}, []);

<OrderScreen
  menuItems={menuItems}
  onCompleteOrder={handleOrderSubmit}
/>
```

---

## 📚 Component Props Reference

### FormInput
- `label`: string (required)
- `name`: string (required)
- `value`: string | number (required)
- `onChange`: function (required)
- `type`: string (optional, default: "text")
- `error`: string (optional)
- `required`: boolean (optional)
- `disabled`: boolean (optional)

### FormSelect
- `label`: string (required)
- `name`: string (required)
- `value`: string | number (required)
- `onChange`: function (required)
- `options`: array (required)
- `error`: string (optional)
- `required`: boolean (optional)

### FormButton
- `label`: string (required)
- `type`: "submit" | "button" | "reset" (optional)
- `variant`: "primary" | "secondary" | "danger" (optional)
- `loading`: boolean (optional)
- `disabled`: boolean (optional)

---

## 🐛 Troubleshooting

### Forms not showing?
- Ensure Tailwind CSS is configured
- Check that `npm install` was run
- Verify imports are correct

### Styling not applied?
- Make sure you're using `npm run dev` (not just `npm start`)
- Clear browser cache
- Rebuild Tailwind with `npm run build`

### Form not validating?
- Check console for JavaScript errors
- Verify validation logic in the form component
- Ensure required fields are filled

---

## 📖 Full Documentation

See `FORMS_DOCUMENTATION.md` in the forms directory for comprehensive documentation.

---

## 🎉 You're All Set!

Your POS system forms are ready to use. Choose one of the options above and start building!

**Next Steps:**
1. ✅ Run `npm run dev`
2. ✅ Choose integration option (Demo, Individual, or Custom)
3. ✅ Connect to your API endpoints
4. ✅ Customize styling as needed

Happy coding! 🚀
