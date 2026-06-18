import { useHasPermission } from '../../hooks/usePermission';
import AdminDashboardPage from './AdminDashboardPage';
import SalesPersonDashboardPage from './SalesPersonDashboardPage';

/**
 * Routes to the admin control panel or personal sales summary based on role permissions.
 * Cashiers / sales staff → personal sales dashboard (even if Dashboard menu is visible).
 * Managers / admins with broad access → full system overview.
 */
export default function DashboardPage() {
  const canPos = useHasPermission('POS Billing', 'view');
  const canSales = useHasPermission('Sales', 'view') || useHasPermission('Orders', 'view');
  const canDashboard = useHasPermission('Dashboard', 'view');
  const canManageUsers = useHasPermission('Users', 'view');
  const canManageProducts = useHasPermission('Products', 'view');
  const canViewReports = useHasPermission('Reports', 'view');

  const hasSalesPersonAccess = canPos || canSales;
  const hasFullAdminAccess =
    canDashboard && (canManageUsers || canManageProducts || canViewReports);

  if (hasSalesPersonAccess && !hasFullAdminAccess) {
    return <SalesPersonDashboardPage />;
  }

  if (canDashboard) {
    return <AdminDashboardPage />;
  }

  if (hasSalesPersonAccess) {
    return <SalesPersonDashboardPage />;
  }

  return (
    <div className="flex items-center justify-center h-64 text-gray-500 p-6 text-center">
      You do not have permission to view the dashboard.
    </div>
  );
}
