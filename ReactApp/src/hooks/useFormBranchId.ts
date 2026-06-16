import { useMemo } from 'react';
import { getBranchRequiredMessage } from '../services/branchContext';
import { useCurrentBranch } from './useCurrentBranch';

/**
 * Resolves branchId for create/edit forms from global context.
 * Entity branch id is kept for edit/image URLs; new records use the header selection.
 */
export const useFormBranchId = (entityBranchId?: number | null) => {
  const { branchId, isAllBranchesMode, hasBranchSelection } = useCurrentBranch();

  const resolvedBranchId = useMemo(() => {
    if (entityBranchId && entityBranchId > 0) {
      return entityBranchId;
    }
    return branchId ?? 0;
  }, [entityBranchId, branchId]);

  const branchError =
    resolvedBranchId <= 0 ? getBranchRequiredMessage(isAllBranchesMode) : null;

  return {
    branchId: resolvedBranchId,
    isAllBranchesMode,
    canSubmit: resolvedBranchId > 0,
    branchError,
    hasBranchSelection,
  };
};
