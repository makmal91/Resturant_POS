import { useBranchStore } from '../stores/useBranchStore'
import { getCurrentBranchId, isAllBranchesMode } from '../services/branchContext'
import { hasBranchContext } from '../types/permissions'
import { useIsGlobalAdmin } from './usePermission'

export const useBranchWriteAccess = () => {
  const selectedBranchId = useBranchStore((state) => state.selectedBranchId)
  const isGlobalAdmin = useIsGlobalAdmin()
  const hasBranchSelection = hasBranchContext(selectedBranchId)
  const isGlobalMode = isAllBranchesMode() || selectedBranchId === 0
  const canWriteInView = isGlobalAdmin
    ? hasBranchSelection && getCurrentBranchId() !== null
    : hasBranchSelection && !isGlobalMode

  const resolveEntityBranchId = (entityBranchId?: number | null): number => {
    if (entityBranchId && entityBranchId > 0) {
      return entityBranchId
    }

    const current = getCurrentBranchId()
    if (current !== null) {
      return current
    }

    return 0
  }

  const getWriteBlockMessage = (): string | null => {
    if (!hasBranchSelection) {
      return 'Please select a branch from the header to continue.'
    }

    if (!isGlobalAdmin && isGlobalMode) {
      return 'Select a specific branch to continue.'
    }

    if (isGlobalMode) {
      return 'Select a specific branch from the header before saving.'
    }

    return null
  }

  return {
    selectedBranchId,
    isGlobalAdmin,
    isMasterUser: isGlobalAdmin,
    hasBranchSelection,
    isGlobalMode,
    canWriteInView,
    resolveEntityBranchId,
    getWriteBlockMessage,
    currentBranchId: getCurrentBranchId(),
  }
}
