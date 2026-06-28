import React, { useEffect, useMemo, useState } from 'react';
import { FormButton, FormInput, FormSelect } from '../../components/forms/index';
import { EntityFormProps } from '../shared/ManagementPage';
import { defaultManagementFormValues, ManagementFormValues } from '../shared/types';

const buildFormData = (source: ManagementFormValues): ManagementFormValues => ({
  name: String(source.name ?? ''),
  code: String(source.code ?? ''),
  defaultConversionFactor: Number(source.defaultConversionFactor ?? source.conversionFactor ?? 1),
  isActive: Boolean(source.isActive ?? true),
  branchId: Number(source.branchId ?? 0),
});

const UnitForm: React.FC<EntityFormProps> = (props) => {
  const { isOpen, isEditMode, initialData, isSubmitting, onCancel, onSubmit } = props;
  const safeInitialData = useMemo(() => initialData ?? defaultManagementFormValues, [initialData]);

  const [formData, setFormData] = useState<ManagementFormValues>(() => buildFormData(safeInitialData));
  const [errors, setErrors] = useState<Partial<Record<keyof ManagementFormValues, string>>>({});

  useEffect(() => {
    if (!isOpen) return;
    setFormData(buildFormData(safeInitialData));
    setErrors({});
  }, [isOpen, safeInitialData]);

  if (!isOpen) return null;

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: name === 'defaultConversionFactor' ? Number(value || 1) : value,
    }));
    setErrors((prev) => ({ ...prev, [name]: '' }));
  };

  const handleReset = () => {
    setFormData(buildFormData(safeInitialData));
    setErrors({});
  };

  const validate = (): boolean => {
    const next: Partial<Record<keyof ManagementFormValues, string>> = {};

    if (!String(formData.name ?? '').trim()) {
      next.name = 'Unit name is required.';
    }

    if (Number(formData.defaultConversionFactor ?? 0) <= 0) {
      next.defaultConversionFactor = 'Default conversion factor must be greater than zero.';
    }

    setErrors(next);
    return Object.keys(next).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!validate()) return;

    await onSubmit({
      ...formData,
      name: String(formData.name ?? '').trim(),
      code: String(formData.code ?? '').trim(),
      defaultConversionFactor: Number(formData.defaultConversionFactor ?? 1),
      isActive: Boolean(formData.isActive ?? true),
    });
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="flex w-full max-w-2xl flex-col rounded-xl bg-white shadow-xl" style={{ maxHeight: '90vh' }}>

        <div className="shrink-0 border-b border-gray-200 px-6 py-4">
          <h3 className="text-lg font-semibold text-gray-900">
            {isEditMode ? 'Edit Unit' : 'Add Unit'}
          </h3>
          <p className="mt-1 text-sm text-gray-500">
            Default factor = child units in 1 base unit (e.g. Feet on Pipe product = 20). Used when adding units to products.
          </p>
        </div>

        <form id="unit-form" onSubmit={handleSubmit} className="flex min-h-0 flex-1 flex-col">
          <div className="flex-1 overflow-y-auto px-6 py-5">
            <div className="grid grid-cols-1 gap-x-6 md:grid-cols-2">
              <FormInput
                label="Unit Name"
                name="name"
                value={String(formData.name ?? '')}
                onChange={handleChange}
                placeholder="e.g. Piece, Box, Feet"
                required
                error={errors.name}
              />

              <FormInput
                label="Short Code"
                name="code"
                value={String(formData.code ?? '')}
                onChange={handleChange}
                placeholder="e.g. PCS, BOX, FT"
              />

              <FormInput
                label="Default Conversion Factor"
                name="defaultConversionFactor"
                type="number"
                value={Number(formData.defaultConversionFactor ?? 1)}
                onChange={handleChange}
                placeholder="1"
                required
                min={0.0001}
                step="0.0001"
                error={errors.defaultConversionFactor}
              />

              <FormSelect
                label="Status"
                name="isActive"
                value={formData.isActive ? 'true' : 'false'}
                onChange={(e) => {
                  setFormData((prev) => ({ ...prev, isActive: e.target.value === 'true' }));
                  setErrors((prev) => ({ ...prev, isActive: '' }));
                }}
                options={[
                  { label: 'Active', value: 'true' },
                  { label: 'Inactive', value: 'false' },
                ]}
                required
              />
            </div>

            <p className="mt-3 text-xs text-gray-500">
              Example: base unit Pipe, child Feet — set 20 (20 feet per 1 pipe). Product child price = base price ÷ 20.
            </p>
          </div>

          <div className="shrink-0 border-t border-gray-200 bg-white px-6 py-4 flex justify-end gap-3">
            <FormButton type="button" label="Cancel" variant="secondary" onClick={onCancel} disabled={isSubmitting} />
            <FormButton type="button" label="Reset" variant="secondary" onClick={handleReset} disabled={isSubmitting} />
            <FormButton type="submit" label={isEditMode ? 'Update Unit' : 'Create Unit'} variant="primary" loading={isSubmitting} />
          </div>
        </form>
      </div>
    </div>
  );
};

export default UnitForm;
