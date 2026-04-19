import React, { useEffect, useMemo, useState } from 'react';
import { defaultManagementFormValues, ManagementFormValues } from './types';

interface ManagementFormProps {
  entityLabel: string;
  isOpen: boolean;
  isEditMode: boolean;
  initialData?: Partial<ManagementFormValues> | null;
  isSubmitting: boolean;
  onCancel: () => void;
  onSubmit: (data: ManagementFormValues) => Promise<void>;
}

const buildSafeFormValues = (
  source?: Partial<ManagementFormValues> | null
): ManagementFormValues => ({
  name: String(source?.name ?? defaultManagementFormValues.name),
  description: String(source?.description ?? defaultManagementFormValues.description),
  isActive: Boolean(source?.isActive ?? defaultManagementFormValues.isActive),
});

const ManagementForm: React.FC<ManagementFormProps> = ({
  entityLabel,
  isOpen,
  isEditMode,
  initialData,
  isSubmitting,
  onCancel,
  onSubmit,
}) => {
  const safeInitialData = useMemo(
    () => initialData ?? defaultManagementFormValues,
    [initialData]
  );

  const [formData, setFormData] = useState<ManagementFormValues>(() =>
    buildSafeFormValues(safeInitialData)
  );
  const [errorMessage, setErrorMessage] = useState<string>('');

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    setFormData(buildSafeFormValues(safeInitialData));
    setErrorMessage('');
  }, [isOpen, safeInitialData]);

  if (!isOpen) {
    return null;
  }

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!(formData?.name ?? '').trim()) {
      setErrorMessage(`${entityLabel} name is required.`);
      return;
    }

    setErrorMessage('');
    await onSubmit({
      name: (formData?.name ?? '').trim(),
      description: formData?.description ?? '',
      isActive: Boolean(formData?.isActive ?? true),
    });
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="w-full max-w-lg rounded-xl bg-white shadow-xl">
        <div className="border-b border-gray-200 px-6 py-4">
          <h3 className="text-lg font-semibold text-gray-900">
            {isEditMode ? `Edit ${entityLabel}` : `Add ${entityLabel}`}
          </h3>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4 px-6 py-5">
          {errorMessage && (
            <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
              {errorMessage}
            </div>
          )}

          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Name</label>
            <input
              type="text"
              value={formData?.name ?? ''}
              onChange={(event) =>
                setFormData((prev) => ({ ...prev, name: event.target.value ?? '' }))
              }
              className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
              placeholder={`Enter ${entityLabel.toLowerCase()} name`}
            />
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Description</label>
            <textarea
              value={formData?.description ?? ''}
              onChange={(event) =>
                setFormData((prev) => ({ ...prev, description: event.target.value ?? '' }))
              }
              className="h-24 w-full resize-none rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
              placeholder={`Enter ${entityLabel.toLowerCase()} description`}
            />
          </div>

          <label className="flex items-center gap-2 text-sm text-gray-700">
            <input
              type="checkbox"
              checked={Boolean(formData?.isActive ?? true)}
              onChange={(event) =>
                setFormData((prev) => ({ ...prev, isActive: Boolean(event.target.checked) }))
              }
              className="h-4 w-4 rounded border-gray-300 text-blue-600"
            />
            Active
          </label>

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

export default ManagementForm;
