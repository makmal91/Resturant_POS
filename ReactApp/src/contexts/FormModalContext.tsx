import React, { createContext, useContext, useState } from 'react';

export type FormType = 'branch' | 'business' | 'user' | 'menu' | 'inventory' | 'category' | null;

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
  currency: 'USD',
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
