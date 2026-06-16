import { useHasPermission } from '../../hooks/usePermission';
import AdminDashboardPage from './AdminDashboardPage';
import SalesPersonDashboardPage from './SalesPersonDashboardPage';

/**
 * Routes to the admin control panel or personal sales summary based on role permissions.
 * - Dashboard module → full system overview (managers/admins)
 * - POS Billing / Sales only → logged-in user's sales data only
 */
export default function DashboardPage() {
  const canAdminDashboard = useHasPermission('Dashboard', 'view');
  const canPos            = useHasPermission('POS Billing', 'view');
  const canSales          = useHasPermission('Sales', 'view');

  if (canAdminDashboard) {
    return <AdminDashboardPage />;
  }

  if (canPos || canSales) {
    return <SalesPersonDashboardPage />;
  }

  return (
    <div className="flex items-center justify-center h-64 text-gray-500 p-6 text-center">
      You do not have permission to view the dashboard.
    </div>
  );
}
