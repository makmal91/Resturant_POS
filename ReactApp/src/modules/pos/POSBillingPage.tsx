import React, {
  useCallback,
  useEffect,
  useRef,
  useState,
} from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { warehouseService, type WarehouseItem } from '../warehouse/warehouseService';
import {
  posService,
  type CartItem,
  type PosCustomer,
  type PosProductLookup,
  type PosSearchGroup,
  type SaleInvoiceDto,
  cartKey,
  computeLineTotal,
  lookupToCartItem,
  groupRowToLookup,
} from './posService';

// ─── helpers ─────────────────────────────────────────────────────────────────

const fmt = (n: number) =>
  n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

function useDebounce<T>(value: T, delay: number): T {
  const [dv, setDv] = useState(value);
  useEffect(() => {
    const t = setTimeout(() => setDv(value), delay);
    return () => clearTimeout(t);
  }, [value, delay]);
  return dv;
}

// ─── Payment Modal ────────────────────────────────────────────────────────────

interface PaymentModalProps {
  grandTotal: number;
  onClose: () => void;
  onConfirm: (method: 'Cash' | 'Card' | 'Mixed', paid: number, cash: number, card: number) => void;
  loading: boolean;
}

const PaymentModal: React.FC<PaymentModalProps> = ({ grandTotal, onClose, onConfirm, loading }) => {
  const [method, setMethod] = useState<'Cash' | 'Card' | 'Mixed'>('Cash');
  const [cash, setCash] = useState(grandTotal);
  const [card, setCard] = useState(0);
  const cashRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    setCash(grandTotal);
    setCard(0);
  }, [grandTotal]);

  useEffect(() => {
    setTimeout(() => { cashRef.current?.focus(); cashRef.current?.select(); }, 50);
  }, [method]);

  const paid = method === 'Mixed' ? cash + card : method === 'Cash' ? cash : card;
  const change = Math.max(0, paid - grandTotal);
  const isValid = method === 'Mixed' ? Math.abs(cash + card - grandTotal) < 0.01 : paid >= grandTotal;

  const handleConfirm = () => {
    if (!isValid) return;
    onConfirm(
      method,
      paid,
      method === 'Cash' || method === 'Mixed' ? cash : 0,
      method === 'Card' || method === 'Mixed' ? card : 0
    );
  };

  const methodBtns: { key: 'Cash' | 'Card' | 'Mixed'; label: string; icon: string }[] = [
    { key: 'Cash', label: 'Cash', icon: '💵' },
    { key: 'Card', label: 'Card', icon: '💳' },
    { key: 'Mixed', label: 'Mixed', icon: '🔀' },
  ];

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-md mx-4 overflow-hidden border border-gray-200">
        {/* Header */}
        <div className="bg-blue-600 px-6 py-5">
          <p className="text-blue-100 text-sm font-medium">Total Due</p>
          <p className="text-4xl font-black text-white mt-1">{fmt(grandTotal)}</p>
        </div>

        <div className="p-6 space-y-5">
          {/* Payment method selector */}
          <div className="flex gap-2">
            {methodBtns.map((m) => (
              <button
                key={m.key}
                onClick={() => {
                  setMethod(m.key);
                  if (m.key === 'Cash') { setCash(grandTotal); setCard(0); }
                  else if (m.key === 'Card') { setCash(0); setCard(grandTotal); }
                  else { setCash(grandTotal); setCard(0); }
                }}
                className={`flex-1 py-3 rounded-xl font-semibold text-sm transition-all border ${
                  method === m.key
                    ? 'bg-blue-600 border-blue-600 text-white shadow-sm'
                    : 'bg-white border-gray-200 text-gray-600 hover:border-blue-300 hover:text-blue-600'
                }`}
              >
                <span className="mr-1">{m.icon}</span>{m.label}
              </button>
            ))}
          </div>

          {/* Amount inputs */}
          {method === 'Cash' && (
            <div>
              <label className="block text-sm font-semibold text-gray-700 mb-1.5">Cash Received</label>
              <input
                ref={cashRef}
                type="number"
                value={cash}
                onChange={(e) => setCash(parseFloat(e.target.value) || 0)}
                className="w-full px-4 py-3 text-2xl font-bold border-2 border-gray-200 rounded-xl focus:outline-none focus:border-blue-500 transition"
              />
            </div>
          )}
          {method === 'Card' && (
            <div>
              <label className="block text-sm font-semibold text-gray-700 mb-1.5">Card Amount</label>
              <input
                type="number"
                value={card}
                onChange={(e) => setCard(parseFloat(e.target.value) || 0)}
                className="w-full px-4 py-3 text-2xl font-bold border-2 border-gray-200 rounded-xl focus:outline-none focus:border-blue-500 transition"
              />
            </div>
          )}
          {method === 'Mixed' && (
            <div className="space-y-3">
              <div>
                <label className="block text-sm font-semibold text-gray-700 mb-1">💵 Cash</label>
                <input
                  ref={cashRef}
                  type="number"
                  value={cash}
                  onChange={(e) => { const c = parseFloat(e.target.value) || 0; setCash(c); setCard(Math.max(0, grandTotal - c)); }}
                  className="w-full px-4 py-3 text-lg font-bold border-2 border-gray-200 rounded-xl focus:outline-none focus:border-blue-500 transition"
                />
              </div>
              <div>
                <label className="block text-sm font-semibold text-gray-700 mb-1">💳 Card</label>
                <input
                  type="number"
                  value={card}
                  onChange={(e) => { const c = parseFloat(e.target.value) || 0; setCard(c); setCash(Math.max(0, grandTotal - c)); }}
                  className="w-full px-4 py-3 text-lg font-bold border-2 border-gray-200 rounded-xl focus:outline-none focus:border-blue-500 transition"
                />
              </div>
              {!isValid && <p className="text-red-500 text-xs">Cash + Card must equal the total.</p>}
            </div>
          )}

          {/* Change */}
          <div className={`rounded-xl p-4 text-center ${change > 0 ? 'bg-green-50 border border-green-200' : 'bg-gray-50 border border-gray-200'}`}>
            <p className="text-xs font-medium text-gray-500 uppercase tracking-wide">Change</p>
            <p className={`text-3xl font-black mt-1 ${change > 0 ? 'text-green-600' : 'text-gray-400'}`}>{fmt(change)}</p>
          </div>
        </div>

        <div className="px-6 pb-6 flex gap-3">
          <button onClick={onClose} className="flex-1 py-3.5 rounded-xl border border-gray-200 text-gray-600 font-semibold hover:bg-gray-50 transition">
            Cancel
          </button>
          <button
            onClick={handleConfirm}
            disabled={!isValid || loading}
            className="flex-1 py-3.5 rounded-xl bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white font-bold text-base transition shadow-sm"
          >
            {loading ? 'Processing…' : '✓ Confirm Payment'}
          </button>
        </div>
      </div>
    </div>
  );
};

