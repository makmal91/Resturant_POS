import React, { useEffect } from 'react';
import Sidebar from './Sidebar';
import TopHeader from './TopHeader';
import { useBranchStore } from '../stores/useBranchStore';
import { useAuth } from '../contexts/AuthContext';

interface LayoutProps {
  children: React.ReactNode;
}

const Layout: React.FC<LayoutProps> = ({ children }) => {
  const fetchBranches = useBranchStore((state) => state.fetchBranches);
  const { selectedBranchId } = useAuth();

  useEffect(() => {
    void fetchBranches();
  }, [fetchBranches]);

  useEffect(() => {
    useBranchStore.getState().setSelectedBranchId(selectedBranchId);
  }, [selectedBranchId]);

  return (
    <div className="flex h-screen bg-gray-50 overflow-hidden">
      <Sidebar />
      <div className="flex-1 min-w-0 flex flex-col overflow-hidden">
        <TopHeader />
        <main className="flex-1 overflow-auto p-6">
          {children}
        </main>
      </div>
    </div>
  );
};

export default Layout;