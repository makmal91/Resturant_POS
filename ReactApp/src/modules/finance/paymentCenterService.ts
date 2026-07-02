import apiClient from '../../services/api';
import type { PartyPaymentDetail } from '../ledger/partyLedgerService';

export type PaymentModule = 'Sale' | 'Purchase';

export interface PaymentListItem {
  id: number;
  module: PaymentModule;
  invoiceId?: number;
  invoiceNo?: string;
  customerId?: number;
  customerName?: string;
  supplierId?: number;
  supplierName?: string;
  paymentType: string;
  category: string;
  amount: number;
  paymentDate: string;
  referenceNo: string;
  notes: string;
  isReversed: boolean;
  hasAllocations: boolean;
}

export interface PaymentListPage {
  payments: PaymentListItem[];
  totalRecords: number;
  totalPages: number;
  currentPage: number;
  pageSize: number;
}

const branchHeader = (branchId: number) => ({ headers: { 'X-Branch-Id': String(branchId) } });

const normalizePayment = (row: Record<string, unknown>): PaymentListItem => ({
  id: Number(row.id ?? row.Id ?? 0),
  module: String(row.module ?? row.Module ?? '') as PaymentModule,
  invoiceId: row.invoiceId != null || row.InvoiceId != null ? Number(row.invoiceId ?? row.InvoiceId) : undefined,
  invoiceNo: row.invoiceNo != null || row.InvoiceNo != null ? String(row.invoiceNo ?? row.InvoiceNo) : undefined,
  customerId: row.customerId != null || row.CustomerId != null ? Number(row.customerId ?? row.CustomerId) : undefined,
  customerName: row.customerName != null || row.CustomerName != null ? String(row.customerName ?? row.CustomerName) : undefined,
  supplierId: row.supplierId != null || row.SupplierId != null ? Number(row.supplierId ?? row.SupplierId) : undefined,
  supplierName: row.supplierName != null || row.SupplierName != null ? String(row.supplierName ?? row.SupplierName) : undefined,
  paymentType: String(row.paymentType ?? row.PaymentType ?? ''),
  category: String(row.category ?? row.Category ?? 'AgainstInvoice'),
  amount: Number(row.amount ?? row.Amount ?? 0),
  paymentDate: String(row.paymentDate ?? row.PaymentDate ?? ''),
  referenceNo: String(row.referenceNo ?? row.ReferenceNo ?? ''),
  notes: String(row.notes ?? row.Notes ?? ''),
  isReversed: Boolean(row.isReversed ?? row.IsReversed ?? false),
  hasAllocations: Boolean(row.hasAllocations ?? row.HasAllocations ?? false),
});

export const paymentCenterService = {
  list: (
    branchId: number,
    module: PaymentModule,
    page = 1,
    pageSize = 25,
    filters?: {
      supplierId?: number;
      customerId?: number;
      fromDate?: string;
      toDate?: string;
      includeReversed?: boolean;
    },
  ) =>
    apiClient
      .get('/payments', {
        params: {
          branchId,
          module: module === 'Purchase' ? 2 : 1,
          page,
          pageSize,
          ...(filters?.supplierId ? { supplierId: filters.supplierId } : {}),
          ...(filters?.customerId ? { customerId: filters.customerId } : {}),
          ...(filters?.fromDate ? { fromDate: filters.fromDate } : {}),
          ...(filters?.toDate ? { toDate: filters.toDate } : {}),
          ...(filters?.includeReversed ? { includeReversed: true } : {}),
        },
        ...branchHeader(branchId),
      })
      .then((res) => {
        const data = res.data as Record<string, unknown>;
        const rows = Array.isArray(data.payments ?? data.Payments) ? (data.payments ?? data.Payments) as unknown[] : [];
        return {
          payments: rows.map((row) => normalizePayment(row as Record<string, unknown>)),
          totalRecords: Number(data.totalRecords ?? data.TotalRecords ?? 0),
          totalPages: Number(data.totalPages ?? data.TotalPages ?? 0),
          currentPage: Number(data.currentPage ?? data.CurrentPage ?? page),
          pageSize: Number(data.pageSize ?? data.PageSize ?? pageSize),
        } satisfies PaymentListPage;
      }),

  getPayment: (branchId: number, paymentId: number) =>
    apiClient
      .get(`/payments/${paymentId}`, { params: { branchId }, ...branchHeader(branchId) })
      .then((res) => res.data as PartyPaymentDetail),

  reverse: (branchId: number, paymentId: number, reason?: string) =>
    apiClient.post(`/payments/${paymentId}/reverse`, { reason }, {
      params: { branchId },
      ...branchHeader(branchId),
    }),
};
