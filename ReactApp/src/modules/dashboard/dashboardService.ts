import apiClient from '../../services/api';

// ─── Types ────────────────────────────────────────────────────────────────────

export interface DashboardKpiDto {
  totalBranches: number;
  totalUsers: number;
  todaySales: number;
  todayInvoices: number;
  monthlySales: number;
  monthlyInvoices: number;
  grossProfit: number;
  netProfit: number;
  stockValue: number;
  lowStockCount: number;
  outOfStockCount: number;
}

export interface BranchAnalyticsDto {
  branchId: number;
  branchName: string;
  totalSales: number;
  invoiceCount: number;
  grossProfit: number;
  netProfit: number;
}

export interface StockAlertItemDto {
  productId: number;
  productName: string;
  productCode: string;
  variantId: number | null;
  variantName: string | null;
  warehouseId: number;
  warehouseName: string;
  quantity: number;
  stockValue: number;
}

export interface WarehouseStockDto {
  warehouseId: number;
  warehouseName: string;
  totalQuantity: number;
  totalValue: number;
  itemCount: number;
}

export interface DashboardStockDto {
  totalProducts: number;
  totalVariants: number;
  totalQuantity: number;
  totalStockValue: number;
  lowStockCount: number;
  outOfStockCount: number;
  lowStockItems: StockAlertItemDto[];
  outOfStockItems: StockAlertItemDto[];
  warehouseDistribution: WarehouseStockDto[];
}

export interface DailyCashFlowDto {
  date: string;
  cashIn: number;
  cashOut: number;
  netFlow: number;
}

export interface DashboardFinancialDto {
  totalSales: number;
  totalPurchases: number;
  grossProfit: number;
  totalExpenses: number;
  netProfit: number;
  totalReceivables: number;
  totalPayables: number;
  dailyCashFlow: DailyCashFlowDto[];
}

export interface RecentUserDto {
  userId: number;
  fullName: string;
  username: string;
  roleName: string;
  isActive: boolean;
  lastActivity: string | null;
}

export interface SalesByUserDto {
  cashierName: string;
  invoiceCount: number;
  totalSales: number;
}

export interface ActivityLogDto {
  type: string;
  reference: string;
  amount: number;
  branchName: string;
  timestamp: string;
  status: string;
}

export interface DashboardUserActivityDto {
  recentUsers: RecentUserDto[];
  salesByUsers: SalesByUserDto[];
  activityLogs: ActivityLogDto[];
}

export interface SalesPersonSummaryDto {
  userId: number;
  fullName: string;
  username: string;
  branchId: number;
  branchName: string;
  generatedAt: string;
  kpis: SalesPersonKpiDto;
  payment: SalesPersonPaymentDto;
  recentSales: RecentSaleDto[];
  salesTrend: SalesTrendPointDto[];
  topProducts: TopProductDto[];
}

export interface SalesPersonKpiDto {
  todaySales: number;
  todayInvoices: number;
  monthlySales: number;
  monthlyInvoices: number;
  averageSale: number;
  todayCash: number;
  todayCard: number;
  pendingPaymentCount: number;
  paidCount: number;
}

export interface SalesPersonPaymentDto {
  totalCash: number;
  totalCard: number;
  totalMixed: number;
  cashInvoices: number;
  cardInvoices: number;
  mixedInvoices: number;
}

export interface RecentSaleDto {
  id: number;
  invoiceNo: string;
  branchName: string;
  cashierName: string;
  grandTotal: number;
  paidAmount: number;
  paymentStatus: string;
  status: string;
  saleDate: string;
}

export interface RecentPurchaseDto {
  id: number;
  invoiceNo: string;
  branchName: string;
  supplierName: string;
  totalAmount: number;
  status: string;
  purchaseDate: string;
}

export interface DashboardRecentTransactionsDto {
  recentSales: RecentSaleDto[];
  recentPurchases: RecentPurchaseDto[];
  returnCount: number;
  pendingPaymentCount: number;
  paidCount: number;
}

export interface SalesTrendPointDto {
  date: string;
  totalSales: number;
  invoiceCount: number;
}

export interface ProfitTrendPointDto {
  date: string;
  revenue: number;
  cost: number;
  grossProfit: number;
}

export interface TopProductDto {
  productId: number;
  productName: string;
  productCode: string;
  totalQuantity: number;
  totalAmount: number;
}

export interface CategoryPerformanceDto {
  categoryId: number;
  categoryName: string;
  totalSales: number;
  totalQuantity: number;
}

export interface DashboardChartsDto {
  salesTrend: SalesTrendPointDto[];
  profitTrend: ProfitTrendPointDto[];
  topProducts: TopProductDto[];
  categoryPerformance: CategoryPerformanceDto[];
}

export interface DashboardOverviewDto {
  branchId: number;
  branchName: string;
  generatedAt: string;
  kpis: DashboardKpiDto;
  branchAnalytics: BranchAnalyticsDto[];
  stock: DashboardStockDto;
  financial: DashboardFinancialDto;
  userActivity: DashboardUserActivityDto;
  recentTransactions: DashboardRecentTransactionsDto;
  charts: DashboardChartsDto;
}

// ─── Service ──────────────────────────────────────────────────────────────────

const bh = (branchId: number) => ({ headers: { 'X-Branch-Id': String(branchId) } });

export const dashboardService = {
  getOverview: (branchId: number) =>
    apiClient.get<DashboardOverviewDto>('/dashboard/overview', {
      params: { branchId },
      ...bh(branchId),
    }),

  getMySalesSummary: (branchId: number) =>
    apiClient.get<SalesPersonSummaryDto>('/dashboard/my-sales-summary', {
      params: { branchId },
      ...bh(branchId),
    }),
};
