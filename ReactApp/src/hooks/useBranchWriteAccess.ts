import { useCallback, useMemo } from 'react'
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

  const resolveEntityBranchId = useCallback((entityBranchId?: number | null): number => {
    if (entityBranchId && entityBranchId > 0) {
      return entityBranchId
    }

    const current = getCurrentBranchId()
    if (current !== null) {
      return current
    }

    return 0
  }, [selectedBranchId])

  const getWriteBlockMessage = useCallback((): string | null => {
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
  }, [hasBranchSelection, isGlobalAdmin, isGlobalMode])

  const currentBranchId = getCurrentBranchId()

  return useMemo(
    () => ({
      selectedBranchId,
      isGlobalAdmin,
      hasBranchSelection,
      isGlobalMode,
      canWriteInView,
      resolveEntityBranchId,
      getWriteBlockMessage,
      currentBranchId,
    }),
    [
      selectedBranchId,
      isGlobalAdmin,
      hasBranchSelection,
      isGlobalMode,
      canWriteInView,
      resolveEntityBranchId,
      getWriteBlockMessage,
      currentBranchId,
    ],
  )
}
