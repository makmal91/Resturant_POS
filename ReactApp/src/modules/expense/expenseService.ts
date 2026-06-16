import apiClient from '../../services/api';

export type ExpenseStatus = 'Pending' | 'Approved' | 'Rejected';
export type ExpensePaymentMethod = 'Cash' | 'Bank' | 'Wallet';

export interface ExpenseDto {
  id: number;
  branchId: number;
  branchName: string;
  categoryName: string;
  description: string;
  amount: number;
  paymentMethod: ExpensePaymentMethod;
  expenseDate: string;
  status: ExpenseStatus;
  referenceNo: string | null;
  notes: string | null;
  createdBy: number | null;
  createdAt: string;
}

export interface CreateExpenseDto {
  branchId: number;
  categoryName: string;
  description: string;
  amount: number;
  paymentMethod: ExpensePaymentMethod;
  expenseDate?: string;
  referenceNo?: string;
  notes?: string;
}

export interface ExpenseSummaryDto {
  totalExpenses: number;
  totalCash: number;
  totalBank: number;
  count: number;
}

export interface ExpenseListResponse {
  expenses: ExpenseDto[];
  totalRecords: number;
  totalPages: number;
  currentPage: number;
  pageSize: number;
  summary: ExpenseSummaryDto;
}

const bh = (branchId: number) => ({ headers: { 'X-Branch-Id': String(branchId) } });

export const expenseService = {
  getAll: (
    branchId: number,
    params: {
      fromDate?: string;
      toDate?: string;
      paymentMethod?: ExpensePaymentMethod | null;
      page?: number;
      pageSize?: number;
    } = {},
  ) =>
    apiClient.get<ExpenseListResponse>('/expenses', {
      params: { branchId, ...params },
      ...bh(branchId),
    }),

  create: (payload: CreateExpenseDto) =>
    apiClient.post<ExpenseDto>('/expenses', payload, bh(payload.branchId)),

  update: (id: number, payload: CreateExpenseDto) =>
    apiClient.put<ExpenseDto>(`/expenses/${id}`, payload, bh(payload.branchId)),

  delete: (id: number, branchId: number) =>
    apiClient.delete(`/expenses/${id}`, bh(branchId)),
};
