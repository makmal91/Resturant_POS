import React, { useState } from 'react';
import { FormInput, FormSelect, FormButton } from './index';

interface Variant {
  name: string;
  price: string;
}

interface MenuFormData {
  name: string;
  category: string;
  price: string;
  variants: Variant[];
}

interface MenuFormProps {
  initialData?: Partial<MenuFormData>;
  onSubmit: (data: MenuFormData) => void;
  categories?: { id: string; name: string }[];
  isLoading?: boolean;
  submitLabel?: string;
}

const MenuForm: React.FC<MenuFormProps> = ({
  initialData = { variants: [] },
  onSubmit,
  categories = [],
  isLoading = false,
  submitLabel = 'Create Menu Item',
}) => {
  const [formData, setFormData] = useState<MenuFormData>({
    name: initialData.name || '',
    category: initialData.category || '',
    price: initialData.price || '',
    variants: initialData.variants || [],
  });

  const [errors, setErrors] = useState<Partial<MenuFormData>>({});
  const [variantError, setVariantError] = useState<string>('');

  const validateForm = (): boolean => {
    const newErrors: Partial<MenuFormData> = {};

    if (!formData.name.trim()) newErrors.name = 'Item name is required';
    if (!formData.category) newErrors.category = 'Category is required';
    if (!formData.price) newErrors.price = 'Price is required';
    if (parseFloat(formData.price) < 0) newErrors.price = 'Price must be positive';

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    setErrors((prev) => ({ ...prev, [name]: '' }));
  };

  const handleAddVariant = () => {
    const newVariant: Variant = { name: '', price: '' };
    setFormData((prev) => ({ ...prev, variants: [...prev.variants, newVariant] }));
    setVariantError('');
  };

  const handleVariantChange = (
    index: number,
    field: keyof Variant,
    value: string
  ) => {
    const updatedVariants = [...formData.variants];
    updatedVariants[index] = { ...updatedVariants[index], [field]: value };
    setFormData((prev) => ({ ...prev, variants: updatedVariants }));
  };

  const handleRemoveVariant = (index: number) => {
    setFormData((prev) => ({
      ...prev,
      variants: prev.variants.filter((_, i) => i !== index),
    }));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validateForm()) {
      onSubmit(formData);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="bg-white p-6 rounded-lg shadow-md max-w-2xl mx-auto">
      <h2 className="text-2xl font-bold mb-6 text-gray-800">Menu Item</h2>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <FormInput
          label="Item Name"
          name="name"
          value={formData.name}
          onChange={handleChange}
          placeholder="Enter item name"
          required
          error={errors.name}
        />

        <FormSelect
          label="Category"
          name="category"
          value={formData.category}
          onChange={handleChange}
          options={categories.map((cat) => ({ label: cat.name, value: cat.id }))}
          placeholder="Select category"
          required
          error={errors.category}
        />
      </div>

      <FormInput
        label="Base Price"
        name="price"
        type="number"
        value={formData.price}
        onChange={handleChange}
        placeholder="Enter price"
        required
        min="0"
        step="0.01"
        error={errors.price}
      />

      <div className="mt-6">
        <div className="flex justify-between items-center mb-4">
          <h3 className="text-lg font-semibold text-gray-800">Variants</h3>
          <FormButton
            type="button"
            label="+ Add Variant"
            onClick={handleAddVariant}
            variant="secondary"
          />
        </div>

        {formData.variants.length > 0 ? (
          <div className="space-y-3 bg-gray-50 p-4 rounded-lg">
            {formData.variants.map((variant, index) => (
              <div key={index} className="flex gap-3 items-end">
                <FormInput
                  label={`Variant ${index + 1} Name`}
                  name={`variant-name-${index}`}
                  value={variant.name}
                  onChange={(e) => handleVariantChange(index, 'name', e.target.value)}
                  placeholder="e.g., Small, Large"
                />
                <FormInput
                  label={`Price Adjustment`}
                  name={`variant-price-${index}`}
                  type="number"
                  value={variant.price}
                  onChange={(e) => handleVariantChange(index, 'price', e.target.value)}
                  placeholder="e.g., 2.50"
                  step="0.01"
                />
                <FormButton
                  type="button"
                  label="Remove"
                  onClick={() => handleRemoveVariant(index)}
                  variant="danger"
                />
              </div>
            ))}
          </div>
        ) : (
          <p className="text-gray-500 italic text-sm">No variants added yet</p>
        )}
      </div>

      <div className="mt-6 flex gap-3">
        <FormButton type="submit" label={submitLabel} loading={isLoading} variant="primary" />
        <FormButton type="reset" label="Reset" variant="secondary" />
      </div>
    </form>
  );
};

export default MenuForm;
