import apiClient from '../../services/api';

// ─── Sales Report Types ───────────────────────────────────────────────────────

export interface SalesDailyTrend {
  date: string;
  invoiceCount: number;
  totalSales: number;
  cashSales: number;
  cardSales: number;
}

export interface SalesSummaryDto {
  branchId: number;
  branchName: string;
  fromDate: string;
  toDate: string;
  totalInvoices: number;
  totalSales: number;
  totalDiscount: number;
  totalTax: number;
  totalCash: number;
  totalCard: number;
  totalPaid: number;
  averageSale: number;
  dailyTrend: SalesDailyTrend[];
}

export interface SalesByProductRow {
  productId: number;
  productName: string;
  productCode: string;
  totalQuantity: number;
  totalAmount: number;
  invoiceCount: number;
}

export interface SalesByProductResponse {
  products: SalesByProductRow[];
  totalRecords: number;
  totalPages: number;
  currentPage: number;
  pageSize: number;
  fromDate: string;
  toDate: string;
}

// ─── Stock Report Types ───────────────────────────────────────────────────────

export interface StockSummaryItem {
  productId: number;
  productName: string;
  closingBalance: number;
  enableLowStockAlert?: boolean;
  lowStockAlertLevel?: number | null;
}

export interface StockSummaryResponse {
  items: StockSummaryItem[];
  totalRecords: number;
  totalPages: number;
  currentPage: number;
  pageSize: number;
  fromDate: string;
  toDate: string;
  totalClosingBalance: number;
}

// ─── Helpers ──────────────────────────────────────────────────────────────────

const bh = (branchId: number) => ({ headers: { 'X-Branch-Id': String(branchId) } });

export interface ReportDateRange {
  fromDate?: string;
  toDate?: string;
}

// ─── Service ──────────────────────────────────────────────────────────────────

export const reportService = {
  getSalesSummary: (branchId: number, params: ReportDateRange = {}) =>
    apiClient.get<SalesSummaryDto>('/reports/sales-summary', {
      params: { branchId, ...params },
      ...bh(branchId),
    }),

  getSalesByProduct: (
    branchId: number,
    params: ReportDateRange & {
      page?: number;
      pageSize?: number;
      search?: string;
      sortBy?: string;
      sortDirection?: 'asc' | 'desc';
    } = {},
  ) =>
    apiClient.get<SalesByProductResponse>('/reports/sales-by-product', {
      params: { branchId, ...params },
      ...bh(branchId),
    }),

  getStockSummary: (
    branchId: number,
    params: ReportDateRange & {
      warehouseId?: number;
      page?: number;
      pageSize?: number;
      search?: string;
      sortBy?: string;
      sortDirection?: 'asc' | 'desc';
    } = {},
  ) =>
    apiClient.get<StockSummaryResponse>('/reports/stock-summary', {
      params: { branchId, ...params },
      ...bh(branchId),
    }),
};