// ─── Held Bills Modal ─────────────────────────────────────────────────────────

interface HeldBillsModalProps {
  bills: SaleInvoiceDto[];
  onResume: (bill: SaleInvoiceDto) => void;
  onCancel: (id: number) => void;
  onClose: () => void;
}

const HeldBillsModal: React.FC<HeldBillsModalProps> = ({ bills, onResume, onCancel, onClose }) => (
  <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm">
    <div className="bg-white rounded-2xl shadow-2xl w-full max-w-lg mx-4 overflow-hidden border border-gray-200 max-h-[80vh] flex flex-col">
      <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
        <div>
          <h2 className="text-lg font-bold text-gray-800">Held Bills</h2>
          <p className="text-sm text-gray-500">{bills.length} bill{bills.length !== 1 ? 's' : ''} on hold</p>
        </div>
        <button onClick={onClose} className="w-8 h-8 flex items-center justify-center rounded-lg hover:bg-gray-100 text-gray-500 text-xl transition">×</button>
      </div>
      <div className="overflow-y-auto flex-1 divide-y divide-gray-100">
        {bills.length === 0 ? (
          <div className="p-12 text-center text-gray-400">
            <p className="text-4xl mb-3">📋</p>
            <p className="font-medium">No held bills</p>
          </div>
        ) : (
          bills.map((b) => (
            <div key={b.id} className="px-5 py-4 flex items-center gap-4 hover:bg-gray-50 transition">
              <div className="flex-1 min-w-0">
                <p className="font-semibold text-gray-800 text-sm">{b.invoiceNo}</p>
                <p className="text-xs text-gray-500 mt-0.5">{b.customerName ?? 'Walk-in Customer'} · {b.items.length} item{b.items.length !== 1 ? 's' : ''}</p>
                {b.heldNote && <p className="text-xs text-amber-600 mt-0.5 italic">"{b.heldNote}"</p>}
              </div>
              <span className="text-sm font-bold text-gray-700 tabular-nums">{fmt(b.grandTotal)}</span>
              <button onClick={() => onResume(b)} className="px-3 py-1.5 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-xs font-semibold transition">Resume</button>
              <button onClick={() => onCancel(b.id)} className="px-3 py-1.5 bg-red-50 hover:bg-red-100 text-red-600 border border-red-200 rounded-lg text-xs font-semibold transition">Cancel</button>
            </div>
          ))
        )}
      </div>
    </div>
  </div>
);

// ─── Receipt Modal ────────────────────────────────────────────────────────────

interface ReceiptModalProps {
  invoice: SaleInvoiceDto;
  onClose: () => void;
  onNewSale: () => void;
}

const ReceiptModal: React.FC<ReceiptModalProps> = ({ invoice, onClose, onNewSale }) => (
  <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm">
    <div className="bg-white rounded-2xl shadow-2xl w-full max-w-sm mx-4 overflow-hidden border border-gray-200 max-h-[90vh] flex flex-col">
      <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
        <div>
          <h2 className="text-lg font-bold text-gray-800">Receipt</h2>
          <p className="text-xs text-gray-500">{invoice.invoiceNo}</p>
        </div>
        <button onClick={onClose} className="w-8 h-8 flex items-center justify-center rounded-lg hover:bg-gray-100 text-gray-500 text-xl transition">×</button>
      </div>

      <div className="overflow-y-auto flex-1 p-5 space-y-4 text-sm">
        <div className="text-center pb-3 border-b border-dashed border-gray-300">
          <p className="text-xs text-gray-500">{new Date(invoice.saleDate).toLocaleString()}</p>
          {invoice.customerName && <p className="text-gray-600 text-xs mt-1">Customer: <span className="font-medium">{invoice.customerName}</span></p>}
          {invoice.warehouseName && <p className="text-gray-600 text-xs">Warehouse: <span className="font-medium">{invoice.warehouseName}</span></p>}
        </div>

        <table className="w-full text-xs">
          <tbody className="divide-y divide-gray-100">
            {invoice.items.map((item) => (
              <tr key={item.id}>
                <td className="py-2 pr-2">
                  <p className="font-medium text-gray-800">{item.productName}</p>
                  {item.variantName && <p className="text-gray-400">{item.variantName}</p>}
                  <p className="text-gray-400">{item.unitName}</p>
                </td>
                <td className="py-2 text-right text-gray-600 whitespace-nowrap">{item.quantity} × {fmt(item.unitPrice)}</td>
                <td className="py-2 pl-2 text-right font-semibold text-gray-800 whitespace-nowrap">{fmt(item.lineTotal)}</td>
              </tr>
            ))}
          </tbody>
        </table>

        <div className="border-t border-dashed border-gray-300 pt-3 space-y-1.5">
          <div className="flex justify-between text-gray-600"><span>Subtotal</span><span>{fmt(invoice.subTotal)}</span></div>
          {invoice.discountAmount > 0 && <div className="flex justify-between text-red-500"><span>Discount</span><span>−{fmt(invoice.discountAmount)}</span></div>}
          {invoice.taxAmount > 0 && <div className="flex justify-between text-gray-600"><span>Tax</span><span>+{fmt(invoice.taxAmount)}</span></div>}
          <div className="flex justify-between font-bold text-gray-900 text-base border-t border-gray-200 pt-2 mt-2">
            <span>Total</span><span>{fmt(invoice.grandTotal)}</span>
          </div>
          <div className="flex justify-between text-gray-600"><span>Paid ({invoice.paymentMethod})</span><span>{fmt(invoice.paidAmount)}</span></div>
          {invoice.returnAmount > 0 && (
            <div className="flex justify-between font-semibold text-green-600"><span>Change</span><span>{fmt(invoice.returnAmount)}</span></div>
          )}
        </div>

        <p className="text-center text-gray-400 text-xs pt-2 border-t border-dashed border-gray-300">Thank you for your purchase!</p>
      </div>

      <div className="px-5 pb-5 flex gap-3 border-t border-gray-100 pt-4">
        <button onClick={() => window.print()} className="flex-1 py-2.5 rounded-xl border border-gray-200 text-gray-600 font-semibold hover:bg-gray-50 transition text-sm">
          🖨️ Print
        </button>
        <button onClick={onNewSale} className="flex-1 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white font-bold transition text-sm">
          + New Sale
        </button>
      </div>
    </div>
  </div>
);

