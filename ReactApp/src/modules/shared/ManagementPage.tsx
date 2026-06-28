import React, { useCallback, useEffect, useMemo, useState } from 'react';
import DataTable, { Action, Column } from '../../components/DataTable';
import { useModuleCrudAccess } from '../../hooks/useModuleCrudAccess';
import { getPermissionDeniedMessage } from '../../utils/permissionUtils';
import PermissionGate from '../../components/PermissionGate';
import { hasBranchContext } from '../../types/permissions';
import {
  CrudEntityService,
  defaultManagementFormValues,
  ManagementEntity,
  ManagementFormValues,
} from './types';
import { extractPagedMeta } from './pagedList';

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
  permissionModule?: string;
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
    code: String(record.code ?? record.Code ?? ''),
    description: String(record.description ?? record.Description ?? record.details ?? record.Details ?? ''),
    conversionFactor: Number(
      record.defaultConversionFactor ?? record.DefaultConversionFactor
      ?? record.conversionFactor ?? record.ConversionFactor ?? 1),
    defaultConversionFactor: Number(
      record.defaultConversionFactor ?? record.DefaultConversionFactor
      ?? record.conversionFactor ?? record.ConversionFactor ?? 1),
    isActive:
      typeof record.isActive === 'boolean'
        ? record.isActive
        : typeof record.IsActive === 'boolean'
        ? record.IsActive
        : typeof statusValue === 'string'
        ? statusValue.toLowerCase() === 'active'
        : typeof statusValue === 'boolean'
        ? statusValue
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

