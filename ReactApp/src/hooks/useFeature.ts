import { useMemo } from 'react'
import { usePermissionStore } from '../stores/usePermissionStore'

export const useHasFeature = (featureKey: string): boolean =>
  usePermissionStore((state) => state.hasFeature(featureKey))

export const useFeature = (featureKey: string) => {
  const hasFeature = usePermissionStore((state) => state.hasFeature)

  return useMemo(
    () => ({
      enabled: hasFeature(featureKey),
      hasFeature,
    }),
    [featureKey, hasFeature],
  )
}

export const hasFeature = (featureKey: string): boolean =>
  usePermissionStore.getState().hasFeature(featureKey)
