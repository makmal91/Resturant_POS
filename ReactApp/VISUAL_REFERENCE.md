# 🎨 POS Forms - Visual Component Reference

This document provides a visual reference for all created components and their usage.

---

## 🏗️ Component Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                     POS Application                              │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ FormDemo (Interactive Showcase)                         │   │
│  │ - Navigation & Form Switching                           │   │
│  │ - Mock Data Integration                                 │   │
│  │ - Example Submit Handlers                               │   │
│  └────────────────────┬────────────────────────────────────┘   │
│                       │                                         │
│       ┌───────────────┼───────────────┬──────────────────┐     │
│       │               │               │                  │     │
│       ▼               ▼               ▼                  ▼     │
│  ┌─────────┐  ┌────────────┐  ┌────────────┐  ┌──────────────┐ │
│  │ Branch  │  │ User Form  │  │ Menu Form  │  │ Inventory    │ │
│  │ Form    │  │            │  │            │  │ Form         │ │
│  └────┬────┘  └─────┬──────┘  └─────┬──────┘  └──────┬───────┘ │
│       │             │               │                 │        │
│       │             │               │                 │        │
│       └─────────────┼───────────────┼─────────────────┘        │
│                     │               │                          │
│       ┌─────────────┴───────────────┘                          │
│       │                                                        │
│       ▼                                                        │
│  ┌────────────────────────────────────┐                       │
│  │ Base Form Components (Composable)   │                       │
│  ├────────────────────────────────────┤                       │
│  │ • FormInput                         │                       │
│  │ • FormSelect                        │                       │
│  │ • FormButton                        │                       │
│  │ • FormTextarea                      │                       │
│  └────────────────────────────────────┘                       │
│                                                                  │
│  ┌────────────────────────────────────┐                       │
│  │ Advanced Component                  │                       │
│  ├────────────────────────────────────┤                       │
│  │ • OrderScreen (Full POS Interface) │                       │
│  │   - Menu Browser                   │                       │
│  │   - Item Selection                 │                       │
│  │   - Order Summary                  │                       │
│  │   - Checkout                       │                       │
│  └────────────────────────────────────┘                       │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## 📐 Component Hierarchy

### Level 1: Base Components
```
FormInput
├── Input types: text, email, number, password, tel, etc.
├── Props: label, name, value, onChange, error, required, disabled
├── Validation: Built-in error display
└── Features: Required indicator, error message, disabled state

FormSelect
├── Options: Dynamic array of {label, value}
├── Props: label, name, value, onChange, options, error, required
├── Validation: Built-in error display
└── Features: Placeholder support, disabled state

FormButton
├── Variants: primary, secondary, danger
├── Props: label, type, onClick, variant, loading, disabled
├── Features: Loading spinner text, focus ring, hover states
└── Events: Support for submit, button, reset types

FormTextarea
├── Props: label, name, value, onChange, rows, error, required
├── Validation: Built-in error display
├── Features: Resizable, configurable rows
└── Styling: Same as FormInput
```

