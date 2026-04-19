import React, { useState } from 'react';
import { useFormModal } from '../contexts/FormModalContext';
import { BranchForm, UserForm, MenuForm, InventoryForm } from './forms';
import { BranchService, UserService, MenuService, InventoryService } from '../services/apiService';

const FormModal: React.FC = () => {
  const { isOpen, formType, editingData, closeForm } = useFormModal();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleBranchSubmit = async (data: any) => {
    setIsSubmitting(true);
    setError(null);
    try {
      if (editingData?.id) {
        // Update
        await BranchService.update(editingData.id, data);
      } else {
        // Create
        await BranchService.create(data);
      }
      closeForm();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to save branch');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleUserSubmit = async (data: any) => {
    setIsSubmitting(true);
    setError(null);
    try {
      if (editingData?.id) {
        await UserService.update(editingData.id, data);
      } else {
        await UserService.create(data);
      }
      closeForm();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to save user');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleMenuSubmit = async (data: any) => {
    setIsSubmitting(true);
    setError(null);
    try {
      if (editingData?.id) {
        await MenuService.update(editingData.id, data);
      } else {
        await MenuService.create(data);
      }
      closeForm();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to save menu item');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleInventorySubmit = async (data: any) => {
    setIsSubmitting(true);
    setError(null);
    try {
      if (editingData?.id) {
        await InventoryService.update(editingData.id, data);
      } else {
        await InventoryService.create(data);
      }
      closeForm();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to save inventory item');
    } finally {
      setIsSubmitting(false);
    }
  };

  if (!isOpen) return null;

  const getFormComponent = () => {
    switch (formType) {
      case 'branch':
        return (
          <BranchForm
            initialData={editingData}
            onSubmit={handleBranchSubmit}
            isLoading={isSubmitting}
            submitLabel={editingData ? 'Update Branch' : 'Create Branch'}
          />
        );
      case 'user':
        return (
          <UserForm
            initialData={editingData}
            onSubmit={handleUserSubmit}
            isLoading={isSubmitting}
            submitLabel={editingData ? 'Update User' : 'Create User'}
          />
        );
      case 'menu':
        return (
          <MenuForm
            initialData={editingData}
            onSubmit={handleMenuSubmit}
            isLoading={isSubmitting}
            submitLabel={editingData ? 'Update Item' : 'Create Item'}
          />
        );
      case 'inventory':
        return (
          <InventoryForm
            initialData={editingData}
            onSubmit={handleInventorySubmit}
            isLoading={isSubmitting}
            submitLabel={editingData ? 'Update Item' : 'Create Item'}
          />
        );
      default:
        return null;
    }
  };

  return (
    <>
      {/* Modal Backdrop */}
      <div
        className="fixed inset-0 bg-black bg-opacity-50 z-40 transition-opacity"
        onClick={closeForm}
      />

      {/* Modal Container */}
      <div className="fixed inset-0 z-50 overflow-y-auto flex items-center justify-center">
        <div
          className="bg-white rounded-lg shadow-xl max-w-2xl w-full mx-4 max-h-[90vh] overflow-y-auto"
          onClick={(e) => e.stopPropagation()}
        >
          {/* Modal Header */}
          <div className="sticky top-0 bg-white border-b border-gray-200 px-6 py-4 flex justify-between items-center">
            <h2 className="text-xl font-semibold text-gray-900">
              {editingData ? 'Edit' : 'Create'} {formType?.charAt(0).toUpperCase() + formType?.slice(1)}
            </h2>
            <button
              onClick={closeForm}
              className="text-gray-400 hover:text-gray-600 transition-colors"
            >
              <svg className="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M6 18L18 6M6 6l12 12"
                />
              </svg>
            </button>
          </div>

          {/* Error Message */}
          {error && (
            <div className="border-b border-red-200 bg-red-50 px-6 py-4">
              <p className="text-sm text-red-700">{error}</p>
            </div>
          )}

          {/* Modal Body */}
          <div className="p-6">
            {getFormComponent()}
          </div>
        </div>
      </div>
    </>
  );
};

export default FormModal;
