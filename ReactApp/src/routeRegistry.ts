export interface RouteDefinition {
  path: string;
  label: string;
  component: React.ComponentType;
  module?: string;
  form?: string;
  feature?: string;
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
import OpeningStockPage from './modules/opening-stock/OpeningStockPage';
import StockTransferPage from './modules/stock-transfer/StockTransferPage';
import StockAdjustmentPage from './modules/stock-adjustment/StockAdjustmentPage';
import StockAdjustmentReportPage from './modules/stock-adjustment/StockAdjustmentReportPage';
import StockLedgerPage from './modules/stock/StockLedgerPage';
import SaleInvoicesPage from './modules/sales/SaleInvoicesPage';
import CashFlowDashboardPage from './modules/cashflow/CashFlowDashboardPage';
import CashLedgerPage from './modules/cashflow/CashLedgerPage';
import CashFlowSummaryPage from './modules/cashflow/CashFlowSummaryPage';
import RegisterHistoryReportPage from './modules/cashflow/RegisterHistoryReportPage';
import CustomerLedgerPage from './modules/ledger/CustomerLedgerPage';
import SupplierLedgerPage from './modules/ledger/SupplierLedgerPage';
import AccountLedgerPage from './modules/accounting/AccountLedgerPage';
import ExpensePage from './modules/expense/ExpensePage';
import PayablesPage from './modules/finance/PayablesPage';
import ReceivablesPage from './modules/finance/ReceivablesPage';
import FinanceExpensesPage from './modules/finance/FinanceExpensesPage';
import JournalVouchersPage from './modules/finance/JournalVouchersPage';
import SizePage from './modules/master/SizePage';
import ColorPage from './modules/master/ColorPage';
import ExpenseCategoryPage from './modules/master/ExpenseCategoryPage';
import CountryPage from './modules/master/CountryPage';
import CityPage from './modules/master/CityPage';
import CustomerOutstandingReportPage from './modules/reports/CustomerOutstandingReportPage';
import PayableAgingReportPage from './modules/reports/PayableAgingReportPage';
import ReceivableAgingReportPage from './modules/reports/ReceivableAgingReportPage';
import ProfitLossReportPage from './modules/reports/ProfitLossReportPage';
import TrialBalanceReportPage from './modules/reports/TrialBalanceReportPage';
import PurchaseReportPage from './modules/reports/PurchaseReportPage';
import SalesReportPage from './modules/reports/SalesReportPage';
import ProductWiseSalesReportPage from './modules/reports/ProductWiseSalesReportPage';
import StockReportPage from './modules/reports/StockReportPage';
import StockByUnitPivotReportPage from './modules/reports/StockByUnitPivotReportPage';
import SupplierPayableReportPage from './modules/reports/SupplierPayableReportPage';
import SettingsPage from './modules/settings/SettingsPage';
import CodeSequencePage from './modules/settings/CodeSequencePage';
import LicensePage from './modules/settings/LicensePage';
import BarcodePrintPage from './modules/barcode/BarcodePrintPage';
// POSBillingPage is registered directly in App.tsx as a fullscreen route (outside Layout)

export const routeRegistry: RouteDefinition[] = [
  { path: '/', label: 'Dashboard', component: DashboardPage },
  { path: '/businesses', label: 'Businesses', component: BusinessesList, module: 'Businesses' },
  { path: '/branches', label: 'Branches', component: BranchesList, module: 'Branches' },
  { path: '/users', label: 'Users', component: UserPage, module: 'Users' },
  { path: '/roles', label: 'User Roles', component: RolePermissionPage, module: 'Roles' },
  { path: '/menu', label: 'Menu', component: MenuList, module: 'Menu' },
  { path: '/categories', label: 'Categories', component: CategoryPage, module: 'Categories' },
  { path: '/subcategories', label: 'SubCategories', component: SubCategoryPage, module: 'SubCategories' },
  { path: '/brands', label: 'Brands', component: BrandPage, module: 'Brands' },
  { path: '/products', label: 'Products', component: ProductPage, module: 'Products' },
  { path: '/barcodes', label: 'Barcodes', component: BarcodePrintPage, module: 'Barcodes', feature: 'product.barcode.enable' },
  { path: '/customers', label: 'Customers', component: CustomerPage, module: 'Customers' },
  { path: '/suppliers', label: 'Suppliers', component: SupplierPage, module: 'Suppliers' },
  { path: '/units', label: 'Units', component: UnitPage, module: 'Units', feature: 'product.unit.enable' },
  { path: '/settings/sizes', label: 'Sizes', component: SizePage, module: 'Sizes', feature: 'product.variant.enable' },
  { path: '/settings/colors', label: 'Colors', component: ColorPage, module: 'Colors', feature: 'product.variant.enable' },
  { path: '/settings/countries', label: 'Countries', component: CountryPage, module: 'Countries' },
  { path: '/settings/cities', label: 'Cities', component: CityPage, module: 'Cities' },
  { path: '/taxes', label: 'Taxes', component: TaxPage, module: 'Taxes' },
  { path: '/discounts', label: 'Discounts', component: DiscountPage, module: 'Discounts' },
  { path: '/inventory', label: 'Inventory', component: InventoryList, module: 'Inventory' },
  { path: '/orders', label: 'Orders', component: OrderScreen, module: 'Orders' },
  { path: '/warehouses', label: 'Warehouses', component: WarehousePage, module: 'Warehouses' },
  { path: '/purchase', label: 'Purchase', component: PurchasePage, module: 'Purchase' },
  { path: '/opening-stock', label: 'Opening Stock', component: OpeningStockPage, module: 'Opening Stock', feature: 'product.stock.enable' },
  { path: '/stock-transfer', label: 'Stock Transfer', component: StockTransferPage, module: 'Stock Transfer', feature: 'product.stock.enable' },
  { path: '/stock-adjustment', label: 'Stock Adjustment', component: StockAdjustmentPage, module: 'Stock Adjustment', feature: 'product.stock.enable' },
  { path: '/stock', label: 'Stock', component: StockLedgerPage, module: 'Stock', feature: 'product.stock.enable' },
  { path: '/reports/stock-adjustment', label: 'Stock Adjustment Report', component: StockAdjustmentReportPage, module: 'Stock Adjustment', feature: 'product.stock.enable' },
  { path: '/reports/sales', label: 'Sales Report', component: SalesReportPage, module: 'Sales Reports' },
  { path: '/reports/product-wise-sales', label: 'Product Wise Sales', component: ProductWiseSalesReportPage, module: 'Product Wise Sales Report' },
  { path: '/reports/purchases', label: 'Purchase Report', component: PurchaseReportPage, module: 'Purchase Reports' },
  { path: '/reports/customer-outstanding', label: 'Customer Outstanding', component: CustomerOutstandingReportPage, module: 'Customer Outstanding Report' },
  { path: '/reports/supplier-payable', label: 'Supplier Payable', component: SupplierPayableReportPage, module: 'Supplier Payable Report' },
  { path: '/reports/profit-loss', label: 'Profit & Loss', component: ProfitLossReportPage, module: 'Profit & Loss Report' },
  { path: '/reports/trial-balance', label: 'Trial Balance', component: TrialBalanceReportPage, module: 'Trial Balance Report' },
  { path: '/reports/stock', label: 'Stock Report', component: StockReportPage, module: 'Stock Reports', feature: 'product.stock.enable' },
  { path: '/reports/stock-by-unit', label: 'Stock By Unit Report', component: StockByUnitPivotReportPage, module: 'StockReports.ByUnit', feature: 'product.stock.enable' },
  { path: '/reports/receivable-aging', label: 'Receivable Aging', component: ReceivableAgingReportPage, module: 'Customer Receivable Aging' },
  { path: '/reports/payable-aging', label: 'Payable Aging', component: PayableAgingReportPage, module: 'Supplier Payable Aging' },
  { path: '/sales-invoices', label: 'Invoice History', component: SaleInvoicesPage, module: 'Sales' },
  { path: '/expenses',          label: 'Expenses',        component: ExpensePage,           module: 'Expenses' },
  { path: '/finance/payables', label: 'Payables', component: PayablesPage, module: 'Party Ledger' },
  { path: '/finance/receivables', label: 'Receivables', component: ReceivablesPage, module: 'Party Ledger' },
  { path: '/finance/expenses', label: 'Expenses', component: FinanceExpensesPage, module: 'Expenses' },
  { path: '/finance/journal-vouchers', label: 'Journal Vouchers', component: JournalVouchersPage, module: 'Cash Flow' },
  { path: '/expenses/categories', label: 'Expense Categories', component: ExpenseCategoryPage, module: 'Expense Categories' },
  { path: '/cashflow',          label: 'Cash Dashboard',  component: CashFlowDashboardPage, module: 'Cash Flow' },
  { path: '/cashflow/ledger',   label: 'Cash Ledger',     component: CashLedgerPage,        module: 'Cash Flow' },
  { path: '/cashflow/summary',  label: 'Cash Summary',    component: CashFlowSummaryPage,   module: 'Cash Flow' },
  { path: '/cashflow/register-history', label: 'Register History', component: RegisterHistoryReportPage, module: 'Register History Report' },
  { path: '/ledger/customers', label: 'Customer Ledger', component: CustomerLedgerPage, module: 'Party Ledger' },
  { path: '/ledger/suppliers', label: 'Supplier Ledger', component: SupplierLedgerPage, module: 'Party Ledger' },
  { path: '/accounting/ledger', label: 'Account Ledger', component: AccountLedgerPage, module: 'Account Ledger' },
  { path: '/settings', label: 'System Settings', component: SettingsPage, module: 'System Settings' },
  { path: '/settings/code-sequences', label: 'Code Sequences', component: CodeSequencePage, module: 'Code Sequences' },
  { path: '/settings/licenses', label: 'System License', component: LicensePage },
];

const routeMap = new Map(routeRegistry.map((route) => [route.path, route]));

export const getRouteDefinition = (path: string): RouteDefinition | undefined => routeMap.get(path);

const extraRouteContext: Record<string, { module: string; form: string }> = {
  '/pos': { module: 'POS Billing', form: 'POSBilling' },
  '/sales-invoices/edit': { module: 'Sales', form: 'EditInvoice' },
  '/cashflow/opening': { module: 'Cash Flow', form: 'OpeningCash' },
  '/cashflow/closing': { module: 'Cash Flow', form: 'ClosingCash' },
  '/cashflow/register-history': { module: 'Register History Report', form: 'RegisterHistory' },
  '/finance/payables': { module: 'Party Ledger', form: 'PaySupplier' },
  '/finance/receivables': { module: 'Party Ledger', form: 'ReceivePayment' },
  '/finance/expenses': { module: 'Expenses', form: 'Expense' },
  '/finance/journal-vouchers': { module: 'Cash Flow', form: 'JournalVoucher' },
};

export const resolveRouteContext = (
  pathname: string,
): { module: string | null; form: string | null } => {
  const exact = getRouteDefinition(pathname);
  if (exact) {
    return {
      module: exact.module ?? null,
      form: exact.form ?? exact.label.replace(/\s+/g, '') ?? null,
    };
  }

  for (const [prefix, context] of Object.entries(extraRouteContext)) {
    if (pathname === prefix || pathname.startsWith(`${prefix}/`)) {
      return { module: context.module, form: context.form };
    }
  }

  const sortedRoutes = [...routeRegistry]
    .filter((route) => route.path !== '/')
    .sort((a, b) => b.path.length - a.path.length);

  const prefixMatch = sortedRoutes.find(
    (route) => pathname.startsWith(`${route.path}/`) || pathname === route.path,
  );

  if (prefixMatch) {
    return {
      module: prefixMatch.module ?? null,
      form: prefixMatch.form ?? prefixMatch.label.replace(/\s+/g, '') ?? null,
    };
  }

  return { module: null, form: null };
};
