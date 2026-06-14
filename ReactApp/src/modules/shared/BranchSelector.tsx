import React from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../../contexts/AuthContext'
import { useBranchStore } from '../../stores/useBranchStore'
import { useTenantStore } from '../../stores/useTenantStore'

interface BranchSelectorProps {
  label?: string
  required?: boolean
  disabled?: boolean
  allowAllBranches?: boolean
  className?: string
}

const BranchSelector: React.FC<BranchSelectorProps> = ({
  label = 'Branch',
  required = true,
  disabled = false,
  allowAllBranches = true,
  className = '',
}) => {
  const navigate = useNavigate()
  const { setBranch } = useAuth()
  const branches = useBranchStore((state) => state.branches)
  const selectedBranchId = useBranchStore((state) => state.selectedBranchId)
  const setSelectedBranchId = useBranchStore((state) => state.setSelectedBranchId)
  const role = useTenantStore((state) => state.session.role)
  const globalViewRoles = ['SuperAdmin', 'Super Admin', 'System Admin', 'Admin']
  const canUseAllBranches = allowAllBranches && globalViewRoles.includes(role)

  return (
    <div className={className}>
      <label className="mb-1 block text-sm font-medium text-gray-700">
        {label}{required ? ' *' : ''}
      </label>
      <select
        value={selectedBranchId ?? ''}
        onChange={(event) => {
          const value = event.target.value
          if (!value) {
            setSelectedBranchId(null)
            return
          }

          const branchId = Number(value)
          if (branchId <= 0) {
            setSelectedBranchId(branchId)
            return
          }

          try {
            setBranch(branchId)
            setSelectedBranchId(branchId)
          } catch {
            navigate('/select-branch')
          }
        }}
        disabled={disabled}
        className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none disabled:cursor-not-allowed disabled:bg-gray-100"
      >
        <option value="">Select Branch</option>
        {canUseAllBranches && <option value={0}>All Branches (Read Only)</option>}
        {branches.map((branch) => (
          <option key={branch.id} value={branch.id}>
            {branch.name}
          </option>
        ))}
      </select>
    </div>
  )
}

export default BranchSelector
