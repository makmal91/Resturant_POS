import apiClient from '../../services/api';

export interface PartyLedgerEntry {
  id: number;
  date: string;
  type: string;
  description: string;
  debit: number;
  credit: number;
  runningBalance: number;
  referenceId: number;
}

export interface PartyLedgerPage {
  partyId: number;
  partyName: string;
  currentBalance: number;
  entries: PartyLedgerEntry[];
  totalRecords: number;
  totalPages: number;
  currentPage: number;
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
  branchId: number;
}

export interface PaySupplierPayload {
  supplierId: number;
  purchaseId?: number;
  paymentType?: 'Cash' | 'Bank' | 'Online';
  amount: number;
  paymentDate?: string;
  referenceNo?: string;
  notes?: string;
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
    })
  ),
  totalRecords: Number(data.totalRecords ?? data.TotalRecords ?? 0),
  totalPages: Number(data.totalPages ?? data.TotalPages ?? 0),
  currentPage: Number(data.currentPage ?? data.CurrentPage ?? 1),
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

export const partyLedgerService = {
  getCustomerLedger: (
    branchId: number,
    customerId: number,
    page = 1,
    pageSize = 50,
    fromDate?: string,
    toDate?: string
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
    toDate?: string
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

  getCustomerOutstandingInvoices: (branchId: number, customerId: number) =>
    apiClient
      .get(`/payments/customers/${customerId}/outstanding-invoices`, {
        params: { branchId },
        ...branchHeader(branchId),
      })
      .then((res) => ({
        ...res,
        data: (Array.isArray(res.data) ? res.data : []).map((row: Record<string, unknown>) =>
          normalizeOutstandingInvoice(row)
        ),
      })),

  getSupplierOutstandingInvoices: (branchId: number, supplierId: number) =>
    apiClient
      .get(`/payments/suppliers/${supplierId}/outstanding-invoices`, {
        params: { branchId },
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
};

export const LEDGER_TYPE_LABELS: Record<string, string> = {
  CreditSale: 'Credit Sale',
  PaymentReceived: 'Payment Received',
  Reversal: 'Reversal',
  OpeningBalance: 'Opening Balance',
  CreditPurchase: 'Credit Purchase',
  PaymentMade: 'Payment Made',
};
