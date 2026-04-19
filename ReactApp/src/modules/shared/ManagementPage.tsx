import React, { useCallback, useEffect, useMemo, useState } from 'react';
import DataTable, { Action, Column } from '../../components/DataTable';
import { CrudEntityService, defaultManagementFormValues, ManagementEntity, ManagementFormValues } from './types';

export interface EntityFormProps {
  isOpen: boolean;
  isEditMode: boolean;
  initialData?: Partial<ManagementFormValues> | null;
  isSubmitting: boolean;
  onCancel: () => void;
  onSubmit: (data: ManagementFormValues) => Promise<void>;
}

interface ManagementPageProps {
  title: string;
  subtitle: string;
  entityLabel: string;
  service: CrudEntityService;
  FormComponent: React.ComponentType<EntityFormProps>;
}

const toRecord = (value: unknown): Record<string, unknown> => {
  if (typeof value === 'object' && value !== null) {
    return value as Record<string, unknown>;
  }

  return {};
};

const getIdValue = (record: Record<string, unknown>): number => {
  const rawId = record.id ?? record.Id;
  const id = Number(rawId);
  return Number.isFinite(id) ? id : 0;
};

const normalizeEntity = (rawItem: unknown): ManagementEntity => {
  const record = toRecord(rawItem);
  const statusValue = record.status ?? record.Status;

  return {
    id: getIdValue(record),
    name: String(record.name ?? record.Name ?? record.title ?? record.Title ?? ''),
    description: String(record.description ?? record.Description ?? record.details ?? record.Details ?? ''),
    isActive:
      typeof record.isActive === 'boolean'
        ? record.isActive
        : typeof record.IsActive === 'boolean'
        ? record.IsActive
        : typeof statusValue === 'string'
        ? statusValue.toLowerCase() === 'active'
        : true,
    branchId: Number(record.branchId ?? record.BranchId ?? 1),
    categoryType: String(record.categoryType ?? record.CategoryType ?? 'Sale') as
      | 'Sale'
      | 'Inventory',
    menuCategoryId: Number(record.menuCategoryId ?? record.MenuCategoryId ?? 0),
    price: Number(record.price ?? record.Price ?? 0),
    tax: Number(record.tax ?? record.Tax ?? record.taxPercentage ?? record.TaxPercentage ?? 0),
    preparationTime: Number(record.preparationTime ?? record.PreparationTime ?? 0),
    productType: String(record.productType ?? record.ProductType ?? 'FinishedGood') as
      | 'RawMaterial'
      | 'FinishedGood'
      | 'SemiFinished'
      | 'Service',
    isSaleable: Boolean(record.isSaleable ?? record.IsSaleable ?? false),
    isInventoryItem: Boolean(record.isInventoryItem ?? record.IsInventoryItem ?? false),
    isRecipeItem: Boolean(record.isRecipeItem ?? record.IsRecipeItem ?? false),
    isPurchasable: Boolean(record.isPurchasable ?? record.IsPurchasable ?? false),
    variants: Array.isArray(record.variants)
      ? (record.variants as Array<Record<string, unknown>>).map((v) => ({
          name: String(v.name ?? v.Name ?? ''),
          price: Number(v.price ?? v.Price ?? 0),
        }))
      : [],
    addons: Array.isArray(record.addons)
      ? (record.addons as Array<Record<string, unknown>>).map((a) => ({
          name: String(a.name ?? a.Name ?? ''),
          price: Number(a.price ?? a.Price ?? 0),
        }))
      : [],
  };
};

const extractEntityList = (payload: unknown): ManagementEntity[] => {
  if (Array.isArray(payload)) {
    return payload.map(normalizeEntity);
  }

  const record = toRecord(payload);
  const candidateArrays = [
    record.data,
    record.items,
    record.results,
    record.categories,
    record.products,
    record.customers,
    record.suppliers,
    record.units,
    record.taxes,
    record.discounts,
  ];

  for (const candidate of candidateArrays) {
    if (Array.isArray(candidate)) {
      return candidate.map(normalizeEntity);
    }
  }

  return [];
};

