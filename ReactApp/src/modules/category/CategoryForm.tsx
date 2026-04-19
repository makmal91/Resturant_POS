import React, { useEffect, useState } from 'react';
import { EntityFormProps } from '../shared/ManagementPage';
import { ManagementFormValues } from '../shared/types';

const CategoryForm: React.FC<EntityFormProps> = (props) => {
  const { isOpen, isEditMode, initialData, isSubmitting, onCancel, onSubmit } = props;
  const [error, setError] = useState('');
  const [formData, setFormData] = useState<ManagementFormValues>({
    name: '',
    description: '',
    isActive: true,
    branchId: 1,
    categoryType: 'Sale',
  });

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    setFormData({
      name: String(initialData?.name ?? ''),
      description: String(initialData?.description ?? ''),
      isActive: Boolean(initialData?.isActive ?? true),
      branchId: Number(initialData?.branchId ?? 1),
      categoryType: (initialData?.categoryType as 'Sale' | 'Inventory' | undefined) ?? 'Sale',
    });
    setError('');
  }, [initialData, isOpen]);

  if (!isOpen) {
    return null;
  }

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!String(formData.name ?? '').trim()) {
      setError('Category name is required.');
      return;
    }

    await onSubmit({
      ...formData,
      name: String(formData.name ?? '').trim(),
      branchId: Number(formData.branchId ?? 1),
      categoryType: formData.categoryType ?? 'Sale',
    });
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="w-full max-w-lg rounded-xl bg-white shadow-xl">
        <div className="border-b border-gray-200 px-6 py-4">
          <h3 className="text-lg font-semibold text-gray-900">
            {isEditMode ? 'Edit Category' : 'Add Category'}
          </h3>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4 px-6 py-5">
          {error && (
            <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
              {error}
            </div>
          )}

          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Name</label>
            <input
              type="text"
              value={String(formData.name ?? '')}
              onChange={(event) => setFormData((prev) => ({ ...prev, name: event.target.value }))}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
              placeholder="Category name"
            />
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Description</label>
            <textarea
              value={String(formData.description ?? '')}
              onChange={(event) => setFormData((prev) => ({ ...prev, description: event.target.value }))}
              className="h-24 w-full resize-none rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
              placeholder="Description"
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">Category Type</label>
              <select
                value={formData.categoryType ?? 'Sale'}
                onChange={(event) =>
                  setFormData((prev) => ({
                    ...prev,
                    categoryType: event.target.value as 'Sale' | 'Inventory',
                  }))
                }
                className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
              >
                <option value="Sale">Sale</option>
                <option value="Inventory">Inventory</option>
              </select>
            </div>

            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">Branch ID</label>
              <input
                type="number"
                min={1}
                value={Number(formData.branchId ?? 1)}
                onChange={(event) => setFormData((prev) => ({ ...prev, branchId: Number(event.target.value || 1) }))}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
              />
            </div>
          </div>

          <div className="rounded-md border border-blue-200 bg-blue-50 p-3 text-xs text-blue-700">
            Sale categories are for FinishedGood products in POS. Inventory categories are for RawMaterial and SemiFinished items.
          </div>

          <div className="flex justify-end gap-3 border-t border-gray-200 pt-4">
            <button
              type="button"
              onClick={onCancel}
              disabled={isSubmitting}
              className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-60"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isSubmitting ? 'Saving...' : isEditMode ? 'Update' : 'Create'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default CategoryForm;
