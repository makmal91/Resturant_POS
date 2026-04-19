import React, { useEffect, useState } from 'react';
import BranchSelector from '../shared/BranchSelector';
import { useBranchStore } from '../../stores/useBranchStore';
import { CategoryPayload } from './categoryService';

export interface CategoryFormProps {
  isOpen: boolean;
  isEditMode: boolean;
  initialData?: Partial<CategoryPayload> | null;
  isSubmitting: boolean;
  onCancel: () => void;
  onSubmit: (data: CategoryPayload) => Promise<void>;
}

const initialFormValues: CategoryPayload = {
  name: '',
  code: '',
  description: '',
  displayOrder: 0,
  imageUrl: '',
  icon: '',
  color: '#2563eb',
  status: true,
  categoryType: 'Sale',
  branchId: 0,
};

const CategoryForm: React.FC<CategoryFormProps> = ({ isOpen, isEditMode, initialData, isSubmitting, onCancel, onSubmit }) => {
  const selectedBranchId = useBranchStore((state) => state.selectedBranchId);
  const [error, setError] = useState('');
  const [formData, setFormData] = useState<CategoryPayload>(initialFormValues);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    setFormData({
      name: String(initialData?.name ?? ''),
      code: String(initialData?.code ?? ''),
      description: String(initialData?.description ?? ''),
      displayOrder: Number(initialData?.displayOrder ?? 0),
      imageUrl: String(initialData?.imageUrl ?? ''),
      icon: String(initialData?.icon ?? ''),
      color: String(initialData?.color ?? '#2563eb'),
      status: Boolean(initialData?.status ?? true),
      categoryType: (initialData?.categoryType ?? 'Sale') as 'Sale' | 'Inventory',
      branchId: Number(initialData?.branchId ?? selectedBranchId ?? 0),
    });
    setError('');
  }, [initialData, isOpen, selectedBranchId]);

  if (!isOpen) {
    return null;
  }

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!String(formData.name ?? '').trim()) {
      setError('Category name is required.');
      return;
    }

    if (!String(formData.code ?? '').trim()) {
      setError('Category code is required.');
      return;
    }

    if (!selectedBranchId || selectedBranchId <= 0) {
      setError('Branch selection is required.');
      return;
    }

    await onSubmit({
      ...formData,
      name: String(formData.name ?? '').trim(),
      code: String(formData.code ?? '').trim(),
      displayOrder: Number(formData.displayOrder ?? 0),
      branchId: selectedBranchId,
      categoryType: formData.categoryType ?? 'Sale',
    });
  };

  const handleIconUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      const result = typeof reader.result === 'string' ? reader.result : '';
      setFormData((prev) => ({ ...prev, imageUrl: result }));
    };
    reader.readAsDataURL(file);
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
            <label className="mb-1 block text-sm font-medium text-gray-700">Code</label>
            <input
              type="text"
              value={String(formData.code ?? '')}
              onChange={(event) => setFormData((prev) => ({ ...prev, code: event.target.value }))}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
              placeholder="CAT-BEVERAGE"
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

          <BranchSelector className="mb-2" />

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
              <label className="mb-1 block text-sm font-medium text-gray-700">Display Order</label>
              <input
                type="number"
                min={0}
                value={Number(formData.displayOrder ?? 0)}
                onChange={(event) => setFormData((prev) => ({ ...prev, displayOrder: Number(event.target.value || 0) }))}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">Color</label>
              <input
                type="color"
                value={String(formData.color ?? '#2563eb')}
                onChange={(event) => setFormData((prev) => ({ ...prev, color: event.target.value }))}
                className="h-10 w-full rounded-lg border border-gray-300 px-1"
              />
            </div>

            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">Status</label>
              <select
                value={formData.status ? 'true' : 'false'}
                onChange={(event) => setFormData((prev) => ({ ...prev, status: event.target.value === 'true' }))}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
              >
                <option value="true">Active</option>
                <option value="false">Inactive</option>
              </select>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">Icon</label>
              <input
                type="text"
                value={String(formData.icon ?? '')}
                onChange={(event) => setFormData((prev) => ({ ...prev, icon: event.target.value }))}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
                placeholder="fa-solid fa-burger"
              />
            </div>

            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">Image Upload</label>
              <input
                type="file"
                accept="image/*"
                onChange={handleIconUpload}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm"
              />
            </div>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Image URL</label>
            <input
              type="text"
              value={String(formData.imageUrl ?? '')}
              onChange={(event) => setFormData((prev) => ({ ...prev, imageUrl: event.target.value }))}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
              placeholder="https://cdn.example.com/category.png"
            />
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
