import React, { useEffect, useMemo, useRef, useState } from 'react';
import type { SaleInvoiceDto } from '../../modules/pos/posService';
import { useAuth } from '../../contexts/AuthContext';
import { useReceiptBusinessInfo } from '../../hooks/useReceiptBusinessInfo';
import { useReceiptBranchInfo } from '../../hooks/useReceiptBranchInfo';
import { resolveSessionBusinessId } from '../../services/receiptBusinessService';
import ReceiptPrint from './ReceiptPrint';
import { triggerReceiptPrint, type ReceiptLayout } from './receiptUtils';
import './receipt.css';

export interface ReceiptPrintModalProps {
  invoice: SaleInvoiceDto;
  onClose: () => void;
  autoPrint?: boolean;
  initialLayout?: ReceiptLayout;
  showBranch?: boolean;
  onNewSale?: () => void;
  onVoid?: () => void;
  voidLoading?: boolean;
}

const ReceiptPrintModal: React.FC<ReceiptPrintModalProps> = ({
  invoice,
  onClose,
  autoPrint = false,
  initialLayout = 'thermal',
  showBranch = true,
  onNewSale,
  onVoid,
  voidLoading = false,
}) => {
  const { user } = useAuth();
  const businessId = resolveSessionBusinessId(user?.businessId);
  const { business, logoUrl, loading, error } = useReceiptBusinessInfo(businessId);
  const { branchAddress, branchPhone, branchEmail } = useReceiptBranchInfo(invoice);
  const displayInvoice = useMemo(
    () => ({
      ...invoice,
      branchAddress: branchAddress || invoice.branchAddress,
      branchPhone: branchPhone || invoice.branchPhone,
      branchEmail: branchEmail || invoice.branchEmail,
    }),
    [invoice, branchAddress, branchPhone, branchEmail],
  );
  const [layout, setLayout] = useState<ReceiptLayout>(initialLayout);
  const autoPrintedRef = useRef(false);
  const sessionCashierName = user?.fullName ?? user?.username ?? null;

  useEffect(() => {
    if (!autoPrint || loading || !business || autoPrintedRef.current) return;
    autoPrintedRef.current = true;
    const timer = window.setTimeout(() => {
      triggerReceiptPrint();
    }, 350);
    return () => window.clearTimeout(timer);
  }, [autoPrint, loading, business]);

  const handlePrint = () => {
    triggerReceiptPrint();
  };

  return (
    <div className="receipt-modal-backdrop receipt-no-print" role="dialog" aria-modal="true" aria-label="Sales receipt">
      <div className={`receipt-modal-panel${layout === 'a4' ? ' receipt-modal-panel--a4-preview' : ''}`}>
        <div className="receipt-modal-toolbar receipt-no-print">
          <div>
            <h2 className="text-lg font-bold text-gray-900">Receipt</h2>
            <p className="text-xs text-gray-500">{invoice.invoiceNo}</p>
          </div>
          <div className="flex items-center gap-2">
            <div className="receipt-layout-toggle" role="group" aria-label="Print layout">
              <button
                type="button"
                className={layout === 'thermal' ? 'is-active' : ''}
                onClick={() => setLayout('thermal')}
              >
                80mm
              </button>
              <button
                type="button"
                className={layout === 'a4' ? 'is-active' : ''}
                onClick={() => setLayout('a4')}
              >
                A4
              </button>
            </div>
            <button
              type="button"
              onClick={onClose}
              className="flex h-8 w-8 items-center justify-center rounded-lg text-gray-500 hover:bg-gray-100 text-xl"
              aria-label="Close"
            >
              ×
            </button>
          </div>
        </div>

        <div className="receipt-modal-body">
          {loading && (
            <div className="flex items-center justify-center py-16 text-sm text-gray-500 receipt-no-print">
              Loading receipt…
            </div>
          )}

          {!loading && error && (
            <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700 receipt-no-print">
              {error}
            </div>
          )}

          {!loading && business && (
            <ReceiptPrint
              invoice={displayInvoice}
              business={business}
              logoUrl={logoUrl}
              layout={layout}
              showBranch={showBranch}
              sessionCashierName={sessionCashierName}
            />
          )}
        </div>

        <div className="receipt-modal-actions receipt-no-print">
          {onVoid && invoice.status === 'Completed' && (
            <button
              type="button"
              onClick={onVoid}
              disabled={voidLoading}
              className="receipt-modal-btn receipt-modal-btn--danger w-full"
              style={{ flex: '1 1 100%' }}
            >
              {voidLoading ? 'Voiding…' : 'Void Invoice'}
            </button>
          )}

          <button type="button" onClick={handlePrint} className="receipt-modal-btn receipt-modal-btn--secondary">
            Print
          </button>

          {onNewSale ? (
            <button type="button" onClick={onNewSale} className="receipt-modal-btn receipt-modal-btn--primary">
              New Sale
            </button>
          ) : (
            <button type="button" onClick={onClose} className="receipt-modal-btn receipt-modal-btn--primary">
              Close
            </button>
          )}
        </div>
      </div>
    </div>
  );
};

export default ReceiptPrintModal;
