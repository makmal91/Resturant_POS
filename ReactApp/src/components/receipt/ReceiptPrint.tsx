import React from 'react';
import type { SaleInvoiceDto } from '../../modules/pos/posService';
import { BASE_CURRENCY } from '../../utils/currencyHelper';
import {
  barcode39Text,
  barcodeFontSize,
  computeRoundOff,
  formatReceiptCurrency,
  formatReceiptDateCompact,
  formatReceiptDateTime,
  formatReceiptAmount,
  formatReceiptNumber,
  getBalanceDue,
  getItemDiscount,
  getItemSkuLabel,
  resolveCashierName,
  type ReceiptBusinessInfo,
  type ReceiptLayout,
} from './receiptUtils';
import './receipt.css';

export interface ReceiptPrintProps {
  invoice: SaleInvoiceDto;
  business: ReceiptBusinessInfo;
  logoUrl?: string | null;
  layout?: ReceiptLayout;
  showBranch?: boolean;
  sessionCashierName?: string | null;
  className?: string;
}

const ReceiptPrint: React.FC<ReceiptPrintProps> = ({
  invoice,
  business,
  logoUrl,
  layout = 'thermal',
  showBranch = true,
  sessionCashierName,
  className = '',
}) => {
  const isVoided = invoice.status === 'Voided';
  const currency = business.currency || BASE_CURRENCY;
  const roundOff = computeRoundOff(invoice);
  const balanceDue = getBalanceDue(invoice);
  const cashierName = resolveCashierName(invoice, sessionCashierName);
  const barcodeText = barcode39Text(invoice.invoiceNo);
  const barcodeSize = barcodeFontSize(barcodeText, layout);
  const branchAddress = invoice.branchAddress?.trim() ?? '';
  const branchPhone = invoice.branchPhone?.trim() ?? '';
  const branchEmail = invoice.branchEmail?.trim() ?? '';
  const headerPhone = branchPhone || business.phone;
  const headerEmail = branchEmail || business.email;
  const contactParts = [headerPhone, headerEmail].filter(Boolean);
  const hasCustomer = Boolean(invoice.customerName?.trim());
  const isMixed = invoice.paymentMethod === 'Mixed';

  const rootClass = [
    'receipt-print',
    'receipt-print-area',
    layout === 'a4' ? 'receipt-print--a4' : 'receipt-print--thermal',
    className,
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <article className={rootClass} aria-label={`Receipt ${invoice.invoiceNo}`}>
      {/* Business header */}
      <header className="receipt-section receipt-header">
        {logoUrl && (
          <img src={logoUrl} alt={`${business.name} logo`} className="receipt-logo" />
        )}
        <h1 className="receipt-business-name">{business.name || 'Business'}</h1>
        {showBranch && invoice.branchName && (
          <p className="receipt-branch-name">{invoice.branchName}</p>
        )}
        {branchAddress ? (
          <p className="receipt-business-meta">{branchAddress}</p>
        ) : business.address ? (
          <p className="receipt-business-meta">{business.address}</p>
        ) : null}
        {contactParts.length > 0 && (
          <p className="receipt-business-meta">{contactParts.join(' · ')}</p>
        )}
        {business.taxNumber && (
          <p className="receipt-business-meta">Tax No: {business.taxNumber}</p>
        )}
        {isVoided && <span className="receipt-void-badge">VOIDED</span>}
      </header>

      <hr className="receipt-divider" />

      {/* Invoice information */}
      <section className="receipt-section">
        <div className="receipt-meta-grid receipt-meta-grid--full">
          <div className="receipt-meta-row receipt-meta-row--invoice-date">
            <span className="receipt-meta-pair">
              <span className="receipt-meta-label">Invoice</span>
              <span className="receipt-meta-value">{invoice.invoiceNo}</span>
            </span>
            <span className="receipt-meta-pair">
              <span className="receipt-meta-label">Date</span>
              <span className="receipt-meta-value">{formatReceiptDateCompact(invoice.saleDate)}</span>
            </span>
          </div>
          {cashierName && (
            <div className="receipt-meta-row">
              <span className="receipt-meta-label">Cashier</span>
              <span className="receipt-meta-value">{cashierName}</span>
            </div>
          )}
        </div>

        {hasCustomer && (
          <>
            <hr className="receipt-divider" />
            <div className="receipt-meta-grid receipt-meta-grid--full">
              <div className="receipt-meta-row">
                <span className="receipt-meta-label">Customer</span>
                <span className="receipt-meta-value">{invoice.customerName}</span>
              </div>
              {invoice.customerPhone && (
                <div className="receipt-meta-row">
                  <span className="receipt-meta-label">Phone</span>
                  <span className="receipt-meta-value">{invoice.customerPhone}</span>
                </div>
              )}
            </div>
          </>
        )}

        {isVoided && invoice.voidedAt && (
          <>
            <hr className="receipt-divider" />
            <p className="receipt-business-meta" style={{ textAlign: 'center', color: '#c62828' }}>
              Voided {formatReceiptDateTime(invoice.voidedAt)}
              {invoice.voidedByName ? ` by ${invoice.voidedByName}` : ''}
            </p>
          </>
        )}
      </section>

      <hr className="receipt-divider receipt-divider--solid" />

      {/* Items */}
      <section className="receipt-section">
        <div className="receipt-items-head">
          <span className="receipt-col-item">Item</span>
          <span className="receipt-col-qty">Qty</span>
          <span className="receipt-col-price">Price</span>
          {layout === 'a4' && <span className="receipt-col-disc">Disc</span>}
          <span className="receipt-col-total">Total</span>
        </div>

        {invoice.items.map((item) => {
          const sku = getItemSkuLabel(item);
          const discount = getItemDiscount(item);
          const variantLabel = [item.variantName, item.unitName].filter(Boolean).join(' · ');

          return (
            <div
              key={item.id}
              className={`receipt-item-row${isVoided ? ' receipt-item-row--voided' : ''}`}
            >
              <div className="receipt-col-item">
                <p className="receipt-item-name">{item.productName}</p>
                {(sku || variantLabel) && (
                  <p className="receipt-item-sub">
                    {sku && <span>{sku}</span>}
                    {sku && variantLabel && ' · '}
                    {variantLabel}
                  </p>
                )}
              </div>
              <span className="receipt-item-qty receipt-col-qty">{formatReceiptAmount(item.quantity)}</span>
              <span className="receipt-item-price receipt-col-price">
                {formatReceiptAmount(item.unitPrice)}
              </span>
              {layout === 'a4' && (
                <span className="receipt-item-disc receipt-col-disc">
                  {discount > 0 ? `−${formatReceiptAmount(discount)}` : '—'}
                </span>
              )}
              <span className="receipt-item-total receipt-col-total">
                {formatReceiptAmount(item.lineTotal)}
              </span>
            </div>
          );
        })}
      </section>

      <hr className="receipt-divider receipt-divider--solid" />

      {/* Totals */}
      <section className="receipt-section receipt-totals">
        <div className="receipt-total-row">
          <span>Subtotal</span>
          <span>{formatReceiptCurrency(invoice.subTotal, currency)}</span>
        </div>
        {invoice.discountAmount > 0 && (
          <div className="receipt-total-row receipt-total-row--discount">
            <span>Discount</span>
            <span>−{formatReceiptCurrency(invoice.discountAmount, currency)}</span>
          </div>
        )}
        {invoice.taxAmount > 0 && (
          <div className="receipt-total-row">
            <span>Tax</span>
            <span>+{formatReceiptCurrency(invoice.taxAmount, currency)}</span>
          </div>
        )}
        {roundOff !== null && (
          <div className="receipt-total-row">
            <span>Round Off</span>
            <span>
              {roundOff >= 0 ? '+' : '−'}
              {formatReceiptCurrency(Math.abs(roundOff), currency)}
            </span>
          </div>
        )}
        <div className="receipt-total-row receipt-total-row--grand">
          <span>Grand Total</span>
          <span>{formatReceiptCurrency(invoice.grandTotal, currency)}</span>
        </div>
      </section>

      <hr className="receipt-divider" />

      {/* Payment */}
      <section className="receipt-section">
        <div className="receipt-payment-row">
          <span>Payment ({invoice.paymentMethod})</span>
          <span>{formatReceiptCurrency(invoice.paidAmount, currency)}</span>
        </div>
        {isMixed && (invoice.cashAmount > 0 || invoice.cardAmount > 0) && (
          <>
            {invoice.cashAmount > 0 && (
              <div className="receipt-payment-row">
                <span>Cash</span>
                <span>{formatReceiptCurrency(invoice.cashAmount, currency)}</span>
              </div>
            )}
            {invoice.cardAmount > 0 && (
              <div className="receipt-payment-row">
                <span>Card</span>
                <span>{formatReceiptCurrency(invoice.cardAmount, currency)}</span>
              </div>
            )}
          </>
        )}
        {invoice.returnAmount > 0 && (
          <div className="receipt-payment-row receipt-payment-row--change">
            <span>Change</span>
            <span>{formatReceiptCurrency(invoice.returnAmount, currency)}</span>
          </div>
        )}
        {balanceDue > 0 && (
          <div className="receipt-payment-row receipt-payment-row--due">
            <span>Balance Due</span>
            <span>{formatReceiptCurrency(balanceDue, currency)}</span>
          </div>
        )}
      </section>

      <hr className="receipt-divider" />

      {/* Footer */}
      <footer className="receipt-section receipt-footer">
        <p className="receipt-footer-title">
          {isVoided ? 'This invoice has been voided.' : 'Thank you for your purchase!'}
        </p>
        {business.slogan && <p>{business.slogan}</p>}
        {business.website && <p>{business.website}</p>}
        {invoice.notes && (
          <p style={{ marginTop: '0.35rem', fontSize: '0.88em' }}>Note: {invoice.notes}</p>
        )}

        {barcodeText && (
          <div className="receipt-barcode-wrap">
            <span
              className="receipt-barcode"
              style={{ fontSize: barcodeSize }}
              aria-label={`Barcode ${invoice.invoiceNo}`}
            >
              {barcodeText}
            </span>
          </div>
        )}

        <div className="receipt-powered-by">
          <p className="receipt-powered-by-title">Powered by AKHSoft</p>
          <p className="receipt-powered-by-contact">akhsoft.com | 0307-1725577</p>
        </div>
      </footer>
    </article>
  );
};

export default ReceiptPrint;
