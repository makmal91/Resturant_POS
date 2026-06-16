import React, { useEffect, useState } from 'react';
import {
  ProductBarcodePayload,
  ProductDetail,
  ProductPayload,
  ProductUnitPayload,
  ProductVariantPayload,
} from './productService';
import { getApiErrorMessage } from '../../services/api';
import { CODE_MODULES, codeGeneratorService } from '../../services/codeGeneratorService';

export interface ProductOption {
  id: number;
  name: string;
  conversionFactor?: number;
}

interface ProductFormProps {
  initialData?: ProductDetail | null;
  branchId: number;
  categories: ProductOption[];
  subCategories: Array<ProductOption & { categoryId: number }>;
  brands: ProductOption[];
  unitOptions: ProductOption[];
  isSubmitting: boolean;
  onCancel: () => void;
  onSubmit: (payload: ProductPayload, primaryImageFile: File | null, imageFiles: File[]) => Promise<void>;
}

const emptyUnit = (): ProductUnitPayload => ({
  unitName: '',
  conversionFactor: 1,
  isBaseUnit: false,
  costPrice: null,
  sellingPrice: null,
  wholesalePrice: null,
});

const emptyVariant = (): ProductVariantPayload => ({
  variantName: '',
  size: '',
  color: '',
  sku: '',
  additionalPrice: 0,
  costPriceOverride: null,
  sellingPriceOverride: null,
  status: true,
});

const emptyBarcode = (): ProductBarcodePayload => ({
  barcodeValue: '',
  unitId: null,
  variantId: null,
  unitName: null,
  variantName: null,
  isPrimary: false,
});

type ProductFormTab = 'basic' | 'pricing' | 'units' | 'variants' | 'barcodes';

const formTabs: Array<{ id: ProductFormTab; label: string; helper: string }> = [
  { id: 'basic', label: 'Basic Info', helper: 'Name, category, brand' },
  { id: 'pricing', label: 'Pricing', helper: 'Retail, wholesale, discount' },
  { id: 'units', label: 'Units', helper: 'Base and alternate units' },
  { id: 'variants', label: 'Variants', helper: 'Size, color, SKU' },
  { id: 'barcodes', label: 'Barcodes & Images', helper: 'Codes and photos' },
];

