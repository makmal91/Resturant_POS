import { useMemo } from 'react';
import { useBranchStore } from '../stores/useBranchStore';
import { getActiveBranches, shouldShowBranchSelector } from '../services/branchContext';
import { useIsGlobalAdmin } from './usePermission';

/**
 * Reactive access to the global branch context.
 * Branch is never chosen in forms — always read from here.
 */
export const useCurrentBranch = () => {
  const selectedBranchId = useBranchStore((state) => state.selectedBranchId);
  const branches = useBranchStore((state) => state.branches);
  const isLoading = useBranchStore((state) => state.isLoading);
  const getSelectedBranch = useBranchStore((state) => state.getSelectedBranch);
  const isGlobalAdmin = useIsGlobalAdmin();

  const activeBranches = useMemo(() => getActiveBranches(branches), [branches]);

  const branchId =
    selectedBranchId !== null && selectedBranchId > 0 ? selectedBranchId : null;
  const isAllBranchesMode = selectedBranchId === 0;
  const canViewAllBranches = isGlobalAdmin && activeBranches.length > 1;

  return {
    selectedBranchId,
    branchId,
    branches,
    activeBranches,
    isLoading,
    isAllBranchesMode,
    hasBranchSelection: selectedBranchId !== null,
    canViewAllBranches,
    showBranchSelector: shouldShowBranchSelector(branches, isLoading),
    getSelectedBranch,
  };
};
