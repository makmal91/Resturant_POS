import React from 'react'
import { useHasFeature } from '../hooks/useFeature'
import type { FeatureKey } from '../types/featurePermissions'

interface FeatureWrapperProps {
  feature: FeatureKey | string
  children: React.ReactNode
}

const FeatureWrapper: React.FC<FeatureWrapperProps> = ({ feature, children }) => {
  const enabled = useHasFeature(feature)
  if (!enabled) return null
  return <>{children}</>
}

export default FeatureWrapper
