import React, { useCallback, useEffect, useMemo, useState } from 'react';
import DataTable, { type Action, type Column } from '../../components/DataTable';
import { FormButton, FormInput, FormSelect, FormTextarea, FormColorPicker } from '../../components/forms';
import { useBranchWriteAccess } from '../../hooks/useBranchWriteAccess';
import { usePermission } from '../../hooks/usePermission';
import { hasBranchContext } from '../../types/permissions';
import { type MasterType } from '../../services/masterDataService';
import { masterService, type MasterManageItem, type SaveMasterPayload } from './masterService';

export interface BranchMasterPageConfig {
  type: MasterType;
  title: string;
  subtitle: string;
  entityLabel: string;
  permissionModule: string;
  showSortOrder?: boolean;
  showHexCode?: boolean;
  showDescription?: boolean;
}

const emptyForm = (): SaveMasterPayload => ({
  name: '',
  description: '',
  hexCode: '',
  sortOrder: 0,
  isActive: true,
  branchId: 0,
});

const BranchMasterPage: React.FC<BranchMasterPageConfig> = ({
  type,
  title,
  subtitle,
  entityLabel,
  permissionModule,
  showSortOrder = false,
  showHexCode = false,
  showDescription = false,
}) => {
  const { selectedBranchId, canWriteInView, getWriteBlockMessage } = useBranchWriteAccess();
  const { canCreate, canEdit, canDelete } = usePermission(permissionModule);
  const hasBranchSelection = hasBranchContext(selectedBranchId);
  const branchId = hasBranchSelection && selectedBranchId !== null ? selectedBranchId : 0;

  const canAdd = canWriteInView && canCreate;
  const canModify = canWriteInView && canEdit;
  const canRemove = canWriteInView && canDelete;

  const [items, setItems] = useState<MasterManageItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editing, setEditing] = useState<MasterManageItem | null>(null);
  const [formData, setFormData] = useState<SaveMasterPayload>(emptyForm());

  const loadItems = useCallback(async () => {
    if (branchId <= 0) {
      setItems([]);
      return;
    }

    setLoading(true);
    setError('');
    try {
      const rows = await masterService.listForManagement(type, branchId, { includeInactive: true });
      setItems(rows);
    } catch (err) {
      setItems([]);
      setError(masterService.getErrorMessage(err, `Failed to load ${entityLabel.toLowerCase()} records.`));
    } finally {
      setLoading(false);
    }
  }, [branchId, type, entityLabel]);

  useEffect(() => {
    void loadItems();
  }, [loadItems]);

  const filteredItems = useMemo(() => {
    const term = searchTerm.trim().toLowerCase();
    if (!term) return items;
    return items.filter((item) =>
      item.name.toLowerCase().includes(term) ||
      (item.description ?? '').toLowerCase().includes(term));
  }, [items, searchTerm]);

  const openCreate = () => {
    const block = getWriteBlockMessage();
    if (!canAdd || block) {
      setError(block ?? `You do not have permission to create ${entityLabel.toLowerCase()} records.`);
      return;
    }
    setEditing(null);
    setFormData({ ...emptyForm(), branchId });
    setIsModalOpen(true);
  };

  const openEdit = (item: MasterManageItem) => {
    const block = getWriteBlockMessage();
    if (!canModify || block) {
      setError(block ?? `You do not have permission to edit ${entityLabel.toLowerCase()} records.`);
      return;
    }
    setEditing(item);
    setFormData({
      name: item.name,
      description: item.description ?? '',
      hexCode: item.hexCode ?? '',
      sortOrder: item.sortOrder ?? 0,
      isActive: item.isActive,
      branchId,
    });
    setIsModalOpen(true);
  };

  const handleDelete = async (item: MasterManageItem) => {
    const block = getWriteBlockMessage();
    if (!canRemove || block) {
      setError(block ?? `You do not have permission to delete ${entityLabel.toLowerCase()} records.`);
      return;
    }

    if (!window.confirm(`Delete ${entityLabel} "${item.name}"?`)) return;

    setSubmitting(true);
    setError('');
    try {
      await masterService.remove(type, item.id, branchId);
      setSuccess(`${entityLabel} deleted successfully.`);
      await loadItems();
    } catch (err) {
      setError(masterService.getErrorMessage(err, `Failed to delete ${entityLabel.toLowerCase()}.`));
    } finally {
      setSubmitting(false);
    }
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!String(formData.name).trim()) {
      setError('Name is required.');
      return;
    }

    setSubmitting(true);
    setError('');
    try {
      const payload: SaveMasterPayload = {
        ...formData,
        name: String(formData.name).trim(),
        description: String(formData.description ?? '').trim(),
        hexCode: String(formData.hexCode ?? '').trim(),
        sortOrder: Number(formData.sortOrder ?? 0),
        branchId,
      };

      if (editing) {
        await masterService.update(type, editing.id, branchId, payload);
        setSuccess(`${entityLabel} updated successfully.`);
      } else {
        await masterService.create(type, branchId, payload);
        setSuccess(`${entityLabel} created successfully.`);
      }

      setIsModalOpen(false);
      setEditing(null);
      await loadItems();
    } catch (err) {
      setError(masterService.getErrorMessage(err, `Failed to save ${entityLabel.toLowerCase()}.`));
    } finally {
      setSubmitting(false);
    }
  };

  const columns: Column<MasterManageItem>[] = [
    { key: 'name', header: 'Name', sortable: true },
    ...(showSortOrder
      ? [{
          key: 'sortOrder' as keyof MasterManageItem,
          header: 'Sort Order',
          render: (value: unknown) => String(value ?? 0),
        }]
      : []),
    ...(showHexCode
      ? [{
          key: 'hexCode' as keyof MasterManageItem,
          header: 'Hex Code',
          render: (_value: unknown, item: MasterManageItem) => (
            <div className="flex items-center gap-2">
              {item.hexCode && (
                <span
                  className="inline-block h-4 w-4 rounded border border-gray-300"
                  style={{ backgroundColor: item.hexCode }}
                />
              )}
              <span>{item.hexCode ?? '—'}</span>
            </div>
          ),
        }]
      : []),
    ...(showDescription
      ? [{
          key: 'description' as keyof MasterManageItem,
          header: 'Description',
          render: (value: unknown) => String(value ?? '—'),
        }]
      : []),
    {
      key: 'isActive',
      header: 'Status',
      render: (value: unknown) => (
        <span className={`inline-flex rounded-full px-2 py-1 text-xs font-medium ${
          Boolean(value) ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-700'
        }`}>
          {Boolean(value) ? 'Active' : 'Inactive'}
        </span>
      ),
    },
  ];

  const actions: Action<MasterManageItem>[] = [];
  if (canModify) {
    actions.push({
      label: 'Edit',
      onClick: openEdit,
      variant: 'primary',
    });
  }
  if (canRemove) {
    actions.push({
      label: 'Delete',
      onClick: (item) => { void handleDelete(item); },
      variant: 'danger',
    });
  }

  return (
    <div>
      <div className="mb-8 flex items-start justify-between">
        <div>
          <h1 className="mb-2 text-3xl font-bold text-gray-900">{title}</h1>
          <p className="text-gray-600">{subtitle}</p>
        </div>
        <button
          type="button"
          onClick={openCreate}
          disabled={!canAdd || branchId <= 0 || submitting}
          className="inline-flex items-center rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
        >
          Add {entityLabel}
        </button>
      </div>

      {!hasBranchSelection && (
        <div className="mb-4 rounded-lg border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-700">
          Select a branch from the header to manage {entityLabel.toLowerCase()} records.
        </div>
      )}

      {error && (
        <div className="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{error}</div>
      )}
      {success && (
        <div className="mb-4 rounded-lg border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700">{success}</div>
      )}

      <DataTable
        data={filteredItems}
        columns={columns}
        actions={actions}
        loading={loading}
        searchable={hasBranchSelection}
        searchPlaceholder={`Search ${entityLabel.toLowerCase()}...`}
        searchTerm={searchTerm}
        onSearchChange={setSearchTerm}
        pagination
        pageSize={10}
        emptyMessage={hasBranchSelection ? `No ${entityLabel.toLowerCase()} records found.` : 'Select a branch to continue.'}
      />

      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div className="w-full max-w-lg rounded-xl bg-white shadow-xl">
            <div className="border-b border-gray-200 px-6 py-4">
              <h3 className="text-lg font-semibold text-gray-900">
                {editing ? `Edit ${entityLabel}` : `Add ${entityLabel}`}
              </h3>
            </div>
            <form onSubmit={handleSubmit} className="px-6 py-5 space-y-4">
              <FormInput
                label="Name"
                name="name"
                value={formData.name}
                onChange={(e) => setFormData((prev) => ({ ...prev, name: e.target.value }))}
                required
              />
              {showSortOrder && (
                <FormInput
                  label="Sort Order"
                  name="sortOrder"
                  type="number"
                  value={Number(formData.sortOrder ?? 0)}
                  onChange={(e) => setFormData((prev) => ({ ...prev, sortOrder: Number(e.target.value || 0) }))}
                />
              )}
              {showHexCode && (
                <FormColorPicker
                  label="Color (Hex)"
                  name="hexCode"
                  value={String(formData.hexCode ?? '')}
                  onChange={(hexCode) => setFormData((prev) => ({ ...prev, hexCode }))}
                  placeholder="#000000"
                />
              )}
              {showDescription && (
                <FormTextarea
                  label="Description"
                  name="description"
                  value={String(formData.description ?? '')}
                  onChange={(e) => setFormData((prev) => ({ ...prev, description: e.target.value }))}
                  rows={3}
                />
              )}
              <FormSelect
                label="Status"
                name="isActive"
                value={formData.isActive ? 'true' : 'false'}
                onChange={(e) => setFormData((prev) => ({ ...prev, isActive: e.target.value === 'true' }))}
                options={[
                  { label: 'Active', value: 'true' },
                  { label: 'Inactive', value: 'false' },
                ]}
              />
              <div className="flex justify-end gap-3 pt-2">
                <FormButton type="button" label="Cancel" variant="secondary" onClick={() => setIsModalOpen(false)} disabled={submitting} />
                <FormButton type="submit" label={editing ? 'Update' : 'Create'} variant="primary" loading={submitting} />
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default BranchMasterPage;
