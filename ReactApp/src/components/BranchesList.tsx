import React, { useState, useEffect, useCallback } from 'react';
import DataTable, { Column, Action } from './DataTable';
import Badge from './Badge';
import { useFormModal } from '../contexts/FormModalContext';
import { useConfirmDialog } from '../contexts/ConfirmDialogContext';
import { BranchService } from '../services/apiService';
import { safeString } from '../utils/safeValues';

interface Branch {
  id: number;
  name: string;
  address: string;
  city: string;
  phone: string;
  taxRate: number;
  status: string;
  createdAt: string;
}

const BranchesList: React.FC = () => {
  const [branches, setBranches] = useState<Branch[]>([]);
  const [loading, setLoading] = useState(true);
  const { openForm, isOpen } = useFormModal();
  const { showConfirm } = useConfirmDialog();

  const normalizeBranches = (payload: unknown): Branch[] => {
    const rows = Array.isArray(payload) ? payload : [];

    return rows
      .map((row): Branch => {
        const item = row as Partial<Branch>;
        return {
          id: Number(item?.id ?? 0),
          name: safeString(item?.name),
          address: safeString(item?.address),
          city: safeString(item?.city),
          phone: safeString(item?.phone),
          taxRate: Number(item?.taxRate ?? 0),
          status: safeString(item?.status, 'Active') || 'Active',
          createdAt: safeString(item?.createdAt),
        };
      })
      .filter((branch) => branch.id > 0);
  };

  const fetchBranches = useCallback(async () => {
    setLoading(true);
    try {
      // Try API first, fall back to mock data
      try {
        const response = await BranchService.getAll();
        setBranches(normalizeBranches(response?.data));
      } catch (err) {
        // Use mock data if API fails
        console.log('Using mock data for branches');
        setBranches([
          {
            id: 1,
            name: 'Main Branch',
            address: '123 Main St',
            city: 'New York',
            phone: '+1-555-0123',
            taxRate: 8.5,
            status: 'Active',
            createdAt: '2024-01-15'
          },
          {
            id: 2,
            name: 'Downtown Branch',
            address: '456 Downtown Ave',
            city: 'New York',
            phone: '+1-555-0124',
            taxRate: 8.5,
            status: 'Active',
            createdAt: '2024-02-20'
          },
          {
            id: 3,
            name: 'Airport Branch',
            address: '789 Airport Rd',
            city: 'New York',
            phone: '+1-555-0125',
            taxRate: 8.5,
            status: 'Inactive',
            createdAt: '2024-03-10'
          }
        ]);
      }
    } finally {
      setLoading(false);
    }
  }, []);

  // Fetch branches on mount and when modal closes
  useEffect(() => {
    fetchBranches();
  }, [fetchBranches]);

  // Refresh list when modal closes (form was submitted)
  useEffect(() => {
    if (!isOpen) {
      fetchBranches();
    }
  }, [isOpen, fetchBranches]);

  const handleAddBranch = () => {
    openForm('branch');
  };

  const handleEditBranch = (branch: Branch) => {
    openForm('branch', branch);
  };

  const handleDeleteBranch = (branch: Branch) => {
    showConfirm({
      title: 'Delete Branch',
      message: `Are you sure you want to delete "${branch.name}"? This action cannot be undone.`,
      confirmLabel: 'Delete',
      cancelLabel: 'Cancel',
      onConfirm: async () => {
        try {
          await BranchService.delete(branch.id);
          setBranches(branches.filter(b => b.id !== branch.id));
        } catch (error) {
          console.error('Failed to delete branch:', error);
          alert('Failed to delete branch. Please try again.');
        }
      }
    });
  };

  const columns: Column<Branch>[] = [
    {
      key: 'name',
      header: 'Branch Name',
      sortable: true,
    },
    {
      key: 'address',
      header: 'Address',
      sortable: true,
    },
    {
      key: 'city',
      header: 'City',
      sortable: true,
    },
    {
      key: 'phone',
      header: 'Phone',
    },
    {
      key: 'taxRate',
      header: 'Tax Rate',
      render: (value) => `${value}%`,
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

  const actions: Action<Branch>[] = [
    {
      label: 'Edit',
      onClick: handleEditBranch,
      variant: 'primary',
    },
    {
      label: 'Delete',
      onClick: handleDeleteBranch,
      variant: 'danger',
    },
  ];

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Branches</h1>
        <p className="text-gray-600">Manage your restaurant branches and locations</p>
      </div>

      <div className="mb-6 flex justify-between items-center">
        <div></div>
        <button
          onClick={handleAddBranch}
          className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors"
        >
          <svg className="-ml-1 mr-2 h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
          </svg>
          Add Branch
        </button>
      </div>

      <DataTable
        data={branches}
        columns={columns}
        actions={actions}
        loading={loading}
        searchable={true}
        pagination={true}
        pageSize={10}
        emptyMessage="No branches found"
      />
    </div>
  );
};

export default BranchesList;