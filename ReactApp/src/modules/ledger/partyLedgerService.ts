import apiClient from '../../services/api';

export interface PartyLedgerInvoiceAllocation {
  invoiceId: number;
  invoiceNo: string;
  appliedAmount: number;
}

export interface PartyLedgerEntry {
  id: number;
  date: string;
  type: string;
  description: string;
  debit: number;
  credit: number;
  runningBalance: number;
  referenceId: number;
  paymentId?: number;
  canReverse: boolean;
  hasInvoiceBreakdown: boolean;
  isActive?: boolean;
  isSuperseded?: boolean;
  isReversal?: boolean;
  isReplacement?: boolean;
  originalGroupId?: string;
  groupId?: string;
  affectsPayableBalance?: boolean;
  invoiceAllocations: PartyLedgerInvoiceAllocation[];
}

export interface PartyPaymentAllocation {
  id: number;
  invoiceId: number;
  invoiceNo?: string;
  appliedAmount: number;
}

export interface PartyPaymentDetail {
  id: number;
  amount: number;
  paymentDate: string;
  paymentType: string;
  category?: string;
  referenceNo: string;
  notes: string;
  isReversed: boolean;
  hasAllocations: boolean;
  customerId?: number;
  supplierId?: number;
  module?: 'Sale' | 'Purchase';
  allocations: PartyPaymentAllocation[];
}

export interface PartyLedgerPage {
  partyId: number;
  partyName: string;
  currentBalance: number;
  periodClosingBalance: number;
  effectiveClosingBalance?: number;
  auditView?: boolean;
  entries: PartyLedgerEntry[];
  totalRecords: number;
  totalPages: number;
  currentPage: number;
  totalDebit: number;
  totalCredit: number;
}

export interface PartyBalance {
  partyId: number;
  partyName: string;
  balance: number;
}

export interface ReceivePaymentPayload {
  customerId: number;
  saleInvoiceId?: number;
  paymentType?: 'Cash' | 'Bank' | 'Online';
  amount: number;
  paymentDate?: string;
  referenceNo?: string;
  notes?: string;
  autoAllocate?: boolean;
  allocations?: { invoiceId: number; appliedAmount: number }[];
  branchId: number;
}

export interface PaySupplierPayload {
  supplierId: number;
  purchaseId?: number;
  paymentType?: 'Cash' | 'Bank' | 'Online';
  category?: 'AgainstInvoice' | 'Advance' | 'Adjustment';
  amount: number;
  paymentDate?: string;
  referenceNo?: string;
  notes?: string;
  autoAllocate?: boolean;
  allocations?: { invoiceId: number; appliedAmount: number }[];
  branchId: number;
}

export interface UpdatePaymentPayload {
  paymentType?: 'Cash' | 'Bank' | 'Online';
  category?: 'AgainstInvoice' | 'Advance' | 'Adjustment';
  amount: number;
  paymentDate?: string;
  referenceNo?: string;
  notes?: string;
  autoAllocate?: boolean;
  allocations?: { invoiceId: number; appliedAmount: number }[];
  branchId: number;
}

export interface InvoiceBalanceInfo {
  invoiceId: number;
  invoiceNo: string;
  invoiceTotal: number;
  paidAmount: number;
  balanceDue: number;
}

export interface OutstandingInvoiceOption {
  invoiceId: number;
  invoiceNo: string;
  invoiceDate: string;
  invoiceTotal: number;
  paidAmount: number;
  balanceDue: number;
}

const branchHeader = (branchId: number) => ({ headers: { 'X-Branch-Id': String(branchId) } });

