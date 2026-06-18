import React, { createContext, useContext, useState } from 'react';

export type FormType =
  | 'branch'
  | 'business'
  | 'user'
  | 'menu'
  | 'inventory'
  | 'category'
  | 'subcategory'
  | 'brand'
  | 'warehouse'
  | 'supplier'
  | 'purchase'
  | 'customer'
  | 'role'
  | 'cashTransaction'
  | 'receivePayment'
  | 'paySupplier'
  | null;

const DEFAULT_BRANCH_FORM_DATA = {
  name: '',
  code: '',
  address: '',
  phone: '',
  email: '',
  businessId: 0,
  countryId: 0,
  cityId: 0,
  status: 'Active',
};

const DEFAULT_USER_FORM_DATA = {
  fullName: '',
  username: '',
  email: '',
  phone: '',
  password: '',
  roleId: '',
  status: 'Active',
  branchIds: [] as number[],
};

const DEFAULT_BUSINESS_FORM_DATA = {
  name: '',
  legalName: '',
  phone: '',
  email: '',
  address: '',
  taxNumber: '',
  currencyId: 1,
  currency: 'PKR',
  timeZone: 'UTC',
  status: 'Active',
};

const DEFAULT_INVENTORY_FORM_DATA = {
  itemName: '',
  unit: 'Piece',
  stock: '',
  minLevel: '',
};

const DEFAULT_MENU_FORM_DATA = {
  name: '',
  price: 0,
  description: '',
  categoryId: null,
  category: '',
  variants: [],
};

const DEFAULT_CATEGORY_FORM_DATA = {
  name: '',
  code: '',
  description: '',
  displayOrder: 0,
  imageUrl: '',
  icon: '',
  color: '#2563eb',
  status: 'Active',
  categoryType: 'Sale',
  branchId: 0,
};

const DEFAULT_SUBCATEGORY_FORM_DATA = {
  name: '',
  code: '',
  description: '',
  displayOrder: 0,
  icon: '',
  status: 'Active',
  categoryId: 0,
  branchId: 0,
};

const DEFAULT_BRAND_FORM_DATA = {
  name: '',
  description: '',
  status: 'Active',
  branchId: 0,
};

const DEFAULT_WAREHOUSE_FORM_DATA = {
  name: '',
  code: '',
  address: '',
  status: 'Active',
  branchId: 0,
};

const DEFAULT_SUPPLIER_FORM_DATA = {
  name: '',
  contactPerson: '',
  phone: '',
  email: '',
  address: '',
  taxNumber: '',
  status: 'Active',
  branchId: 0,
};

const DEFAULT_CUSTOMER_FORM_DATA = {
  name: '',
  phone: '',
  email: '',
  address: '',
  countryId: 0,
  cityId: 0,
  cnic: '',
  customerType: 'Retail',
  creditLimit: '0',
  openingBalance: '0',
  status: 'Active',
  branchId: 0,
};

const DEFAULT_ROLE_FORM_DATA = {
  name: '',
  description: '',
  status: 'Active',
};

const DEFAULT_CASH_TRANSACTION_FORM_DATA = {
  transactionType: 'CashIn' as const,
};

const DEFAULT_RECEIVE_PAYMENT_FORM_DATA = {
  customerId: 0,
};

const DEFAULT_PAY_SUPPLIER_FORM_DATA = {
  supplierId: 0,
};

const DEFAULT_PURCHASE_FORM_DATA = {
  invoiceNo: '',
  supplierId: 0,
  warehouseId: 0,
  purchaseDate: new Date().toISOString().slice(0, 10),
  notes: '',
  branchId: 0,
  items: [],
};

interface FormModalContextType {
  isOpen: boolean;
  formType: FormType;
  editingId: string | number | null;
  editingData: any;
  openForm: (formType: FormType, editingData?: any) => void;
  closeForm: () => void;
  setEditingData: (data: any) => void;
}

const getDefaultFormData = (type: FormType) => {
  if (type === 'branch') {
    return DEFAULT_BRANCH_FORM_DATA;
  }

  if (type === 'user') {
    return DEFAULT_USER_FORM_DATA;
  }

  if (type === 'business') {
    return DEFAULT_BUSINESS_FORM_DATA;
  }

  if (type === 'inventory') {
    return DEFAULT_INVENTORY_FORM_DATA;
  }

  if (type === 'menu') {
    return DEFAULT_MENU_FORM_DATA;
  }

  if (type === 'category') {
    return DEFAULT_CATEGORY_FORM_DATA;
  }

  if (type === 'subcategory') {
    return DEFAULT_SUBCATEGORY_FORM_DATA;
  }

  if (type === 'brand') {
    return DEFAULT_BRAND_FORM_DATA;
  }

  if (type === 'warehouse') {
    return DEFAULT_WAREHOUSE_FORM_DATA;
  }

  if (type === 'supplier') {
    return DEFAULT_SUPPLIER_FORM_DATA;
  }

  if (type === 'purchase') {
    return DEFAULT_PURCHASE_FORM_DATA;
  }

  if (type === 'customer') {
    return DEFAULT_CUSTOMER_FORM_DATA;
  }

  if (type === 'role') {
    return DEFAULT_ROLE_FORM_DATA;
  }

  if (type === 'cashTransaction') {
    return DEFAULT_CASH_TRANSACTION_FORM_DATA;
  }

  if (type === 'receivePayment') {
    return DEFAULT_RECEIVE_PAYMENT_FORM_DATA;
  }

  if (type === 'paySupplier') {
    return DEFAULT_PAY_SUPPLIER_FORM_DATA;
  }

  return {};
};

const FormModalContext = createContext<FormModalContextType | undefined>(undefined);

export const FormModalProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [isOpen, setIsOpen] = useState(false);
  const [formType, setFormType] = useState<FormType>(null);
  const [editingId, setEditingId] = useState<string | number | null>(null);
  const [editingData, setEditingDataState] = useState<any>({});

  const openForm = (type: FormType, data?: any) => {
    const payload = data ?? getDefaultFormData(type);
    const safePayload = payload ?? getDefaultFormData(type);

    setFormType(type);
    setEditingDataState(safePayload);
    setEditingId(safePayload?.id ?? null);
    setIsOpen(true);
  };

  const closeForm = () => {
    setIsOpen(false);
    setFormType(null);
    setEditingDataState({});
    setEditingId(null);
  };

  const setEditingData = (data: any) => {
    setEditingDataState(data ?? {});
  };

  return (
    <FormModalContext.Provider
      value={{
        isOpen,
        formType,
        editingId,
        editingData,
        openForm,
        closeForm,
        setEditingData,
      }}
    >
      {children}
    </FormModalContext.Provider>
  );
};

export const useFormModal = () => {
  const context = useContext(FormModalContext);
  if (!context) {
    throw new Error('useFormModal must be used within FormModalProvider');
  }
  return context;
};
