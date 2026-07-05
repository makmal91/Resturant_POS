import apiClient from '../../services/api';

// ─── Types ────────────────────────────────────────────────────────────────────

export type CashFlowTransactionType =
  | 'Sale' | 'Expense' | 'CashIn' | 'CashOut' | 'BankTransfer' | 'OpeningBalance' | 'OpeningStockVoucher' | 'ClosingBalance' | 'Reversal';

export type CashFlowPaymentMethod = 'Cash' | 'Bank' | 'Wallet';

export interface CashFlowTransactionDto {
  id: number;
  branchId: number;
  branchName: string;
  transactionType: CashFlowTransactionType;
  paymentMethod: CashFlowPaymentMethod;
  amount: number;
  debit: number;
  credit: number;
  displayAmount: number;
  isInflow: boolean;
  referenceNo: string | null;
  description: string | null;
  accountName: string;
  transactionDate: string;
  runningBalance: number;
  createdBy: number | null;
  createdAt: string;
}

export interface CashRegisterDto {
  id: number;
  branchId: number;
  branchName: string;
  registerDate: string;
  openingCash: number;
  closingCash: number | null;
  expectedCash: number | null;
  actualCash: number | null;
  difference: number | null;
  isClosed: boolean;
  notes: string | null;
}

export interface DailyCashSummaryDto {
  branchId: number;
  branchName: string;
  date: string;
  openingCash: number;
  totalCashSales: number;
  totalCardSales: number;
  totalExpensesCash: number;
  totalCashIn: number;
  totalCashOut: number;
  totalBankTransfers: number;
  expectedClosingCash: number;
  actualClosingCash: number | null;
  difference: number | null;
  isRegistered: boolean;
  isClosed: boolean;
}

export interface MonthlyCashSummaryDto {
  branchId: number;
  branchName: string;
  year: number;
  month: number;
  totalCashIn: number;
  totalCashOut: number;
  totalSales: number;
  totalExpenses: number;
  netCashFlow: number;
  dailyTrend: { date: string; cashIn: number; cashOut: number; net: number }[];
}

export interface BranchCashSummaryDto {
  branchId: number;
  branchName: string;
  todayCashIn: number;
  todayCashOut: number;
  netPosition: number;
  openingCash: number;
  isOpenForDay: boolean;
  status: 'Open' | 'Closed' | 'NotStarted';
}

export interface LedgerResponse {
  accountName: string;
  transactions: CashFlowTransactionDto[];
  totalRecords: number;
  totalPages: number;
  currentPage: number;
  pageSize: number;
  totalIn: number;
  totalOut: number;
  netTotal: number;
  periodOpeningBalance: number;
  totalDebit: number;
  totalCredit: number;
  closingBalance: number;
}

export interface RegisterSessionDto {
  id: number;
  posRegisterId: number;
  registerName: string;
  branchId: number;
  sessionDate: string;
  openingBalance: number;
  isOpeningOverride: boolean;
  openingOverrideReason: string | null;
  openedBy: number | null;
  openedByName: string | null;
  openedAt: string;
  expectedClosing: number | null;
  physicalCash: number | null;
  difference: number | null;
  totalCashSales: number;
  totalExpensesCash: number;
  totalCashIn: number;
  totalCashOut: number;
  totalAdjustments: number;
  isClosed: boolean;
  closedBy: number | null;
  closedByName: string | null;
  closedAt: string | null;
  closeMismatchReason: string | null;
  notes: string | null;
}

export interface PosRegisterDto {
  id: number;
  branchId: number;
  name: string;
  linkedCashAccountId: number;
  linkedCashAccountName: string;
  linkedCashAccountCode: string;
  isActive: boolean;
  isDefault: boolean;
  hasOpenSession: boolean;
  currentBalance: number | null;
}

export interface RegisterOpeningHintDto {
  posRegisterId: number;
  registerName: string;
  isFirstTime: boolean;
  lastClosingBalance: number | null;
  lastClosedAt: string | null;
  suggestedOpeningBalance: number;
  hasOpenSessionToday: boolean;
  openSession: RegisterSessionDto | null;
}

export interface RegisterClosePreviewDto {
  posRegisterId: number;
  registerName: string;
  sessionId: number;
  openingBalance: number;
  totalCashSales: number;
  totalExpensesCash: number;
  totalCashIn: number;
  totalCashOut: number;
  totalAdjustments: number;
  expectedCash: number;
  isClosed: boolean;
}

export interface RegisterDashboardDto {
  registers: PosRegisterDto[];
  openSessions: RegisterSessionDto[];
}

