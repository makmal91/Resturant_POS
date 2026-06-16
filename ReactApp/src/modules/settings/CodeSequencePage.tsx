import React, { useCallback, useEffect, useState } from 'react';
import DataTable, { Column } from '../../components/DataTable';
import { useBranchStore } from '../../stores/useBranchStore';
import { usePermission } from '../../hooks/usePermission';
import { getApiErrorMessage } from '../../services/api';
import { codeSequenceService, CodeSequenceItem } from './codeSequenceService';

const CodeSequencePage: React.FC = () => {
  const selectedBranchId = useBranchStore((state) => state.selectedBranchId);
  const branchId =
    selectedBranchId !== null && selectedBranchId > 0 ? selectedBranchId : undefined;
  const { canEdit } = usePermission('Code Sequences');

  const [items, setItems] = useState<CodeSequenceItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editValue, setEditValue] = useState('');
  const [saving, setSaving] = useState(false);

  const loadItems = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const data = await codeSequenceService.getAll(branchId);
      setItems(data);
    } catch (err) {
      setItems([]);
      setError(getApiErrorMessage(err, 'Failed to load code sequences.'));
    } finally {
      setLoading(false);
    }
  }, [branchId]);

  useEffect(() => {
    void loadItems();
  }, [loadItems]);

  const startEdit = (item: CodeSequenceItem) => {
    setEditingId(item.id);
    setEditValue(String(item.lastNumber));
    setSuccess('');
    setError('');
  };

  const cancelEdit = () => {
    setEditingId(null);
    setEditValue('');
  };

  const saveEdit = async (id: number) => {
    const lastNumber = Number(editValue);
    if (!Number.isFinite(lastNumber) || lastNumber < 0) {
      setError('Last number must be zero or greater.');
      return;
    }

    setSaving(true);
    setError('');
    setSuccess('');
    try {
      await codeSequenceService.updateLastNumber(id, lastNumber);
      setSuccess('Sequence updated successfully.');
      setEditingId(null);
      setEditValue('');
      await loadItems();
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to update sequence.'));
    } finally {
      setSaving(false);
    }
  };

  const columns: Column<CodeSequenceItem>[] = [
    {
      key: 'moduleName',
      header: 'Module',
      render: (value) => <span className="font-medium text-gray-900">{String(value)}</span>,
    },
    {
      key: 'branchName',
      header: 'Branch',
      render: (value) => <span className="text-gray-700">{String(value ?? 'Global')}</span>,
    },
    {
      key: 'prefix',
      header: 'Prefix',
      render: (value) => <span className="font-mono text-sm">{String(value)}</span>,
    },
    {
      key: 'lastNumber',
      header: 'Last Number',
      render: (_value, item) =>
        editingId === item.id ? (
          <input
            type="number"
            min={0}
            value={editValue}
            onChange={(e) => setEditValue(e.target.value)}
            className="w-28 rounded border border-gray-300 px-2 py-1 text-sm"
          />
        ) : (
          <span className="font-mono text-sm">{item.lastNumber}</span>
        ),
    },
    {
      key: 'nextCodePreview',
      header: 'Next Code',
      render: (value) => (
        <span className="font-mono text-sm font-medium text-blue-700">{String(value)}</span>
      ),
    },
    {
      key: 'resetType',
      header: 'Reset',
      render: (value) => <span className="text-sm text-gray-600">{String(value)}</span>,
    },
  ];

  if (canEdit) {
    columns.push({
      key: 'id',
      header: 'Actions',
      render: (_value, item) =>
        editingId === item.id ? (
          <div className="flex gap-2">
            <button
              type="button"
              disabled={saving}
              onClick={() => void saveEdit(item.id)}
              className="rounded bg-blue-600 px-2 py-1 text-xs font-medium text-white hover:bg-blue-700 disabled:opacity-60"
            >
              Save
            </button>
            <button
              type="button"
              onClick={cancelEdit}
              className="rounded border border-gray-300 px-2 py-1 text-xs font-medium text-gray-700 hover:bg-gray-50"
            >
              Cancel
            </button>
          </div>
        ) : (
          <button
            type="button"
            onClick={() => startEdit(item)}
            className="rounded border border-gray-300 px-2 py-1 text-xs font-medium text-gray-700 hover:bg-gray-50"
          >
            Adjust
          </button>
        ),
    });
  }

  return (
    <div className="p-6">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Code Sequences</h1>
        <p className="mt-1 text-sm text-gray-600">
          Auto codes are assigned when records are saved. Preview in forms shows the next value without consuming the sequence.
        </p>
      </div>

      {error && (
        <div className="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </div>
      )}
      {success && (
        <div className="mb-4 rounded-lg border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700">
          {success}
        </div>
      )}

      <DataTable
        data={items}
        columns={columns}
        loading={loading}
        searchable
        searchPlaceholder="Search module, branch, prefix…"
        emptyMessage="No code sequences found."
      />
    </div>
  );
};

export default CodeSequencePage;
