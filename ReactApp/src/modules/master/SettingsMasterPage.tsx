import React, { useCallback, useEffect, useMemo, useState } from 'react';
import DataTable, { type Action, type Column } from '../../components/DataTable';
import { FormButton, FormInput, FormSelect } from '../../components/forms';
import { usePermission } from '../../hooks/usePermission';
import { masterDataService, type MasterType } from '../../services/masterDataService';
import { masterService, type MasterManageItem, type SaveMasterPayload } from './masterService';

export interface SettingsMasterPageConfig {
  type: Extract<MasterType, 'country' | 'city'>;
  title: string;
  subtitle: string;
  entityLabel: string;
  permissionModule: string;
}

const emptyForm = (countryId = 0): SaveMasterPayload => ({
  name: '',
  code: '',
  isActive: true,
  countryId,
});

const SettingsMasterPage: React.FC<SettingsMasterPageConfig> = ({
  type,
  title,
  subtitle,
  entityLabel,
  permissionModule,
}) => {
  const { canCreate, canEdit, canDelete } = usePermission(permissionModule);
  const [items, setItems] = useState<MasterManageItem[]>([]);
  const [countries, setCountries] = useState<MasterManageItem[]>([]);
  const [selectedCountryId, setSelectedCountryId] = useState(0);
  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editing, setEditing] = useState<MasterManageItem | null>(null);
  const [formData, setFormData] = useState<SaveMasterPayload>(emptyForm());

  const loadCountries = useCallback(async () => {
    try {
      const rows = await masterService.listForManagement('country', 0, { includeInactive: true });
      setCountries(rows.filter((c) => c.isActive));
    } catch {
      setCountries([]);
    }
  }, []);

  const loadItems = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      if (type === 'country') {
        const rows = await masterService.listForManagement('country', 0, { includeInactive: true });
        setItems(rows);
        return;
      }

      if (selectedCountryId <= 0) {
        setItems([]);
        return;
      }

      const rows = await masterService.listForManagement('city', 0, {
        countryId: selectedCountryId,
        includeInactive: true,
      });
      setItems(rows);
    } catch (err) {
      setItems([]);
      setError(masterService.getErrorMessage(err, `Failed to load ${entityLabel.toLowerCase()} records.`));
    } finally {
      setLoading(false);
    }
  }, [type, selectedCountryId, entityLabel]);

  useEffect(() => {
    if (type === 'city') {
      void loadCountries();
    }
  }, [type, loadCountries]);

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
    if (!canCreate) {
      setError(`You do not have permission to create ${entityLabel.toLowerCase()} records.`);
      return;
    }
    if (type === 'city' && selectedCountryId <= 0) {
      setError('Select a country before adding a city.');
      return;
    }
    setEditing(null);
    setFormData(emptyForm(type === 'city' ? selectedCountryId : 0));
    setIsModalOpen(true);
  };

  const openEdit = (item: MasterManageItem) => {
    if (!canEdit) {
      setError(`You do not have permission to edit ${entityLabel.toLowerCase()} records.`);
      return;
    }
    setEditing(item);
    setFormData({
      name: item.name,
      code: item.description ?? '',
      isActive: item.isActive,
      countryId: item.countryId ?? selectedCountryId,
    });
    setIsModalOpen(true);
  };

  const handleDelete = async (item: MasterManageItem) => {
    if (!canDelete) {
      setError(`You do not have permission to delete ${entityLabel.toLowerCase()} records.`);
      return;
    }
    if (!window.confirm(`Deactivate ${entityLabel} "${item.name}"?`)) return;

    setSubmitting(true);
    setError('');
    try {
      await masterService.remove(type, item.id, 0, item.countryId ?? selectedCountryId);
      setSuccess(`${entityLabel} deactivated successfully.`);
      await loadItems();
      if (type === 'city') await loadCountries();
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
    if (type === 'country' && !String(formData.code ?? '').trim()) {
      setError('Country code is required.');
      return;
    }
    if (type === 'city' && Number(formData.countryId ?? selectedCountryId) <= 0) {
      setError('Country is required.');
      return;
    }

    setSubmitting(true);
    setError('');
    try {
      const payload: SaveMasterPayload = {
        name: String(formData.name).trim(),
        code: String(formData.code ?? '').trim(),
        isActive: Boolean(formData.isActive),
        countryId: type === 'city' ? Number(formData.countryId ?? selectedCountryId) : undefined,
      };

      if (editing) {
        await masterService.update(type, editing.id, 0, payload);
        setSuccess(`${entityLabel} updated successfully.`);
      } else {
        await masterService.create(type, 0, payload);
        setSuccess(`${entityLabel} created successfully.`);
      }

      setIsModalOpen(false);
      setEditing(null);
      masterDataService.clearMasterCache(type);
      if (type === 'city') {
        masterDataService.clearMasterCache('country');
        await loadCountries();
      }
      await loadItems();
    } catch (err) {
      setError(masterService.getErrorMessage(err, `Failed to save ${entityLabel.toLowerCase()}.`));
    } finally {
      setSubmitting(false);
    }
  };

  const columns: Column<MasterManageItem>[] = [
    { key: 'name', header: 'Name', sortable: true },
    ...(type === 'country'
      ? [{
          key: 'description' as keyof MasterManageItem,
          header: 'Code',
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
  if (canEdit) {
    actions.push({ label: 'Edit', onClick: openEdit, variant: 'primary' });
  }
  if (canDelete) {
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
          disabled={!canCreate || submitting || (type === 'city' && selectedCountryId <= 0)}
          className="inline-flex items-center rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
        >
          Add {entityLabel}
        </button>
      </div>

      {type === 'city' && (
        <div className="mb-6 max-w-sm">
          <FormSelect
            label="Country"
            name="countryId"
            value={selectedCountryId || ''}
            onChange={(e) => setSelectedCountryId(Number(e.target.value || 0))}
            placeholder="Select country"
            options={countries.map((c) => ({ label: c.name, value: c.id }))}
          />
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
        searchable
        searchPlaceholder={`Search ${entityLabel.toLowerCase()}...`}
        searchTerm={searchTerm}
        onSearchChange={setSearchTerm}
        pagination
        pageSize={10}
        emptyMessage={type === 'city' && selectedCountryId <= 0 ? 'Select a country to view cities.' : `No ${entityLabel.toLowerCase()} records found.`}
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
              {type === 'city' && (
                <FormSelect
                  label="Country"
                  name="countryId"
                  value={formData.countryId || selectedCountryId || ''}
                  onChange={(e) => setFormData((prev) => ({ ...prev, countryId: Number(e.target.value || 0) }))}
                  options={countries.map((c) => ({ label: c.name, value: c.id }))}
                  required
                />
              )}
              <FormInput
                label="Name"
                name="name"
                value={formData.name}
                onChange={(e) => setFormData((prev) => ({ ...prev, name: e.target.value }))}
                required
              />
              {type === 'country' && (
                <FormInput
                  label="Code"
                  name="code"
                  value={String(formData.code ?? '')}
                  onChange={(e) => setFormData((prev) => ({ ...prev, code: e.target.value.toUpperCase() }))}
                  placeholder="e.g. PK, US"
                  required
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

export default SettingsMasterPage;
