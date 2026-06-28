export const LABEL_SIZE_STORAGE_KEY = 'barcode-print-label-size';

export interface LabelDimensions {
  labelWidth: number;
  labelHeight: number;
}

export type LabelSizePresetId = 'small' | 'medium' | 'large' | 'custom';

export const LABEL_PRESETS: Record<Exclude<LabelSizePresetId, 'custom'>, LabelDimensions> = {
  small: { labelWidth: 40, labelHeight: 20 },
  medium: { labelWidth: 50, labelHeight: 25 },
  large: { labelWidth: 70, labelHeight: 40 },
};

export const LABEL_SIZE_LIMITS = {
  minWidth: 30,
  maxWidth: 100,
  minHeight: 15,
  maxHeight: 50,
};

export interface SavedLabelSizePreference extends LabelDimensions {
  preset: LabelSizePresetId;
}

export const DEFAULT_LABEL_SIZE: SavedLabelSizePreference = {
  preset: 'medium',
  ...LABEL_PRESETS.medium,
};

export const clampCustomSize = (width: number, height: number): LabelDimensions => ({
  labelWidth: Math.min(
    LABEL_SIZE_LIMITS.maxWidth,
    Math.max(LABEL_SIZE_LIMITS.minWidth, Math.round(width)),
  ),
  labelHeight: Math.min(
    LABEL_SIZE_LIMITS.maxHeight,
    Math.max(LABEL_SIZE_LIMITS.minHeight, Math.round(height)),
  ),
});

export const computeBarcodeScale = (size: LabelDimensions) => {
  const { labelWidth, labelHeight } = size;
  const compact = labelHeight <= 22;
  return {
    barWidth: Math.min(1.08, labelWidth / 48),
    paddingMm: Math.max(0.8, Number((labelHeight * 0.04).toFixed(1))),
    namePx: Math.max(compact ? 5 : 6, Math.round(labelHeight * 0.26)),
    subtitlePx: Math.max(compact ? 4 : 5, Math.round(labelHeight * 0.18)),
    barsPx: Math.max(compact ? 18 : 22, Math.round(labelHeight * 0.78)),
    numberPx: Math.max(compact ? 4 : 5, Math.round(labelHeight * 0.19)),
    pricePx: Math.max(compact ? 6 : 7, Math.round(labelHeight * 0.28)),
  };
};

export const loadLabelSizePreference = (): SavedLabelSizePreference => {
  try {
    const raw = localStorage.getItem(LABEL_SIZE_STORAGE_KEY);
    if (!raw) return DEFAULT_LABEL_SIZE;

    const parsed = JSON.parse(raw) as Partial<SavedLabelSizePreference>;
    const clamped = clampCustomSize(
      Number(parsed.labelWidth ?? DEFAULT_LABEL_SIZE.labelWidth),
      Number(parsed.labelHeight ?? DEFAULT_LABEL_SIZE.labelHeight),
    );
    const preset = parsed.preset ?? 'medium';
    return { preset, ...clamped };
  } catch {
    return DEFAULT_LABEL_SIZE;
  }
};

export const saveLabelSizePreference = (pref: SavedLabelSizePreference): void => {
  localStorage.setItem(LABEL_SIZE_STORAGE_KEY, JSON.stringify(pref));
};

export const validateCustomSize = (width: number, height: number): string | null => {
  if (!Number.isFinite(width) || !Number.isFinite(height)) {
    return 'Width and height must be valid numbers.';
  }
  if (width < LABEL_SIZE_LIMITS.minWidth || width > LABEL_SIZE_LIMITS.maxWidth) {
    return `Width must be between ${LABEL_SIZE_LIMITS.minWidth}mm and ${LABEL_SIZE_LIMITS.maxWidth}mm.`;
  }
  if (height < LABEL_SIZE_LIMITS.minHeight || height > LABEL_SIZE_LIMITS.maxHeight) {
    return `Height must be between ${LABEL_SIZE_LIMITS.minHeight}mm and ${LABEL_SIZE_LIMITS.maxHeight}mm.`;
  }
  return null;
};
