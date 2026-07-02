import apiClient from '../../services/api';

export type TrialBalanceAccountLevel = 'ParentOnly' | 'ParentAndChild';

export interface TrialBalanceRow {
  accountId: number;
  accountCode: string;
  accountName: string;
  parentAccountId?: number | null;
  level: number;
  hasChildren: boolean;
  debit: number;
  credit: number;
}

export interface TrialBalanceReport {
  fromDate?: string | null;
  toDate?: string | null;
  branchId?: number | null;
  accountLevel: TrialBalanceAccountLevel | number;
  showZeroBalance: boolean;
  rows: TrialBalanceRow[];
  totalDebit: number;
  totalCredit: number;
  isBalanced: boolean;
  balanceMessage?: string | null;
}

export interface TrialBalanceQuery {
  fromDate?: string;
  toDate?: string;
  branchId?: number;
  accountLevel?: TrialBalanceAccountLevel | number;
  showZeroBalance?: boolean;
}

const levelMap: Record<TrialBalanceAccountLevel, number> = {
  ParentOnly: 1,
  ParentAndChild: 2,
};

export const trialBalanceService = {
  async getReport(query: TrialBalanceQuery = {}): Promise<TrialBalanceReport> {
    const params: Record<string, string | number | boolean> = {};
    if (query.fromDate) params.fromDate = query.fromDate;
    if (query.toDate) params.toDate = query.toDate;
    if (query.branchId && query.branchId > 0) params.branchId = query.branchId;
    if (query.accountLevel) {
      params.accountLevel =
        typeof query.accountLevel === 'number'
          ? query.accountLevel
          : levelMap[query.accountLevel];
    }
    if (query.showZeroBalance) params.showZeroBalance = true;

    const res = await apiClient.get<TrialBalanceReport>('/reports/trial-balance', { params });
    return res.data;
  },
};