### Level 2: Entity Forms
```
BranchForm
├── Extends: FormInput, FormSelect, FormButton
├── Fields:
│   ├── Name (FormInput)
│   ├── Address (FormInput)
│   ├── City (FormInput)
│   ├── Phone (FormInput)
│   ├── Tax Rate (FormInput, type=number)
│   └── Status (FormSelect)
├── Validation: All fields required, tax rate 0-100%
└── OnSubmit: Passes BranchFormData

UserForm
├── Extends: FormInput, FormSelect, FormButton
├── Fields:
│   ├── Name (FormInput)
│   ├── Role (FormSelect)
│   ├── Branch (FormSelect, dynamic)
│   ├── Salary (FormInput, type=number)
│   └── Shift (FormSelect)
├── Validation: All fields required, salary non-negative
└── OnSubmit: Passes UserFormData

MenuForm
├── Extends: FormInput, FormSelect, FormButton
├── Fields:
│   ├── Name (FormInput)
│   ├── Category (FormSelect, dynamic)
│   ├── Price (FormInput, type=number)
│   └── Variants (Dynamic Array)
│       ├── Variant Name (FormInput)
│       └── Price Adjustment (FormInput, type=number)
├── Validation: Required fields, positive prices
├── Features: Add/remove variants dynamically
└── OnSubmit: Passes MenuFormData with variants array

InventoryForm
├── Extends: FormInput, FormSelect, FormButton
├── Fields:
│   ├── Item Name (FormInput)
│   ├── Unit (FormSelect, predefined options)
│   ├── Stock (FormInput, type=number)
│   └── Min Level (FormInput, type=number)
├── Validation: Stock >= Min Level, non-negative values
└── OnSubmit: Passes InventoryFormData

OrderScreen
├── Extends: FormInput, FormSelect, FormButton
├── Sections:
│   ├── Menu Browser
│   │   ├── Category Filter (Dynamic buttons)
│   │   └── Item Grid (Dynamic cards with variants)
│   └── Order Summary
│       ├── Customer Info (FormInput)
│       ├── Table Number (FormInput)
│       ├── Special Notes (FormTextarea)
│       ├── Order Items (List with qty controls)
│       ├── Calculations (Subtotal, tax, total)
│       └── Actions (Complete, Clear)
├── Features:
│   ├── Real-time calculations
│   ├── Variant selection
│   ├── Quantity adjustment (±, delete)
│   ├── Tax calculation (10% default)
│   └── Sticky order summary
└── OnSubmit: Passes OrderScreenData
```

---

## 🎯 Data Flow Diagram

```
User Input (Form Fields)
        │
        ▼
handleChange() - Update formData state
        │
        ▼
User clicks Submit
        │
        ▼
handleSubmit() - Validate form
        │
        ├─── Validation FAILS ─────► Display errors
        │
        ├─── Validation PASSES ────► setIsLoading(true)
        │
        ▼
onSubmit() callback
        │
        ├─── API Call (POST/PUT)
        │
        ├─── Success ───────────────► Alert user
        │                            Reset form
        │                            Update state
        │
        └─── Error ────────────────► Alert error
                                     Show error state
```

---

## 🎨 UI Layout Examples

### FormInput Component
```
┌────────────────────────────┐
│ Branch Name *              │
│ ┌──────────────────────┐   │
│ │ Enter branch name    │   │
│ └──────────────────────┘   │
│ (Error message appears here)│
└────────────────────────────┘
```

### FormSelect Component
```
┌────────────────────────────┐
│ Status *                   │
│ ┌──────────────────────┐   │
│ │ Select status      ▼ │   │
│ │ Active             │   │
│ │ Inactive           │   │
│ └──────────────────────┘   │
└────────────────────────────┘
```

### Form Container
```
┌─────────────────────────────────────┐
│ Branch Information                  │
├─────────────────────────────────────┤
│                                     │
│ ┌──────────────────────────────┐   │
│ │ Branch Name *                │   │
│ │ [input field]                │   │
│ └──────────────────────────────┘   │
│                                     │
│ ┌──────────────────────────────┐   │
│ │ Address *                    │   │
│ │ [input field]                │   │
│ └──────────────────────────────┘   │
│                                     │
│ ┌────────────┐  ┌───────────────┐  │
│ │  Create    │  │  Reset        │  │
│ └────────────┘  └───────────────┘  │
│                                     │
└─────────────────────────────────────┘
```

### Menu Form with Variants
```
┌────────────────────────────────────────┐
│ Menu Item                              │
├────────────────────────────────────────┤
│                                        │
│ Item Name:         [input]             │
│ Category:          [select]            │
│ Base Price:        [input: 8.99]       │
│                                        │
│ ┌─────────────────────────────────┐   │
│ │ Variants           + Add        │   │
│ ├─────────────────────────────────┤   │
│ │ ┌─ Variant 1 ─────────────────┐ │   │
│ │ │ Name: Small     Price: -1.00│ │   │
│ │ │ [Remove]                     │ │   │
│ │ └─────────────────────────────┘ │   │
│ │ ┌─ Variant 2 ─────────────────┐ │   │
│ │ │ Name: Large     Price: +1.00│ │   │
│ │ │ [Remove]                     │ │   │
│ │ └─────────────────────────────┘ │   │
│ └─────────────────────────────────┘   │
│                                        │
│ ┌──────────┐  ┌──────────────────┐   │
│ │  Create  │  │  Reset           │   │
│ └──────────┘  └──────────────────┘   │
│                                        │
└────────────────────────────────────────┘
```

