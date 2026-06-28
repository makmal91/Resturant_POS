import React, { useEffect, useState } from 'react';
import {
  clampCustomSize,
  LABEL_PRESETS,
  LABEL_SIZE_LIMITS,
  LabelDimensions,
  LabelSizePresetId,
  loadLabelSizePreference,
  saveLabelSizePreference,
  validateCustomSize,
} from './labelSize';

interface LabelSizeSelectorProps {
  value: LabelDimensions;
  preset: LabelSizePresetId;
  onChange: (preset: LabelSizePresetId, size: LabelDimensions) => void;
}

const PRESET_BUTTONS: Array<{ id: Exclude<LabelSizePresetId, 'custom'>; label: string; hint: string }> = [
  { id: 'small', label: 'Small', hint: '40 × 20 mm' },
  { id: 'medium', label: 'Medium', hint: '50 × 25 mm' },
  { id: 'large', label: 'Large', hint: '70 × 40 mm' },
];

const LabelSizeSelector: React.FC<LabelSizeSelectorProps> = ({ value, preset, onChange }) => {
  const [customWidth, setCustomWidth] = useState(String(value.labelWidth));
  const [customHeight, setCustomHeight] = useState(String(value.labelHeight));
  const [customError, setCustomError] = useState('');

  useEffect(() => {
    if (preset === 'custom') {
      setCustomWidth(String(value.labelWidth));
      setCustomHeight(String(value.labelHeight));
    }
  }, [preset, value.labelWidth, value.labelHeight]);

  const selectPreset = (id: Exclude<LabelSizePresetId, 'custom'>) => {
    setCustomError('');
    onChange(id, LABEL_PRESETS[id]);
  };

  const selectCustom = () => {
    onChange('custom', clampCustomSize(value.labelWidth, value.labelHeight));
  };

  const applyCustomSize = () => {
    const width = Number(customWidth);
    const height = Number(customHeight);
    const validationError = validateCustomSize(width, height);
    if (validationError) {
      setCustomError(validationError);
      return;
    }
    setCustomError('');
    onChange('custom', clampCustomSize(width, height));
  };

  return (
    <section className="mb-6 rounded-lg border border-gray-200 bg-white p-4">
      <div className="mb-3 text-sm font-semibold text-gray-800">Label Size</div>
      <div className="flex flex-wrap gap-2">
        {PRESET_BUTTONS.map((item) => (
          <button
            key={item.id}
            type="button"
            onClick={() => selectPreset(item.id)}
            className={`rounded-lg border px-4 py-2 text-left text-sm transition-colors ${
              preset === item.id
                ? 'border-blue-600 bg-blue-50 text-blue-800'
                : 'border-gray-300 bg-white text-gray-700 hover:bg-gray-50'
            }`}
          >
            <div className="font-medium">{item.label}</div>
            <div className="text-xs opacity-75">{item.hint}</div>
          </button>
        ))}
        <button
          type="button"
          onClick={selectCustom}
          className={`rounded-lg border px-4 py-2 text-left text-sm transition-colors ${
            preset === 'custom'
              ? 'border-blue-600 bg-blue-50 text-blue-800'
              : 'border-gray-300 bg-white text-gray-700 hover:bg-gray-50'
          }`}
        >
          <div className="font-medium">Custom</div>
          <div className="text-xs opacity-75">Set width & height</div>
        </button>
      </div>

      {preset === 'custom' && (
        <div className="mt-4 flex flex-wrap items-end gap-3">
          <div>
            <label className="mb-1 block text-xs font-semibold uppercase tracking-wide text-gray-500">
              Width (mm)
            </label>
            <input
              type="number"
              min={LABEL_SIZE_LIMITS.minWidth}
              max={LABEL_SIZE_LIMITS.maxWidth}
              value={customWidth}
              onChange={(event) => setCustomWidth(event.target.value)}
              className="w-28 rounded-lg border border-gray-300 px-3 py-2 text-sm"
            />
          </div>
          <div>
            <label className="mb-1 block text-xs font-semibold uppercase tracking-wide text-gray-500">
              Height (mm)
            </label>
            <input
              type="number"
              min={LABEL_SIZE_LIMITS.minHeight}
              max={LABEL_SIZE_LIMITS.maxHeight}
              value={customHeight}
              onChange={(event) => setCustomHeight(event.target.value)}
              className="w-28 rounded-lg border border-gray-300 px-3 py-2 text-sm"
            />
          </div>
          <button
            type="button"
            onClick={applyCustomSize}
            className="rounded-md bg-gray-800 px-4 py-2 text-sm font-medium text-white hover:bg-gray-900"
          >
            Apply
          </button>
          {customError && <p className="w-full text-sm text-red-600">{customError}</p>}
        </div>
      )}

      <p className="mt-3 text-xs text-gray-500">
        Current: {value.labelWidth}mm × {value.labelHeight}mm
        {preset === 'custom' && ` (${LABEL_SIZE_LIMITS.minWidth}–${LABEL_SIZE_LIMITS.maxWidth}mm wide, ${LABEL_SIZE_LIMITS.minHeight}–${LABEL_SIZE_LIMITS.maxHeight}mm tall)`}
      </p>
    </section>
  );
};

export const useLabelSizePreference = () => {
  const [preference, setPreference] = useState(loadLabelSizePreference);

  const updateLabelSize = (preset: LabelSizePresetId, size: LabelDimensions) => {
    const next = { preset, ...size };
    setPreference(next);
    saveLabelSizePreference(next);
  };

  return {
    labelSize: preference,
    labelPreset: preference.preset,
    updateLabelSize,
  };
};

export default LabelSizeSelector;
