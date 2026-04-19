import React, { useEffect, useMemo, useState } from 'react';
import { FormInput, FormSelect, FormButton } from './index';
import { safeString } from '../../utils/safeValues';

interface InventoryFormData {
  itemName: string;
  unit: string;
  stock: string;
  minLevel: string;
}

interface InventoryFormProps {
  initialData?: Partial<InventoryFormData> | null;
  onSubmit: (data: InventoryFormData) => void;
  isLoading?: boolean;
  submitLabel?: string;
}

const DEFAULT_INVENTORY_FORM_DATA: InventoryFormData = {
  itemName: '',
  unit: 'Piece',
  stock: '',
  minLevel: '',
};

const buildInventoryFormData = (
  source?: Partial<InventoryFormData> | null
): InventoryFormData => ({
  itemName: safeString(source?.itemName),
  unit: safeString(source?.unit, 'Piece') || 'Piece',
  stock: safeString(source?.stock),
  minLevel: safeString(source?.minLevel),
});

const InventoryForm: React.FC<InventoryFormProps> = ({
  initialData,
  onSubmit,
  isLoading = false,
  submitLabel = 'Add Item',
}) => {
  const safeInitialData = useMemo(
    () => initialData ?? DEFAULT_INVENTORY_FORM_DATA,
    [initialData]
  );

  const [formData, setFormData] = useState<InventoryFormData>(() =>
    buildInventoryFormData(safeInitialData)
  );

  const [errors, setErrors] = useState<Partial<InventoryFormData>>({});

  useEffect(() => {
    setFormData(buildInventoryFormData(safeInitialData));
    setErrors({});
  }, [safeInitialData]);

  const validateForm = (): boolean => {
    const newErrors: Partial<InventoryFormData> = {};

    if (!formData.itemName.trim()) newErrors.itemName = 'Item name is required';
    if (!formData.unit) newErrors.unit = 'Unit is required';
    if (!formData.stock) newErrors.stock = 'Stock quantity is required';
    if (parseFloat(formData.stock) < 0) newErrors.stock = 'Stock must be non-negative';
    if (!formData.minLevel) newErrors.minLevel = 'Minimum level is required';
    if (parseFloat(formData.minLevel) < 0) newErrors.minLevel = 'Minimum level must be non-negative';
    if (parseFloat(formData.stock) < parseFloat(formData.minLevel)) {
      newErrors.stock = 'Stock cannot be less than minimum level';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    setErrors((prev) => ({ ...prev, [name]: '' }));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validateForm()) {
      onSubmit(formData);
    }
  };

  return (
    <div className="max-w-3xl mx-auto">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Inventory Management</h1>
        <p className="text-gray-600">Manage inventory items and stock levels</p>
      </div>

      <div className="bg-white rounded-lg shadow-sm border border-gray-200">
        <div className="px-6 py-4 border-b border-gray-200">
          <h2 className="text-xl font-semibold text-gray-900">
            {safeString(safeInitialData?.itemName).trim() ? 'Update Item' : 'Add New Item'}
          </h2>
          <p className="text-sm text-gray-600 mt-1">
            {safeString(safeInitialData?.itemName).trim()
              ? 'Update inventory item details'
              : 'Add a new item to your inventory'}
          </p>
        </div>

        <form onSubmit={handleSubmit} className="p-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {/* Item Information */}
            <div className="md:col-span-2">
              <h3 className="text-base font-semibold text-gray-900 mb-4">Item Information</h3>
              <FormInput
                label="Item Name"
                name="itemName"
                value={formData.itemName}
                onChange={handleChange}
                placeholder="e.g., Tomato, Chicken Breast, Olive Oil"
                required
                error={errors.itemName}
              />
            </div>

            {/* Unit & Stock Section */}
            <div className="md:col-span-2">
              <h3 className="text-base font-semibold text-gray-900 mb-4">Stock Details</h3>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                <FormSelect
                  label="Unit of Measurement"
                  name="unit"
                  value={formData.unit}
                  onChange={handleChange}
                  options={[
                    { label: 'Piece', value: 'Piece' },
                    { label: 'kg', value: 'kg' },
                    { label: 'g', value: 'g' },
                    { label: 'L', value: 'L' },
                    { label: 'ml', value: 'ml' },
                    { label: 'Box', value: 'Box' },
                    { label: 'Dozen', value: 'Dozen' },
                    { label: 'Bundle', value: 'Bundle' },
                  ]}
                  required
                  error={errors.unit}
                />

                <FormInput
                  label="Current Stock"
                  name="stock"
                  type="number"
                  value={formData.stock}
                  onChange={handleChange}
                  placeholder="0.00"
                  required
                  min="0"
                  step="0.01"
                  error={errors.stock}
                />

                <FormInput
                  label="Minimum Level"
                  name="minLevel"
                  type="number"
                  value={formData.minLevel}
                  onChange={handleChange}
                  placeholder="0.00"
                  required
                  min="0"
                  step="0.01"
                  error={errors.minLevel}
                />
              </div>
            </div>
          </div>

          {/* Action Buttons */}
          <div className="mt-8 flex justify-end space-x-3">
            <FormButton type="reset" label="Clear" variant="secondary" />
            <FormButton type="submit" label={submitLabel} loading={isLoading} variant="primary" />
          </div>
        </form>
      </div>
    </div>
  );
};

export default InventoryForm;
