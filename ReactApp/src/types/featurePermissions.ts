export const FEATURE_KEYS = {
  UNIT: 'product.unit.enable',
  VARIANT: 'product.variant.enable',
  STOCK: 'product.stock.enable',
  BARCODE: 'product.barcode.enable',
} as const

export type FeatureKey = (typeof FEATURE_KEYS)[keyof typeof FEATURE_KEYS]

/** Module permissions that control each product feature across the system. */
export const FEATURE_MODULE_MAP: Record<string, string> = {
  [FEATURE_KEYS.UNIT]: 'Units',
  [FEATURE_KEYS.VARIANT]: 'Variants',
  [FEATURE_KEYS.STOCK]: 'Stock',
  [FEATURE_KEYS.BARCODE]: 'Barcodes',
}

export const isFeatureFormCode = (formCode: string): boolean =>
  formCode.toLowerCase().endsWith('.enable')

export const MODULE_FEATURE_MAP: Record<string, FeatureKey> = {
  Units: FEATURE_KEYS.UNIT,
  Variants: FEATURE_KEYS.VARIANT,
  Stock: FEATURE_KEYS.STOCK,
  'Stock Transfer': FEATURE_KEYS.STOCK,
  'Stock Reports': FEATURE_KEYS.STOCK,
  Barcodes: FEATURE_KEYS.BARCODE,
  Sizes: FEATURE_KEYS.VARIANT,
  Colors: FEATURE_KEYS.VARIANT,
}

export const ROUTE_FEATURE_MAP: Record<string, FeatureKey> = {
  '/barcodes': FEATURE_KEYS.BARCODE,
  '/units': FEATURE_KEYS.UNIT,
  '/settings/sizes': FEATURE_KEYS.VARIANT,
  '/settings/colors': FEATURE_KEYS.VARIANT,
  '/stock': FEATURE_KEYS.STOCK,
  '/reports/stock': FEATURE_KEYS.STOCK,
  '/reports/stock-by-unit': FEATURE_KEYS.STOCK,
}

export const parseFeaturesResponse = (value: unknown): string[] => {
  if (!Array.isArray(value)) return []
  return value.map((item) => String(item)).filter(Boolean)
}