const normalizeLedgerPage = (data: Record<string, unknown>): PartyLedgerPage => ({
  partyId: Number(data.partyId ?? data.PartyId ?? 0),
  partyName: String(data.partyName ?? data.PartyName ?? ''),
  currentBalance: Number(data.currentBalance ?? data.CurrentBalance ?? 0),
  periodClosingBalance: Number(
    data.periodClosingBalance ?? data.PeriodClosingBalance ?? data.currentBalance ?? data.CurrentBalance ?? 0
  ),
  effectiveClosingBalance: Number(
    data.effectiveClosingBalance ?? data.EffectiveClosingBalance ?? data.periodClosingBalance ?? data.PeriodClosingBalance ?? 0
  ),
  auditView: Boolean(data.auditView ?? data.AuditView ?? false),
  entries: (Array.isArray(data.entries ?? data.Entries) ? (data.entries ?? data.Entries) : []).map(
    (row: Record<string, unknown>) => ({
      id: Number(row.id ?? row.Id ?? 0),
      date: String(row.date ?? row.Date ?? ''),
      type: String(row.type ?? row.Type ?? ''),
      description: String(row.description ?? row.Description ?? ''),
      debit: Number(row.debit ?? row.Debit ?? 0),
      credit: Number(row.credit ?? row.Credit ?? 0),
      runningBalance: Number(row.runningBalance ?? row.RunningBalance ?? 0),
      referenceId: Number(row.referenceId ?? row.ReferenceId ?? 0),
      paymentId: row.paymentId != null || row.PaymentId != null
        ? Number(row.paymentId ?? row.PaymentId)
        : undefined,
      canReverse: Boolean(row.canReverse ?? row.CanReverse ?? false),
      hasInvoiceBreakdown: Boolean(row.hasInvoiceBreakdown ?? row.HasInvoiceBreakdown ?? false),
      isActive: Boolean(row.isActive ?? row.IsActive ?? true),
      isSuperseded: Boolean(row.isSuperseded ?? row.IsSuperseded ?? row.isEdited ?? row.IsEdited ?? false),
      isReversal: Boolean(row.isReversal ?? row.IsReversal ?? false),
      isReplacement: Boolean(row.isReplacement ?? row.IsReplacement ?? row.isUpdated ?? row.IsUpdated ?? false),
      originalGroupId: row.originalGroupId != null || row.OriginalGroupId != null
        ? String(row.originalGroupId ?? row.OriginalGroupId)
        : undefined,
      groupId: row.groupId != null || row.GroupId != null
        ? String(row.groupId ?? row.GroupId)
        : undefined,
      affectsPayableBalance: Boolean(
        row.affectsPayableBalance ?? row.AffectsPayableBalance ?? true
      ),
      invoiceAllocations: (Array.isArray(row.invoiceAllocations ?? row.InvoiceAllocations)
        ? (row.invoiceAllocations ?? row.InvoiceAllocations)
        : []
      ).map((alloc: Record<string, unknown>) => ({
        invoiceId: Number(alloc.invoiceId ?? alloc.InvoiceId ?? 0),
        invoiceNo: String(alloc.invoiceNo ?? alloc.InvoiceNo ?? ''),
        appliedAmount: Number(alloc.appliedAmount ?? alloc.AppliedAmount ?? 0),
      })),
    })
  ),
  totalRecords: Number(data.totalRecords ?? data.TotalRecords ?? 0),
  totalPages: Number(data.totalPages ?? data.TotalPages ?? 0),
  currentPage: Number(data.currentPage ?? data.CurrentPage ?? 1),
  totalDebit: Number(data.totalDebit ?? data.TotalDebit ?? 0),
  totalCredit: Number(data.totalCredit ?? data.TotalCredit ?? 0),
});

const normalizeBalance = (data: Record<string, unknown>): PartyBalance => ({
  partyId: Number(data.partyId ?? data.PartyId ?? 0),
  partyName: String(data.partyName ?? data.PartyName ?? ''),
  balance: Number(data.balance ?? data.Balance ?? 0),
});

const normalizeInvoiceBalance = (data: Record<string, unknown>): InvoiceBalanceInfo => ({
  invoiceId: Number(data.invoiceId ?? data.InvoiceId ?? 0),
  invoiceNo: String(data.invoiceNo ?? data.InvoiceNo ?? ''),
  invoiceTotal: Number(data.invoiceTotal ?? data.InvoiceTotal ?? 0),
  paidAmount: Number(data.paidAmount ?? data.PaidAmount ?? 0),
  balanceDue: Number(data.balanceDue ?? data.BalanceDue ?? 0),
});

