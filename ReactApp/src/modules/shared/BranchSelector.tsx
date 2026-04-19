import React from 'react'
import { useBranchStore } from '../../stores/useBranchStore'

interface BranchSelectorProps {
  label?: string
  required?: boolean
  disabled?: boolean
  className?: string
}

const BranchSelector: React.FC<BranchSelectorProps> = ({
  label = 'Branch',
  required = true,
  disabled = false,
  className = '',
}) => {
  const branches = useBranchStore((state) => state.branches)
  const selectedBranchId = useBranchStore((state) => state.selectedBranchId)
  const setSelectedBranchId = useBranchStore((state) => state.setSelectedBranchId)

  return (
    <div className={className}>
      <label className="mb-1 block text-sm font-medium text-gray-700">
        {label}{required ? ' *' : ''}
      </label>
      <select
        value={selectedBranchId ?? ''}
        onChange={(event) => {
          const value = event.target.value
          setSelectedBranchId(value ? Number(value) : null)
        }}
        disabled={disabled}
        className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none disabled:cursor-not-allowed disabled:bg-gray-100"
      >
        <option value="">Select Branch</option>
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
