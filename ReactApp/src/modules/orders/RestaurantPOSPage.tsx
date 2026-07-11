import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { useBusinessCurrency } from '../../hooks/useBusinessCurrency';
import { useDebounce } from '../../hooks/useDebounce';
import { useHasFeature } from '../../hooks/useFeature';
import { FEATURE_KEYS } from '../../types/featurePermissions';
import { getApiErrorMessage } from '../../services/api';
import { ReceiptPrintModal } from '../../components/receipt';
import POSPaymentModal from '../pos/components/POSPaymentModal';
import { usePOSBillingSetup } from '../pos/hooks/usePOSBillingSetup';
import { usePOSCart } from '../pos/hooks/usePOSCart';
import { usePOSBillingTotals } from '../pos/hooks/usePOSBillingTotals';
import {
  posService,
  type PosSearchGroup,
  type SaleInvoiceDto,
  groupRowToLookup,
  validateCartStock,
} from '../pos/posService';
import CategoriesSidebar from './components/CategoriesSidebar';
import ProductsGrid from './components/ProductsGrid';
import OrderPanel from './components/OrderPanel';
import { useRestaurantCategories } from './hooks/useRestaurantCategories';
import { useCategoryProducts } from './hooks/useCategoryProducts';
import { useCategoryProductCounts } from './hooks/useCategoryProductCounts';
import { POS_INTERACTION, POS_THEME } from './theme';
import {
  type OrderType,
  type VariantPickerState,
  loadLocalHeldOrders,
  saveLocalHeldOrder,
  removeLocalHeldOrder,
  type LocalHeldOrder,
} from './localHeldOrders';

const ORDER_TYPES: OrderType[] = ['Dine-in', 'Takeaway', 'Delivery'];