// ─── Main POS Billing Page ────────────────────────────────────────────────────

const POSBillingPage: React.FC = () => {
  const { user, selectedBranchId } = useAuth();
  const branchId: number = selectedBranchId ?? (user as { branchId?: number })?.branchId ?? 1;
  const businessId: number = (user as { businessId?: number })?.businessId ?? 1;

  // ── State ──
  const [warehouses, setWarehouses] = useState<WarehouseItem[]>([]);
  const [warehouseId, setWarehouseId] = useState<number>(0);
  const [cart, setCart] = useState<CartItem[]>([]);
  const [discountMode, setDiscountMode] = useState<'percent' | 'amount'>('percent');
  const [discountInput, setDiscountInput] = useState('0');
  const [pricingType, setPricingType] = useState<'Retail' | 'Wholesale'>('Retail');
  const [customer, setCustomer] = useState<PosCustomer | null>(null);
  const [customerQuery, setCustomerQuery] = useState('');
  const [customerResults, setCustomerResults] = useState<PosCustomer[]>([]);
  const [barcodeInput, setBarcodeInput] = useState('');
  const [barcodeError, setBarcodeError] = useState('');
  const [barcodeLoading, setBarcodeLoading] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [searchResults, setSearchResults] = useState<PosSearchGroup[]>([]);
  const [searchLoading, setSearchLoading] = useState(false);
  const [expandedGroups, setExpandedGroups] = useState<Set<number>>(new Set());
  const [showPayment, setShowPayment] = useState(false);
  const [paymentLoading, setPaymentLoading] = useState(false);
  const [showHeld, setShowHeld] = useState(false);
  const [heldBills, setHeldBills] = useState<SaleInvoiceDto[]>([]);
  const [completedInvoice, setCompletedInvoice] = useState<SaleInvoiceDto | null>(null);
  const [error, setError] = useState('');

  const barcodeRef = useRef<HTMLInputElement>(null);
  const searchRef = useRef<HTMLInputElement>(null);

  const debouncedSearch = useDebounce(searchQuery, 250);
  const debouncedCustomer = useDebounce(customerQuery, 300);

  // ── Load warehouses ──
  useEffect(() => {
    if (!branchId) return;
    warehouseService.getAllActive(branchId)
      .then((r) => {
        setWarehouses(r.data);
        if (r.data.length > 0) setWarehouseId(r.data[0].id);
      })
      .catch(() => setError('Failed to load warehouses.'));
  }, [branchId]);

  // ── Keyboard shortcuts ──
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'F2') { e.preventDefault(); barcodeRef.current?.focus(); barcodeRef.current?.select(); }
      if (e.key === 'F3') { e.preventDefault(); searchRef.current?.focus(); searchRef.current?.select(); }
      if (e.key === 'F4') { e.preventDefault(); if (cart.length > 0 && warehouseId) setShowPayment(true); }
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [cart, warehouseId]);

  // ── Auto-focus barcode ──
  useEffect(() => { setTimeout(() => barcodeRef.current?.focus(), 200); }, []);

  // ── Product search (grouped with variants) ──
  useEffect(() => {
    if (!debouncedSearch.trim()) { setSearchResults([]); setExpandedGroups(new Set()); return; }
    setSearchLoading(true);
    posService.searchProductsGrouped(debouncedSearch, branchId, warehouseId || undefined)
      .then((r) => {
        setSearchResults(r.data);
        // Auto-expand if only one product returned, or if any product has variants
        const ids = new Set(r.data.filter(g => g.isVariantEnabled).map(g => g.productId));
        setExpandedGroups(ids);
      })
      .catch(() => setSearchResults([]))
      .finally(() => setSearchLoading(false));
  }, [debouncedSearch, branchId, warehouseId]);

  // ── Customer search ──
  useEffect(() => {
    if (!debouncedCustomer.trim()) { setCustomerResults([]); return; }
    posService.searchCustomers(debouncedCustomer, branchId)
      .then((r) => setCustomerResults(r.data))
      .catch(() => setCustomerResults([]));
  }, [debouncedCustomer, branchId]);

  // ── Cart helpers ──
  const addToCart = useCallback((lookup: PosProductLookup) => {
    const item = lookupToCartItem(lookup, pricingType);
    setCart((prev) => {
      const idx = prev.findIndex((c) => c.cartKey === item.cartKey);
      if (idx >= 0) {
        const updated = [...prev];
        const existing = updated[idx];
        updated[idx] = { ...existing, quantity: existing.quantity + 1, lineTotal: computeLineTotal({ ...existing, quantity: existing.quantity + 1 }) };
        return updated;
      }
      return [...prev, item];
    });
    setBarcodeError('');
  }, [pricingType]);

  const updateQuantity = (key: string, qty: number) => {
    if (qty <= 0) { removeFromCart(key); return; }
    setCart((prev) => prev.map((c) => c.cartKey === key ? { ...c, quantity: qty, lineTotal: computeLineTotal({ ...c, quantity: qty }) } : c));
  };

  const updateItemDiscount = (key: string, percent: number) => {
    setCart((prev) => prev.map((c) => c.cartKey === key ? { ...c, discountPercent: percent, discountAmount: 0, lineTotal: computeLineTotal({ ...c, discountPercent: percent, discountAmount: 0 }) } : c));
  };

  const removeFromCart = (key: string) => setCart((prev) => prev.filter((c) => c.cartKey !== key));

  const clearCart = () => {
    setCart([]); setDiscountInput('0'); setDiscountMode('percent'); setCustomer(null); setCustomerQuery('');
    setTimeout(() => barcodeRef.current?.focus(), 50);
  };

  // ── Totals ──
  const subTotal = cart.reduce((s, c) => s + c.quantity * c.unitPrice, 0);
  const totalItemDiscount = cart.reduce((s, c) => {
    const d = c.discountAmount > 0 ? c.discountAmount : (c.quantity * c.unitPrice * c.discountPercent) / 100;
    return s + d;
  }, 0);
  const totalTax = cart.reduce((s, c) => {
    const net = c.quantity * c.unitPrice - (c.discountAmount > 0 ? c.discountAmount : (c.quantity * c.unitPrice * c.discountPercent) / 100);
    return s + (net * c.taxPercent) / 100;
  }, 0);
  const discountRaw = Math.max(0, parseFloat(discountInput) || 0);
  const billDiscount = discountMode === 'percent'
    ? (subTotal * Math.min(100, discountRaw)) / 100
    : Math.min(subTotal, discountRaw);
  const billDiscountPercent = subTotal > 0 ? (billDiscount / subTotal) * 100 : 0;

  const grandTotal = Math.max(0, subTotal - totalItemDiscount - billDiscount + totalTax);

  // ── Barcode scan ──
  const handleBarcodeSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const code = barcodeInput.trim();
    if (!code) return;
    if (!warehouseId) { setBarcodeError('Select a warehouse first.'); return; }
    setBarcodeLoading(true); setBarcodeError('');
    try {
      const res = await posService.getProductByBarcode(code, branchId);
      addToCart(res.data);
      setBarcodeInput('');
    } catch {
      setBarcodeError(`No product found for "${code}"`);
    } finally {
      setBarcodeLoading(false);
      barcodeRef.current?.focus();
    }
  };

  // ── Payment ──
  const handlePaymentConfirm = async (method: 'Cash' | 'Card' | 'Mixed', paid: number, cash: number, card: number) => {
    if (!warehouseId) return;
    setPaymentLoading(true);
    try {
      const res = await posService.createInvoice({
        customerId: customer?.id ?? null, warehouseId, pricingType,
        paymentMethod: method, paidAmount: paid, cashAmount: cash, cardAmount: card,
        discountAmount: billDiscount,
        cashierName: user?.fullName ?? user?.username ?? undefined,
        businessId, branchId,
        items: cart.map((c) => ({ productId: c.productId, variantId: c.variantId, unitId: c.unitId, quantity: c.quantity, conversionFactor: c.conversionFactor, unitPrice: c.unitPrice, discountPercent: c.discountPercent, discountAmount: c.discountAmount, taxPercent: c.taxPercent, itemNote: c.itemNote ?? undefined }))
      });
      setCompletedInvoice(res.data);
      setShowPayment(false);
      clearCart();
    } catch {
      setError('Failed to save invoice. Please try again.');
    } finally {
      setPaymentLoading(false);
    }
  };

  // ── Hold Bill ──
  const handleHoldBill = async () => {
    if (cart.length === 0 || !warehouseId) return;
    const note = window.prompt('Hold note (optional):') ?? undefined;
    try {
      await posService.holdBill({ heldNote: note, customerId: customer?.id ?? null, warehouseId, pricingType, discountAmount: billDiscount, businessId, branchId, items: cart.map((c) => ({ productId: c.productId, variantId: c.variantId, unitId: c.unitId, quantity: c.quantity, conversionFactor: c.conversionFactor, unitPrice: c.unitPrice, discountPercent: c.discountPercent, discountAmount: c.discountAmount, taxPercent: c.taxPercent, itemNote: c.itemNote ?? undefined })) });
      clearCart();
    } catch { setError('Failed to hold bill.'); }
  };

  const handleResumeBill = (bill: SaleInvoiceDto) => {
    setCart(bill.items.map((i) => ({
      cartKey: cartKey(i.productId, i.variantId, i.unitId),
      productId: i.productId, productName: i.productName, productCode: i.productCode, barcode: '',
      variantId: i.variantId, variantName: i.variantName, variantSize: i.variantSize, variantColor: i.variantColor,
      unitId: i.unitId, unitName: i.unitName, conversionFactor: 1, quantity: i.quantity, unitPrice: i.unitPrice,
      discountPercent: i.discountPercent, discountAmount: i.discountAmount, taxPercent: i.taxPercent,
      lineTotal: i.lineTotal, itemNote: i.itemNote, availableUnits: [], availableVariants: []
    })));
    setWarehouseId(bill.warehouseId);
    setDiscountMode('amount');
    setDiscountInput(String(bill.discountAmount > 0 ? bill.discountAmount : 0));
    setShowHeld(false);
    barcodeRef.current?.focus();
  };

  const loadHeldBills = async () => {
    try { const r = await posService.getHeldBills(branchId); setHeldBills(r.data); setShowHeld(true); }
    catch { setError('Failed to load held bills.'); }
  };

  const cancelHeldBill = async (id: number) => {
    try { await posService.cancelHeldBill(id, branchId); setHeldBills((prev) => prev.filter((b) => b.id !== id)); }
    catch { setError('Failed to cancel bill.'); }
  };

  // ─── Render ───────────────────────────────────────────────────────────────

  return (
    <>
      {/* True fullscreen — h-screen, no Layout wrapper */}
      <div className="flex flex-col h-screen overflow-hidden bg-gray-50">

        {/* ══ Slim top bar ══ */}
        <div className="flex items-center justify-between px-4 py-2 bg-white border-b border-gray-200 flex-shrink-0">
          <div className="flex items-center gap-3">
            <Link to="/" className="flex items-center gap-1.5 text-gray-400 hover:text-gray-700 transition text-sm">
              ← Back
            </Link>
            <div className="w-px h-4 bg-gray-200" />
            <span className="font-bold text-gray-800">POS Billing</span>
            {cart.length > 0 && (
              <span className="px-2 py-0.5 bg-blue-100 text-blue-700 rounded-full text-xs font-semibold">
                {cart.length} item{cart.length !== 1 ? 's' : ''}
              </span>
            )}
            <span className={`px-2 py-0.5 rounded-full text-xs font-semibold ${pricingType === 'Retail' ? 'bg-green-100 text-green-700' : 'bg-purple-100 text-purple-700'}`}>
              {pricingType}
            </span>
          </div>
          <div className="flex items-center gap-2">
            <span className="hidden md:flex items-center gap-1 text-xs text-gray-400 bg-gray-100 px-2 py-1 rounded">
              <kbd className="font-mono font-semibold">F2</kbd> Scan ·
              <kbd className="font-mono font-semibold">F3</kbd> Search ·
              <kbd className="font-mono font-semibold">F4</kbd> Pay
            </span>
            <button onClick={loadHeldBills} className="px-3 py-1.5 text-xs font-semibold text-amber-700 bg-amber-50 hover:bg-amber-100 border border-amber-200 rounded-lg transition">
              ⏸ Held Bills
            </button>
            {cart.length > 0 && (
              <button onClick={clearCart} className="px-3 py-1.5 text-xs font-semibold text-red-600 bg-red-50 hover:bg-red-100 border border-red-200 rounded-lg transition">
                ✕ Clear
              </button>
            )}
          </div>
        </div>

        {/* ══ Main split: LEFT 70% + RIGHT 30% ══ */}
        <div className="flex flex-1 overflow-hidden">

        {/* ══ LEFT: Cart (70%) ══ */}
        <div className="flex flex-col bg-white border-r border-gray-200" style={{ flex: '0 0 70%', minWidth: 0 }}>

          {/* Error banner */}
          {error && (
            <div className="mx-4 mt-3 px-4 py-2.5 bg-red-50 border border-red-200 text-red-700 rounded-lg text-sm flex items-center justify-between">
              <span>{error}</span>
              <button onClick={() => setError('')} className="text-red-400 hover:text-red-600 ml-3 text-lg leading-none">×</button>
            </div>
          )}

          {/* Cart table */}
          <div className="flex-1 overflow-y-auto">
            {cart.length === 0 ? (
              <div className="flex flex-col items-center justify-center h-full text-gray-400">
                <div className="w-20 h-20 bg-gray-100 rounded-full flex items-center justify-center text-4xl mb-4">🛒</div>
                <p className="text-lg font-semibold text-gray-500">Cart is empty</p>
                <p className="text-sm mt-1">Scan a barcode or search for a product</p>
                <p className="text-xs mt-3 text-gray-300 bg-gray-100 px-3 py-1.5 rounded-full">Press F2 to focus barcode input</p>
              </div>
            ) : (
              <table className="w-full text-sm">
                <thead className="sticky top-0 z-10 bg-gray-50 border-b border-gray-200">
                  <tr className="text-xs font-semibold text-gray-500 uppercase tracking-wide">
                    <th className="text-left px-4 py-2.5 w-8">#</th>
                    <th className="text-left px-4 py-2.5">Product</th>
                    <th className="text-left px-4 py-2.5 w-20">Unit</th>
                    <th className="text-center px-4 py-2.5 w-32">Qty</th>
                    <th className="text-right px-4 py-2.5 w-24">Price</th>
                    <th className="text-right px-4 py-2.5 w-24">Disc %</th>
                    <th className="text-right px-4 py-2.5 w-28">Total</th>
                    <th className="w-10 px-2"></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {cart.map((item, idx) => (
                    <CartRow
                      key={item.cartKey}
                      item={item}
                      idx={idx}
                      onUpdateQty={updateQuantity}
                      onUpdateDiscount={updateItemDiscount}
                      onRemove={removeFromCart}
                    />
                  ))}
                </tbody>
              </table>
            )}
          </div>

          {/* Footer totals */}
          <div className="border-t border-gray-200 bg-gray-50 px-5 py-3">
            <div className="flex items-center justify-end gap-6 text-sm">
              <span className="text-gray-500">Subtotal: <span className="font-semibold text-gray-800">{fmt(subTotal)}</span></span>
              {totalItemDiscount > 0 && <span className="text-red-500">Item Disc: <span className="font-semibold">−{fmt(totalItemDiscount)}</span></span>}
              {billDiscount > 0 && <span className="text-orange-500">Bill Disc<span className="ml-1 text-orange-400">({discountMode === 'percent' ? `${discountRaw}%` : `${billDiscountPercent.toFixed(1)}%`})</span>: <span className="font-semibold">−{fmt(billDiscount)}</span></span>}
              {totalTax > 0 && <span className="text-gray-500">Tax: <span className="font-semibold">+{fmt(totalTax)}</span></span>}
              <div className="flex items-center gap-2 ml-2 pl-4 border-l border-gray-300">
                <span className="text-gray-600 font-medium">Grand Total</span>
                <span className="text-2xl font-black text-blue-600 tabular-nums">{fmt(grandTotal)}</span>
              </div>
            </div>
          </div>
        </div>

        {/* ══ RIGHT: Controls (30%) — top-to-bottom, actions pinned at bottom ══ */}
        <div className="flex flex-col bg-white overflow-hidden" style={{ flex: '0 0 30%', alignItems: 'stretch' }}>

          {/* ── Barcode scanner ── */}
          <div className="px-4 pt-3 pb-2 border-b border-gray-100">
            <p className="text-[11px] font-semibold text-gray-400 uppercase tracking-wide mb-1">
              Barcode <span className="font-normal normal-case text-gray-300">[F2]</span>
            </p>
            <form onSubmit={handleBarcodeSubmit} className="flex gap-2">
              <input
                ref={barcodeRef}
                type="text"
                value={barcodeInput}
                onChange={(e) => setBarcodeInput(e.target.value)}
                placeholder="Scan or type barcode…"
                autoComplete="off"
                disabled={barcodeLoading}
                className="flex-1 px-3 py-2 border border-gray-300 rounded-lg text-sm text-gray-800 placeholder-gray-400 focus:outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100 transition"
              />
              <button
                type="submit"
                disabled={barcodeLoading || !barcodeInput.trim()}
                className="px-4 py-2 bg-blue-600 hover:bg-blue-700 disabled:opacity-40 text-white rounded-lg text-sm font-bold transition"
              >
                {barcodeLoading ? '…' : '→'}
              </button>
            </form>
            {barcodeError && (
              <p className="mt-1 text-red-500 text-xs">⚠ {barcodeError}</p>
            )}
          </div>

          {/* ── Product search ── */}
          <div className="px-4 py-2 border-b border-gray-100 relative">
            <p className="text-[11px] font-semibold text-gray-400 uppercase tracking-wide mb-1">
              Search <span className="font-normal normal-case text-gray-300">[F3]</span>
            </p>
            <div className="relative">
              <input
                ref={searchRef}
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Name / code / SKU…"
                autoComplete="off"
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm text-gray-800 placeholder-gray-400 focus:outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100 transition"
              />
              {searchLoading && (
                <span className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 text-xs animate-pulse">…</span>
              )}
            </div>

            {/* ── Grouped search dropdown ── */}
            {searchResults.length > 0 && (
              <div className="absolute left-0 right-0 z-30 mt-1 bg-white border border-gray-200 rounded-xl shadow-xl overflow-hidden"
                   style={{ top: '100%', maxHeight: '60vh', overflowY: 'auto' }}>
                {searchResults.map((group) => {
                  const isExpanded = expandedGroups.has(group.productId);
                  const closeSearch = () => { setSearchQuery(''); setSearchResults([]); setExpandedGroups(new Set()); barcodeRef.current?.focus(); };

                  return (
                    <div key={group.productId} className="border-b border-gray-100 last:border-0">
                      {/* Product header row */}
                      <div
                        className={`flex items-center gap-2 px-3 py-2 cursor-pointer transition ${group.isVariantEnabled ? 'hover:bg-gray-50' : 'hover:bg-blue-50'}`}
                        onClick={() => {
                          if (group.isVariantEnabled) {
                            setExpandedGroups(prev => {
                              const next = new Set(prev);
                              next.has(group.productId) ? next.delete(group.productId) : next.add(group.productId);
                              return next;
                            });
                          } else {
                            addToCart(groupRowToLookup(group, null, pricingType));
                            closeSearch();
                          }
                        }}
                      >
                        <div className="flex-1 min-w-0">
                          <p className="text-sm font-semibold text-gray-800 truncate">{group.productName}</p>
                          <div className="flex items-center gap-2 mt-0.5">
                            {group.categoryName && <span className="text-[10px] text-gray-400">{group.categoryName}</span>}
                            {group.brandName    && <span className="text-[10px] text-gray-400">· {group.brandName}</span>}
                            <span className="text-[10px] text-gray-400">· {group.productCode}</span>
                          </div>
                        </div>
                        <div className="flex items-center gap-2 flex-shrink-0">
                          {!group.isVariantEnabled && (
                            <>
                              <span className="text-xs font-bold text-blue-600">
                                {fmt(pricingType === 'Wholesale' ? group.wholesalePrice : group.retailPrice)}
                              </span>
                              <StockBadge qty={group.stock} />
                            </>
                          )}
                          {group.isVariantEnabled && (
                            <div className="flex items-center gap-1">
                              <span className="text-[10px] text-gray-400">{group.variants.length} variants</span>
                              <span className="text-gray-400 text-xs">{isExpanded ? '▲' : '▼'}</span>
                            </div>
                          )}
                        </div>
                      </div>

                      {/* Variant rows — shown when expanded */}
                      {group.isVariantEnabled && isExpanded && (
                        <div className="bg-gray-50 border-t border-gray-100">
                          {group.variants.length === 0 ? (
                            <p className="px-6 py-2 text-xs text-gray-400 italic">No active variants</p>
                          ) : (
                            group.variants.map((v) => (
                              <button
                                key={v.variantId}
                                onClick={() => {
                                  addToCart(groupRowToLookup(group, v, pricingType));
                                  closeSearch();
                                }}
                                className="w-full flex items-center gap-2 px-6 py-2 hover:bg-blue-50 transition text-left border-b border-gray-100 last:border-0"
                              >
                                <span className="text-xs text-gray-500 mr-1">└</span>
                                <div className="flex-1 min-w-0">
                                  <p className="text-sm font-medium text-gray-800 truncate">{v.variantName}</p>
                                  <div className="flex items-center gap-2 mt-0.5">
                                    {v.size  && <span className="text-[10px] text-gray-400">Size: {v.size}</span>}
                                    {v.color && <span className="text-[10px] text-gray-400">Color: {v.color}</span>}
                                    {v.sku   && <span className="text-[10px] text-gray-400">SKU: {v.sku}</span>}
                                  </div>
                                </div>
                                <div className="flex items-center gap-2 flex-shrink-0">
                                  <span className="text-xs font-bold text-blue-600">
                                    {fmt(pricingType === 'Wholesale' ? v.wholesalePrice : v.retailPrice)}
                                  </span>
                                  <StockBadge qty={v.stock} />
                                </div>
                              </button>
                            ))
                          )}
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            )}
          </div>

          {/* ── Warehouse + Pricing (single row each) ── */}
          <div className="px-4 py-2 border-b border-gray-100 space-y-2">
            <div className="flex items-center gap-2">
              <label className="text-[11px] font-semibold text-gray-400 uppercase tracking-wide w-20 flex-shrink-0">
                Warehouse<span className="text-red-400">*</span>
              </label>
              <select
                value={warehouseId}
                onChange={(e) => setWarehouseId(Number(e.target.value))}
                className="flex-1 px-2 py-1.5 border border-gray-300 rounded-lg text-sm text-gray-800 focus:outline-none focus:border-blue-500 transition bg-white"
              >
                <option value={0}>Select…</option>
                {warehouses.map((w) => <option key={w.id} value={w.id}>{w.name}</option>)}
              </select>
            </div>
            <div className="flex items-center gap-2">
              <label className="text-[11px] font-semibold text-gray-400 uppercase tracking-wide w-20 flex-shrink-0">Pricing</label>
              <div className="flex flex-1 gap-1">
                {(['Retail', 'Wholesale'] as const).map((pt) => (
                  <button key={pt} onClick={() => setPricingType(pt)}
                    className={`flex-1 py-1.5 rounded-lg text-xs font-semibold transition border ${pricingType === pt ? 'bg-blue-600 border-blue-600 text-white' : 'bg-white border-gray-200 text-gray-600 hover:border-blue-300'}`}>
                    {pt}
                  </button>
                ))}
              </div>
            </div>
          </div>

          {/* ── Customer ── */}
          <div className="px-4 py-2 border-b border-gray-100">
            <p className="text-[11px] font-semibold text-gray-400 uppercase tracking-wide mb-1">Customer</p>
            {customer ? (
              <div className="flex items-center gap-2 bg-blue-50 border border-blue-200 rounded-lg px-3 py-1.5">
                <div className="w-6 h-6 bg-blue-600 rounded-full flex items-center justify-center text-white text-xs font-bold flex-shrink-0">
                  {customer.name.charAt(0).toUpperCase()}
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-semibold text-gray-800 truncate leading-tight">{customer.name}</p>
                  {customer.phone && <p className="text-xs text-gray-400 leading-tight">{customer.phone}</p>}
                </div>
                <button onClick={() => { setCustomer(null); setCustomerQuery(''); }} className="text-gray-300 hover:text-red-500 text-lg leading-none transition">×</button>
              </div>
            ) : (
              <div className="relative">
                <input
                  type="text"
                  value={customerQuery}
                  onChange={(e) => setCustomerQuery(e.target.value)}
                  placeholder="Name or phone…"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm text-gray-800 placeholder-gray-400 focus:outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100 transition"
                />
                {customerResults.length > 0 && (
                  <div className="absolute left-0 right-0 top-full mt-1 border border-gray-200 rounded-xl overflow-hidden bg-white shadow-lg z-10 max-h-32 overflow-y-auto">
                    {customerResults.map((c) => (
                      <button key={c.id} onClick={() => { setCustomer(c); setCustomerQuery(''); setCustomerResults([]); }}
                        className="w-full text-left px-3 py-2 hover:bg-blue-50 transition border-b border-gray-100 last:border-0">
                        <p className="text-sm font-medium text-gray-800">{c.name}</p>
                        {c.phone && <p className="text-xs text-gray-400">{c.phone}</p>}
                      </button>
                    ))}
                  </div>
                )}
              </div>
            )}
          </div>

          {/* ── Bill Discount — % and Amount toggle ── */}
          <div className="px-4 py-2 border-b border-gray-100">
            <div className="flex items-center justify-between mb-1.5">
              <p className="text-[11px] font-semibold text-gray-400 uppercase tracking-wide">Bill Discount</p>
              {/* Toggle: Percent / Amount */}
              <div className="flex border border-gray-200 rounded-lg overflow-hidden text-xs">
                <button
                  onClick={() => { setDiscountMode('percent'); setDiscountInput('0'); }}
                  className={`px-3 py-1 font-semibold transition ${discountMode === 'percent' ? 'bg-blue-600 text-white' : 'bg-white text-gray-500 hover:bg-gray-50'}`}
                >
                  %
                </button>
                <button
                  onClick={() => { setDiscountMode('amount'); setDiscountInput('0'); }}
                  className={`px-3 py-1 font-semibold transition border-l border-gray-200 ${discountMode === 'amount' ? 'bg-blue-600 text-white' : 'bg-white text-gray-500 hover:bg-gray-50'}`}
                >
                  Amt
                </button>
              </div>
            </div>

            {/* Quick preset % buttons */}
            <div className="flex gap-1 mb-1.5">
              {[0, 5, 10, 15, 20].map((d) => {
                const active = discountMode === 'percent' && parseFloat(discountInput) === d;
                return (
                  <button key={d}
                    onClick={() => { setDiscountMode('percent'); setDiscountInput(String(d)); }}
                    className={`flex-1 py-1 rounded-md text-xs font-semibold transition border ${active ? 'bg-blue-600 border-blue-600 text-white' : 'bg-white border-gray-200 text-gray-500 hover:border-blue-300 hover:text-blue-600'}`}>
                    {d === 0 ? '—' : `${d}%`}
                  </button>
                );
              })}
            </div>

            {/* Input field + derived value */}
            <div className="flex items-center gap-2">
              <div className="flex-1 relative">
                <input
                  type="number"
                  value={discountInput}
                  min={0}
                  max={discountMode === 'percent' ? 100 : undefined}
                  onChange={(e) => setDiscountInput(e.target.value)}
                  className="w-full px-3 py-2 pr-9 border border-gray-300 rounded-lg text-sm text-gray-800 focus:outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100 transition"
                />
                <span className="absolute right-3 top-1/2 -translate-y-1/2 text-xs font-semibold text-gray-400 pointer-events-none">
                  {discountMode === 'percent' ? '%' : 'Rs'}
                </span>
              </div>
              {/* Show derived value */}
              <div className="text-right w-20 flex-shrink-0">
                {billDiscount > 0 && discountMode === 'percent' && (
                  <p className="text-xs font-semibold text-orange-500 tabular-nums">−{fmt(billDiscount)}</p>
                )}
                {billDiscount > 0 && discountMode === 'amount' && subTotal > 0 && (
                  <p className="text-xs font-semibold text-orange-500 tabular-nums">{billDiscountPercent.toFixed(1)}%</p>
                )}
                {billDiscount <= 0 && <p className="text-xs text-gray-300">No disc.</p>}
              </div>
            </div>
          </div>

          {/* ── Summary totals ── */}
          <div className="px-4 py-2 border-b border-gray-100 space-y-1 text-sm">
            <div className="flex justify-between text-gray-500 text-xs">
              <span>Subtotal</span>
              <span className="tabular-nums">{fmt(subTotal)}</span>
            </div>
            {totalItemDiscount > 0 && (
              <div className="flex justify-between text-red-500 text-xs">
                <span>Item Discounts</span>
                <span className="tabular-nums">−{fmt(totalItemDiscount)}</span>
              </div>
            )}
            {billDiscount > 0 && (
              <div className="flex justify-between text-orange-500 text-xs">
                <span>Bill Discount {discountMode === 'percent' ? `(${discountRaw}%)` : `(${billDiscountPercent.toFixed(1)}%)`}</span>
                <span className="tabular-nums">−{fmt(billDiscount)}</span>
              </div>
            )}
            {totalTax > 0 && (
              <div className="flex justify-between text-gray-500 text-xs">
                <span>Tax</span>
                <span className="tabular-nums">+{fmt(totalTax)}</span>
              </div>
            )}
            <div className="flex justify-between items-center pt-1.5 border-t border-gray-200">
              <span className="font-bold text-gray-700 text-sm">Grand Total</span>
              <span className="text-xl font-black text-blue-600 tabular-nums">{fmt(grandTotal)}</span>
            </div>
          </div>

          {/* ── Action buttons — pushed to the bottom ── */}
          <div className="mt-auto px-4 py-3 space-y-2 border-t border-gray-100">
            <button
              onClick={() => setShowPayment(true)}
              disabled={cart.length === 0 || !warehouseId}
              className="w-full py-3.5 rounded-xl bg-blue-600 hover:bg-blue-700 active:bg-blue-800 disabled:opacity-40 disabled:cursor-not-allowed text-white font-bold text-base shadow-sm transition flex items-center justify-center gap-2"
            >
              💳 Checkout
              <kbd className="text-xs font-normal bg-blue-500 px-1.5 py-0.5 rounded opacity-80">[F4]</kbd>
            </button>
            <div className="flex gap-2">
              <button onClick={handleHoldBill} disabled={cart.length === 0 || !warehouseId}
                className="flex-1 py-2 rounded-lg border border-amber-200 bg-amber-50 hover:bg-amber-100 disabled:opacity-40 text-amber-700 font-semibold text-sm transition">
                ⏸ Hold
              </button>
              <button onClick={clearCart} disabled={cart.length === 0}
                className="flex-1 py-2 rounded-lg border border-red-200 bg-red-50 hover:bg-red-100 disabled:opacity-40 text-red-600 font-semibold text-sm transition">
                ✕ Cancel
              </button>
            </div>
          </div>
        </div>  {/* end right panel */}
        </div>  {/* end split row */}
      </div>    {/* end h-screen */}

      {/* ══ Modals ══ */}
      {showPayment && (
        <PaymentModal grandTotal={grandTotal} onClose={() => setShowPayment(false)} onConfirm={handlePaymentConfirm} loading={paymentLoading} />
      )}
      {showHeld && (
        <HeldBillsModal bills={heldBills} onResume={handleResumeBill} onCancel={cancelHeldBill} onClose={() => setShowHeld(false)} />
      )}
      {completedInvoice && (
        <ReceiptModal invoice={completedInvoice} onClose={() => setCompletedInvoice(null)} onNewSale={() => { setCompletedInvoice(null); barcodeRef.current?.focus(); }} />
      )}
    </>
  );
};

// ─── Stock Badge ──────────────────────────────────────────────────────────────

const StockBadge: React.FC<{ qty: number }> = ({ qty }) => {
  if (qty <= 0) return <span className="text-[10px] font-semibold px-1.5 py-0.5 rounded-full bg-red-100 text-red-600">Out</span>;
  if (qty <= 5) return <span className="text-[10px] font-semibold px-1.5 py-0.5 rounded-full bg-amber-100 text-amber-700">{qty}</span>;
  return <span className="text-[10px] font-semibold px-1.5 py-0.5 rounded-full bg-green-100 text-green-700">{qty}</span>;
};

// ─── Cart Row ─────────────────────────────────────────────────────────────────

interface CartRowProps {
  item: CartItem;
  idx: number;
  onUpdateQty: (key: string, qty: number) => void;
  onUpdateDiscount: (key: string, percent: number) => void;
  onRemove: (key: string) => void;
}

const CartRow: React.FC<CartRowProps> = React.memo(({ item, idx, onUpdateQty, onUpdateDiscount, onRemove }) => {
  const [editingQty, setEditingQty] = useState(false);
  const [editingDisc, setEditingDisc] = useState(false);
  const [qtyInput, setQtyInput] = useState(String(item.quantity));
  const [discInput, setDiscInput] = useState(String(item.discountPercent));
  const qtyRef = useRef<HTMLInputElement>(null);

  const handleQtyClick = () => {
    setEditingQty(true);
    setQtyInput(String(item.quantity));
    setTimeout(() => { qtyRef.current?.focus(); qtyRef.current?.select(); }, 0);
  };

  const commitQty = () => {
    const v = parseFloat(qtyInput);
    if (!isNaN(v)) onUpdateQty(item.cartKey, v);
    setEditingQty(false);
  };

  const commitDisc = () => {
    const v = parseFloat(discInput);
    if (!isNaN(v) && v >= 0 && v <= 100) onUpdateDiscount(item.cartKey, v);
    setEditingDisc(false);
  };

  const variantLabel = item.variantName ?? [item.variantSize, item.variantColor].filter(Boolean).join(' / ');

  return (
    <tr className="hover:bg-blue-50/40 transition group">
      <td className="px-4 py-2.5 text-gray-400 text-xs">{idx + 1}</td>
      <td className="px-4 py-2.5">
        <p className="font-semibold text-gray-800 text-sm leading-tight">{item.productName}</p>
        {variantLabel && <p className="text-xs text-purple-600 mt-0.5">{variantLabel}</p>}
        {item.productCode && <p className="text-xs text-gray-400">{item.productCode}</p>}
      </td>
      <td className="px-4 py-2.5 text-gray-500 text-xs">{item.unitName}</td>

      {/* Quantity */}
      <td className="px-4 py-2.5">
        <div className="flex items-center justify-center gap-1">
          <button onClick={() => onUpdateQty(item.cartKey, item.quantity - 1)}
            className="w-6 h-6 flex items-center justify-center bg-gray-100 hover:bg-gray-200 text-gray-600 rounded-md text-sm font-bold transition">
            −
          </button>
          {editingQty ? (
            <input ref={qtyRef} type="number" value={qtyInput}
              onChange={(e) => setQtyInput(e.target.value)}
              onBlur={commitQty}
              onKeyDown={(e) => { if (e.key === 'Enter') commitQty(); if (e.key === 'Escape') setEditingQty(false); }}
              className="w-14 text-center border-2 border-blue-400 rounded-lg text-sm py-0.5 text-gray-800 focus:outline-none bg-white"
            />
          ) : (
            <span onClick={handleQtyClick}
              className="w-10 text-center font-bold text-gray-700 text-sm cursor-pointer hover:bg-gray-100 rounded py-0.5 tabular-nums">
              {item.quantity}
            </span>
          )}
          <button onClick={() => onUpdateQty(item.cartKey, item.quantity + 1)}
            className="w-6 h-6 flex items-center justify-center bg-gray-100 hover:bg-gray-200 text-gray-600 rounded-md text-sm font-bold transition">
            +
          </button>
        </div>
      </td>

      {/* Price */}
      <td className="px-4 py-2.5 text-right text-gray-700 text-sm tabular-nums">{fmt(item.unitPrice)}</td>

      {/* Discount */}
      <td className="px-4 py-2.5 text-right">
        {editingDisc ? (
          <input type="number" value={discInput}
            onChange={(e) => setDiscInput(e.target.value)}
            onBlur={commitDisc}
            onKeyDown={(e) => { if (e.key === 'Enter') commitDisc(); if (e.key === 'Escape') setEditingDisc(false); }}
            autoFocus
            className="w-16 text-right border-2 border-orange-400 rounded-lg text-sm py-0.5 px-1 text-gray-800 focus:outline-none bg-white"
          />
        ) : (
          <span onClick={() => { setEditingDisc(true); setDiscInput(String(item.discountPercent)); }}
            className={`cursor-pointer text-sm px-1.5 py-0.5 rounded hover:bg-gray-100 tabular-nums ${item.discountPercent > 0 ? 'text-orange-600 font-semibold' : 'text-gray-300 hover:text-gray-500'}`}>
            {item.discountPercent > 0 ? `${item.discountPercent}%` : '—'}
          </span>
        )}
      </td>

      {/* Line total */}
      <td className="px-4 py-2.5 text-right font-bold text-gray-800 text-sm tabular-nums">{fmt(item.lineTotal)}</td>

      {/* Remove */}
      <td className="px-2 py-2.5 text-right">
        <button onClick={() => onRemove(item.cartKey)}
          className="opacity-0 group-hover:opacity-100 w-7 h-7 flex items-center justify-center text-red-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition text-base">
          ×
        </button>
      </td>
    </tr>
  );
});

CartRow.displayName = 'CartRow';

export default POSBillingPage;
