import { useBranchStore } from '../stores/useBranchStore'
import { hasBranchContext } from '../types/permissions'
import { useIsGlobalAdmin } from './usePermission'

export const useBranchWriteAccess = () => {
  const selectedBranchId = useBranchStore((state) => state.selectedBranchId)
  const isGlobalAdmin = useIsGlobalAdmin()
  const hasBranchSelection = hasBranchContext(selectedBranchId)
  const isGlobalMode = selectedBranchId === 0
  const canWriteInView = isGlobalAdmin
    ? hasBranchSelection
    : hasBranchSelection && !isGlobalMode

  const resolveEntityBranchId = (entityBranchId?: number | null): number => {
    if (entityBranchId && entityBranchId > 0) {
      return entityBranchId
    }

    if (selectedBranchId && selectedBranchId > 0) {
      return selectedBranchId
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
  }
}
