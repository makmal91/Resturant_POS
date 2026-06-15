export interface RouteDefinition {
  path: string;
  label: string;
  component: React.ComponentType;
  module?: string;
}

import POS from './components/POS';
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

export const routeRegistry: RouteDefinition[] = [
  { path: '/', label: 'Dashboard', component: POS, module: 'POS Billing' },
  { path: '/businesses', label: 'Businesses', component: BusinessesList, module: 'Businesses' },
  { path: '/branches', label: 'Branches', component: BranchesList, module: 'Branches' },
  { path: '/users', label: 'Users', component: UserPage, module: 'Users' },
  { path: '/roles', label: 'Roles', component: RolePermissionPage, module: 'Roles' },
  { path: '/menu', label: 'Menu', component: MenuList, module: 'Menu' },
  { path: '/categories', label: 'Categories', component: CategoryPage, module: 'Categories' },
  { path: '/subcategories', label: 'SubCategories', component: SubCategoryPage, module: 'SubCategories' },
  { path: '/brands', label: 'Brands', component: BrandPage, module: 'Brands' },
  { path: '/products', label: 'Products', component: ProductPage, module: 'Products' },
  { path: '/customers', label: 'Customers', component: CustomerPage },
  { path: '/suppliers', label: 'Suppliers', component: SupplierPage },
  { path: '/units', label: 'Units', component: UnitPage },
  { path: '/taxes', label: 'Taxes', component: TaxPage },
  { path: '/discounts', label: 'Discounts', component: DiscountPage },
  { path: '/inventory', label: 'Inventory', component: InventoryList, module: 'Inventory' },
  { path: '/orders', label: 'Orders', component: OrderScreen, module: 'Orders' },
];

const routeMap = new Map(routeRegistry.map((route) => [route.path, route]));

export const getRouteDefinition = (path: string): RouteDefinition | undefined => routeMap.get(path);