const ProductForm: React.FC<ProductFormProps> = ({
  initialData,
  branchId,
  categories,
  subCategories,
  brands,
  unitOptions,
  isSubmitting,
  onCancel,
  onSubmit,
}) => {
  const [error, setError] = useState('');
  const [isGeneratingCode, setIsGeneratingCode] = useState(false);
  const [isGeneratingBarcode, setIsGeneratingBarcode] = useState(false);
  const [activeTab, setActiveTab] = useState<ProductFormTab>('basic');
  const [primaryImageFile, setPrimaryImageFile] = useState<File | null>(null);
  const [imageFiles, setImageFiles] = useState<File[]>([]);
  const [formData, setFormData] = useState<ProductPayload>({
    productName: '',
    productCode: '',
    sku: '',
    categoryId: 0,
    subCategoryId: null,
    brandId: null,
    description: '',
    status: true,
    costPrice: 0,
    sellingPrice: 0,
    wholesalePrice: 0,
    isVariantEnabled: false,
    isDiscountAllowed: false,
    discountType: 'Percentage',
    discountValue: 0,
    branchId,
    units: [{ ...emptyUnit(), unitName: 'Piece', isBaseUnit: true }],
    variants: [],
    barcodes: [],
  });

  const defaultUnitName = unitOptions[0]?.name ?? 'Piece';

  useEffect(() => {
    if (initialData) {
      setFormData({
        id: initialData.id,
        productName: initialData.productName,
        productCode: initialData.productCode,
        sku: initialData.sku,
        categoryId: initialData.categoryId,
        subCategoryId: initialData.subCategoryId ?? null,
        brandId: initialData.brandId ?? null,
        description: initialData.description,
        status: initialData.status,
        costPrice: initialData.costPrice,
        sellingPrice: initialData.sellingPrice,
        wholesalePrice: initialData.wholesalePrice,
        isVariantEnabled: initialData.isVariantEnabled,
        isDiscountAllowed: initialData.isDiscountAllowed,
        discountType: initialData.discountType ?? 'Percentage',
        discountValue: initialData.discountValue,
        branchId: initialData.branchId,
        units: initialData.units.length ? initialData.units : [{ ...emptyUnit(), unitName: defaultUnitName, isBaseUnit: true }],
        variants: initialData.variants ?? [],
        barcodes: initialData.barcodes ?? [],
      });
    } else {
      setFormData((prev) => ({
        ...prev,
        branchId,
        categoryId: categories[0]?.id ?? 0,
        subCategoryId: null,
        brandId: null,
        units:
          prev.units.length > 0
            ? prev.units.map((unit) => ({
                ...unit,
                unitName: unit.unitName || defaultUnitName,
              }))
            : [{ ...emptyUnit(), unitName: defaultUnitName, isBaseUnit: true }],
      }));
    }
    setPrimaryImageFile(null);
    setImageFiles([]);
    setError('');
    setActiveTab('basic');
  }, [branchId, categories, defaultUnitName, initialData]);

  const filteredSubCategories = subCategories.filter((item) => item.categoryId === Number(formData.categoryId));

  const updateUnit = (index: number, changes: Partial<ProductUnitPayload>) => {
    setFormData((prev) => ({
      ...prev,
      units: prev.units.map((unit, unitIndex) => {
        if (unitIndex !== index) return unit;
        return { ...unit, ...changes };
      }),
    }));
  };

  const setBaseUnit = (index: number) => {
    setFormData((prev) => ({
      ...prev,
      units: prev.units.map((unit, unitIndex) => ({ ...unit, isBaseUnit: unitIndex === index })),
    }));
  };

  const updateVariant = (index: number, changes: Partial<ProductVariantPayload>) => {
    setFormData((prev) => ({
      ...prev,
      variants: prev.variants.map((variant, variantIndex) =>
        variantIndex === index ? { ...variant, ...changes } : variant
      ),
    }));
  };

  const updateBarcode = (index: number, changes: Partial<ProductBarcodePayload>) => {
    setFormData((prev) => ({
      ...prev,
      barcodes: prev.barcodes.map((barcode, barcodeIndex) =>
        barcodeIndex === index ? { ...barcode, ...changes } : barcode
      ),
    }));
  };

  const showValidationError = (message: string, tab: ProductFormTab) => {
    setError(message);
    setActiveTab(tab);
  };

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!formData.productName.trim()) {
      showValidationError('Product name is required.', 'basic');
      return;
    }

    if (Number(formData.categoryId) <= 0) {
      showValidationError(
        categories.length === 0 ? 'Please create/select a category before creating a product.' : 'Please select a category.',
        'basic'
      );
      return;
    }

    if (formData.units.length === 0 || formData.units.filter((unit) => unit.isBaseUnit).length !== 1) {
      showValidationError('Exactly one base unit is required.', 'units');
      return;
    }

    if (formData.units.some((unit) => !unit.unitName.trim() || Number(unit.conversionFactor) <= 0)) {
      showValidationError('Every unit needs a name and a conversion factor greater than zero.', 'units');
      return;
    }

    setError('');
    try {
      await onSubmit({
        ...formData,
        productName: formData.productName.trim(),
        productCode: formData.productCode?.trim() ?? '',
        sku: formData.sku?.trim() ?? '',
        description: formData.description?.trim() ?? '',
        branchId,
        categoryId: Number(formData.categoryId),
        subCategoryId: formData.subCategoryId ? Number(formData.subCategoryId) : null,
        brandId: formData.brandId ? Number(formData.brandId) : null,
        costPrice: Number(formData.costPrice ?? 0),
        sellingPrice: Number(formData.sellingPrice ?? 0),
        wholesalePrice: Number(formData.wholesalePrice ?? 0),
        discountValue: Number(formData.discountValue ?? 0),
        units: formData.units.map((unit) => ({
          ...unit,
          unitName: unit.unitName.trim(),
          conversionFactor: Number(unit.conversionFactor),
        })),
        variants: formData.isVariantEnabled
          ? formData.variants
              .filter((variant) => variant.variantName.trim())
              .map((variant) => ({
                ...variant,
                variantName: variant.variantName.trim(),
                additionalPrice: Number(variant.additionalPrice ?? 0),
              }))
          : [],
        barcodes: formData.barcodes
          .filter((barcode) => barcode.barcodeValue.trim())
          .map((barcode) => ({ ...barcode, barcodeValue: barcode.barcodeValue.trim() })),
      }, primaryImageFile, imageFiles);
    } catch (submitError) {
      setError(getApiErrorMessage(submitError, 'Failed to save product. Please check the form and try again.'));
    }
  };

  return (
    <form onSubmit={handleSubmit} className="flex h-full flex-col">
      <div className="border-b border-gray-200 bg-gray-50 px-6 py-4">
        <div className="grid grid-cols-1 gap-2 md:grid-cols-5">
          {formTabs.map((tab, index) => {
            const isActive = activeTab === tab.id;
            return (
              <button
                key={tab.id}
                type="button"
                onClick={() => setActiveTab(tab.id)}
                className={`rounded-lg border px-3 py-2 text-left transition-colors ${
                  isActive
                    ? 'border-blue-300 bg-white text-blue-700 shadow-sm'
                    : 'border-transparent bg-transparent text-gray-600 hover:bg-white'
                }`}
              >
                <div className="text-xs font-semibold uppercase tracking-wide">
                  {index + 1}. {tab.label}
                </div>
                <div className="mt-0.5 text-xs text-gray-500">{tab.helper}</div>
              </button>
            );
          })}
        </div>
      </div>

      <div className="flex-1 overflow-y-auto px-6 py-5">
        {error && (
          <div className="mb-5 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            {error}
          </div>
        )}

        {activeTab === 'basic' && (
          <section className="space-y-5">
          <div>
            <h3 className="text-lg font-semibold text-gray-900">Basic Product Information</h3>
            <p className="mt-1 text-sm text-gray-500">
              Start with the required product name and category. Product code can stay empty and will be generated automatically.
            </p>
          </div>
          <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">Product Name *</label>
              <input
                type="text"
                value={formData.productName}
                onChange={(event) => setFormData((prev) => ({ ...prev, productName: event.target.value }))}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">Product Code</label>
              <div className="flex gap-2">
                <input
                  type="text"
                  value={formData.productCode ?? ''}
                  placeholder="Auto-generated if empty"
                  onChange={(event) => setFormData((prev) => ({ ...prev, productCode: event.target.value }))}
                  className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
                />
                <button
                  type="button"
                  disabled={isGeneratingCode}
                  onClick={async () => {
                    setIsGeneratingCode(true);
                    try {
                      const code = await codeGeneratorService.generate(CODE_MODULES.Product, branchId);
                      setFormData((prev) => ({ ...prev, productCode: code }));
                    } catch (err) {
                      setError(getApiErrorMessage(err, 'Unable to generate product code.'));
                    } finally {
                      setIsGeneratingCode(false);
                    }
                  }}
                  className="shrink-0 rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-60"
                >
                  {isGeneratingCode ? '…' : 'Auto Generate'}
                </button>
              </div>
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">SKU</label>
              <input
                type="text"
                value={formData.sku ?? ''}
                onChange={(event) => setFormData((prev) => ({ ...prev, sku: event.target.value }))}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">Category *</label>
              <select
                value={formData.categoryId}
                onChange={(event) => setFormData((prev) => ({ ...prev, categoryId: Number(event.target.value), subCategoryId: null }))}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
              >
                <option value={0}>Select Category</option>
                {categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}
              </select>
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">SubCategory</label>
              <select
                value={formData.subCategoryId ?? 0}
                onChange={(event) => setFormData((prev) => ({ ...prev, subCategoryId: Number(event.target.value) || null }))}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
              >
                <option value={0}>None</option>
                {filteredSubCategories.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
              </select>
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">Brand</label>
              <select
                value={formData.brandId ?? 0}
                onChange={(event) => setFormData((prev) => ({ ...prev, brandId: Number(event.target.value) || null }))}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
              >
                <option value={0}>None</option>
                {brands.map((brand) => <option key={brand.id} value={brand.id}>{brand.name}</option>)}
              </select>
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">Status</label>
              <select value={formData.status ? 'active' : 'inactive'} onChange={(event) => setFormData((prev) => ({ ...prev, status: event.target.value === 'active' }))} className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none">
                <option value="active">Active</option>
                <option value="inactive">Inactive</option>
              </select>
            </div>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Description</label>
            <textarea value={formData.description ?? ''} onChange={(event) => setFormData((prev) => ({ ...prev, description: event.target.value }))} className="h-20 w-full resize-none rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none" />
          </div>
          <div className="rounded-lg border border-dashed border-gray-300 bg-gray-50 p-4">
            <label className="mb-1 block text-sm font-medium text-gray-700">Primary Product Image</label>
            <p className="mb-3 text-xs text-gray-500">
              This image will be marked as the main product image in product detail and listings.
            </p>
            <input
              type="file"
              accept="image/*"
              onChange={(event) => setPrimaryImageFile(event.target.files?.[0] ?? null)}
              className="block w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm"
            />
            {primaryImageFile ? (
              <p className="mt-2 text-xs font-medium text-blue-700">Selected primary image: {primaryImageFile.name}</p>
            ) : initialData?.images?.some((image) => image.isPrimary) ? (
              <p className="mt-2 text-xs text-gray-500">Existing primary image will remain unless a new one is selected.</p>
            ) : null}
          </div>
        </section>
        )}

        {activeTab === 'pricing' && (
        <section className="space-y-5">
          <div>
            <h3 className="text-lg font-semibold text-gray-900">Pricing & Discount</h3>
            <p className="mt-1 text-sm text-gray-500">
              Set product-level prices. Unit and variant prices can override these values where needed.
            </p>
          </div>
          <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
            {(['costPrice', 'sellingPrice', 'wholesalePrice'] as const).map((field) => (
              <div key={field}>
                <label className="mb-1 block text-sm font-medium text-gray-700">{field === 'costPrice' ? 'Cost Price' : field === 'sellingPrice' ? 'Selling Price' : 'Wholesale Price'}</label>
                <input type="number" min={0} step="0.01" value={Number(formData[field] ?? 0)} onChange={(event) => setFormData((prev) => ({ ...prev, [field]: Number(event.target.value || 0) }))} className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none" />
              </div>
            ))}
          </div>
          <label className="flex items-center gap-2 text-sm text-gray-700">
            <input type="checkbox" checked={formData.isDiscountAllowed} onChange={(event) => setFormData((prev) => ({ ...prev, isDiscountAllowed: event.target.checked }))} />
            Discount allowed
          </label>
          {formData.isDiscountAllowed && (
            <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
              <select value={formData.discountType ?? 'Percentage'} onChange={(event) => setFormData((prev) => ({ ...prev, discountType: event.target.value as 'Percentage' | 'Fixed' }))} className="rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none">
                <option value="Percentage">Percentage</option>
                <option value="Fixed">Fixed</option>
              </select>
              <input type="number" min={0} step="0.01" value={Number(formData.discountValue ?? 0)} onChange={(event) => setFormData((prev) => ({ ...prev, discountValue: Number(event.target.value || 0) }))} className="rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none" />
            </div>
          )}
        </section>
        )}

        {activeTab === 'units' && (
        <section className="space-y-5">
          <div>
            <h3 className="text-lg font-semibold text-gray-900">Units</h3>
            <p className="mt-1 text-sm text-gray-500">
              Add all sellable/purchase units. Exactly one unit must be marked as the base unit.
            </p>
          </div>
        <DynamicSection title="Units" onAdd={() => setFormData((prev) => ({ ...prev, units: [...prev.units, emptyUnit()] }))}>
          {formData.units.map((unit, index) => (
            <div key={index} className="grid grid-cols-1 gap-3 rounded-lg border border-gray-200 p-3 md:grid-cols-7">
              <select
                value={unit.unitName}
                onChange={(event) => {
                  const selectedUnit = unitOptions.find((option) => option.name === event.target.value);
                  updateUnit(index, {
                    unitName: event.target.value,
                    conversionFactor: Number(selectedUnit?.conversionFactor ?? unit.conversionFactor ?? 1),
                  });
                }}
                className="rounded border px-3 py-2 md:col-span-2"
              >
                <option value="">Select unit</option>
                {unitOptions.map((option) => (
                  <option key={option.id} value={option.name}>{option.name}</option>
                ))}
                {unit.unitName && !unitOptions.some((option) => option.name === unit.unitName) ? (
                  <option value={unit.unitName}>{unit.unitName}</option>
                ) : null}
              </select>
              <input type="number" min={0.0001} step="0.0001" value={unit.conversionFactor} onChange={(event) => updateUnit(index, { conversionFactor: Number(event.target.value || 1) })} className="rounded border px-3 py-2" />
              <input type="number" step="0.01" placeholder="Cost" value={unit.costPrice ?? ''} onChange={(event) => updateUnit(index, { costPrice: event.target.value ? Number(event.target.value) : null })} className="rounded border px-3 py-2" />
              <input type="number" step="0.01" placeholder="Selling" value={unit.sellingPrice ?? ''} onChange={(event) => updateUnit(index, { sellingPrice: event.target.value ? Number(event.target.value) : null })} className="rounded border px-3 py-2" />
              <label className="flex items-center gap-2 text-sm"><input type="radio" checked={unit.isBaseUnit} onChange={() => setBaseUnit(index)} /> Base</label>
              <button type="button" onClick={() => setFormData((prev) => ({ ...prev, units: prev.units.filter((_, i) => i !== index) }))} className="rounded border px-3 py-2 text-sm text-red-600">Remove</button>
            </div>
          ))}
        </DynamicSection>
        </section>
        )}

        {activeTab === 'variants' && (
        <section className="space-y-5">
          <div>
            <h3 className="text-lg font-semibold text-gray-900">Variants</h3>
            <p className="mt-1 text-sm text-gray-500">
              Enable variants for products with sizes, colors, batches, or combined options. This section is optional.
            </p>
          </div>
          <label className="flex items-center gap-2 text-sm font-medium text-gray-700">
            <input type="checkbox" checked={formData.isVariantEnabled} onChange={(event) => setFormData((prev) => ({ ...prev, isVariantEnabled: event.target.checked, variants: event.target.checked ? prev.variants : [] }))} />
            Enable variants
          </label>
          {formData.isVariantEnabled && (
            <DynamicSection title="Variants" onAdd={() => setFormData((prev) => ({ ...prev, variants: [...prev.variants, emptyVariant()] }))}>
              <div className="overflow-x-auto rounded-lg border border-gray-200">
                <table className="min-w-[980px] table-fixed divide-y divide-gray-200">
                  <colgroup>
                    <col className="w-[24%]" />
                    <col className="w-[12%]" />
                    <col className="w-[12%]" />
                    <col className="w-[16%]" />
                    <col className="w-[14%]" />
                    <col className="w-[10%]" />
                    <col className="w-[12%]" />
                  </colgroup>
                  <thead className="bg-gray-50">
                    <tr>
                      {['Variant Name', 'Size', 'Color', 'SKU', 'Additional Price', 'Status', 'Action'].map((header) => (
                        <th key={header} className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-gray-500">
                          {header}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100 bg-white">
                    {formData.variants.map((variant, index) => (
                      <tr key={index}>
                        <td className="px-3 py-3 align-middle">
                          <input placeholder="e.g. Large Red" value={variant.variantName} onChange={(event) => updateVariant(index, { variantName: event.target.value })} className="w-full rounded border px-3 py-2" />
                        </td>
                        <td className="px-3 py-3 align-middle">
                          <input placeholder="Size" value={variant.size ?? ''} onChange={(event) => updateVariant(index, { size: event.target.value })} className="w-full rounded border px-3 py-2" />
                        </td>
                        <td className="px-3 py-3 align-middle">
                          <input placeholder="Color" value={variant.color ?? ''} onChange={(event) => updateVariant(index, { color: event.target.value })} className="w-full rounded border px-3 py-2" />
                        </td>
                        <td className="px-3 py-3 align-middle">
                          <input placeholder="SKU" value={variant.sku ?? ''} onChange={(event) => updateVariant(index, { sku: event.target.value })} className="w-full rounded border px-3 py-2" />
                        </td>
                        <td className="px-3 py-3 align-middle">
                          <input type="number" step="0.01" placeholder="0.00" value={variant.additionalPrice} onChange={(event) => updateVariant(index, { additionalPrice: Number(event.target.value || 0) })} className="w-full rounded border px-3 py-2" />
                        </td>
                        <td className="px-3 py-3 align-middle">
                          <label className="flex items-center gap-2 text-sm">
                            <input type="checkbox" checked={variant.status} onChange={(event) => updateVariant(index, { status: event.target.checked })} />
                            Active
                          </label>
                        </td>
                        <td className="px-3 py-3 align-middle">
                          <button type="button" onClick={() => setFormData((prev) => ({ ...prev, variants: prev.variants.filter((_, i) => i !== index) }))} className="w-full rounded border px-3 py-2 text-sm text-red-600">
                            Remove
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </DynamicSection>
          )}
        </section>
        )}

        {activeTab === 'barcodes' && (
        <section className="space-y-6">
          <div>
            <h3 className="text-lg font-semibold text-gray-900">Barcodes & Images</h3>
            <p className="mt-1 text-sm text-gray-500">
              Add one or more unique barcodes and tie each one to a specific unit or variant. Both are optional.
            </p>
          </div>
        <DynamicSection title="Barcodes" onAdd={() => setFormData((prev) => ({ ...prev, barcodes: [...prev.barcodes, emptyBarcode()] }))}>
          <div className="mb-3">
            <button
              type="button"
              disabled={isGeneratingBarcode}
              onClick={async () => {
                setIsGeneratingBarcode(true);
                try {
                  const barcode = await codeGeneratorService.generateBarcode(branchId);
                  setFormData((prev) => ({
                    ...prev,
                    barcodes: [...prev.barcodes, { ...emptyBarcode(), barcodeValue: barcode, isPrimary: prev.barcodes.length === 0 }],
                  }));
                } catch (err) {
                  setError(getApiErrorMessage(err, 'Unable to generate barcode.'));
                } finally {
                  setIsGeneratingBarcode(false);
                }
              }}
              className="rounded-lg border border-gray-300 bg-white px-3 py-1.5 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-60"
            >
              {isGeneratingBarcode ? 'Generating…' : 'Generate Barcode'}
            </button>
          </div>
          {formData.barcodes.length > 0 && (
            <div className="overflow-x-auto rounded-lg border border-gray-200">
              <table className="w-full table-fixed divide-y divide-gray-200" style={{ minWidth: formData.isVariantEnabled ? 780 : 580 }}>
                <colgroup>
                  <col style={{ width: formData.isVariantEnabled ? '30%' : '40%' }} />
                  <col style={{ width: formData.isVariantEnabled ? '20%' : '25%' }} />
                  {formData.isVariantEnabled && <col style={{ width: '20%' }} />}
                  <col style={{ width: '10%' }} />
                  <col style={{ width: formData.isVariantEnabled ? '20%' : '25%' }} />
                </colgroup>
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-gray-500">Barcode Value</th>
                    <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-gray-500">Unit</th>
                    {formData.isVariantEnabled && (
                      <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-gray-500">Variant</th>
                    )}
                    <th className="px-3 py-2 text-center text-xs font-semibold uppercase tracking-wide text-gray-500">Primary</th>
                    <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-gray-500">Action</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 bg-white">
                  {formData.barcodes.map((barcode, index) => (
                    <tr key={index}>
                      <td className="px-3 py-2 align-middle">
                        <input
                          placeholder="e.g. 8901234567890"
                          value={barcode.barcodeValue}
                          onChange={(event) => updateBarcode(index, { barcodeValue: event.target.value })}
                          className="w-full rounded border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
                        />
                      </td>
                      <td className="px-3 py-2 align-middle">
                        <select
                          value={barcode.unitName ?? ''}
                          onChange={(event) => {
                            const name = event.target.value || null;
                            const matched = formData.units.find((u) => u.unitName === name);
                            updateBarcode(index, {
                              unitName: name,
                              unitId: matched?.id ?? null,
                            });
                          }}
                          className="w-full rounded border border-gray-300 px-2 py-2 text-sm focus:border-blue-500 focus:outline-none"
                        >
                          <option value="">— None —</option>
                          {formData.units
                            .filter((u) => u.unitName.trim())
                            .map((u, ui) => (
                              <option key={ui} value={u.unitName}>{u.unitName}</option>
                            ))}
                        </select>
                      </td>
                      {formData.isVariantEnabled && (
                        <td className="px-3 py-2 align-middle">
                          <select
                            value={barcode.variantName ?? ''}
                            onChange={(event) => {
                              const name = event.target.value || null;
                              const matched = formData.variants.find((v) => v.variantName === name);
                              updateBarcode(index, {
                                variantName: name,
                                variantId: matched?.id ?? null,
                              });
                            }}
                            className="w-full rounded border border-gray-300 px-2 py-2 text-sm focus:border-blue-500 focus:outline-none"
                          >
                            <option value="">— None —</option>
                            {formData.variants
                              .filter((v) => v.variantName.trim())
                              .map((v, vi) => (
                                <option key={vi} value={v.variantName}>{v.variantName}</option>
                              ))}
                          </select>
                        </td>
                      )}
                      <td className="px-3 py-2 align-middle text-center">
                        <input
                          type="checkbox"
                          checked={barcode.isPrimary}
                          onChange={(event) => updateBarcode(index, { isPrimary: event.target.checked })}
                          className="h-4 w-4 rounded border-gray-300 text-blue-600"
                        />
                      </td>
                      <td className="px-3 py-2 align-middle">
                        <button
                          type="button"
                          onClick={() => setFormData((prev) => ({ ...prev, barcodes: prev.barcodes.filter((_, i) => i !== index) }))}
                          className="w-full rounded border border-red-200 px-3 py-2 text-sm text-red-600 hover:bg-red-50"
                        >
                          Remove
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          {formData.barcodes.length === 0 && (
            <p className="rounded-lg border border-dashed border-gray-300 px-4 py-6 text-center text-sm text-gray-400">
              No barcodes added yet. Click "Add Barcode" to tie a barcode to a unit or variant.
            </p>
          )}
        </DynamicSection>

        <section className="space-y-2">
          <h4 className="text-sm font-semibold uppercase tracking-wide text-gray-500">Additional Product Images</h4>
          <p className="text-xs text-gray-500">
            Upload multiple gallery images here. Use Basic Info for the primary product image.
          </p>
          <input type="file" multiple accept="image/*" onChange={(event) => setImageFiles(Array.from(event.target.files ?? []))} className="block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm" />
          {imageFiles.length > 0 ? <p className="text-xs text-gray-500">{imageFiles.length} new image(s) selected.</p> : null}
          {imageFiles.length > 0 ? (
            <ul className="space-y-1 rounded-lg border border-gray-200 bg-gray-50 p-3 text-xs text-gray-600">
              {imageFiles.map((file) => (
                <li key={`${file.name}-${file.lastModified}`}>{file.name}</li>
              ))}
            </ul>
          ) : null}
          {initialData?.images?.length ? <p className="text-xs text-gray-500">{initialData.images.length} existing image(s). New files will be added after save.</p> : null}
        </section>
        </section>
        )}
      </div>

      <div className="flex justify-end gap-3 border-t border-gray-200 px-6 py-4">
        <div className="mr-auto flex gap-2">
          <button
            type="button"
            onClick={() => {
              const currentIndex = formTabs.findIndex((tab) => tab.id === activeTab);
              setActiveTab(formTabs[Math.max(0, currentIndex - 1)].id);
            }}
            disabled={activeTab === formTabs[0].id}
            className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-50"
          >
            Previous
          </button>
          <button
            type="button"
            onClick={() => {
              const currentIndex = formTabs.findIndex((tab) => tab.id === activeTab);
              setActiveTab(formTabs[Math.min(formTabs.length - 1, currentIndex + 1)].id);
            }}
            disabled={activeTab === formTabs[formTabs.length - 1].id}
            className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-50"
          >
            Next
          </button>
        </div>
        <button type="button" onClick={onCancel} disabled={isSubmitting} className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-60">
          Cancel
        </button>
        <button type="submit" disabled={isSubmitting} className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-60">
          {isSubmitting ? 'Saving...' : initialData ? 'Update Product' : 'Create Product'}
        </button>
      </div>
    </form>
  );
};

const DynamicSection: React.FC<{ title: string; onAdd: () => void; children: React.ReactNode }> = ({ title, onAdd, children }) => (
  <section className="space-y-3">
    <div className="flex items-center justify-between">
      <h3 className="text-sm font-semibold uppercase tracking-wide text-gray-500">{title}</h3>
      <button type="button" onClick={onAdd} className="rounded-md border border-blue-200 px-3 py-1 text-sm font-medium text-blue-700 hover:bg-blue-50">
        Add {title.slice(0, -1)}
      </button>
    </div>
    <div className="space-y-3">{children}</div>
  </section>
);

export default ProductForm;
