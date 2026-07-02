/**
 * Mirrors Application/Common/Constants/PermissionModuleResolver.cs so route
 * guards and role editors resolve the same module names as the API.
 */
export const PERMISSION_MODULES = {
  Categories: 'Categories',
  SubCategories: 'SubCategories',
  Brands: 'Brands',
  Products: 'Products',
  Units: 'Units',
  Menu: 'Menu',
  Orders: 'Orders',
  Inventory: 'Inventory',
  Reports: 'Reports',
  PosBilling: 'POS Billing',
  Users: 'Users',
  Roles: 'Roles',
  Branches: 'Branches',
  Businesses: 'Businesses',
  Warehouses: 'Warehouses',
  Suppliers: 'Suppliers',
  Purchase: 'Purchase',
  Stock: 'Stock',
  Sales: 'Sales',
  Customers: 'Customers',
  Expenses: 'Expenses',
  CashFlow: 'Cash Flow',
  PartyLedger: 'Party Ledger',
  Dashboard: 'Dashboard',
  Variants: 'Variants',
  Barcodes: 'Barcodes',
  Sizes: 'Sizes',
  Colors: 'Colors',
  ExpenseCategories: 'Expense Categories',
  Countries: 'Countries',
  Cities: 'Cities',
  StockTransfer: 'Stock Transfer',
  SalesReports: 'Sales Reports',
  ProductWiseSalesReport: 'Product Wise Sales Report',
  PurchaseReports: 'Purchase Reports',
  StockReports: 'Stock Reports',
  CustomerOutstandingReport: 'Customer Outstanding Report',
  SupplierPayableReport: 'Supplier Payable Report',
  ProfitLossReport: 'Profit & Loss Report',
  TrialBalanceReport: 'Trial Balance Report',
  AccountLedger: 'Account Ledger',
  CustomerReceivableAgingReport: 'Customer Receivable Aging',
  SupplierPayableAgingReport: 'Supplier Payable Aging',
  SystemSettings: 'System Settings',
  CodeSequences: 'Code Sequences',
  Taxes: 'Taxes',
  Discounts: 'Discounts',
} as const

const ALL_MODULES: readonly string[] = Object.values(PERMISSION_MODULES)

const MODULE_ALIASES: Record<string, string> = {
  CashFlow: PERMISSION_MODULES.CashFlow,
  'CashFlow.Ledger': PERMISSION_MODULES.CashFlow,
  'CashFlow.Summary': PERMISSION_MODULES.CashFlow,
  'StockReports.ByUnit': PERMISSION_MODULES.StockReports,
  'CashFlow.Record': PERMISSION_MODULES.CashFlow,
  PartyLedger: PERMISSION_MODULES.PartyLedger,
  'PartyLedger.PaySupplier': PERMISSION_MODULES.PartyLedger,
  'PartyLedger.CustomerLedger': PERMISSION_MODULES.PartyLedger,
  'PartyLedger.SupplierLedger': PERMISSION_MODULES.PartyLedger,
  POS: PERMISSION_MODULES.PosBilling,
  PosBilling: PERMISSION_MODULES.PosBilling,
  Orders: PERMISSION_MODULES.Orders,
  Sales: PERMISSION_MODULES.Sales,
  Purchases: PERMISSION_MODULES.Purchase,
  Invoices: PERMISSION_MODULES.Sales,
  'User Roles': PERMISSION_MODULES.Roles,
  expensecategories: PERMISSION_MODULES.ExpenseCategories,
  codeseq: PERMISSION_MODULES.CodeSequences,
  settings: PERMISSION_MODULES.SystemSettings,
  'Sub Categories': PERMISSION_MODULES.SubCategories,
}

const canonicalLookup = new Map(
  ALL_MODULES.map((module) => [module.trim().toLowerCase(), module]),
)

export const normalizeModuleName = (moduleName: string): string => {
  const trimmed = moduleName.trim()
  if (!trimmed) {
    return ''
  }

  const alias = MODULE_ALIASES[trimmed] ?? MODULE_ALIASES[trimmed.toLowerCase()]
  if (alias) {
    return alias
  }

  const canonical = canonicalLookup.get(trimmed.toLowerCase())
  if (canonical) {
    return canonical
  }

  const prefix = trimmed.split('.')[0]
  const prefixAlias = MODULE_ALIASES[prefix] ?? MODULE_ALIASES[prefix.toLowerCase()]
  if (prefixAlias) {
    return prefixAlias
  }

  return trimmed
}

export const modulePermissionMatches = (storedName: string, requestedName: string): boolean => {
  if (!storedName.trim() || !requestedName.trim()) {
    return false
  }

  if (storedName.trim().toLowerCase() === requestedName.trim().toLowerCase()) {
    return true
  }

  return (
    normalizeModuleName(storedName).toLowerCase() ===
    normalizeModuleName(requestedName).toLowerCase()
  )
}
