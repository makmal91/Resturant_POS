import React, { useState, useEffect, useCallback } from 'react';
import DataTable, { Column, Action } from './DataTable';
import Badge from './Badge';
import { useFormModal } from '../contexts/FormModalContext';
import { useConfirmDialog } from '../contexts/ConfirmDialogContext';
import { UserService } from '../services/apiService';
import { safeString } from '../utils/safeValues';

interface User {
  id: number;
  name: string;
  role: string;
  branch: string;
  salary: number;
  shift: string;
  status: string;
}

const UsersList: React.FC = () => {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const { openForm, isOpen } = useFormModal();
  const { showConfirm } = useConfirmDialog();

  const normalizeUsers = (payload: unknown): User[] => {
    const rows = Array.isArray(payload) ? payload : [];

    return rows
      .map((row): User => {
        const item = row as Partial<User>;
        return {
          id: Number(item?.id ?? 0),
          name: safeString(item?.name),
          role: safeString(item?.role),
          branch: safeString(item?.branch),
          salary: Number(item?.salary ?? 0),
          shift: safeString(item?.shift),
          status: safeString(item?.status, 'Active') || 'Active',
        };
      })
      .filter((user) => user.id > 0);
  };

  const fetchUsers = useCallback(async () => {
    setLoading(true);
    try {
      // Try API first, fall back to mock data
      try {
        const response = await UserService.getAll();
        setUsers(normalizeUsers(response?.data));
      } catch (err) {
        // Use mock data if API fails
        console.log('Using mock data for users');
        setUsers([
          {
            id: 1,
            name: 'John Smith',
            role: 'Manager',
            branch: 'Main Branch',
            salary: 50000,
            shift: 'Morning',
            status: 'Active'
          },
          {
            id: 2,
            name: 'Sarah Johnson',
            role: 'Cashier',
            branch: 'Downtown Branch',
            salary: 30000,
            shift: 'Afternoon',
            status: 'Active'
          },
          {
            id: 3,
            name: 'Mike Chen',
            role: 'Chef',
            branch: 'Main Branch',
            salary: 45000,
            shift: 'Evening',
            status: 'Active'
          },
        ]);
      }
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchUsers();
  }, [fetchUsers]);

  useEffect(() => {
    if (!isOpen) {
      fetchUsers();
    }
  }, [isOpen, fetchUsers]);

  const handleAddUser = () => {
    openForm('user');
  };

  const handleEditUser = (user: User) => {
    openForm('user', user);
  };

  const handleDeleteUser = (user: User) => {
    showConfirm({
      title: 'Delete User',
      message: `Are you sure you want to delete "${user.name}"? This action cannot be undone.`,
      confirmLabel: 'Delete',
      cancelLabel: 'Cancel',
      onConfirm: async () => {
        try {
          await UserService.delete(user.id);
          setUsers(users.filter(u => u.id !== user.id));
        } catch (error) {
          console.error('Failed to delete user:', error);
          alert('Failed to delete user. Please try again.');
        }
      }
    });
  };

  const columns: Column<User>[] = [
    {
      key: 'name',
      header: 'Name',
      sortable: true,
    },
    {
      key: 'role',
      header: 'Role',
      render: (value) => (
        <Badge variant="primary" size="sm">{safeString(value)}</Badge>
      ),
    },
    {
      key: 'branch',
      header: 'Branch',
      sortable: true,
    },
    {
      key: 'shift',
      header: 'Shift',
      render: (value) => (
        <Badge variant="info" size="sm">{safeString(value)}</Badge>
      ),
    },
    {
      key: 'salary',
      header: 'Salary',
      render: (value) => `$${(value || 0).toLocaleString()}`,
    },
    {
      key: 'status',
      header: 'Status',
      render: (value) => (
        <Badge variant={value === 'Active' ? 'success' : 'danger'} size="sm" dot>
          {safeString(value)}
        </Badge>
      ),
    },
  ];

  const actions: Action<User>[] = [
    {
      label: 'Edit',
      onClick: handleEditUser,
      variant: 'primary',
    },
    {
      label: 'Delete',
      onClick: handleDeleteUser,
      variant: 'danger',
    },
  ];

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Users</h1>
        <p className="text-gray-600">Manage staff and user accounts</p>
      </div>

      <div className="mb-6 flex justify-end">
        <button
          onClick={handleAddUser}
          className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors"
        >
          <svg className="-ml-1 mr-2 h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
          </svg>
          Add User
        </button>
      </div>

      <DataTable
        data={users}
        columns={columns}
        actions={actions}
        loading={loading}
        searchable={true}
        pagination={true}
        pageSize={10}
      />
    </div>
  );
};

export default UsersList;