/** Pick the branch id used for branch-scoped API calls (codes, warehouses, etc.). */
export const resolveEffectiveBranchId = (
  branchId?: number | null,
  selectedBranchId?: number | null,
): number | undefined => {
  if (branchId !== undefined && branchId !== null && branchId > 0) {
    return branchId;
  }

  if (selectedBranchId !== undefined && selectedBranchId !== null && selectedBranchId > 0) {
    return selectedBranchId;
  }

  return undefined;
};
