import React, { useEffect, useState } from 'react';
import BranchSelector from '../shared/BranchSelector';
import { useBranchStore } from '../../stores/useBranchStore';
import { subCategoryService, SubCategoryPayload } from './subcategoryService';
import { getApiErrorMessage } from '../../services/api';
import { categoryService } from '../category/categoryService';

interface CategoryOption {
  id: number;
  name: string;
}

interface SubCategoryFormProps {
  isOpen: boolean;
  isEditMode: boolean;
  initialData?: Partial<SubCategoryPayload> | null;
  isSubmitting: boolean;
  onCancel: () => void;
  onSubmit: (data: SubCategoryPayload) => Promise<void>;
}

const initialFormValues: SubCategoryPayload = {
  name: '',
  description: '',
  displayOrder: 0,
  status: true,
  icon: '',
  categoryId: 0,
  branchId: 0,
};

const SubCategoryForm: React.FC<SubCategoryFormProps> = ({
  isOpen,
  isEditMode,
  initialData,
  isSubmitting,
  onCancel,
  onSubmit,
}) => {
  const selectedBranchId = useBranchStore((state) => state.selectedBranchId);
  const [formData, setFormData] = useState<SubCategoryPayload>(initialFormValues);
  const [categories, setCategories] = useState<CategoryOption[]>([]);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    setFormData({
      name: String(initialData?.name ?? ''),
      description: String(initialData?.description ?? ''),
      displayOrder: Number(initialData?.displayOrder ?? 0),
      status: Boolean(initialData?.status ?? true),
      icon: String(initialData?.icon ?? ''),
      categoryId: Number(initialData?.categoryId ?? 0),
      branchId: Number(initialData?.branchId ?? selectedBranchId ?? 0),
    });
    setError('');
  }, [initialData, isOpen, selectedBranchId]);

  useEffect(() => {
    if (!isOpen || !selectedBranchId) {
      setCategories([]);
      return;
    }

    const loadCategories = async () => {
      try {
        const response = await categoryService.getAll(selectedBranchId);
        const rows = Array.isArray(response.data?.categories) ? response.data.categories : [];
        setCategories(
          rows.map((row: Record<string, unknown>) => ({
            id: Number(row.id ?? row.Id),
            name: String(row.name ?? row.Name ?? ''),
          }))
        );
      } catch (err) {
        setCategories([]);
        setError(getApiErrorMessage(err, 'Failed to load categories for selected branch.'));
      }
    };

    void loadCategories();
  }, [isOpen, selectedBranchId]);

  if (!isOpen) {
    return null;
  }

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!selectedBranchId || selectedBranchId <= 0) {
      setError('Branch selection is required.');
      return;
    }

    if (!formData.categoryId || formData.categoryId <= 0) {
      setError('Category selection is required.');
      return;
    }

    if (!formData.name.trim()) {
      setError('SubCategory name is required.');
      return;
    }

    await onSubmit({
      ...formData,
      name: formData.name.trim(),
      branchId: selectedBranchId,
    });
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="w-full max-w-lg rounded-xl bg-white shadow-xl">
        <div className="border-b border-gray-200 px-6 py-4">
          <h3 className="text-lg font-semibold text-gray-900">
            {isEditMode ? 'Edit SubCategory' : 'Add SubCategory'}
          </h3>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4 px-6 py-5">
          {error && <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{error}</div>}

          <BranchSelector />

          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Category *</label>
            <select
              value={formData.categoryId}
              onChange={(event) => setFormData((prev) => ({ ...prev, categoryId: Number(event.target.value || 0) }))}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
            >
              <option value={0}>Select Category</option>
              {categories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Name *</label>
            <input
              type="text"
              value={formData.name}
              onChange={(event) => setFormData((prev) => ({ ...prev, name: event.target.value }))}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
              placeholder="SubCategory name"
            />
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Description</label>
            <textarea
              value={formData.description}
              onChange={(event) => setFormData((prev) => ({ ...prev, description: event.target.value }))}
              className="h-24 w-full resize-none rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
              placeholder="Description"
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">Display Order</label>
              <input
                type="number"
                min={0}
                value={formData.displayOrder}
                onChange={(event) => setFormData((prev) => ({ ...prev, displayOrder: Number(event.target.value || 0) }))}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
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

          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Icon</label>
            <input
              type="text"
              value={formData.icon}
              onChange={(event) => setFormData((prev) => ({ ...prev, icon: event.target.value }))}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
              placeholder="fa-solid fa-layer-group"
            />
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

export default SubCategoryForm;