const normalizeOutstandingInvoice = (row: Record<string, unknown>): OutstandingInvoiceOption => ({
  invoiceId: Number(row.invoiceId ?? row.InvoiceId ?? 0),
  invoiceNo: String(row.invoiceNo ?? row.InvoiceNo ?? ''),
  invoiceDate: String(row.invoiceDate ?? row.InvoiceDate ?? ''),
  invoiceTotal: Number(row.invoiceTotal ?? row.InvoiceTotal ?? 0),
  paidAmount: Number(row.paidAmount ?? row.PaidAmount ?? 0),
  balanceDue: Number(row.balanceDue ?? row.BalanceDue ?? 0),
});

export interface LedgerViewOptions {
  auditView?: boolean;
  groupByChain?: boolean;
}

export const partyLedgerService = {
  getCustomerLedger: (
    branchId: number,
    customerId: number,
    page = 1,
    pageSize = 50,
    fromDate?: string,
    toDate?: string,
    view?: LedgerViewOptions,
  ) =>
    apiClient
      .get('/ledger/customers', {
        params: {
          branchId,
          customerId,
          page,
          pageSize,
          ...(fromDate ? { fromDate } : {}),
          ...(toDate ? { toDate } : {}),
          ...(view?.auditView ? { auditView: true } : {}),
          ...(view?.groupByChain ? { groupByChain: true } : {}),
        },
        ...branchHeader(branchId),
      })
      .then((res) => ({ ...res, data: normalizeLedgerPage(res.data as Record<string, unknown>) })),

  getSupplierLedger: (
    branchId: number,
    supplierId: number,
    page = 1,
    pageSize = 50,
    fromDate?: string,
    toDate?: string,
    view?: LedgerViewOptions,
  ) =>
    apiClient
      .get('/ledger/suppliers', {
        params: {
          branchId,
          supplierId,
          page,
          pageSize,
          ...(fromDate ? { fromDate } : {}),
          ...(toDate ? { toDate } : {}),
          ...(view?.auditView ? { auditView: true } : {}),
          ...(view?.groupByChain ? { groupByChain: true } : {}),
        },
        ...branchHeader(branchId),
      })
      .then((res) => ({ ...res, data: normalizeLedgerPage(res.data as Record<string, unknown>) })),

  getCustomerBalance: (branchId: number, customerId: number) =>
    apiClient
      .get(`/ledger/customers/${customerId}/balance`, {
        params: { branchId },
        ...branchHeader(branchId),
      })
      .then((res) => ({ ...res, data: normalizeBalance(res.data as Record<string, unknown>) })),

  getSupplierBalance: (branchId: number, supplierId: number) =>
    apiClient
      .get(`/ledger/suppliers/${supplierId}/balance`, {
        params: { branchId },
        ...branchHeader(branchId),
      })
      .then((res) => ({ ...res, data: normalizeBalance(res.data as Record<string, unknown>) })),

  getSaleInvoiceBalance: (branchId: number, saleInvoiceId: number) =>
    apiClient
      .get(`/payments/sales/${saleInvoiceId}/balance`, {
        params: { branchId },
        ...branchHeader(branchId),
      })
      .then((res) => ({ ...res, data: normalizeInvoiceBalance(res.data as Record<string, unknown>) })),

  getPurchaseBalance: (branchId: number, purchaseId: number) =>
    apiClient
      .get(`/payments/purchases/${purchaseId}/balance`, {
        params: { branchId },
        ...branchHeader(branchId),
      })
      .then((res) => ({ ...res, data: normalizeInvoiceBalance(res.data as Record<string, unknown>) })),

  getCustomerOutstandingInvoices: (branchId: number, customerId: number, excludePaymentId?: number) =>
    apiClient
      .get(`/payments/customers/${customerId}/outstanding-invoices`, {
        params: {
          branchId,
          ...(excludePaymentId ? { excludePaymentId } : {}),
        },
        ...branchHeader(branchId),
      })
      .then((res) => ({
        ...res,
        data: (Array.isArray(res.data) ? res.data : []).map((row: Record<string, unknown>) =>
          normalizeOutstandingInvoice(row)
        ),
      })),

  getSupplierOutstandingInvoices: (branchId: number, supplierId: number, excludePaymentId?: number) =>
    apiClient
      .get(`/payments/suppliers/${supplierId}/outstanding-invoices`, {
        params: {
          branchId,
          ...(excludePaymentId ? { excludePaymentId } : {}),
        },
        ...branchHeader(branchId),
      })
      .then((res) => ({
        ...res,
        data: (Array.isArray(res.data) ? res.data : []).map((row: Record<string, unknown>) =>
          normalizeOutstandingInvoice(row)
        ),
      })),

  receivePayment: (payload: ReceivePaymentPayload) =>
    apiClient.post('/ledger/customers/payment', payload, branchHeader(payload.branchId)),

  paySupplier: (payload: PaySupplierPayload) =>
    apiClient.post('/ledger/suppliers/payment', payload, branchHeader(payload.branchId)),

  reversePayment: (branchId: number, paymentId: number, reason?: string) =>
    apiClient.post(
      `/payments/${paymentId}/reverse`,
      { reason },
      { params: { branchId }, ...branchHeader(branchId) },
    ),

  updatePayment: (branchId: number, paymentId: number, payload: UpdatePaymentPayload) =>
    apiClient.put(`/payments/${paymentId}`, payload, {
      params: { branchId },
      ...branchHeader(branchId),
    }),

  getPayment: (branchId: number, paymentId: number) =>
    apiClient
      .get(`/payments/${paymentId}`, {
        params: { branchId },
        ...branchHeader(branchId),
      })
      .then((res) => {
        const data = res.data as Record<string, unknown>;
        const allocations = Array.isArray(data.allocations ?? data.Allocations)
          ? (data.allocations ?? data.Allocations as unknown[]).map((row: Record<string, unknown>) => ({
              id: Number(row.id ?? row.Id ?? 0),
              invoiceId: Number(row.invoiceId ?? row.InvoiceId ?? 0),
              invoiceNo: String(row.invoiceNo ?? row.InvoiceNo ?? ''),
              appliedAmount: Number(row.appliedAmount ?? row.AppliedAmount ?? 0),
            }))
          : [];

        return {
          ...res,
          data: {
            id: Number(data.id ?? data.Id ?? 0),
            amount: Number(data.amount ?? data.Amount ?? 0),
            paymentDate: String(data.paymentDate ?? data.PaymentDate ?? ''),
            paymentType: String(data.paymentType ?? data.PaymentType ?? ''),
            category: String(data.category ?? data.Category ?? 'AgainstInvoice'),
            referenceNo: String(data.referenceNo ?? data.ReferenceNo ?? ''),
            notes: String(data.notes ?? data.Notes ?? ''),
            isReversed: Boolean(data.isReversed ?? data.IsReversed ?? false),
            hasAllocations: Boolean(data.hasAllocations ?? data.HasAllocations ?? allocations.length > 0),
            customerId: data.customerId != null || data.CustomerId != null
              ? Number(data.customerId ?? data.CustomerId)
              : undefined,
            supplierId: data.supplierId != null || data.SupplierId != null
              ? Number(data.supplierId ?? data.SupplierId)
              : undefined,
            module: String(data.module ?? data.Module ?? '') as PartyPaymentDetail['module'],
            allocations,
          } satisfies PartyPaymentDetail,
        };
      }),
};

export const LEDGER_TYPE_LABELS: Record<string, string> = {
  CreditSale: 'Credit Sale',
  CashSale: 'Cash Sale',
  PaymentReceived: 'Payment Received',
  Reversal: 'Reversal',
  OpeningBalance: 'Opening Balance',
  CreditPurchase: 'Credit Purchase',
  CashPurchase: 'Cash Purchase',
  PaymentMade: 'Payment Made',
  AgainstInvoice: 'Payment',
  Advance: 'Payment',
  Adjustment: 'Adjustment',
  Sale: 'Sale',
  Purchase: 'Purchase',
  Receipt: 'Receipt',
  Payment: 'Payment',
  Expense: 'Expense',
  JournalVoucher: 'Journal Voucher',
};
