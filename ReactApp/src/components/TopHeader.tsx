import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { useIsGlobalAdmin } from '../hooks/usePermission';
import { useBranchStore } from '../stores/useBranchStore';

const TopHeader: React.FC = () => {
  const navigate = useNavigate();
  const { user, logout, selectedBranchId, setBranch } = useAuth();
  const branches = useBranchStore((state) => state.branches);
  const setSelectedBranchId = useBranchStore((state) => state.setSelectedBranchId);
  const isGlobalAdmin = useIsGlobalAdmin();

  const selectedBranch = branches.find((branch) => branch.id === selectedBranchId) ?? null;
  const initials = (user?.fullName || user?.username || 'U')
    .split(' ')
    .map((part) => part.charAt(0))
    .join('')
    .slice(0, 2)
    .toUpperCase();

  const handleLogout = () => {
    logout();
    navigate('/login', { replace: true });
  };

  const handleBranchChange = (event: React.ChangeEvent<HTMLSelectElement>) => {
    const value = event.target.value;
    if (!value) {
      return;
    }

    const branchId = Number(value);
    if (!Number.isFinite(branchId)) {
      return;
    }

    try {
      setBranch(branchId);
      setSelectedBranchId(branchId);
    } catch {
      navigate('/select-branch');
    }
  };

  const showBranchSelector = isGlobalAdmin
    ? branches.length > 0
    : branches.length > 1;
  const branchLabel =
    selectedBranchId === 0
      ? 'All Branches'
      : selectedBranch?.name ?? 'Select Branch';

  return (
    <header className="bg-white shadow-sm border-b border-gray-200 px-6 py-4">
      <div className="flex items-center justify-between gap-4">
        <div className="flex-1 max-w-md">
          <div className="relative">
            <input
              type="text"
              placeholder="Search..."
              className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            />
            <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
              <svg className="h-5 w-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
            </div>
          </div>
        </div>

        <div className="flex items-center space-x-4">
          {showBranchSelector && (
            <div className="min-w-[220px]">
              <label htmlFor="header-branch" className="sr-only">
                Active Branch
              </label>
              <select
                id="header-branch"
                value={selectedBranchId ?? ''}
                onChange={handleBranchChange}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
              >
                {!isGlobalAdmin && <option value="">Select Branch</option>}
                {isGlobalAdmin && <option value={0}>All Branches</option>}
                {branches.map((branch) => (
                  <option key={branch.id} value={branch.id}>
                    {branch.name}
                  </option>
                ))}
              </select>
            </div>
          )}

          {!showBranchSelector && selectedBranch && (
            <span className="hidden text-sm text-gray-600 md:inline">
              Branch: <span className="font-medium text-gray-900">{selectedBranch.name}</span>
            </span>
          )}

          {isGlobalAdmin && selectedBranchId === 0 && (
            <span className="hidden text-sm text-gray-600 md:inline">
              Branch: <span className="font-medium text-gray-900">{branchLabel}</span>
            </span>
          )}

          <div className="flex items-center space-x-3">
            <div className="flex items-center space-x-2">
              <div className="w-8 h-8 bg-blue-500 rounded-full flex items-center justify-center">
                <span className="text-white text-sm font-medium">{initials}</span>
              </div>
              <div className="hidden sm:block">
                <p className="text-sm font-medium text-gray-900">{user?.fullName || user?.username || 'User'}</p>
                <p className="text-xs text-gray-500">{user?.roleName || 'Branch User'}</p>
              </div>
            </div>

            <button
              type="button"
              onClick={handleLogout}
              className="rounded-lg border border-gray-300 px-3 py-1.5 text-sm font-medium text-gray-700 transition hover:bg-gray-50"
            >
              Logout
            </button>
          </div>
        </div>
      </div>
    </header>
  );
};

export default TopHeader;
