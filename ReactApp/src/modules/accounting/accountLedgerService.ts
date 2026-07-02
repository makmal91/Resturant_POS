import apiClient from '../../services/api';

export interface GlAccountListItem {
  id: number;
  name: string;
  type: string;
  parentId?: number | null;
  isActive: boolean;
}

export interface AccountLedgerEntry {
  id: number;
  date: string;
  referenceType: string;
  referenceId?: number | null;
  description: string;
  accountName?: string;
  lineAccountName?: string;
  debit: number;
  credit: number;
  runningBalance: number;
  isOpeningBalance: boolean;
  isActive?: boolean;
  isSuperseded?: boolean;
  isReversal?: boolean;
  isReplacement?: boolean;
  originalGroupId?: string;
  groupId?: string;
}

export interface AccountLedgerPage {
  accountId: number;
  accountName: string;
  accountType: string;
  openingBalance: number;
  closingBalance: number;
  effectiveClosingBalance?: number;
  auditView?: boolean;
  includesSubAccounts?: boolean;
  totalDebit: number;
  totalCredit: number;
  periodNet: number;
  entries: AccountLedgerEntry[];
  totalRecords: number;
  totalPages: number;
  currentPage: number;
  pageSize: number;
}

export interface LedgerViewOptions {
  auditView?: boolean;
  groupByChain?: boolean;
}

export const accountLedgerService = {
  async listAccounts(): Promise<GlAccountListItem[]> {
    const res = await apiClient.get<GlAccountListItem[]>('/accounting/accounts');
    return res.data;
  },

  async getLedger(
    accountId: number,
    page = 1,
    pageSize = 50,
    fromDate?: string,
    toDate?: string,
    branchId?: number,
    view?: LedgerViewOptions,
  ): Promise<AccountLedgerPage> {
    const params: Record<string, string | number | boolean> = {
      accountId,
      page,
      pageSize,
    };
    if (fromDate) params.fromDate = fromDate;
    if (toDate) params.toDate = toDate;
    if (branchId && branchId > 0) params.branchId = branchId;
    if (view?.auditView) params.auditView = true;
    if (view?.groupByChain) params.groupByChain = true;

    const res = await apiClient.get<AccountLedgerPage>('/accounting/ledger', { params });
    return res.data;
  },
};
