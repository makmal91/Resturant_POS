import React, { useCallback, useEffect, useMemo, useState } from 'react';
import DataTable, { Action, Column } from '../../components/DataTable';
import Badge from '../../components/Badge';
import { getActiveBranches } from '../../services/branchContext';
import { useBranchStore } from '../../stores/useBranchStore';
import { useFormModal } from '../../contexts/FormModalContext';
import { useConfirmDialog } from '../../contexts/ConfirmDialogContext';
import { getApiErrorMessage } from '../../services/api';
import { safeString } from '../../utils/safeValues';
import { userService, UserListItem } from './userService';
import { useModuleCrudAccess } from '../../hooks/useModuleCrudAccess';
import { getPermissionDeniedMessage } from '../../utils/permissionUtils';
import PermissionGate from '../../components/PermissionGate';
import { useIsMasterUser, useIsSuperAdmin } from '../../hooks/usePermission';
import { isProtectedRole } from '../../types/permissions';

const UserPage: React.FC = () => {
  const selectedBranchId = useBranchStore((state) => state.selectedBranchId);
  const branches = useBranchStore((state) => state.branches);
  const hasMultipleBranches = getActiveBranches(branches).length > 1;
  const { openForm, isOpen } = useFormModal();
  const { showConfirm } = useConfirmDialog();
  const {
    canAdd,
    canModify,
    canRemove,
    isGlobalMode,
    canWriteInView,
    resolveEntityBranchId,
    getWriteBlockMessage,
  } = useModuleCrudAccess('Users');
  const isSuperAdmin = useIsSuperAdmin();
  const isMasterUser = useIsMasterUser();

  const [users, setUsers] = useState<UserListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(5);
  const [searchTerm, setSearchTerm] = useState('');
  const [sortColumn, setSortColumn] = useState('fullName');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [notification, setNotification] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  const hasBranchSelection = selectedBranchId !== null;

  const showNotification = useCallback((type: 'success' | 'error', message: string) => {
    setNotification({ type, message });
    setTimeout(() => setNotification(null), 4000);
  }, []);

  const fetchUsers = useCallback(async () => {
    if (selectedBranchId === null) {
      setUsers([]);
      setTotalRecords(0);
      setTotalPages(0);
      setLoading(false);
      return;
    }

    setLoading(true);
    try {
      const result = await userService.getAll({
        branchId: selectedBranchId,
        page: currentPage,
        pageSize,
        search: searchTerm,
        sortBy: sortColumn,
        sortDirection,
      });
      setUsers(result.data);
      setTotalRecords(result.totalRecords);
      setTotalPages(result.totalPages);
    } catch (error) {
      console.error('Failed to fetch users:', error);
      setUsers([]);
      setTotalRecords(0);
      setTotalPages(0);
      showNotification('error', getApiErrorMessage(error, 'Failed to load users.'));
    } finally {
      setLoading(false);
    }
  }, [selectedBranchId, currentPage, pageSize, searchTerm, sortColumn, sortDirection, showNotification]);

  useEffect(() => {
    const timer = setTimeout(() => {
      fetchUsers();
    }, searchTerm ? 300 : 0);
    return () => clearTimeout(timer);
  }, [fetchUsers, searchTerm]);

  useEffect(() => {
    if (!isOpen) return undefined;
    return () => {
      fetchUsers();
    };
  }, [isOpen, fetchUsers]);

  const resolveUserBranchId = (user: UserListItem): number =>
    resolveEntityBranchId(user.primaryBranchId || user.branches[0]?.branchId);

  const handleAddUser = () => {
    const blockMessage = getWriteBlockMessage();
    if (!canAdd || blockMessage) {
      if (blockMessage) {
        showNotification('error', blockMessage);
      } else {
        showNotification('error', getPermissionDeniedMessage('create', 'Users'));
      }
      return;
    }

    openForm('user', {
      branchIds: isGlobalMode ? [] : [selectedBranchId!],
    });
  };

  const canManageUser = (user: UserListItem): boolean => {
    if (isMasterUser) {
      return true;
    }

    if (isSuperAdmin && isProtectedRole(user.roleName)) {
      return false;
    }

    return true;
  };

  const handleEditUser = async (user: UserListItem) => {
    if (!canManageUser(user)) {
      showNotification('error', `You do not have permission to edit ${user.roleName} accounts.`);
      return;
    }
    const blockMessage = getWriteBlockMessage();
    if (!canModify || blockMessage) {
      if (blockMessage) {
        showNotification('error', blockMessage);
      } else {
        showNotification('error', getPermissionDeniedMessage('edit', 'Users'));
      }
      return;
    }

    const branchId = resolveUserBranchId(user);
    if (branchId <= 0) {
      showNotification('error', 'Unable to determine the branch for this user.');
      return;
    }

    try {
      const detail = await userService.getById(user.id, branchId);
      openForm('user', {
        id: detail.id,
        fullName: detail.fullName,
        username: detail.username,
        email: detail.email,
        phone: detail.phone,
        roleId: detail.roleId,
        isActive: detail.isActive,
        branchIds: detail.branches.map((branch) => branch.branchId),
      });
    } catch (error) {
      console.error('Failed to load user details:', error);
      showNotification('error', getApiErrorMessage(error, 'Failed to load user details.'));
    }
  };

  const handleDeleteUser = (user: UserListItem) => {
    if (!canManageUser(user)) {
      showNotification('error', `You do not have permission to delete ${user.roleName} accounts.`);
      return;
    }
    const blockMessage = getWriteBlockMessage();
    if (!canRemove || blockMessage) {
      if (blockMessage) {
        showNotification('error', blockMessage);
      } else {
        showNotification('error', getPermissionDeniedMessage('delete', 'Users'));
      }
      return;
    }

    const branchId = resolveUserBranchId(user);
    if (branchId <= 0) {
      showNotification('error', 'Unable to determine the branch for this user.');
      return;
    }

    showConfirm({
      title: 'Delete User?',
      message: 'This user account will be deactivated and removed from the system.',
      highlightText: user.fullName,
      variant: 'danger',
      confirmLabel: 'Yes, Delete',
      cancelLabel: 'Keep User',
      onConfirm: async () => {
        try {
          await userService.delete(user.id, branchId);
          await fetchUsers();
          showNotification('success', `User "${user.fullName}" deleted successfully.`);
        } catch (error) {
          console.error('Failed to delete user:', error);
          showNotification('error', getApiErrorMessage(error, 'Failed to delete user.'));
        }
      },
    });
  };

  const handleSearchChange = (value: string) => {
    setSearchTerm(value);
    setCurrentPage(1);
  };

  const handleSortChange = (column: string, direction: 'asc' | 'desc') => {
    setSortColumn(column);
    setSortDirection(direction);
    setCurrentPage(1);
  };

  const handlePageSizeChange = (nextPageSize: number) => {
    setPageSize(nextPageSize);
    setCurrentPage(1);
  };

  const columns: Column<UserListItem>[] = useMemo(() => {
    const base: Column<UserListItem>[] = [
      {
        key: 'fullName',
        header: 'Full Name',
        sortable: true,
      },
      {
        key: 'username',
        header: 'Username',
        sortable: true,
      },
      {
        key: 'email',
        header: 'Email',
        sortable: true,
      },
      {
        key: 'roleName',
        header: 'Role',
        sortable: true,
        render: (value) => <Badge variant="primary" size="sm">{safeString(value)}</Badge>,
      },
      {
        key: 'assignedBranchesDisplay',
        header: 'Assigned Branches',
        render: (_, row) =>
          safeString(row.assignedBranchesDisplay || row.branches.map((b) => b.branchName).join(', ')),
      },
      {
        key: 'isActive',
        header: 'Status',
        sortable: true,
        render: (value) => (
          <Badge variant={value ? 'success' : 'danger'} size="sm" dot>
            {value ? 'Active' : 'Inactive'}
          </Badge>
        ),
      },
    ];

    if (isGlobalMode) {
      base.splice(4, 0, {
        key: 'primaryBranchName',
        header: 'Primary Branch',
        sortable: true,
        render: (_, row) => safeString(row.primaryBranchName),
      });
    }

    return base;
  }, [isGlobalMode]);

  const actions: Action<UserListItem>[] = [
    ...(canModify
      ? [{
          label: '',
          onClick: handleEditUser,
          variant: 'secondary' as const,
          hidden: (row: UserListItem) => !canManageUser(row),
          icon: (
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" title="Edit">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
            </svg>
          ),
        }]
      : []),
    ...(canRemove
      ? [{
          label: '',
          onClick: handleDeleteUser,
          variant: 'danger' as const,
          hidden: (row: UserListItem) => !canManageUser(row),
          icon: (
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" title="Delete">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
            </svg>
          ),
        }]
      : []),
  ];

  return (
    <div>
      {notification && (
        <div
          className={`mb-6 p-4 rounded-md flex items-center ${
            notification.type === 'success'
              ? 'bg-green-50 text-green-800'
              : 'bg-red-50 text-red-800'
          }`}
        >
          {notification.type === 'success' ? (
            <svg className="w-5 h-5 mr-3" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
            </svg>
          ) : (
            <svg className="w-5 h-5 mr-3" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
            </svg>
          )}
          <span className="font-medium">{notification.message}</span>
        </div>
      )}

      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Users</h1>
        <p className="text-gray-600">Manage staff accounts, roles, and branch access</p>
      </div>

      <div className="mb-6 flex justify-end">
        <PermissionGate module="Users" action="create">
          {canAdd && (
            <button
              onClick={handleAddUser}
              className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors"
            >
              <svg className="-ml-1 mr-2 h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
              </svg>
              Add User
            </button>
          )}
        </PermissionGate>
      </div>

      {isGlobalMode && !canWriteInView && hasMultipleBranches && (
        <div className="mb-6 rounded-md border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
          Global view is read-only. Select a specific branch to create or edit users.
        </div>
      )}

      {isGlobalMode && canWriteInView && hasMultipleBranches && (
        <div className="mb-6 rounded-md border border-blue-200 bg-blue-50 px-4 py-3 text-sm text-blue-800">
          Global view is active. Assign branches in the user form when creating or editing users.
        </div>
      )}

      {!hasBranchSelection && (
        <div className="mb-6 rounded-md border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-700">
          Select a branch from the header to load users.
        </div>
      )}

      <DataTable
        data={users}
        columns={columns}
        actions={actions}
        loading={loading}
        searchable
        searchPlaceholder="Search by name, username, email, or role..."
        pagination
        pageSize={pageSize}
        pageSizeOptions={[5, 10, 25, 50]}
        onPageSizeChange={handlePageSizeChange}
        emptyMessage={
          !hasBranchSelection
            ? 'Select a branch from the header to view users.'
            : searchTerm
              ? 'No users match your search.'
              : 'No users found for the selected branch.'
        }
        serverSide
        totalRecords={totalRecords}
        totalPages={totalPages}
        currentPage={currentPage}
        onPageChange={setCurrentPage}
        searchTerm={searchTerm}
        onSearchChange={handleSearchChange}
        sortColumn={sortColumn}
        sortDirection={sortDirection}
        onSortChange={handleSortChange}
      />
    </div>
  );
};

export default UserPage;