export interface RegisterHistoryPageDto {
  items: RegisterSessionDto[];
  totalRecords: number;
  totalPages: number;
}

// ─── Helpers ──────────────────────────────────────────────────────────────────

const bh = (branchId: number) => ({ headers: { 'X-Branch-Id': String(branchId) } });

// ─── Service ──────────────────────────────────────────────────────────────────

export const cashFlowService = {
  // Cash Register
  openCash: (branchId: number, amount: number, notes?: string, date?: string) =>
    apiClient.post<CashRegisterDto>(
      '/cashflow/opening',
      { branchId, amount, notes, date },
      bh(branchId),
    ),

  closeCash: (branchId: number, actualCash: number, notes?: string, date?: string) =>
    apiClient.post<CashRegisterDto>(
      '/cashflow/closing',
      { branchId, actualCash, notes, date },
      bh(branchId),
    ),

  getTodayRegister: (branchId: number) =>
    apiClient.get<CashRegisterDto | null>('/cashflow/register/today', {
      params: { branchId },
      ...bh(branchId),
    }),

  // Transactions
  recordTransaction: (
    branchId: number,
    transactionType: CashFlowTransactionType,
    amount: number,
    paymentMethod: CashFlowPaymentMethod,
    description?: string,
    referenceNo?: string,
  ) =>
    apiClient.post<CashFlowTransactionDto>(
      '/cashflow/transaction',
      { branchId, transactionType, amount, paymentMethod, description, referenceNo },
      bh(branchId),
    ),

  getLedger: (
    branchId: number,
    params: {
      fromDate?: string;
      toDate?: string;
      transactionType?: CashFlowTransactionType | null;
      paymentMethod?: CashFlowPaymentMethod | null;
      page?: number;
      pageSize?: number;
    } = {},
  ) =>
    apiClient.get<LedgerResponse>('/cashflow/ledger', {
      params: { branchId, ...params },
      ...bh(branchId),
    }),

  // Summaries
  getDailySummary: (branchId: number, date?: string) =>
    apiClient.get<DailyCashSummaryDto>('/cashflow/summary/daily', {
      params: { branchId, ...(date ? { date } : {}) },
      ...bh(branchId),
    }),

  getMonthlySummary: (branchId: number, year?: number, month?: number) =>
    apiClient.get<MonthlyCashSummaryDto>('/cashflow/summary/monthly', {
      params: { branchId, ...(year ? { year } : {}), ...(month ? { month } : {}) },
      ...bh(branchId),
    }),

  getBranchSummary: (date?: string) =>
    apiClient.get<BranchCashSummaryDto[]>('/cashflow/summary/branch', {
      params: date ? { date } : {},
    }),

  // Multi-register POS drawers
  getRegisterDashboard: (branchId: number) =>
    apiClient.get<RegisterDashboardDto>('/cashflow/registers/dashboard', {
      params: { branchId },
      ...bh(branchId),
    }),

  getRegisters: (branchId: number) =>
    apiClient.get<PosRegisterDto[]>('/cashflow/registers', {
      params: { branchId },
      ...bh(branchId),
    }),

  getOpeningHint: (branchId: number, registerId: number) =>
    apiClient.get<RegisterOpeningHintDto>(`/cashflow/registers/${registerId}/opening-hint`, {
      params: { branchId },
      ...bh(branchId),
    }),

  openRegister: (
    branchId: number,
    posRegisterId: number,
    openingBalance: number,
    overrideOpening = false,
    overrideReason?: string,
  ) =>
    apiClient.post<RegisterSessionDto>(
      '/cashflow/registers/open',
      { posRegisterId, openingBalance, overrideOpening, overrideReason },
      bh(branchId),
    ),

  getClosePreview: (branchId: number, registerId: number) =>
    apiClient.get<RegisterClosePreviewDto>(`/cashflow/registers/${registerId}/close-preview`, {
      params: { branchId },
      ...bh(branchId),
    }),

  closeRegister: (
    branchId: number,
    posRegisterId: number,
    physicalCash: number,
    mismatchReason?: string,
    notes?: string,
  ) =>
    apiClient.post<RegisterSessionDto>(
      '/cashflow/registers/close',
      { posRegisterId, physicalCash, mismatchReason, notes },
      bh(branchId),
    ),

  getRegisterHistory: (
    branchId: number,
    params: { posRegisterId?: number; from?: string; to?: string; page?: number; pageSize?: number } = {},
  ) =>
    apiClient.get<RegisterHistoryPageDto>('/cashflow/registers/history', {
      params: { branchId, ...params },
      ...bh(branchId),
    }),
};
