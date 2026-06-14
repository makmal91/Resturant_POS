import React from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'

interface ProtectedRouteProps {
  children: React.ReactNode
  requireBranch?: boolean
}

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children, requireBranch = true }) => {
  const { isAuthenticated, isHydrated, selectedBranchId } = useAuth()
  const location = useLocation()

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

  if (requireBranch && selectedBranchId === null) {
    return <Navigate to="/select-branch" replace state={{ from: location.pathname }} />
  }

  return <>{children}</>
}

export default ProtectedRoute
