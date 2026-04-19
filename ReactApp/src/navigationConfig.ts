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