import React, { createContext, useContext, useState } from 'react';

export type FormType = 'branch' | 'user' | 'menu' | 'inventory' | null;

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
  if (type === 'menu') {
    return {
      name: '',
      price: 0,
      description: '',
      categoryId: null,
      category: '',
      variants: [],
    };
  }

  return null;
};

const FormModalContext = createContext<FormModalContextType | undefined>(undefined);

export const FormModalProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [isOpen, setIsOpen] = useState(false);
  const [formType, setFormType] = useState<FormType>(null);
  const [editingId, setEditingId] = useState<string | number | null>(null);
  const [editingData, setEditingDataState] = useState<any>(null);

  const openForm = (type: FormType, data?: any) => {
    const payload = data ?? getDefaultFormData(type);

    setFormType(type);
    setEditingDataState(payload);
    setEditingId(payload?.id ?? null);
    setIsOpen(true);
  };

  const closeForm = () => {
    setIsOpen(false);
    setFormType(null);
    setEditingDataState(null);
    setEditingId(null);
  };

  const setEditingData = (data: any) => {
    setEditingDataState(data);
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