const RestaurantPOSPage: React.FC = () => {
  const { fmt } = useBusinessCurrency();
  const variantFeatureEnabled = useHasFeature(FEATURE_KEYS.VARIANT);
  const stockFeatureEnabled = useHasFeature(FEATURE_KEYS.STOCK);
  const productSearchRef = useRef<HTMLInputElement>(null);

  const {
    user,
    businessId,
    effectiveBranchId,
    branchId,
    warehouseId,
    pricingType,
    customer,
    resetCustomer,
    error,
    setError,
  } = usePOSBillingSetup();

  const {
    cart,
    setCart,
    addToCart,
    updateQuantity,
    removeFromCart,
    clearCart,
    cartStockError,
  } = usePOSCart({ pricingType, stockFeatureEnabled });

  const [selectedCategoryId, setSelectedCategoryId] = useState<number | null>(null);
  const [orderType, setOrderType] = useState<OrderType>('Dine-in');
  const [discountMode] = useState<'percent' | 'amount'>('percent');
  const [discountInput, setDiscountInput] = useState('0');
  const [showPayment, setShowPayment] = useState(false);
  const [paymentLoading, setPaymentLoading] = useState(false);
  const [completedInvoice, setCompletedInvoice] = useState<SaleInvoiceDto | null>(null);
  const [addingProductId, setAddingProductId] = useState<number | null>(null);
  const [variantPicker, setVariantPicker] = useState<VariantPickerState | null>(null);
  const [showHeld, setShowHeld] = useState(false);
  const [localHeldOrders, setLocalHeldOrders] = useState<LocalHeldOrder[]>([]);
  const [heldCount, setHeldCount] = useState(0);
  const [productError, setProductError] = useState('');
  const [categorySearch, setCategorySearch] = useState('');
  const [productSearch, setProductSearch] = useState('');

  const debouncedProductSearch = useDebounce(productSearch, 200);

  const activeBranchId = effectiveBranchId > 0 ? effectiveBranchId : branchId;

  const { categories, loading: categoriesLoading, error: categoriesError } =
    useRestaurantCategories(activeBranchId);

  const categoryIds = useMemo(() => categories.map((c) => c.id), [categories]);
  const categoryCounts = useCategoryProductCounts(activeBranchId, categoryIds);

  const { products, loading: productsLoading, error: productsLoadError, resolveProductGroup } =
    useCategoryProducts(activeBranchId, selectedCategoryId, warehouseId);

  const { subTotal, totalTax, grandTotal, billDiscount } = usePOSBillingTotals({
    cart,
    discountMode,
    discountInput,
  });

  const filteredCategories = useMemo(() => {
    const q = categorySearch.trim().toLowerCase();
    if (!q) return categories;
    return categories.filter((c) => c.name.toLowerCase().includes(q));
  }, [categories, categorySearch]);

  const filteredProducts = useMemo(() => {
    const q = debouncedProductSearch.trim().toLowerCase();
    if (!q) return products;
    return products.filter(
      (p) => p.name.toLowerCase().includes(q) || p.code.toLowerCase().includes(q),
    );
  }, [products, debouncedProductSearch]);

  const selectedCategoryName = useMemo(
    () => categories.find((c) => c.id === selectedCategoryId)?.name ?? 'Products',
    [categories, selectedCategoryId],
  );

  const formatPrice = useCallback((price: number) => fmt(price), [fmt]);

  useEffect(() => {
    if (categories.length > 0 && selectedCategoryId == null) {
      setSelectedCategoryId(categories[0].id);
    }
  }, [categories, selectedCategoryId]);

  useEffect(() => {
    setHeldCount(loadLocalHeldOrders(activeBranchId).length);
  }, [activeBranchId]);

  useEffect(() => {
    setProductSearch('');
  }, [selectedCategoryId]);

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      const target = e.target as HTMLElement;
      const isInput = target.tagName === 'INPUT' || target.tagName === 'TEXTAREA';
      if (e.key === '/' && !isInput) {
        e.preventDefault();
        productSearchRef.current?.focus();
      }
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, []);

  const handleIncreaseQty = useCallback(
    (key: string) => {
      const item = cart.find((c) => c.cartKey === key);
      if (!item) return;
      updateQuantity(key, item.quantity + 1);
    },
    [cart, updateQuantity],
  );

  const handleDecreaseQty = useCallback(
    (key: string) => {
      const item = cart.find((c) => c.cartKey === key);
      if (!item) return;
      updateQuantity(key, item.quantity - 1);
    },
    [cart, updateQuantity],
  );

  const addGroupToCart = useCallback(
    (group: PosSearchGroup, variantId?: number | null) => {
      const variant =
        variantId != null
          ? group.variants.find((v) => v.variantId === variantId) ?? null
          : group.isVariantEnabled && group.variants.length === 1
            ? group.variants[0]
            : null;

      if (group.isVariantEnabled && group.variants.length > 0 && !variant) {
        setVariantPicker({ group, productName: group.productName });
        return;
      }

      const lookup = groupRowToLookup(group, variant, pricingType);
      const result = addToCart(lookup);
      if (!result.success) {
        setProductError(result.error ?? 'Cannot add this product.');
        return;
      }
      setProductError('');
      setVariantPicker(null);
    },
    [addToCart, pricingType],
  );

  const handleProductClick = useCallback(
    async (productId: number) => {
      const product = products.find((p) => p.id === productId);
      if (!product || !warehouseId) return;

      setAddingProductId(productId);
      setProductError('');
      try {
        const group = await resolveProductGroup(product);
        if (!group) {
          setProductError('Product not available for sale.');
          return;
        }
        addGroupToCart(group);
      } catch {
        setProductError('Failed to add product.');
      } finally {
        setAddingProductId(null);
      }
    },
    [products, warehouseId, resolveProductGroup, addGroupToCart],
  );

  const handleCategorySearchChange = useCallback((value: string) => {
    setCategorySearch(value);
  }, []);

  const handleProductSearchChange = useCallback((value: string) => {
    setProductSearch(value);
  }, []);

  const handleSelectCategory = useCallback((id: number) => {
    setSelectedCategoryId(id);
  }, []);

  const openPayment = useCallback(() => {
    if (cart.length === 0 || !warehouseId) return;
    if (cartStockError) {
      setError(cartStockError);
      return;
    }
    setError('');
    setShowPayment(true);
  }, [cart.length, warehouseId, cartStockError, setError]);

  const handlePaymentConfirm = async (
    method: 'Cash' | 'Card' | 'Mixed' | 'Credit',
    paid: number,
    cash: number,
    card: number,
  ) => {
    if (!warehouseId || activeBranchId <= 0) return;

    const stockErr = stockFeatureEnabled ? validateCartStock(cart) : null;
    if (stockErr) {
      setError(stockErr);
      setShowPayment(false);
      return;
    }

    const missingVariant = variantFeatureEnabled
      ? cart.find((c) => c.availableVariants.length > 0 && (c.variantId == null || c.variantId <= 0))
      : undefined;
    if (missingVariant) {
      setError(`"${missingVariant.productName}" requires a variant selection.`);
      setShowPayment(false);
      return;
    }

    setPaymentLoading(true);
    try {
      const isCreditSale = method === 'Credit';
      const res = await posService.createInvoice({
        customerId: customer?.id ?? null,
        warehouseId,
        pricingType,
        paymentMethod: isCreditSale ? 'Cash' : method,
        paidAmount: paid,
        cashAmount: cash,
        cardAmount: card,
        isCreditSale,
        discountAmount: billDiscount,
        notes: `Order Type: ${orderType}`,
        cashierName: user?.fullName ?? user?.username ?? undefined,
        businessId,
        branchId: activeBranchId,
        items: cart.map((c) => ({
          productId: c.productId,
          variantId: c.variantId,
          unitId: c.unitId,
          quantity: c.quantity,
          conversionFactor: c.conversionFactor,
          unitPrice: c.unitPrice,
          discountPercent: c.discountPercent,
          discountAmount: c.discountAmount,
          taxPercent: c.taxPercent,
          itemNote: c.itemNote ?? undefined,
        })),
      });
      setCompletedInvoice(res.data);
      setShowPayment(false);
      clearCart();
      setDiscountInput('0');
      resetCustomer();
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to save invoice. Please try again.'));
      setShowPayment(false);
    } finally {
      setPaymentLoading(false);
    }
  };

  const handleHoldOrder = useCallback(() => {
    if (cart.length === 0 || !warehouseId) return;
    const order: LocalHeldOrder = {
      id: `held-${Date.now()}`,
      heldAt: new Date().toISOString(),
      orderType,
      cart,
      discountMode,
      discountInput,
      pricingType,
      warehouseId,
      customerId: customer?.id ?? null,
      customerName: customer?.name ?? null,
    };
    saveLocalHeldOrder(activeBranchId, order);
    setHeldCount(loadLocalHeldOrders(activeBranchId).length);
    clearCart();
    setDiscountInput('0');
    resetCustomer();
    setError('');
  }, [
    cart,
    warehouseId,
    orderType,
    discountMode,
    discountInput,
    pricingType,
    customer,
    activeBranchId,
    clearCart,
    resetCustomer,
    setError,
  ]);

  const openHeldOrders = useCallback(() => {
    const orders = loadLocalHeldOrders(activeBranchId);
    setLocalHeldOrders(orders);
    setHeldCount(orders.length);
    setShowHeld(true);
  }, [activeBranchId]);

  const resumeHeldOrder = (order: LocalHeldOrder) => {
    setCart(order.cart);
    setOrderType(order.orderType);
    setDiscountInput(order.discountInput);
    removeLocalHeldOrder(activeBranchId, order.id);
    const remaining = loadLocalHeldOrders(activeBranchId);
    setLocalHeldOrders(remaining);
    setHeldCount(remaining.length);
    setShowHeld(false);
  };

  const deleteHeldOrder = (id: string) => {
    removeLocalHeldOrder(activeBranchId, id);
    const remaining = loadLocalHeldOrders(activeBranchId);
    setLocalHeldOrders(remaining);
    setHeldCount(remaining.length);
  };

  const cartItemCount = useMemo(
    () => cart.reduce((sum, item) => sum + item.quantity, 0),
    [cart],
  );

  const bannerError = error || categoriesError || productsLoadError || productError;

  return (
    <>
      <div className="flex flex-col h-screen overflow-hidden" style={{ backgroundColor: POS_THEME.background }}>
        <header className="flex-shrink-0 bg-white border-b border-gray-200 px-4 py-3">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div className="flex items-center gap-3">
              <Link
                to="/"
                className="text-sm text-gray-600 hover:text-gray-900"
              >
                ← Back
              </Link>
              <div className="w-px h-5 bg-gray-200" />
              <div>
                <h1 className="text-base font-semibold text-gray-900">Restaurant POS</h1>
                <p className="text-xs text-gray-500 hidden sm:block">Press / to search products</p>
              </div>
              {cartItemCount > 0 && (
                <span
                  className="px-2 py-1 rounded-lg text-xs font-medium text-white"
                  style={{ backgroundColor: POS_THEME.accent }}
                >
                  {cartItemCount} items
                </span>
              )}
            </div>

            <div className="flex items-center gap-1 p-1 rounded-xl border border-gray-200 bg-white">
              {ORDER_TYPES.map((type) => (
                <button
                  key={type}
                  type="button"
                  onClick={() => setOrderType(type)}
                  className={`min-h-[40px] px-3 rounded-lg text-sm font-medium ${POS_INTERACTION.button} ${
                    orderType === type
                      ? 'text-white'
                      : 'text-gray-600 hover:bg-gray-50'
                  }`}
                  style={orderType === type ? { backgroundColor: POS_THEME.primary } : undefined}
                >
                  {type}
                </button>
              ))}
            </div>
          </div>
        </header>

        {bannerError && (
          <div className="flex-shrink-0 mx-4 mt-3 rounded-xl bg-red-50 border border-red-200 px-4 py-2 text-sm text-red-700">
            {bannerError}
          </div>
        )}

        <div className="flex flex-1 min-h-0 gap-3 p-3">
          <CategoriesSidebar
            categories={categories}
            filteredCategories={filteredCategories}
            categoryCounts={categoryCounts}
            selectedCategoryId={selectedCategoryId}
            categorySearch={categorySearch}
            loading={categoriesLoading}
            onCategorySearchChange={handleCategorySearchChange}
            onSelectCategory={handleSelectCategory}
          />

          <ProductsGrid
            categoryName={selectedCategoryName}
            products={products}
            filteredProducts={filteredProducts}
            productSearch={productSearch}
            loading={productsLoading}
            addingProductId={addingProductId}
            branchId={activeBranchId}
            productSearchRef={productSearchRef}
            formatPrice={formatPrice}
            onProductSearchChange={handleProductSearchChange}
            onSelectProduct={handleProductClick}
          />

          <OrderPanel
            orderType={orderType}
            cart={cart}
            cartItemCount={cartItemCount}
            subtotalLabel={fmt(subTotal)}
            totalLabel={fmt(grandTotal)}
            taxLabel={fmt(totalTax)}
            showTax={totalTax > 0}
            heldCount={heldCount}
            warehouseId={warehouseId}
            cartStockError={cartStockError}
            onIncreaseQty={handleIncreaseQty}
            onDecreaseQty={handleDecreaseQty}
            onRemove={removeFromCart}
            onHold={handleHoldOrder}
            onOpenHeld={openHeldOrders}
            onPay={openPayment}
          />
        </div>
      </div>

      {showPayment && (
        <POSPaymentModal
          grandTotal={grandTotal}
          hasCustomer={!!customer?.id}
          onClose={() => setShowPayment(false)}
          onConfirm={handlePaymentConfirm}
          loading={paymentLoading}
        />
      )}

      {variantPicker && (
        <div className="fixed inset-0 z-50 flex items-end sm:items-center justify-center bg-black/40 p-4">
          <div className="bg-white rounded-xl w-full max-w-md overflow-hidden border border-gray-200 shadow-md">
            <div className="px-4 py-3 border-b border-gray-200">
              <h3 className="font-semibold text-gray-900">{variantPicker.productName}</h3>
              <p className="text-sm text-gray-500 mt-0.5">Select a variant</p>
            </div>
            <div className="p-3 space-y-2 max-h-[60vh] overflow-y-auto">
              {variantPicker.group.variants.map((variant) => (
                <button
                  key={variant.variantId}
                  type="button"
                  onClick={() => addGroupToCart(variantPicker.group, variant.variantId)}
                  className={`w-full min-h-[48px] rounded-xl border border-gray-200 px-4 py-3 text-left text-sm font-medium text-gray-800 bg-white ${POS_INTERACTION.button}`}
                >
                  <span>{variant.variantName || variant.size || variant.color || `Variant ${variant.variantId}`}</span>
                  <span className="float-right text-gray-500 tabular-nums">
                    {fmt(pricingType === 'Wholesale' ? variant.wholesalePrice : variant.retailPrice)}
                  </span>
                </button>
              ))}
            </div>
            <div className="p-3 border-t border-gray-200">
              <button
                type="button"
                onClick={() => setVariantPicker(null)}
                className={`w-full min-h-[44px] rounded-xl border border-gray-200 text-gray-600 text-sm font-medium bg-white ${POS_INTERACTION.button}`}
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}

      {showHeld && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="bg-white rounded-xl w-full max-w-lg max-h-[80vh] flex flex-col border border-gray-200 shadow-md overflow-hidden">
            <div className="flex items-center justify-between px-4 py-3 border-b border-gray-200">
              <div>
                <h2 className="text-base font-semibold text-gray-900">Held Orders</h2>
                <p className="text-xs text-gray-500">{localHeldOrders.length} saved locally</p>
              </div>
              <button
                type="button"
                onClick={() => setShowHeld(false)}
                className="w-8 h-8 rounded-lg text-gray-500 hover:bg-gray-50"
              >
                ×
              </button>
            </div>
            <div className="overflow-y-auto flex-1 divide-y divide-gray-200">
              {localHeldOrders.length === 0 ? (
                <p className="text-center text-gray-500 text-sm py-12">No held orders</p>
              ) : (
                localHeldOrders.map((order) => (
                  <div key={order.id} className="px-4 py-3 flex items-center gap-3">
                    <div className="flex-1 min-w-0">
                      <p className="font-medium text-gray-900 text-sm">{order.orderType}</p>
                      <p className="text-xs text-gray-500 mt-0.5">
                        {order.cart.length} line{order.cart.length !== 1 ? 's' : ''} ·{' '}
                        {new Date(order.heldAt).toLocaleString()}
                      </p>
                    </div>
                    <button
                      type="button"
                      onClick={() => resumeHeldOrder(order)}
                      className={`px-3 py-2 rounded-lg text-white text-xs font-medium ${POS_INTERACTION.button}`}
                      style={{ backgroundColor: POS_THEME.primary }}
                    >
                      Resume
                    </button>
                    <button
                      type="button"
                      onClick={() => deleteHeldOrder(order.id)}
                      className={`px-3 py-2 rounded-lg border border-gray-200 text-gray-600 text-xs font-medium bg-white ${POS_INTERACTION.button}`}
                    >
                      Delete
                    </button>
                  </div>
                ))
              )}
            </div>
          </div>
        </div>
      )}

      {completedInvoice && (
        <ReceiptPrintModal
          invoice={completedInvoice}
          autoPrint
          onClose={() => setCompletedInvoice(null)}
          onNewSale={() => setCompletedInvoice(null)}
        />
      )}
    </>
  );
};

export default RestaurantPOSPage;
