import React, { useState } from 'react';
import { FormInput, FormSelect, FormButton } from './index';

interface InventoryFormData {
  itemName: string;
  unit: string;
  stock: string;
  minLevel: string;
}

interface InventoryFormProps {
  initialData?: Partial<InventoryFormData>;
  onSubmit: (data: InventoryFormData) => void;
  isLoading?: boolean;
  submitLabel?: string;
}

const InventoryForm: React.FC<InventoryFormProps> = ({
  initialData = {},
  onSubmit,
  isLoading = false,
  submitLabel = 'Add Item',
}) => {
  const [formData, setFormData] = useState<InventoryFormData>({
    itemName: initialData.itemName || '',
    unit: initialData.unit || 'Piece',
    stock: initialData.stock || '',
    minLevel: initialData.minLevel || '',
  });

  const [errors, setErrors] = useState<Partial<InventoryFormData>>({});

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
    <form onSubmit={handleSubmit} className="bg-white p-6 rounded-lg shadow-md max-w-md mx-auto">
      <h2 className="text-2xl font-bold mb-6 text-gray-800">Inventory Item</h2>

      <FormInput
        label="Item Name"
        name="itemName"
        value={formData.itemName}
        onChange={handleChange}
        placeholder="Enter item name"
        required
        error={errors.itemName}
      />

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
        placeholder="Enter stock quantity"
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
        placeholder="Enter minimum stock level"
        required
        min="0"
        step="0.01"
        error={errors.minLevel}
      />

      <div className="mt-6 flex gap-3">
        <FormButton type="submit" label={submitLabel} loading={isLoading} variant="primary" />
        <FormButton type="reset" label="Reset" variant="secondary" />
      </div>
    </form>
  );
};

export default InventoryForm;