### OrderScreen Layout
```
┌───────────────────────────────────────────────────────────┐
│                                          POS Order Screen  │
├────────────────────────────────┬─────────────────────────┤
│                                │                         │
│ Menu Items                     │  Order Summary          │
│ ┌──────────────────────────┐   │ ┌────────────────────┐ │
│ │ Categories:              │   │ │ Customer: [input]  │ │
│ │ [All] [Main] [Apps]...  │   │ │ Table: [input]     │ │
│ ├──────────────────────────┤   │ ├────────────────────┤ │
│ │ ┌──────────┬─────────────┐   │ │ Items:             │ │
│ │ │ Burger   │    $8.99    │   │ │ ┌──────────────┐  │ │
│ │ │ [Add]    │  • Small -1 │   │ │ │ Burger   $8.99│ │ │
│ │ │          │  • Large +1 │   │ │ │ [-] 1 [+]    │ │ │
│ │ └──────────┴─────────────┘   │ │ │ [Delete]     │ │ │
│ │                               │ │ └──────────────┘  │ │
│ │ ┌──────────┬─────────────┐   │ │                    │ │
│ │ │ Pizza    │   $12.99    │   │ │ ┌──────────────┐  │ │
│ │ │ [Add]    │ • Thin Crust│   │ │ │ Pizza   $12.99│ │ │
│ │ │          │ • Thick +1.5│   │ │ │ [-] 2 [+]    │ │ │
│ │ └──────────┴─────────────┘   │ │ │ [Delete]     │ │ │
│ │                               │ │ └──────────────┘  │ │
│ │ ┌──────────┬─────────────┐   │ ├────────────────────┤ │
│ │ │ Ice Cream│    $4.99    │   │ │ Subtotal: $32.97   │ │
│ │ │ [Add]    │             │   │ │ Tax (10%): $3.30   │ │
│ │ │          │             │   │ │ ────────────────── │ │
│ │ └──────────┴─────────────┘   │ │ Total:     $36.27  │ │
│ │                               │ ├────────────────────┤ │
│ │                               │ │ [Complete Order]   │ │
│ │                               │ │ [Clear Order]      │ │
│ │                               │ └────────────────────┘ │
│                                                          │
└────────────────────────────────┴──────────────────────────┘
```

---

## 🔄 Variant Selection Flow

```
User sees Menu Item Card
        │
        ├─ No Variants
        │   └─ Single [Add to Order] button
        │
        └─ Has Variants
            └─ Multiple variant buttons
                    │
        ┌───────────┼───────────┐
        │           │           │
        ▼           ▼           ▼
      Small      Medium      Large
      [-$1]        [+$0]     [+$1]
        │           │           │
        └───────────┴───────────┘
                    │
                    ▼
            Add item with variant
            to OrderScreen order
```

---

## 💾 Data Models

### BranchFormData
```typescript
{
  name: string,           // "Downtown"
  address: string,        // "123 Main St"
  city: string,           // "New York"
  phone: string,          // "555-0123"
  taxRate: string,        // "10"
  status: string          // "Active" | "Inactive"
}
```

### UserFormData
```typescript
{
  name: string,           // "John Doe"
  role: string,           // "Manager" | "Cashier" | "Chef"
  branch: string,         // Branch ID
  salary: string,         // "2500.00"
  shift: string           // "Morning" | "Afternoon"
}
```

### MenuFormData
```typescript
{
  name: string,           // "Burger"
  category: string,       // Category ID
  price: string,          // "8.99"
  variants: Array<{
    name: string,         // "Small"
    price: string         // "-1.00"
  }>
}
```

