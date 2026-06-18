import apiClient from '../../services/api';

// ─── Shared paged response ────────────────────────────────────────────────────

export interface ReportPagedResponse<T> {
  data: T[];
  totalRecords: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface ReportQueryParams {
  fromDate?: string;
  toDate?: string;
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  sortColumn?: string;
  sortDirection?: 'asc' | 'desc';
  customerId?: number;
  supplierId?: number;
  agingBucket?: string;
}

// ─── Report row types ─────────────────────────────────────────────────────────

export interface SalesReportRow {
  id: number;
  invoiceNo: string;
  saleDate: string;
  customerId?: number | null;
  customerName: string;
  subTotal: number;
  discountAmount: number;
  taxAmount: number;
  grandTotal: number;
  paidAmount: number;
  balanceDue: number;
  paymentMethod: string;
  isCreditSale: boolean;
  cashAmount: number;
  cardAmount: number;
  status: string;
  cashierName?: string | null;
}

export interface PurchaseReportRow {
  id: number;
  invoiceNo: string;
  purchaseDate: string;
  supplierId: number;
  supplierName: string;
  totalAmount: number;
  paidAmount: number;
  balanceDue: number;
  status: string;
  isCreditPurchase: boolean;
}

export interface CustomerOutstandingRow {
  customerId: number;
  customerCode: string;
  customerName: string;
  phone?: string | null;
  openingBalance: number;
  outstandingInvoices: number;
  invoiceOutstanding: number;
  outstandingAmount: number;
  lastSaleDate?: string | null;
}

export interface SupplierPayableRow {
  supplierId: number;
  supplierCode: string;
  supplierName: string;
  phone: string;
  outstandingInvoices: number;
  invoicePayable: number;
  payableAmount: number;
  lastPurchaseDate?: string | null;
}

export interface ProfitLossRow {
  date: string;
  revenue: number;
  discounts: number;
  tax: number;
  costOfGoodsSold: number;
  grossProfit: number;
  expenses: number;
  netProfit: number;
  salesCount: number;
}

export interface ReceivableAgingRow {
  invoiceId: number;
  customerId: number;
  customerName: string;
  invoiceNo: string;
  invoiceDate: string;
  totalAmount: number;
  paidAmount: number;
  outstanding: number;
  daysOverdue: number;
  agingBucket: string;
}

export interface PayableAgingRow {
  invoiceId: number;
  supplierId: number;
  supplierName: string;
  invoiceNo: string;
  invoiceDate: string;
  totalAmount: number;
  paidAmount: number;
  outstanding: number;
  daysOverdue: number;
  agingBucket: string;
}

export interface AgingReportSummary {
  totalOutstanding: number;
  bucket0To30: number;
  bucket31To60: number;
  bucket61To90: number;
  bucket90Plus: number;
  asOfDate: string;
}

export interface AgingReportPagedResponse<T> extends ReportPagedResponse<T> {
  summary: AgingReportSummary;
}

// ─── Legacy report types (stock / summary) ────────────────────────────────────

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

export interface ReportDateRange {
  fromDate?: string;
  toDate?: string;
}

const bh = (branchId: number) => ({ headers: { 'X-Branch-Id': String(branchId) } });

const reportParams = (branchId: number, params: ReportQueryParams = {}) => ({
  branchId,
  ...params,
});

// ─── Service ──────────────────────────────────────────────────────────────────

export const reportService = {
  getSalesReport: (branchId: number, params: ReportQueryParams = {}) =>
    apiClient.get<ReportPagedResponse<SalesReportRow>>('/reports/sales', {
      params: reportParams(branchId, params),
      ...bh(branchId),
    }),

  getPurchaseReport: (branchId: number, params: ReportQueryParams = {}) =>
    apiClient.get<ReportPagedResponse<PurchaseReportRow>>('/reports/purchases', {
      params: reportParams(branchId, params),
      ...bh(branchId),
    }),

  getCustomerOutstandingReport: (branchId: number, params: ReportQueryParams = {}) =>
    apiClient.get<ReportPagedResponse<CustomerOutstandingRow>>('/reports/customer-outstanding', {
      params: reportParams(branchId, params),
      ...bh(branchId),
    }),

  getSupplierPayableReport: (branchId: number, params: ReportQueryParams = {}) =>
    apiClient.get<ReportPagedResponse<SupplierPayableRow>>('/reports/supplier-payable', {
      params: reportParams(branchId, params),
      ...bh(branchId),
    }),

  getProfitLossReport: (branchId: number, params: ReportQueryParams = {}) =>
    apiClient.get<ReportPagedResponse<ProfitLossRow>>('/reports/profit-loss', {
      params: reportParams(branchId, params),
      ...bh(branchId),
    }),

  getReceivableAgingReport: (branchId: number, params: ReportQueryParams = {}) =>
    apiClient.get<AgingReportPagedResponse<ReceivableAgingRow>>('/reports/receivable-aging', {
      params: reportParams(branchId, params),
      ...bh(branchId),
    }),

  getPayableAgingReport: (branchId: number, params: ReportQueryParams = {}) =>
    apiClient.get<AgingReportPagedResponse<PayableAgingRow>>('/reports/payable-aging', {
      params: reportParams(branchId, params),
      ...bh(branchId),
    }),

  getSalesSummary: (branchId: number, params: ReportDateRange = {}) =>
    apiClient.get<SalesSummaryDto>('/reports/sales-summary', {
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
