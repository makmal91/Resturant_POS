import React from 'react'
import { useIsGlobalAdmin } from '../../hooks/usePermission'
import { useBranchStore } from '../../stores/useBranchStore'
import { useAuth } from '../../contexts/AuthContext'

interface BranchSelectorProps {
  /** Label shown above the selector (only visible when rendered) */
  label?: string
  required?: boolean
  disabled?: boolean
  /** Allow the "All Branches" option for global admins */
  allowAllBranches?: boolean
  className?: string
  /**
   * When true this component is for admin/reporting screens and will show
   * a selector even for single-branch users so admins can filter by branch.
   * When false (default) the component auto-hides for single-branch users
   * because branch context is global and needs no UI.
   */
  adminMode?: boolean
}

/**
 * Branch selector for admin/reporting screens ONLY.
 * For regular operations the active branch is set globally via the TopHeader
 * and injected automatically into every API request.
 * This component MUST NOT appear in create/edit forms for standard modules.
 */
const BranchSelector: React.FC<BranchSelectorProps> = ({
  label = 'Branch',
  required = true,
  disabled = false,
  allowAllBranches = true,
  className = '',
  adminMode = false,
}) => {
  const { setBranch } = useAuth()
  const branches = useBranchStore((state) => state.branches)
  const selectedBranchId = useBranchStore((state) => state.selectedBranchId)
  const setSelectedBranchId = useBranchStore((state) => state.setSelectedBranchId)
  const isGlobalAdmin = useIsGlobalAdmin()

  // Single-branch users should never see a branch selector in forms.
  // The active branch is already globally set via the TopHeader.
  if (!adminMode && !isGlobalAdmin && branches.length <= 1) {
    return null
  }

  const canUseAllBranches = allowAllBranches && isGlobalAdmin

  const handleChange = (event: React.ChangeEvent<HTMLSelectElement>) => {
    const value = event.target.value
    if (!value) return

    const branchId = Number(value)
    try {
      setBranch(branchId)
      setSelectedBranchId(branchId)
    } catch {
      // ignore
    }
  }

  return (
    <div className={className}>
      <label className="mb-1 block text-sm font-medium text-gray-700">
        {label}{required ? ' *' : ''}
      </label>
      <select
        value={selectedBranchId ?? ''}
        onChange={handleChange}
        disabled={disabled}
        className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none disabled:cursor-not-allowed disabled:bg-gray-100"
      >
        <option value="">Select Branch</option>
        {canUseAllBranches && <option value={0}>All Branches</option>}
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
