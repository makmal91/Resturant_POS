import React, { useEffect, useState } from 'react';
import BranchSelector from '../shared/BranchSelector';
import { useBranchStore } from '../../stores/useBranchStore';
import { getApiErrorMessage } from '../../services/api';
import { categoryService } from '../category/categoryService';
import SubCategoryForm from './SubCategoryForm';
import { subCategoryService, SubCategoryPayload } from './subcategoryService';

interface CategoryOption {
  id: number;
  name: string;
}

interface SubCategoryItem extends SubCategoryPayload {
  id: number;
  categoryName?: string;
  branchName?: string;
}

const SubCategoryPage: React.FC = () => {
  const branches = useBranchStore((state) => state.branches);
  const selectedBranchId = useBranchStore((state) => state.selectedBranchId);
  const fetchBranches = useBranchStore((state) => state.fetchBranches);

  const [categories, setCategories] = useState<CategoryOption[]>([]);
  const [selectedCategoryId, setSelectedCategoryId] = useState<number | null>(null);
  const [items, setItems] = useState<SubCategoryItem[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<SubCategoryItem | null>(null);

  const loadCategories = async (branchId: number) => {
    const response = await categoryService.getAll(branchId);
    const rows = Array.isArray(response.data?.categories) ? response.data.categories : [];
    setCategories(
      rows.map((row: Record<string, unknown>) => ({
        id: Number(row.id ?? row.Id),
        name: String(row.name ?? row.Name ?? ''),
      }))
    );
  };

  const loadSubCategories = async (branchId: number, categoryId?: number) => {
    setIsLoading(true);
    setErrorMessage('');

    try {
      const response = await subCategoryService.getAll(branchId, categoryId);
      const rows = Array.isArray(response.data?.subCategories) ? response.data.subCategories : [];
      setItems(
        rows.map((row: Record<string, unknown>) => ({
          id: Number(row.id ?? row.Id),
          name: String(row.name ?? row.Name ?? ''),
          description: String(row.description ?? row.Description ?? ''),
          displayOrder: Number(row.displayOrder ?? row.DisplayOrder ?? 0),
          status: Boolean(row.status ?? row.Status ?? true),
          icon: String(row.icon ?? row.Icon ?? ''),
          categoryId: Number(row.categoryId ?? row.CategoryId ?? 0),
          categoryName: String(row.categoryName ?? row.CategoryName ?? ''),
          branchId: Number(row.branchId ?? row.BranchId ?? branchId),
          branchName: String(row.branchName ?? row.BranchName ?? ''),
        }))
      );
    } catch (error) {
      setItems([]);
      setErrorMessage(getApiErrorMessage(error, 'Failed to load subcategories.'));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void fetchBranches();
  }, [fetchBranches]);

  useEffect(() => {
    if (!selectedBranchId) {
      setCategories([]);
      setItems([]);
      setSelectedCategoryId(null);
      return;
    }

    const run = async () => {
      try {
        await loadCategories(selectedBranchId);
        await loadSubCategories(selectedBranchId);
      } catch (error) {
        setErrorMessage(getApiErrorMessage(error, 'Failed to initialize subcategory screen.'));
      }
    };

    void run();
  }, [selectedBranchId]);

  useEffect(() => {
    if (!selectedBranchId) {
      return;
    }

    void loadSubCategories(selectedBranchId, selectedCategoryId ?? undefined);
  }, [selectedBranchId, selectedCategoryId]);

  const openCreate = () => {
    setEditingItem(null);
    setErrorMessage('');
    setSuccessMessage('');
    setIsModalOpen(true);
  };

  const openEdit = async (item: SubCategoryItem) => {
    if (!selectedBranchId) {
      setErrorMessage('Please select a branch first.');
      return;
    }

    try {
      const response = await subCategoryService.getById(item.id, selectedBranchId);
      const data = response.data as Record<string, unknown>;
      setEditingItem({
        id: Number(data.id ?? data.Id ?? item.id),
        name: String(data.name ?? data.Name ?? item.name),
        description: String(data.description ?? data.Description ?? item.description),
        displayOrder: Number(data.displayOrder ?? data.DisplayOrder ?? item.displayOrder),
        status: Boolean(data.status ?? data.Status ?? item.status),
        icon: String(data.icon ?? data.Icon ?? item.icon),
        categoryId: Number(data.categoryId ?? data.CategoryId ?? item.categoryId),
        categoryName: String(data.categoryName ?? data.CategoryName ?? item.categoryName ?? ''),
        branchId: Number(data.branchId ?? data.BranchId ?? item.branchId),
        branchName: String(data.branchName ?? data.BranchName ?? item.branchName ?? ''),
      });
      setIsModalOpen(true);
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error, 'Failed to load subcategory details.'));
    }
  };

  const closeModal = () => {
    setIsModalOpen(false);
    setEditingItem(null);
  };

  const handleSubmit = async (data: SubCategoryPayload) => {
    if (!selectedBranchId) {
      setErrorMessage('Please select a branch first.');
      return;
    }

    setIsSaving(true);
    setErrorMessage('');

    try {
      const payload = { ...data, branchId: selectedBranchId };
      if (editingItem) {
        await subCategoryService.update(editingItem.id, payload);
        setSuccessMessage('SubCategory updated successfully.');
      } else {
        await subCategoryService.create(payload);
        setSuccessMessage('SubCategory created successfully.');
      }

      closeModal();
      await loadSubCategories(selectedBranchId, selectedCategoryId ?? undefined);
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error, 'Failed to save subcategory.'));
    } finally {
      setIsSaving(false);
    }
  };

  const handleDelete = async (item: SubCategoryItem) => {
    if (!selectedBranchId) {
      setErrorMessage('Please select a branch first.');
      return;
    }

    const confirmed = window.confirm(`Delete subcategory "${item.name}"?`);
    if (!confirmed) {
      return;
    }

    setIsSaving(true);
    setErrorMessage('');

    try {
      await subCategoryService.delete(item.id, selectedBranchId);
      setSuccessMessage('SubCategory deleted successfully.');
      await loadSubCategories(selectedBranchId, selectedCategoryId ?? undefined);
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error, 'Failed to delete subcategory.'));
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div>
      <div className="mb-6 flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">SubCategory Management</h1>
          <p className="text-gray-600">Strict branch + category scoped subcategory master.</p>
        </div>
        <button
          onClick={openCreate}
          disabled={!selectedBranchId || isSaving}
          className="rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
        >
          Add SubCategory
        </button>
      </div>

      <div className="mb-4 grid gap-4 rounded-xl border border-gray-200 bg-white p-4 md:grid-cols-2">
        <BranchSelector />
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">Category Filter</label>
          <select
            value={selectedCategoryId ?? ''}
            onChange={(event) => {
              const value = event.target.value;
              setSelectedCategoryId(value ? Number(value) : null);
            }}
            disabled={!selectedBranchId}
            className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none disabled:cursor-not-allowed disabled:bg-gray-100"
          >
            <option value="">All Categories</option>
            {categories.map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </select>
        </div>
      </div>

      {!selectedBranchId && <div className="mb-4 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-700">Branch selection is mandatory before any subcategory action.</div>}
      {errorMessage && <div className="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{errorMessage}</div>}
      {successMessage && <div className="mb-4 rounded-lg border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700">{successMessage}</div>}

      <div className="overflow-hidden rounded-xl border border-gray-200 bg-white">
        <table className="min-w-full divide-y divide-gray-200 text-sm">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-4 py-3 text-left font-semibold text-gray-700">SubCategory Name</th>
              <th className="px-4 py-3 text-left font-semibold text-gray-700">Category Name</th>
              <th className="px-4 py-3 text-left font-semibold text-gray-700">Branch Name</th>
              <th className="px-4 py-3 text-left font-semibold text-gray-700">Status</th>
              <th className="px-4 py-3 text-left font-semibold text-gray-700">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100 bg-white">
            {!selectedBranchId && (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center text-gray-500">
                  Branch selection is required.
                </td>
              </tr>
            )}

            {selectedBranchId && isLoading && (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center text-gray-500">
                  Loading subcategories...
                </td>
              </tr>
            )}

            {selectedBranchId && !isLoading && items.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center text-gray-500">
                  No subcategories found for selected branch.
                </td>
              </tr>
            )}

            {selectedBranchId && !isLoading && items.map((item) => (
              <tr key={item.id}>
                <td className="px-4 py-3">{item.name}</td>
                <td className="px-4 py-3">{item.categoryName || categories.find((category) => category.id === item.categoryId)?.name || '-'}</td>
                <td className="px-4 py-3">{item.branchName || branches.find((branch) => branch.id === item.branchId)?.name || '-'}</td>
                <td className="px-4 py-3">
                  <span className={`inline-flex rounded-full px-2 py-1 text-xs font-medium ${item.status ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-700'}`}>
                    {item.status ? 'Active' : 'Inactive'}
                  </span>
                </td>
                <td className="px-4 py-3">
                  <div className="flex gap-2">
                    <button
                      onClick={() => {
                        void openEdit(item);
                      }}
                      className="rounded border border-blue-200 px-3 py-1 text-xs font-medium text-blue-700 hover:bg-blue-50"
                    >
                      Edit
                    </button>
                    <button
                      onClick={() => {
                        void handleDelete(item);
                      }}
                      className="rounded border border-red-200 px-3 py-1 text-xs font-medium text-red-700 hover:bg-red-50"
                    >
                      Delete
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <SubCategoryForm
        isOpen={isModalOpen}
        isEditMode={Boolean(editingItem)}
        initialData={editingItem}
        isSubmitting={isSaving}
        onCancel={closeModal}
        onSubmit={handleSubmit}
      />
    </div>
  );
};

export default SubCategoryPage;
