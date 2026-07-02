import { useCallback } from 'react';
import type { PartyLedgerEntry } from './partyLedgerService';
import { resolvePartyLedgerSource } from '../finance/financeVoucherNav';
import { useFinanceSourceNav } from '../finance/useFinanceSourceNav';

export function usePartyLedgerVoucher(branchId: number) {
  const { openSource } = useFinanceSourceNav(branchId);

  const openVoucher = useCallback(
    (row: PartyLedgerEntry) => {
      const target = resolvePartyLedgerSource(row);
      if (target) openSource(target);
    },
    [openSource],
  );

  return { openVoucher };
}
