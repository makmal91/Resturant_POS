import { useEffect, useState } from 'react';
import { BranchService } from '../services/apiService';
import type { SaleInvoiceDto } from '../modules/pos/posService';

export const useReceiptBranchInfo = (invoice: SaleInvoiceDto) => {
  const [branchAddress, setBranchAddress] = useState(invoice.branchAddress?.trim() ?? '');
  const [branchPhone, setBranchPhone] = useState(invoice.branchPhone?.trim() ?? '');
  const [branchEmail, setBranchEmail] = useState(invoice.branchEmail?.trim() ?? '');

  useEffect(() => {
    setBranchAddress(invoice.branchAddress?.trim() ?? '');
    setBranchPhone(invoice.branchPhone?.trim() ?? '');
    setBranchEmail(invoice.branchEmail?.trim() ?? '');

    if (invoice.branchAddress?.trim() || invoice.branchId <= 0) return;

    let cancelled = false;

    void (async () => {
      try {
        const response = await BranchService.getById(invoice.branchId);
        const d = response.data as Record<string, unknown>;
        const row = (d.data ?? d) as Record<string, unknown>;
        if (cancelled) return;
        setBranchAddress(String(row.address ?? row.Address ?? '').trim());
        setBranchPhone(String(row.phone ?? row.Phone ?? '').trim());
        setBranchEmail(String(row.email ?? row.Email ?? '').trim());
      } catch {
        /* keep empty */
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [invoice.branchId, invoice.branchAddress, invoice.branchPhone, invoice.branchEmail]);

  return { branchAddress, branchPhone, branchEmail };
};
