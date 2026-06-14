import React, { useState } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'

const BranchSelectionPage: React.FC = () => {
  const { branches, selectedBranchId, setBranch, isAuthenticated } = useAuth()
  const navigate = useNavigate()
  const [pendingBranchId, setPendingBranchId] = useState<number | null>(selectedBranchId)
  const [error, setError] = useState<string | null>(null)

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  if (branches.length === 1) {
    return <Navigate to="/" replace />
  }

  if (selectedBranchId !== null) {
    return <Navigate to="/" replace />
  }

  const handleContinue = () => {
    if (pendingBranchId === null) {
      setError('Please select a branch to continue.')
      return
    }

    try {
      setBranch(pendingBranchId)
      navigate('/', { replace: true })
    } catch (branchError) {
      setError(branchError instanceof Error ? branchError.message : 'Unable to select branch.')
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-50 px-4">
      <div className="w-full max-w-lg rounded-2xl bg-white p-8 shadow-lg">
        <div className="mb-6">
          <h1 className="text-2xl font-bold text-gray-900">Select Branch</h1>
          <p className="mt-2 text-sm text-gray-500">
            Your account is assigned to multiple branches. Choose one to continue.
          </p>
        </div>

        <div className="space-y-3">
          {branches.map((branch) => {
            const isSelected = pendingBranchId === branch.id

            return (
              <button
                key={branch.id}
                type="button"
                onClick={() => {
                  setPendingBranchId(branch.id)
                  setError(null)
                }}
                className={`flex w-full items-center justify-between rounded-xl border px-4 py-3 text-left transition ${
                  isSelected
                    ? 'border-blue-500 bg-blue-50 ring-2 ring-blue-200'
                    : 'border-gray-200 hover:border-blue-300 hover:bg-gray-50'
                }`}
              >
                <span className="font-medium text-gray-900">{branch.name}</span>
                {isSelected && <span className="text-sm font-semibold text-blue-600">Selected</span>}
              </button>
            )
          })}
        </div>

        {error && (
          <div className="mt-4 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
            {error}
          </div>
        )}

        <button
          type="button"
          onClick={handleContinue}
          className="mt-6 w-full rounded-lg bg-blue-600 px-4 py-2.5 font-medium text-white transition hover:bg-blue-700"
        >
          Continue to Dashboard
        </button>
      </div>
    </div>
  )
}

export default BranchSelectionPage
