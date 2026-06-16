import { useBranchStore } from '../stores/useBranchStore';
import { authStorage } from '../utils/storage';

/** Dispatched when the global branch selection changes. */
export const BRANCH_CHANGED_EVENT = 'branch:changed';

/** Sentinel value: All Branches mode (admin only). */
export const ALL_BRANCHES_ID = 0;

export const dispatchBranchChanged = (branchId: number | null): void => {
  window.dispatchEvent(new CustomEvent(BRANCH_CHANGED_EVENT, { detail: { branchId } }));
};

/**
 * Raw global selection including All Branches (0) and null (unset).
 */
export const getSelectedBranchId = (): number | null => {
  const fromStore = useBranchStore.getState().selectedBranchId;
  if (fromStore !== null) {
    return fromStore;
  }
  return authStorage.getSelectedBranchId();
};

/**
 * Active branch for scoped operations (creates, writes, single-branch filters).
 * Returns null when All Branches mode is active or no branch is selected.
 */
export const getCurrentBranchId = (): number | null => {
  const id = getSelectedBranchId();
  if (id === null || id === ALL_BRANCHES_ID) {
    return null;
  }
  return id;
};

export const isAllBranchesMode = (): boolean => getSelectedBranchId() === ALL_BRANCHES_ID;

export const hasBranchSelection = (): boolean => getSelectedBranchId() !== null;

/**
 * Branch filter for list/report queries. Returns 0 for All Branches, null when unset.
 */
export const getBranchFilterId = (): number | null => getSelectedBranchId();

/**
 * Resolves a concrete branch id for API calls (code generation, writes).
 * Explicit entity branch wins; otherwise uses global context.
 */
export const getEffectiveBranchId = (explicitBranchId?: number | null): number | undefined => {
  if (explicitBranchId !== undefined && explicitBranchId !== null && explicitBranchId > 0) {
    return explicitBranchId;
  }
  const current = getCurrentBranchId();
  return current ?? undefined;
};

/** Whether the header branch selector should be visible. */
export type BranchLike = { isActive?: boolean };

/** Active branches only (treat missing isActive as active). */
export const getActiveBranches = <T extends BranchLike>(branches?: T[] | null): T[] =>
  (branches ?? []).filter((branch) => branch.isActive !== false);

/**
 * Show selector only when branch data is loaded and there are 2+ active branches.
 */
export const shouldShowBranchSelector = (
  branches: BranchLike[] | null | undefined,
  isLoading: boolean,
): boolean => {
  if (isLoading) {
    return false;
  }
  if (!branches) {
    return false;
  }
  const activeBranches = getActiveBranches(branches);
  return activeBranches.length > 1;
};

export const getBranchRequiredMessage = (isAllBranches: boolean): string =>
  isAllBranches
    ? 'Select a specific branch from the header before saving.'
    : 'Select a branch from the header to continue.';