const extractEntityList = (payload: unknown, listKey?: string): ManagementEntity[] => {
  if (Array.isArray(payload)) {
    return payload.map(normalizeEntity);
  }

  const record = toRecord(payload);
  const candidateArrays = [
    listKey ? record[listKey] : undefined,
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
  permissionModule,
}) => {
  const {
    canAdd,
    canModify,
    canRemove,
    selectedBranchId,
    getWriteBlockMessage,
  } = useModuleCrudAccess(permissionModule ?? '', {
    requireBranchWrite: Boolean(permissionModule),
  });
  const hasBranchSelection = hasBranchContext(selectedBranchId);
  const usesServerSide = Boolean(service.getPaged);

  const [items, setItems] = useState<ManagementEntity[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string>('');
  const [successMessage, setSuccessMessage] = useState<string>('');
  const [isModalOpen, setIsModalOpen] = useState<boolean>(false);
  const [selectedEntity, setSelectedEntity] = useState<ManagementEntity | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [searchTerm, setSearchTerm] = useState('');
  const [sortColumn, setSortColumn] = useState('name');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages] = useState(0);

  const isEditMode = selectedEntity !== null;
  const branchId = hasBranchSelection && selectedBranchId !== null ? selectedBranchId : 0;

  const initialFormData = useMemo<ManagementFormValues>(
    () => ({
      name: selectedEntity?.name ?? defaultManagementFormValues.name,
      description: selectedEntity?.description ?? defaultManagementFormValues.description,
      isActive: selectedEntity?.isActive ?? defaultManagementFormValues.isActive,
      code: selectedEntity?.code,
      conversionFactor: selectedEntity?.defaultConversionFactor ?? selectedEntity?.conversionFactor,
      defaultConversionFactor: selectedEntity?.defaultConversionFactor ?? selectedEntity?.conversionFactor,
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
    if (usesServerSide && branchId <= 0) {
      setItems([]);
      setTotalRecords(0);
      setTotalPages(0);
      return;
    }

    setIsLoading(true);
    setErrorMessage('');

    try {
      if (usesServerSide && service.getPaged) {
        const response = await service.getPaged(branchId, {
          page: currentPage,
          pageSize,
          search: searchTerm.trim() || undefined,
          sortBy: sortColumn,
          sortDirection,
        });
        const payload = toRecord(response?.data);
        const normalizedItems = extractEntityList(payload, service.listKey);
        setItems(normalizedItems.filter((item) => item.id > 0));
        const meta = extractPagedMeta(payload);
        setTotalRecords(meta.totalRecords);
        setTotalPages(meta.totalPages);
      } else if (service.getAll) {
        const response = await service.getAll();
        const normalizedItems = extractEntityList(response?.data, service.listKey);
        setItems(normalizedItems.map((item, index) => ({
          ...item,
          id: item.id > 0 ? item.id : index + 1,
        })));
        setTotalRecords(0);
        setTotalPages(0);
      } else {
        setItems([]);
      }
    } catch {
      setErrorMessage(`Failed to load ${entityLabel.toLowerCase()} records.`);
      setItems([]);
      setTotalRecords(0);
      setTotalPages(0);
    } finally {
      setIsLoading(false);
    }
  }, [
    usesServerSide,
    branchId,
    service,
    currentPage,
    pageSize,
    searchTerm,
    sortColumn,
    sortDirection,
    entityLabel,
  ]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void loadItems();
    }, searchTerm && usesServerSide ? 300 : 0);
    return () => window.clearTimeout(timer);
  }, [loadItems, searchTerm, usesServerSide]);

  useEffect(() => {
    setCurrentPage(1);
  }, [branchId, pageSize]);

  const openAddModal = () => {
    const blockMessage = getWriteBlockMessage();
    if (permissionModule && (!canAdd || blockMessage)) {
      setErrorMessage(blockMessage ?? getPermissionDeniedMessage('create', permissionModule));
      return;
    }
    setSelectedEntity(null);
    setSuccessMessage('');
    setErrorMessage('');
    setIsModalOpen(true);
  };

  const openEditModal = async (item: ManagementEntity) => {
    const blockMessage = getWriteBlockMessage();
    if (permissionModule && (!canModify || blockMessage)) {
      setErrorMessage(blockMessage ?? getPermissionDeniedMessage('edit', permissionModule));
      return;
    }

    try {
      const response = await service.getById(item.id, item.branchId ?? branchId);
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
    const blockMessage = getWriteBlockMessage();
    if (permissionModule && (!canRemove || blockMessage)) {
      setErrorMessage(blockMessage ?? getPermissionDeniedMessage('delete', permissionModule));
      return;
    }

    const confirmed = window.confirm(`Delete ${entityLabel} "${item.name}"?`);
    if (!confirmed) {
      return;
    }

    setIsSubmitting(true);
    setErrorMessage('');

    try {
      await service.delete(item.id, item.branchId ?? branchId);
      setSuccessMessage(`${entityLabel} deleted successfully.`);
      await loadItems();
    } catch {
      setErrorMessage(`Failed to delete ${entityLabel.toLowerCase()}.`);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleSubmit = async (data: ManagementFormValues) => {
    const blockMessage = getWriteBlockMessage();
    const isEdit = Boolean(isEditMode && selectedEntity?.id);
    if (permissionModule) {
      if (isEdit && (!canModify || blockMessage)) {
        setErrorMessage(blockMessage ?? getPermissionDeniedMessage('edit', permissionModule));
        return;
      }
      if (!isEdit && (!canAdd || blockMessage)) {
        setErrorMessage(blockMessage ?? getPermissionDeniedMessage('create', permissionModule));
        return;
      }
    }

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
    ...(entityLabel === 'Unit'
      ? [
          {
            key: 'code' as keyof ManagementEntity,
            header: 'Short Code',
            render: (value: unknown) => String(value ?? '-'),
            sortable: true,
          },
          {
            key: 'defaultConversionFactor' as keyof ManagementEntity,
            header: 'Default Factor',
            render: (_value: unknown, row: ManagementEntity) =>
              Number(row.defaultConversionFactor ?? row.conversionFactor ?? 1).toString(),
            sortable: true,
          },
        ]
      : []),
    ...(entityLabel !== 'Unit'
      ? [
          {
            key: 'description' as keyof ManagementEntity,
            header: 'Description',
            render: (value: unknown) => String(value ?? '-'),
          },
        ]
      : []),
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
    ...(canModify || !permissionModule
      ? [{
          label: 'Edit',
          onClick: (item: ManagementEntity) => {
            void openEditModal(item);
          },
          variant: 'primary' as const,
        }]
      : []),
    ...(canRemove || !permissionModule
      ? [{
          label: 'Delete',
          onClick: (item: ManagementEntity) => {
            void handleDelete(item);
          },
          variant: 'danger' as const,
        }]
      : []),
  ];

  const tableEnabled = !usesServerSide || hasBranchSelection;

  return (
    <div>
      <div className="mb-8 flex items-start justify-between">
        <div>
          <h1 className="mb-2 text-3xl font-bold text-gray-900">{title}</h1>
          <p className="text-gray-600">{subtitle}</p>
        </div>
        {(!permissionModule || canAdd) && (
          permissionModule ? (
            <PermissionGate module={permissionModule} action="create">
              <button
                onClick={openAddModal}
                disabled={isSubmitting || (usesServerSide && !hasBranchSelection) || !canAdd}
                className="inline-flex items-center rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
              >
                Add {entityLabel}
              </button>
            </PermissionGate>
          ) : (
            <button
              onClick={openAddModal}
              disabled={isSubmitting || (usesServerSide && !hasBranchSelection)}
              className="inline-flex items-center rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
            >
              Add {entityLabel}
            </button>
          )
        )}
      </div>

      {usesServerSide && !hasBranchSelection && (
        <div className="mb-4 rounded-lg border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-700">
          Select a branch from the header to load {entityLabel.toLowerCase()} records.
        </div>
      )}

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
        searchable={tableEnabled}
        pagination={tableEnabled}
        pageSize={pageSize}
        pageSizeOptions={[5, 10, 25, 50]}
        onPageSizeChange={(size) => {
          setPageSize(size);
          setCurrentPage(1);
        }}
        emptyMessage={`No ${entityLabel.toLowerCase()} records found.`}
        serverSide={usesServerSide}
        totalRecords={usesServerSide ? totalRecords : undefined}
        totalPages={usesServerSide ? totalPages : undefined}
        currentPage={usesServerSide ? currentPage : undefined}
        onPageChange={usesServerSide ? setCurrentPage : undefined}
        searchTerm={usesServerSide ? searchTerm : undefined}
        onSearchChange={
          usesServerSide
            ? (value) => {
                setSearchTerm(value);
                setCurrentPage(1);
              }
            : undefined
        }
        sortColumn={usesServerSide ? sortColumn : undefined}
        sortDirection={usesServerSide ? sortDirection : undefined}
        onSortChange={
          usesServerSide
            ? (column, direction) => {
                setSortColumn(column);
                setSortDirection(direction);
                setCurrentPage(1);
              }
            : undefined
        }
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
