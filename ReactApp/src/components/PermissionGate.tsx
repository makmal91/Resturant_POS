import React from 'react'
import { useHasPermission } from '../hooks/usePermission'
import type { PermissionAction } from '../types/permissions'

interface PermissionGateProps {
  module: string
  action?: PermissionAction
  fallback?: React.ReactNode
  children: React.ReactNode
}

const PermissionGate: React.FC<PermissionGateProps> = ({
  module,
  action = 'view',
  fallback = null,
  children,
}) => {
  const allowed = useHasPermission(module, action)

  if (!allowed) {
    return <>{fallback}</>
  }

  return <>{children}</>
}

export default PermissionGate
