export interface RouteDefinition {
  path: string;
  label: string;
  component: React.ComponentType;
  module?: string;
}

import DashboardPage from './modules/dashboard/DashboardPage';
import BusinessesList from './components/BusinessesList';
import BranchesList from './components/BranchesList';
import UserPage from './modules/user/UserPage';
import RolePermissionPage from './modules/role/RolePermissionPage';
import MenuList from './components/MenuList';
import InventoryList from './components/InventoryList';
import { OrderScreen } from './components/forms';
import CategoryPage from './modules/category/CategoryPage';
import SubCategoryPage from './modules/subcategory/SubCategoryPage';
import BrandPage from './modules/brand/BrandPage';
import ProductPage from './modules/product/ProductPage';
import CustomerPage from './modules/customer/CustomerPage';
import SupplierPage from './modules/supplier/SupplierPage';
import UnitPage from './modules/unit/UnitPage';
import TaxPage from './modules/tax/TaxPage';
import DiscountPage from './modules/discount/DiscountPage';
import WarehousePage from './modules/warehouse/WarehousePage';
import PurchasePage from './modules/purchase/PurchasePage';
import StockLedgerPage from './modules/stock/StockLedgerPage';
import SaleInvoicesPage from './modules/sales/SaleInvoicesPage';
import CashFlowDashboardPage from './modules/cashflow/CashFlowDashboardPage';
import CashLedgerPage from './modules/cashflow/CashLedgerPage';
import CashFlowSummaryPage from './modules/cashflow/CashFlowSummaryPage';
import ExpensePage from './modules/expense/ExpensePage';
import ReportsPage from './modules/reports/ReportsPage';
// POSBillingPage is registered directly in App.tsx as a fullscreen route (outside Layout)

export const routeRegistry: RouteDefinition[] = [
  { path: '/', label: 'Dashboard', component: DashboardPage },
  { path: '/businesses', label: 'Businesses', component: BusinessesList, module: 'Businesses' },
  { path: '/branches', label: 'Branches', component: BranchesList, module: 'Branches' },
  { path: '/users', label: 'Users', component: UserPage, module: 'Users' },
  { path: '/roles', label: 'Roles', component: RolePermissionPage, module: 'Roles' },
  { path: '/menu', label: 'Menu', component: MenuList, module: 'Menu' },
  { path: '/categories', label: 'Categories', component: CategoryPage, module: 'Categories' },
  { path: '/subcategories', label: 'SubCategories', component: SubCategoryPage, module: 'SubCategories' },
  { path: '/brands', label: 'Brands', component: BrandPage, module: 'Brands' },
  { path: '/products', label: 'Products', component: ProductPage, module: 'Products' },
  { path: '/customers', label: 'Customers', component: CustomerPage, module: 'Customers' },
  { path: '/suppliers', label: 'Suppliers', component: SupplierPage, module: 'Suppliers' },
  { path: '/units', label: 'Units', component: UnitPage, module: 'Units' },
  { path: '/taxes', label: 'Taxes', component: TaxPage },
  { path: '/discounts', label: 'Discounts', component: DiscountPage },
  { path: '/inventory', label: 'Inventory', component: InventoryList, module: 'Inventory' },
  { path: '/orders', label: 'Orders', component: OrderScreen, module: 'Orders' },
  { path: '/warehouses', label: 'Warehouses', component: WarehousePage, module: 'Warehouses' },
  { path: '/purchase', label: 'Purchase', component: PurchasePage, module: 'Purchase' },
  { path: '/stock', label: 'Stock', component: StockLedgerPage, module: 'Stock' },
  { path: '/reports', label: 'Reports', component: ReportsPage, module: 'Reports' },
  { path: '/sales-invoices', label: 'Invoice History', component: SaleInvoicesPage, module: 'Sales' },
  { path: '/expenses',          label: 'Expenses',        component: ExpensePage,           module: 'Expenses' },
  { path: '/cashflow',          label: 'Cash Dashboard',  component: CashFlowDashboardPage, module: 'Cash Flow' },
  { path: '/cashflow/ledger',   label: 'Cash Ledger',     component: CashLedgerPage,        module: 'Cash Flow' },
  { path: '/cashflow/summary',  label: 'Cash Summary',    component: CashFlowSummaryPage,   module: 'Cash Flow' },
];

const routeMap = new Map(routeRegistry.map((route) => [route.path, route]));

export const getRouteDefinition = (path: string): RouteDefinition | undefined => routeMap.get(path);
