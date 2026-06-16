import apiClient from '../../services/api';
import type {
  SaleInvoiceDto,
  SaleInvoiceItem,
  SaleLedgerEntry,
  VoidInvoicePayload,
  UpdateSaleInvoicePayload,
} from '../pos/posService';

export type { SaleInvoiceDto, SaleInvoiceItem, SaleLedgerEntry };

export type SaleInvoiceStatus =
  | 'Draft' | 'Completed' | 'Held' | 'Cancelled' | 'Returned' | 'Voided';

export interface SaleInvoiceListDto {
  id: number;
  invoiceNo: string;
  customerName: string | null;
  customerPhone: string | null;
  warehouseName: string;
  saleDate: string;
  grandTotal: number;
  paidAmount: number;
  paymentMethod: string;
  status: SaleInvoiceStatus;
  cashierName: string | null;
  itemCount: number;
  createdDate: string;
  voidedAt: string | null;
  branchId: number;
  warehouseId: number;
  customerId: number | null;
}

export interface SaleInvoicesResponse {
  invoices: SaleInvoiceListDto[];
  totalRecords: number;
  totalPages: number;
  currentPage: number;
  pageSize: number;
}

const bh = (branchId: number) => ({ headers: { 'X-Branch-Id': String(branchId) } });

export const salesService = {
  getAll: (
    branchId: number,
    page = 1,
    pageSize = 25,
    search?: string,
    status?: SaleInvoiceStatus | null,
    dateFrom?: string | null,
    dateTo?: string | null,
  ) =>
    apiClient.get<SaleInvoicesResponse>('/sales/invoices', {
      params: {
        branchId, page, pageSize,
        ...(search ? { search } : {}),
        ...(status ? { status } : {}),
        ...(dateFrom ? { dateFrom } : {}),
        ...(dateTo ? { dateTo } : {}),
      },
      ...bh(branchId),
    }),

  getById: (id: number, branchId: number) =>
    apiClient.get<SaleInvoiceDto>(`/sales/invoice/${id}`, {
      params: { branchId },
      ...bh(branchId),
    }),

  voidInvoice: (id: number, payload: VoidInvoicePayload) =>
    apiClient.post<SaleInvoiceDto>(`/sales/invoice/${id}/void`, payload, bh(payload.branchId)),

  updateInvoice: (id: number, payload: UpdateSaleInvoicePayload) =>
    apiClient.put<SaleInvoiceDto>(`/sales/invoice/${id}`, payload, bh(payload.branchId)),

  getLedgerHistory: (id: number, branchId: number) =>
    apiClient.get<SaleLedgerEntry[]>(`/sales/invoice/${id}/ledger`, {
      params: { branchId },
      ...bh(branchId),
    }),
};