### InventoryFormData
```typescript
{
  itemName: string,       // "Tomatoes"
  unit: string,           // "kg" | "piece" | "box"
  stock: string,          // "50"
  minLevel: string        // "10"
}
```

### OrderScreenData
```typescript
{
  items: Array<{
    menuItemId: string,
    itemName: string,
    quantity: number,
    unitPrice: number,
    variantId?: string,
    variantName?: string
  }>,
  customerName: string,   // "John Smith"
  tableNumber: string,    // "5"
  notes: string,          // "No onions please"
  subtotal: number,       // 32.97
  tax: number,            // 3.30
  total: number           // 36.27
}
```

---

## 🎯 State Management Pattern

```
Component State:
├─ formData: FormDataType
│  ├─ name: string
│  ├─ email: string
│  └─ ...other fields
│
├─ errors: Partial<FormDataType>
│  ├─ name?: string  (error message)
│  ├─ email?: string
│  └─ ...other fields
│
└─ isLoading: boolean
   └─ Used for showing loading spinner

Event Handlers:
├─ handleChange()
│  └─ Updates formData on input change
│
├─ handleSubmit()
│  ├─ Validates form
│  ├─ Shows errors if invalid
│  └─ Calls onSubmit if valid
│
└─ validateForm()
   ├─ Checks all fields
   ├─ Sets errors state
   └─ Returns boolean
```

---

## 🎨 Responsive Breakpoints

### Mobile (< 768px)
- Single column layout
- Full width inputs
- Stacked buttons

### Tablet (768px - 1024px)
- 2 column grid for some forms
- Medium width containers
- Side-by-side buttons

### Desktop (> 1024px)
- Full multi-column layouts
- Max-width containers (max-w-2xl)
- Optimal spacing and typography

---

## 🔗 Import Patterns

### Pattern 1: Import Everything
```tsx
import * as Forms from '@/components/forms';

<Forms.BranchForm />
<Forms.FormInput />
```

### Pattern 2: Named Imports
```tsx
import { BranchForm, UserForm, FormInput } from '@/components/forms';

<BranchForm />
<UserForm />
<FormInput />
```

### Pattern 3: Default Import
```tsx
import FormDemo from '@/components/FormDemo';

<FormDemo />
```

### Pattern 4: Mix and Match
```tsx
import { FormButton, FormInput } from '@/components/forms';
import MenuForm from '@/components/forms';

// Build custom form
<FormInput ... />
<FormButton ... />
```

---

## 🎓 Learning Path

**Beginner:**
1. View FormDemo component → Understand UI
2. Read QUICK_START.md → Learn basics
3. Try Option 1 integration → Get it running

**Intermediate:**
1. Study FORMS_DOCUMENTATION.md → Understand APIs
2. Try Option 2 integration → Use individual forms
3. Review INTEGRATION_EXAMPLES.md → See patterns

**Advanced:**
1. Create custom forms using base components
2. Implement custom validation logic
3. Integrate with state management (Zustand)
4. Connect to real API endpoints

---

## 🚀 Quick Copy-Paste Examples

### Use BranchForm
```tsx
import { BranchForm } from '@/components/forms';

<BranchForm
  onSubmit={(data) => console.log(data)}
  submitLabel="Create Branch"
/>
```

### Use OrderScreen
```tsx
import { OrderScreen } from '@/components/forms';

<OrderScreen
  menuItems={menuItems}
  onCompleteOrder={(order) => console.log(order)}
/>
```

### Build Custom Form
```tsx
import { FormInput, FormButton } from '@/components/forms';

<FormInput label="Name" ... />
<FormButton label="Submit" />
```

---

## 📞 Support Resources

- `FORMS_DOCUMENTATION.md` - Complete API reference
- `QUICK_START.md` - Getting started guide
- `INTEGRATION_EXAMPLES.md` - Real implementation examples
- `FORMS_SUMMARY.md` - Component overview
- `FormDemo.tsx` - Interactive showcase

---

**Last Updated:** April 18, 2026
