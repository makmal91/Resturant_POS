import React, { useEffect, useState } from 'react'
import axios from 'axios'
import { usePOSStore, MenuCategory, MenuItem, CartItem, MenuItemAddon, MenuItemVariant } from '../stores/usePOSStore'

const POS: React.FC = () => {
  const {
    menu,
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
  } = usePOSStore()

  const [selectedVariant, setSelectedVariant] = useState<MenuItemVariant | null>(null)
  const [selectedAddons, setSelectedAddons] = useState<MenuItemAddon[]>([])
  const [quantity, setQuantity] = useState(1)

  useEffect(() => {
    fetchMenu(1) // Assuming branchId = 1
  }, [fetchMenu])

  const handleAddToCart = (item: MenuItem) => {
    openItemModal(item)
    setSelectedVariant(null)
    setSelectedAddons([])
    setQuantity(1)
  }

  const handleConfirmAddToCart = () => {
    if (selectedItem) {
      addToCart(selectedItem, quantity, selectedAddons, selectedVariant || undefined)
      closeItemModal()
    }
  }

  const handleUpdateQuantity = (id: string, quantity: number) => {
    if (quantity <= 0) {
      removeFromCart(id)
    } else {
      updateCartItem(id, quantity)
    }
  }

  const handleCheckout = async () => {
    // Implement checkout logic, e.g., create order and add items
    // For now, just log
    console.log('Checkout', cart)
  }

  return (
    <div className="h-screen flex flex-col bg-gray-100">
      {/* Header */}
      <header className="bg-blue-600 text-white p-4">
        <h1 className="text-2xl font-bold">Restaurant POS</h1>
      </header>

      {/* Main Content */}
      <div className="flex flex-1 overflow-hidden">
        {/* Left Side: Categories and Menu Items */}
        <div className="w-1/2 flex flex-col border-r border-gray-300">
          {/* Categories */}
          <div className="bg-white p-4 border-b border-gray-300">
            <h2 className="text-lg font-semibold mb-2">Categories</h2>
            <div className="flex space-x-2 overflow-x-auto">
              {menu.map((category: MenuCategory) => (
                <button
                  key={category.id}
                  onClick={() => selectCategory(category.id)}
                  className={`px-4 py-2 rounded ${
                    selectedCategory === category.id
                      ? 'bg-blue-500 text-white'
                      : 'bg-gray-200 text-gray-700'
                  }`}
                >
                  {category.name}
                </button>
              ))}
            </div>
          </div>

          {/* Menu Items */}
          <div className="flex-1 p-4 overflow-y-auto">
            <h2 className="text-lg font-semibold mb-4">Menu Items</h2>
            <div className="grid grid-cols-2 gap-4">
              {selectedCategory &&
                menu
                  .find((cat) => cat.id === selectedCategory)
                  ?.items.map((item: MenuItem) => (
                    <div
                      key={item.id}
                      className="bg-white p-4 rounded shadow cursor-pointer hover:shadow-md"
                      onClick={() => handleAddToCart(item)}
                    >
                      <h3 className="font-semibold">{item.name}</h3>
                      <p className="text-gray-600">${item.price.toFixed(2)}</p>
                    </div>
                  ))}
            </div>
          </div>
        </div>

        {/* Right Side: Cart */}
        <div className="w-1/2 flex flex-col">
          <div className="bg-white p-4 border-b border-gray-300">
            <h2 className="text-lg font-semibold">Cart</h2>
          </div>
          <div className="flex-1 p-4 overflow-y-auto">
            {cart.length === 0 ? (
              <p className="text-gray-500">Cart is empty</p>
            ) : (
              <div className="space-y-2">
                {cart.map((item: CartItem) => (
                  <div key={item.id} className="bg-white p-3 rounded shadow">
                    <div className="flex justify-between items-center">
                      <div>
                        <h3 className="font-semibold">{item.name}</h3>
                        <p className="text-sm text-gray-600">
                          ${item.price.toFixed(2)} x {item.quantity}
                        </p>
                        {item.addons.length > 0 && (
                          <p className="text-sm text-gray-500">
                            Addons: {item.addons.map(a => a.name).join(', ')}
                          </p>
                        )}
                        {item.variant && (
                          <p className="text-sm text-gray-500">
                            Variant: {item.variant.name}
                          </p>
                        )}
                      </div>
                      <div className="flex items-center space-x-2">
                        <button
                          onClick={() => handleUpdateQuantity(item.id, item.quantity - 1)}
                          className="bg-gray-200 px-2 py-1 rounded"
                        >
                          -
                        </button>
                        <span>{item.quantity}</span>
                        <button
                          onClick={() => handleUpdateQuantity(item.id, item.quantity + 1)}
                          className="bg-gray-200 px-2 py-1 rounded"
                        >
                          +
                        </button>
                        <button
                          onClick={() => removeFromCart(item.id)}
                          className="bg-red-500 text-white px-2 py-1 rounded ml-2"
                        >
                          Remove
                        </button>
                      </div>
                    </div>
                    <p className="text-right font-semibold">
                      Total: ${item.total.toFixed(2)}
                    </p>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Bottom: Payment Summary */}
      <div className="bg-white p-4 border-t border-gray-300">
        <div className="flex justify-between items-center">
          <div className="flex space-x-4">
            <div>
              <label className="block text-sm font-medium text-gray-700">Discount</label>
              <input
                type="number"
                value={discount}
                onChange={(e) => setDiscount(parseFloat(e.target.value) || 0)}
                className="mt-1 block w-20 px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
              />
            </div>
          </div>
          <div className="text-right">
            <p>Subtotal: ${getSubtotal().toFixed(2)}</p>
            <p>Tax: ${getTax().toFixed(2)}</p>
            <p>Discount: ${discount.toFixed(2)}</p>
            <p className="text-xl font-bold">Grand Total: ${getGrandTotal().toFixed(2)}</p>
          </div>
        </div>
        <div className="mt-4 flex justify-end space-x-4">
          <button
            onClick={clearCart}
            className="bg-gray-500 text-white px-4 py-2 rounded hover:bg-gray-600"
          >
            Clear Cart
          </button>
          <button
            onClick={handleCheckout}
            className="bg-green-500 text-white px-4 py-2 rounded hover:bg-green-600"
            disabled={cart.length === 0}
          >
            Checkout
          </button>
        </div>
      </div>

      {/* Item Modal */}
      {showItemModal && selectedItem && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center">
          <div className="bg-white p-6 rounded-lg max-w-md w-full">
            <h2 className="text-xl font-bold mb-4">{selectedItem.name}</h2>
            <p className="mb-4">Price: ${selectedItem.price.toFixed(2)}</p>

            {/* Variants */}
            {selectedItem.variants.length > 0 && (
              <div className="mb-4">
                <h3 className="font-semibold mb-2">Variants</h3>
                {selectedItem.variants.map((variant) => (
                  <label key={variant.id} className="block">
                    <input
                      type="radio"
                      name="variant"
                      value={variant.id}
                      checked={selectedVariant?.id === variant.id}
                      onChange={() => setSelectedVariant(variant)}
                      className="mr-2"
                    />
                    {variant.name} (+${variant.price.toFixed(2)})
                  </label>
                ))}
              </div>
            )}

            {/* Addons */}
            {selectedItem.addons.length > 0 && (
              <div className="mb-4">
                <h3 className="font-semibold mb-2">Addons</h3>
                {selectedItem.addons.map((addon) => (
                  <label key={addon.id} className="block">
                    <input
                      type="checkbox"
                      checked={selectedAddons.some(a => a.id === addon.id)}
                      onChange={(e) => {
                        if (e.target.checked) {
                          setSelectedAddons([...selectedAddons, addon])
                        } else {
                          setSelectedAddons(selectedAddons.filter(a => a.id !== addon.id))
                        }
                      }}
                      className="mr-2"
                    />
                    {addon.name} (+${addon.price.toFixed(2)})
                  </label>
                ))}
              </div>
            )}

            {/* Quantity */}
            <div className="mb-4">
              <label className="block font-semibold mb-2">Quantity</label>
              <input
                type="number"
                min="1"
                value={quantity}
                onChange={(e) => setQuantity(parseInt(e.target.value) || 1)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md"
              />
            </div>

            <div className="flex justify-end space-x-2">
              <button
                onClick={closeItemModal}
                className="px-4 py-2 bg-gray-500 text-white rounded hover:bg-gray-600"
              >
                Cancel
              </button>
              <button
                onClick={handleConfirmAddToCart}
                className="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600"
              >
                Add to Cart
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

export default POS