const ManagementPage: React.FC<ManagementPageProps> = ({
  title,
  subtitle,
  entityLabel,
  service,
  FormComponent,
}) => {
  const [items, setItems] = useState<ManagementEntity[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string>('');
  const [successMessage, setSuccessMessage] = useState<string>('');
  const [isModalOpen, setIsModalOpen] = useState<boolean>(false);
  const [selectedEntity, setSelectedEntity] = useState<ManagementEntity | null>(null);

  const isEditMode = selectedEntity !== null;

  const initialFormData = useMemo<ManagementFormValues>(
    () => ({
      name: selectedEntity?.name ?? defaultManagementFormValues.name,
      description: selectedEntity?.description ?? defaultManagementFormValues.description,
      isActive: selectedEntity?.isActive ?? defaultManagementFormValues.isActive,
      branchId: selectedEntity?.branchId,
      categoryType: selectedEntity?.categoryType,
      menuCategoryId: selectedEntity?.menuCategoryId,
      price: selectedEntity?.price,
      tax: selectedEntity?.tax,
      preparationTime: selectedEntity?.preparationTime,
      productType: selectedEntity?.productType,
      isSaleable: selectedEntity?.isSaleable,
      isInventoryItem: selectedEntity?.isInventoryItem,
      isRecipeItem: selectedEntity?.isRecipeItem,
      isPurchasable: selectedEntity?.isPurchasable,
      variants: selectedEntity?.variants,
      addons: selectedEntity?.addons,
    }),
    [selectedEntity]
  );

  const loadItems = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage('');

    try {
      const response = await service.getAll();
      const normalizedItems = extractEntityList(response?.data);
      const fallbackWithIds = normalizedItems.map((item, index) => ({
        ...item,
        id: item.id > 0 ? item.id : index + 1,
      }));
      setItems(fallbackWithIds);
    } catch {
      setErrorMessage(`Failed to load ${entityLabel.toLowerCase()} records.`);
    } finally {
      setIsLoading(false);
    }
  }, [entityLabel, service]);

  useEffect(() => {
    void loadItems();
  }, [loadItems]);

  const openAddModal = () => {
    setSelectedEntity(null);
    setSuccessMessage('');
    setErrorMessage('');
    setIsModalOpen(true);
  };

  const openEditModal = async (item: ManagementEntity) => {
    setErrorMessage('');
    setSuccessMessage('');

    try {
      const response = await service.getById(item.id);
      const details = normalizeEntity(response?.data ?? item);
      setSelectedEntity(details);
    } catch {
      setSelectedEntity(item);
    }

    setIsModalOpen(true);
  };

  const closeModal = () => {
    setIsModalOpen(false);
    setSelectedEntity(null);
  };

  const handleDelete = async (item: ManagementEntity) => {
    const confirmed = window.confirm(`Delete ${entityLabel} \"${item.name}\"?`);
    if (!confirmed) {
      return;
    }

    setIsSubmitting(true);
    setErrorMessage('');

    try {
      await service.delete(item.id);
      setSuccessMessage(`${entityLabel} deleted successfully.`);
      await loadItems();
    } catch {
      setErrorMessage(`Failed to delete ${entityLabel.toLowerCase()}.`);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleSubmit = async (data: ManagementFormValues) => {
    setIsSubmitting(true);
    setErrorMessage('');

    try {
      if (isEditMode && selectedEntity?.id) {
        await service.update(selectedEntity.id, data);
        setSuccessMessage(`${entityLabel} updated successfully.`);
      } else {
        await service.create(data);
        setSuccessMessage(`${entityLabel} created successfully.`);
      }

      closeModal();
      await loadItems();
    } catch {
      setErrorMessage(`Failed to save ${entityLabel.toLowerCase()}.`);
    } finally {
      setIsSubmitting(false);
    }
  };

  const columns: Column<ManagementEntity>[] = [
    {
      key: 'name',
      header: 'Name',
      sortable: true,
    },
    {
      key: 'description',
      header: 'Description',
      render: (value: unknown) => String(value ?? '-'),
    },
    {
      key: 'isActive',
      header: 'Status',
      render: (value: unknown) => (
        <span
          className={`inline-flex rounded-full px-2 py-1 text-xs font-medium ${
            Boolean(value) ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-700'
          }`}
        >
          {Boolean(value) ? 'Active' : 'Inactive'}
        </span>
      ),
      sortable: true,
    },
  ];

  const actions: Action<ManagementEntity>[] = [
    {
      label: 'Edit',
      onClick: (item) => {
        void openEditModal(item);
      },
      variant: 'primary',
    },
    {
      label: 'Delete',
      onClick: (item) => {
        void handleDelete(item);
      },
      variant: 'danger',
    },
  ];

  return (
    <div>
      <div className="mb-8 flex items-start justify-between">
        <div>
          <h1 className="mb-2 text-3xl font-bold text-gray-900">{title}</h1>
          <p className="text-gray-600">{subtitle}</p>
        </div>
        <button
          onClick={openAddModal}
          disabled={isSubmitting}
          className="inline-flex items-center rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
        >
          Add {entityLabel}
        </button>
      </div>

      {errorMessage && (
        <div className="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {errorMessage}
        </div>
      )}

      {successMessage && (
        <div className="mb-4 rounded-lg border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700">
          {successMessage}
        </div>
      )}

      <DataTable
        data={items}
        columns={columns}
        actions={actions}
        loading={isLoading}
        searchable={true}
        pagination={true}
        pageSize={10}
        emptyMessage={`No ${entityLabel.toLowerCase()} records found.`}
      />

      <FormComponent
        isOpen={isModalOpen}
        isEditMode={isEditMode}
        initialData={initialFormData}
        isSubmitting={isSubmitting}
        onCancel={closeModal}
        onSubmit={handleSubmit}
      />
    </div>
  );
};

export default ManagementPage;
