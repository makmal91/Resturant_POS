import React, { useMemo } from 'react';

interface FormColorPickerProps {
  label: string;
  name: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  required?: boolean;
  error?: string;
  disabled?: boolean;
}

const HEX_PATTERN = /^#[0-9A-Fa-f]{6}$/;

export const normalizeHexColor = (raw: string): string => {
  const trimmed = raw.trim();
  if (!trimmed) return '';
  if (trimmed.startsWith('#')) {
    const hex = trimmed.slice(1).replace(/[^0-9A-Fa-f]/g, '').slice(0, 6);
    return hex.length === 6 ? `#${hex.toUpperCase()}` : trimmed;
  }
  const hex = trimmed.replace(/[^0-9A-Fa-f]/g, '').slice(0, 6);
  return hex.length === 6 ? `#${hex.toUpperCase()}` : '';
};

export const toPickerValue = (hex: string): string => {
  const normalized = normalizeHexColor(hex);
  return HEX_PATTERN.test(normalized) ? normalized : '#000000';
};

const FormColorPicker: React.FC<FormColorPickerProps> = ({
  label,
  name,
  value,
  onChange,
  placeholder = '#000000',
  required = false,
  error,
  disabled = false,
}) => {
  const pickerValue = useMemo(() => toPickerValue(value), [value]);
  const previewColor = HEX_PATTERN.test(normalizeHexColor(value)) ? normalizeHexColor(value) : '#E5E7EB';

  return (
    <div className="mb-5">
      <label htmlFor={name} className="mb-2 block text-sm font-medium text-gray-800">
        {label}
        {required && <span className="ml-1 text-red-500">*</span>}
      </label>
      <div className="flex items-center gap-3">
        <label
          className={`relative shrink-0 overflow-hidden rounded-lg border border-gray-300 ${
            disabled ? 'cursor-not-allowed opacity-60' : 'cursor-pointer'
          }`}
          title="Pick a color"
        >
          <span
            className="block h-11 w-11"
            style={{ backgroundColor: previewColor }}
          />
          <input
            type="color"
            value={pickerValue}
            disabled={disabled}
            onChange={(e) => onChange(e.target.value.toUpperCase())}
            className="absolute inset-0 h-full w-full cursor-pointer opacity-0"
            aria-label={`${label} picker`}
          />
        </label>

        <input
          id={name}
          name={name}
          type="text"
          value={value}
          disabled={disabled}
          onChange={(e) => onChange(e.target.value)}
          onBlur={(e) => {
            const normalized = normalizeHexColor(e.target.value);
            if (normalized) onChange(normalized);
          }}
          placeholder={placeholder}
          className={`min-w-0 flex-1 rounded-lg border px-4 py-3 font-mono text-sm uppercase shadow-sm focus:outline-none focus:ring-2 ${
            error
              ? 'border-red-300 focus:border-red-500 focus:ring-red-500'
              : 'border-gray-300 focus:border-blue-500 focus:ring-blue-500'
          } ${disabled ? 'cursor-not-allowed bg-gray-50 text-gray-500' : 'bg-white'}`}
        />

        <div
          className="h-11 w-11 shrink-0 rounded-lg border border-gray-200 shadow-inner"
          style={{ backgroundColor: previewColor }}
          title="Preview"
        />
      </div>
      <p className="mt-1.5 text-xs text-gray-500">
        Click the swatch to open the color picker, or type a hex code (e.g. #FF0000).
      </p>
      {error && (
        <p className="mt-1 text-sm text-red-600">{error}</p>
      )}
    </div>
  );
};

export default FormColorPicker;
