import React, { useEffect, useState } from 'react';
import { usePOSStore, MenuCategory, MenuItem, CartItem, MenuItemAddon, MenuItemVariant } from '../stores/usePOSStore';
import Badge from './Badge';

const POS: React.FC = () => {
  const {
    menu,
    isMenuLoading,
    menuError,
    selectedCategory,
    cart,
    discount,
    selectedItem,
    showItemModal,
    fetchMenu,
    selectCategory,
    openItemModal,
    closeItemModal,
    addToCart,
    updateCartItem,
    removeFromCart,
    clearCart,
    setDiscount,
    getSubtotal,
    getTax,
    getGrandTotal
  } = usePOSStore();

  const [selectedVariant, setSelectedVariant] = useState<MenuItemVariant | null>(null);
  const [selectedAddons, setSelectedAddons] = useState<MenuItemAddon[]>([]);
  const [quantity, setQuantity] = useState(1);

  useEffect(() => {
    fetchMenu(1);
  }, [fetchMenu]);

  const handleAddToCart = (item: MenuItem) => {
    openItemModal(item);
    setSelectedVariant(null);
    setSelectedAddons([]);
    setQuantity(1);
  };

  const handleConfirmAddToCart = () => {
    if (selectedItem) {
      addToCart(selectedItem, quantity, selectedAddons, selectedVariant || undefined);
      closeItemModal();
    }
  };

  const handleUpdateQuantity = (id: string, qty: number) => {
    if (qty <= 0) {
      removeFromCart(id);
    } else {
      updateCartItem(id, qty);
    }
  };

  const handleCheckout = async () => {
    console.log('Processing checkout...', { cart, total: getGrandTotal() });
    // TODO: Implement checkout logic
  };

  const kpis = [
    {
      title: "Today's Sales",
      value: '$2,450.00',
      change: '+12.5%',
      changeType: 'positive' as const,
      icon: '💰'
    },
    {
      title: 'Orders Today',
      value: '48',
      change: '+8.2%',
      changeType: 'positive' as const,
      icon: '📋'
    },
    {
      title: 'Active Tables',
      value: '12',
      change: '0%',
      changeType: 'neutral' as const,
      icon: '🍽️'
    },
    {
      title: 'Avg Order Value',
      value: '$51.04',
      change: '+5.1%',
      changeType: 'positive' as const,
      icon: '📊'
    }
  ];

  const selectedCategoryData = menu.find(cat => cat.id === selectedCategory);

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Dashboard</h1>
        <p className="text-gray-600">Point of Sale Management System</p>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {kpis.map((kpi, index) => (
          <div key={index} className="bg-white rounded-lg shadow-sm border border-gray-200 p-6 hover:shadow-md transition-shadow">
            <div className="flex items-start justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">{kpi.title}</p>
                <p className="text-2xl font-bold text-gray-900 mt-2">{kpi.value}</p>
                <p className={`text-xs font-medium mt-3 ${
                  kpi.changeType === 'positive' ? 'text-green-600' :
                  kpi.changeType === 'negative' ? 'text-red-600' : 'text-gray-500'
                }`}>
                  {kpi.change} vs yesterday
                </p>
              </div>
              <div className="text-3xl">{kpi.icon}</div>
            </div>
          </div>
        ))}
      </div>

      {/* POS Terminal */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
        <div className="px-6 py-4 border-b border-gray-200 bg-gradient-to-r from-blue-50 to-blue-100">
          <h2 className="text-xl font-semibold text-gray-900">Point of Sale Terminal</h2>
          <p className="text-sm text-gray-600 mt-1">Browse menu items and manage orders</p>
        </div>

        <div className="p-6">
          {isMenuLoading && (
            <div className="mb-4 rounded-md border border-blue-200 bg-blue-50 px-4 py-3 text-sm text-blue-700">
              Loading menu from server...
            </div>
          )}

          {menuError && (
            <div className="mb-4 rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
              {menuError}
            </div>
          )}

          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* Left: Menu & Items */}
            <div className="lg:col-span-2 space-y-6">
              {/* Categories */}
              <div>
                <h3 className="text-sm font-semibold text-gray-900 mb-4">CATEGORIES</h3>
                <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-2">
                  {menu.map((category: MenuCategory) => (
                    <button
                      key={category.id}
                      onClick={() => selectCategory(category.id)}
                      className={`py-2 px-3 rounded-lg font-medium text-sm transition-colors ${
                        selectedCategory === category.id
                          ? 'bg-blue-600 text-white shadow-md'
                          : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                      }`}
                    >
                      {category.name}
                    </button>
                  ))}
                </div>
              </div>

              {/* Menu Items */}
              <div>
                <h3 className="text-sm font-semibold text-gray-900 mb-4">ITEMS</h3>
                <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
                  {selectedCategoryData?.items.map((item: MenuItem) => (
                    <div
                      key={item.id}
                      onClick={() => handleAddToCart(item)}
                      className="bg-gradient-to-br from-white to-gray-50 border border-gray-200 rounded-lg p-4 cursor-pointer hover:shadow-md hover:border-blue-300 transition-all"
                    >
                      <h4 className="font-semibold text-gray-900 text-sm mb-1 truncate">{item.name}</h4>
                      <p className="text-gray-600 text-sm mb-3">
                        <Badge variant="info" size="sm">${item.price.toFixed(2)}</Badge>
                      </p>
                      {item.variants.length > 0 && (
                        <p className="text-xs text-gray-500">{item.variants.length} variants</p>
                      )}
                      {item.addons.length > 0 && (
                        <p className="text-xs text-gray-500">{item.addons.length} addons</p>
                      )}
                    </div>
                  ))}
                </div>
                {!selectedCategoryData && (
                  <div className="flex flex-col items-center justify-center py-12 text-gray-500">
                    <svg className="w-12 h-12 mb-2 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" />
                    </svg>
                    <p>Select a category to view items</p>
                  </div>
                )}
              </div>
            </div>

            {/* Right: Cart */}
            <div className="lg:col-span-1">
              <div className="bg-gradient-to-br from-blue-50 to-blue-100 rounded-lg border border-blue-200 overflow-hidden flex flex-col h-full">
                {/* Cart Header */}
                <div className="bg-blue-600 text-white px-4 py-3">
                  <h3 className="font-semibold flex items-center gap-2">
                    <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                      <path d="M3 1a1 1 0 000 2h1.22l.305 1.222a.997.997 0 00.01.042l1.358 5.43-.893.892C3.74 11.846 4.632 14 6.414 14H15a1 1 0 000-2H6.414l1-1H14a1 1 0 00.894-.553l3-6A1 1 0 0017 6H6.28l-.31-1.243A1 1 0 005 4H3z" />
                    </svg>
                    ORDER ({cart.length})
                  </h3>
                </div>

                {/* Cart Items */}
                <div className="flex-1 overflow-y-auto p-4 space-y-3">
                  {cart.length === 0 ? (
                    <div className="flex flex-col items-center justify-center h-full text-gray-600 py-8">
                      <svg className="w-12 h-12 text-gray-400 mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z" />
                      </svg>
                      <p className="text-sm">Cart is empty</p>
                    </div>
                  ) : (
                    cart.map((item: CartItem) => (
                      <div key={item.id} className="bg-white rounded-lg p-3 border border-gray-200">
                        <div className="flex justify-between items-start mb-2">
                          <h4 className="font-semibold text-sm text-gray-900 flex-1">{item.name}</h4>
                          <button
                            onClick={() => removeFromCart(item.id)}
                            className="text-red-600 hover:text-red-800 text-lg"
                          >
                            ×
                          </button>
                        </div>
                        <p className="text-xs text-gray-600 mb-2">${item.price.toFixed(2)}</p>
                        <div className="flex items-center justify-between">
                          <div className="flex items-center gap-1">
                            <button
                              onClick={() => handleUpdateQuantity(item.id, item.quantity - 1)}
                              className="bg-gray-300 text-gray-700 w-6 h-6 rounded text-xs font-bold hover:bg-gray-400"
                            >
                              −
                            </button>
                            <span className="w-8 text-center font-semibold text-sm">{item.quantity}</span>
                            <button
                              onClick={() => handleUpdateQuantity(item.id, item.quantity + 1)}
                              className="bg-gray-300 text-gray-700 w-6 h-6 rounded text-xs font-bold hover:bg-gray-400"
                            >
                              +
                            </button>
                          </div>
                          <p className="font-bold text-sm text-blue-600">${item.total.toFixed(2)}</p>
                        </div>
                      </div>
                    ))
                  )}
                </div>

                {/* Cart Summary */}
                <div className="bg-white border-t border-blue-200 p-4 space-y-2">
                  <div className="flex justify-between text-sm">
                    <span className="text-gray-600">Subtotal:</span>
                    <span className="font-semibold">${getSubtotal().toFixed(2)}</span>
                  </div>
                  <div className="flex justify-between text-sm">
                    <span className="text-gray-600">Tax:</span>
                    <span className="font-semibold">${getTax().toFixed(2)}</span>
                  </div>
                  <div className="flex justify-between text-sm">
                    <span className="text-gray-600">Discount:</span>
                    <span className="font-semibold">-${discount.toFixed(2)}</span>
                  </div>
                  <div className="border-t pt-2 flex justify-between text-base font-bold text-blue-600">
                    <span>TOTAL:</span>
                    <span>${getGrandTotal().toFixed(2)}</span>
                  </div>
                </div>

                {/* Cart Actions */}
                <div className="p-4 space-y-2 border-t border-blue-200">
                  <button
                    onClick={clearCart}
                    disabled={cart.length === 0}
                    className="w-full py-2 bg-gray-500 text-white rounded-lg font-medium text-sm hover:bg-gray-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                  >
                    Clear
                  </button>
                  <button
                    onClick={handleCheckout}
                    disabled={cart.length === 0}
                    className="w-full py-2 bg-green-600 text-white rounded-lg font-medium text-sm hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                  >
                    Checkout
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Item Modal */}
      {showItemModal && selectedItem && (
        <>
          <div className="fixed inset-0 bg-black bg-opacity-50 z-40" onClick={closeItemModal} />
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <div className="bg-white rounded-lg shadow-xl max-w-md w-full">
              <div className="px-6 py-4 border-b border-gray-200 flex justify-between items-center">
                <h2 className="text-xl font-semibold text-gray-900">{selectedItem.name}</h2>
                <button
                  onClick={closeItemModal}
                  className="text-gray-400 hover:text-gray-600"
                >
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>

              <div className="p-6 space-y-4">
                {/* Price */}
                <div>
                  <p className="text-sm text-gray-600">Price</p>
                  <p className="text-2xl font-bold text-gray-900">${selectedItem.price.toFixed(2)}</p>
                </div>

                {/* Variants */}
                {selectedItem.variants.length > 0 && (
                  <div>
                    <label className="block text-sm font-semibold text-gray-900 mb-2">Variants</label>
                    <div className="space-y-2">
                      {selectedItem.variants.map((variant) => (
                        <label key={variant.id} className="flex items-center">
                          <input
                            type="radio"
                            name="variant"
                            checked={selectedVariant?.id === variant.id}
                            onChange={() => setSelectedVariant(variant)}
                            className="rounded"
                          />
                          <span className="ml-3 text-sm text-gray-700">
                            {variant.name} <span className="text-gray-500">(+${variant.price.toFixed(2)})</span>
                          </span>
                        </label>
                      ))}
                    </div>
                  </div>
                )}

                {/* Addons */}
                {selectedItem.addons.length > 0 && (
                  <div>
                    <label className="block text-sm font-semibold text-gray-900 mb-2">Add-ons</label>
                    <div className="space-y-2">
                      {selectedItem.addons.map((addon) => (
                        <label key={addon.id} className="flex items-center">
                          <input
                            type="checkbox"
                            checked={selectedAddons.some(a => a.id === addon.id)}
                            onChange={(e) => {
                              if (e.target.checked) {
                                setSelectedAddons([...selectedAddons, addon]);
                              } else {
                                setSelectedAddons(selectedAddons.filter(a => a.id !== addon.id));
                              }
                            }}
                            className="rounded"
                          />
                          <span className="ml-3 text-sm text-gray-700">
                            {addon.name} <span className="text-gray-500">(+${addon.price.toFixed(2)})</span>
                          </span>
                        </label>
                      ))}
                    </div>
                  </div>
                )}

                {/* Quantity */}
                <div>
                  <label className="block text-sm font-semibold text-gray-900 mb-2">Quantity</label>
                  <div className="flex items-center gap-2">
                    <button
                      onClick={() => setQuantity(Math.max(1, quantity - 1))}
                      className="bg-gray-200 text-gray-700 w-8 h-8 rounded font-bold hover:bg-gray-300"
                    >
                      −
                    </button>
                    <input
                      type="number"
                      min="1"
                      value={quantity}
                      onChange={(e) => setQuantity(Math.max(1, parseInt(e.target.value) || 1))}
                      className="w-16 text-center border border-gray-300 rounded py-2"
                    />
                    <button
                      onClick={() => setQuantity(quantity + 1)}
                      className="bg-gray-200 text-gray-700 w-8 h-8 rounded font-bold hover:bg-gray-300"
                    >
                      +
                    </button>
                  </div>
                </div>
              </div>

              {/* Actions */}
              <div className="px-6 py-4 border-t border-gray-200 flex gap-3">
                <button
                  onClick={closeItemModal}
                  className="flex-1 px-4 py-2 border border-gray-300 rounded-lg text-gray-700 font-medium hover:bg-gray-50 transition-colors"
                >
                  Cancel
                </button>
                <button
                  onClick={handleConfirmAddToCart}
                  className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-lg font-medium hover:bg-blue-700 transition-colors"
                >
                  Add to Cart
                </button>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
};

export default POS;