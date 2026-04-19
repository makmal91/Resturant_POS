export interface NavigationItem {
  path: string;
  label: string;
  component: React.ComponentType;
  icon?: string;
  group?: string;
}

export interface NavigationGroup {
  name: string;
  items: NavigationItem[];
}

import POS from './components/POS';
import BranchesList from './components/BranchesList';
import UsersList from './components/UsersList';
import MenuList from './components/MenuList';
import InventoryList from './components/InventoryList';
import { OrderScreen } from './components/forms';
import CategoryPage from './modules/category/CategoryPage';
import SubCategoryPage from './modules/subcategory/SubCategoryPage';
import ProductPage from './modules/product/ProductPage';
import CustomerPage from './modules/customer/CustomerPage';
import SupplierPage from './modules/supplier/SupplierPage';
import UnitPage from './modules/unit/UnitPage';
import TaxPage from './modules/tax/TaxPage';
import DiscountPage from './modules/discount/DiscountPage';

export const navigationItems: NavigationItem[] = [
  {
    path: '/',
    label: 'Dashboard',
    component: POS,
    icon: '📊',
  },
  {
    path: '/branches',
    label: 'Branches',
    component: BranchesList,
    icon: '🏢',
    group: 'Master Data',
  },
  {
    path: '/users',
    label: 'Users',
    component: UsersList,
    icon: '👥',
    group: 'Master Data',
  },
  {
    path: '/menu',
    label: 'Menu',
    component: MenuList,
    icon: '🍽️',
    group: 'Master Data',
  },
  {
    path: '/categories',
    label: 'Categories',
    component: CategoryPage,
    icon: '🗂️',
    group: 'Master Data',
  },
  {
    path: '/subcategories',
    label: 'SubCategories',
    component: SubCategoryPage,
    icon: '🧩',
    group: 'Master Data',
  },
  {
    path: '/products',
    label: 'Products',
    component: ProductPage,
    icon: '🧾',
    group: 'Master Data',
  },
  {
    path: '/customers',
    label: 'Customers',
    component: CustomerPage,
    icon: '🙋',
    group: 'Master Data',
  },
  {
    path: '/suppliers',
    label: 'Suppliers',
    component: SupplierPage,
    icon: '🚚',
    group: 'Master Data',
  },
  {
    path: '/units',
    label: 'Units',
    component: UnitPage,
    icon: '⚖️',
    group: 'Master Data',
  },
  {
    path: '/taxes',
    label: 'Taxes',
    component: TaxPage,
    icon: '💸',
    group: 'Master Data',
  },
  {
    path: '/discounts',
    label: 'Discounts',
    component: DiscountPage,
    icon: '🏷️',
    group: 'Master Data',
  },
  {
    path: '/inventory',
    label: 'Inventory',
    component: InventoryList,
    icon: '📦',
    group: 'Operations',
  },
  {
    path: '/orders',
    label: 'Orders',
    component: OrderScreen,
    icon: '📋',
    group: 'Operations',
  },
];

export const navigationGroups: NavigationGroup[] = [
  {
    name: 'Dashboard',
    items: navigationItems.filter(item => item.path === '/'),
  },
  {
    name: 'Master Data',
    items: navigationItems.filter(item => item.group === 'Master Data'),
  },
  {
    name: 'Operations',
    items: navigationItems.filter(item => item.group === 'Operations'),
  },
];