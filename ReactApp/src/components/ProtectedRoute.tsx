import React from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'
import { usePermissionStore } from '../stores/usePermissionStore'
import { isBranchSelectionReady, type PermissionAction } from '../types/permissions'

interface ProtectedRouteProps {
  children: React.ReactNode
  requireBranch?: boolean
  module?: string
  feature?: string
  action?: PermissionAction
}

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
  children,
  requireBranch = true,
  module,
  feature,
  action = 'view',
}) => {
  const { isAuthenticated, isHydrated, selectedBranchId } = useAuth()
  const location = useLocation()
  const hasModulePermission = usePermissionStore(
    (state) => !module || state.can(module, action),
  )
  const hasFeatureAccess = usePermissionStore((state) => !feature || state.hasFeature(feature))

  if (!isHydrated) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-gray-50">
        <div className="rounded-lg bg-white px-6 py-4 shadow-sm">
          <p className="text-sm text-gray-600">Loading session...</p>
        </div>
      </div>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  if (requireBranch && !isBranchSelectionReady(selectedBranchId)) {
    return <Navigate to="/select-branch" replace state={{ from: location.pathname }} />
  }

  if (!hasModulePermission || !hasFeatureAccess) {
    return <Navigate to="/" replace />
  }

  return <>{children}</>
}

export default ProtectedRoute
