import { useMemo } from 'react'
import { usePermission } from './usePermission'
import { useBranchWriteAccess } from './useBranchWriteAccess'

export interface UseModuleCrudAccessOptions {
  /** When false, branch selection is not required for writes (e.g. Businesses, Branches). */
  requireBranchWrite?: boolean
}

/**
 * Combines module RBAC with branch write context.
 * System Admin bypass is handled inside the permission store only.
 */
export const useModuleCrudAccess = (
  moduleName: string,
  options: UseModuleCrudAccessOptions = {},
) => {
  const requireBranchWrite = options.requireBranchWrite ?? true
  const permissions = usePermission(moduleName)
  const branch = useBranchWriteAccess()

  return useMemo(() => {
    const writeReady = requireBranchWrite ? branch.canWriteInView : true

    return {
      ...branch,
      ...permissions,
      canAdd: writeReady && permissions.canCreate,
      canModify: writeReady && permissions.canEdit,
      canRemove: writeReady && permissions.canDelete,
      canExportData: writeReady && permissions.canExport,
      canUploadData: writeReady && permissions.canUpload,
    }
  }, [branch, permissions, requireBranchWrite])
}
