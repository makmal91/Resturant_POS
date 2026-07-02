import apiClient from '../../services/api';

export type JournalVoucherType = 'CashIn' | 'CashOut';
export type JournalVoucherPaymentMethod = 'Cash' | 'Bank' | 'Wallet';

export interface JournalVoucherDto {
  id: number;
  branchId: number;
  voucherNo: string;
  transactionType: JournalVoucherType;
  paymentMethod: JournalVoucherPaymentMethod;
  amount: number;
  description: string | null;
  voucherDate: string;
  createdAt: string;
}

export interface JournalVoucherListResponse {
  vouchers: JournalVoucherDto[];
  totalRecords: number;
  totalPages: number;
  currentPage: number;
  pageSize: number;
}

const bh = (branchId: number) => ({ headers: { 'X-Branch-Id': String(branchId) } });

const mapVoucher = (row: Record<string, unknown>): JournalVoucherDto => ({
  id: Number(row.id ?? row.Id ?? 0),
  branchId: Number(row.branchId ?? row.BranchId ?? 0),
  voucherNo: String(row.voucherNo ?? row.VoucherNo ?? ''),
  transactionType: String(row.transactionType ?? row.TransactionType ?? 'CashIn') as JournalVoucherType,
  paymentMethod: String(row.paymentMethod ?? row.PaymentMethod ?? 'Cash') as JournalVoucherPaymentMethod,
  amount: Number(row.amount ?? row.Amount ?? 0),
  description: (row.description ?? row.Description ?? null) as string | null,
  voucherDate: String(row.voucherDate ?? row.VoucherDate ?? ''),
  createdAt: String(row.createdAt ?? row.CreatedAt ?? ''),
});

export const journalVoucherService = {
  list: async (
    branchId: number,
    page = 1,
    pageSize = 25,
    filters: {
      fromDate?: string;
      toDate?: string;
      transactionType?: JournalVoucherType | '';
    } = {},
  ): Promise<JournalVoucherListResponse> => {
    const res = await apiClient.get<Record<string, unknown>>('/cashflow/journal-vouchers', {
      params: {
        branchId,
        page,
        pageSize,
        fromDate: filters.fromDate || undefined,
        toDate: filters.toDate || undefined,
        transactionType: filters.transactionType || undefined,
      },
      ...bh(branchId),
    });

    const payload = res.data ?? {};
    const rows = (payload.vouchers ?? payload.Vouchers ?? []) as Record<string, unknown>[];

    return {
      vouchers: rows.map(mapVoucher),
      totalRecords: Number(payload.totalRecords ?? payload.TotalRecords ?? 0),
      totalPages: Number(payload.totalPages ?? payload.TotalPages ?? 0),
      currentPage: Number(payload.currentPage ?? payload.CurrentPage ?? page),
      pageSize: Number(payload.pageSize ?? payload.PageSize ?? pageSize),
    };
  },
};
