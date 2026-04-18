import React, { useState } from 'react';
import { FormButton, FormSelect, FormInput } from './index';

interface MenuItem {
  id: string;
  name: string;
  price: number;
  category: string;
  variants?: { id: string; name: string; priceAdjustment: number }[];
}

interface OrderItem {
  menuItemId: string;
  itemName: string;
  quantity: number;
  unitPrice: number;
  variantId?: string;
  variantName?: string;
}

interface OrderScreenProps {
  menuItems?: MenuItem[];
  onCompleteOrder?: (order: OrderScreenData) => void;
  isLoading?: boolean;
}

interface OrderScreenData {
  items: OrderItem[];
  customerName: string;
  tableNumber: string;
  notes: string;
  subtotal: number;
  tax: number;
  total: number;
}

const OrderScreen: React.FC<OrderScreenProps> = ({
  menuItems = [],
  onCompleteOrder,
  isLoading = false,
}) => {
  const [orderItems, setOrderItems] = useState<OrderItem[]>([]);
  const [customerName, setCustomerName] = useState('');
  const [tableNumber, setTableNumber] = useState('');
  const [notes, setNotes] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string>('All');

  const TAX_RATE = 0.1; // 10% tax

  // Get unique categories
  const categories = ['All', ...new Set(menuItems.map((item) => item.category))];

  // Filter items by category
  const filteredItems =
    selectedCategory === 'All'
      ? menuItems
      : menuItems.filter((item) => item.category === selectedCategory);

  const addItemToOrder = (item: MenuItem, variantId?: string) => {
    const variant = item.variants?.find((v) => v.id === variantId);
    const price = variant ? item.price + variant.priceAdjustment : item.price;
    const variantName = variant?.name || '';

    const existingItem = orderItems.find(
      (oi) => oi.menuItemId === item.id && oi.variantId === variantId
    );

    if (existingItem) {
      updateItemQuantity(orderItems.indexOf(existingItem), existingItem.quantity + 1);
    } else {
      setOrderItems((prev) => [
        ...prev,
        {
          menuItemId: item.id,
          itemName: item.name,
          quantity: 1,
          unitPrice: price,
          variantId,
          variantName,
        },
      ]);
    }
  };

  const updateItemQuantity = (index: number, quantity: number) => {
    if (quantity <= 0) {
      removeItem(index);
      return;
    }
    const updated = [...orderItems];
    updated[index].quantity = quantity;
    setOrderItems(updated);
  };

  const removeItem = (index: number) => {
    setOrderItems((prev) => prev.filter((_, i) => i !== index));
  };

  // Calculate totals
  const subtotal = orderItems.reduce((sum, item) => sum + item.unitPrice * item.quantity, 0);
  const tax = subtotal * TAX_RATE;
  const total = subtotal + tax;

  const handleCompleteOrder = () => {
    if (orderItems.length === 0) {
      alert('Please add items to the order');
      return;
    }

    if (!customerName.trim() || !tableNumber.trim()) {
      alert('Please enter customer name and table number');
      return;
    }

    const orderData: OrderScreenData = {
      items: orderItems,
      customerName,
      tableNumber,
      notes,
      subtotal,
      tax,
      total,
    };

    onCompleteOrder?.(orderData);

    // Reset form
    setOrderItems([]);
    setCustomerName('');
    setTableNumber('');
    setNotes('');
  };

  return (
    <div className="min-h-screen bg-gray-50 p-4">
      <div className="max-w-7xl mx-auto grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Menu Items */}
        <div className="lg:col-span-2">
          <div className="bg-white rounded-lg shadow-md p-6">
            <h2 className="text-2xl font-bold mb-4 text-gray-800">Menu</h2>

            {/* Category Filter */}
            <div className="mb-6">
              <div className="flex gap-2 flex-wrap">
                {categories.map((category) => (
                  <button
                    key={category}
                    onClick={() => setSelectedCategory(category)}
                    className={`px-4 py-2 rounded-lg font-medium transition ${
                      selectedCategory === category
                        ? 'bg-blue-600 text-white'
                        : 'bg-gray-200 text-gray-800 hover:bg-gray-300'
                    }`}
                  >
                    {category}
                  </button>
                ))}
              </div>
            </div>

            {/* Menu Items Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {filteredItems.map((item) => (
                <div
                  key={item.id}
                  className="bg-gray-50 border border-gray-200 rounded-lg p-4 hover:shadow-lg transition"
                >
                  <h3 className="text-lg font-semibold text-gray-800 mb-2">{item.name}</h3>
                  <p className="text-2xl font-bold text-blue-600 mb-4">${item.price.toFixed(2)}</p>

                  {item.variants && item.variants.length > 0 ? (
                    <div className="space-y-2 mb-3">
                      {item.variants.map((variant) => (
                        <button
                          key={variant.id}
                          onClick={() => addItemToOrder(item, variant.id)}
                          className="w-full bg-blue-600 hover:bg-blue-700 text-white py-2 px-3 rounded text-sm transition font-medium"
                        >
                          {variant.name} (+${variant.priceAdjustment.toFixed(2)})
                        </button>
                      ))}
                    </div>
                  ) : (
                    <FormButton
                      type="button"
                      label="Add to Order"
                      onClick={() => addItemToOrder(item)}
                      variant="primary"
                      className="w-full"
                    />
                  )}
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Order Summary */}
        <div className="lg:col-span-1">
          <div className="bg-white rounded-lg shadow-md p-6 sticky top-4">
            <h2 className="text-2xl font-bold mb-4 text-gray-800">Order Summary</h2>

            {/* Customer Info */}
            <FormInput
              label="Customer Name"
              name="customerName"
              value={customerName}
              onChange={(e) => setCustomerName(e.target.value)}
              placeholder="Enter name"
              required
            />

            <FormInput
              label="Table Number"
              name="tableNumber"
              value={tableNumber}
              onChange={(e) => setTableNumber(e.target.value)}
              placeholder="Enter table #"
              required
            />

            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Special Notes
              </label>
              <textarea
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                placeholder="Any special requests?"
                rows={3}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>

            {/* Order Items */}
            <div className="bg-gray-50 rounded-lg p-4 mb-4 max-h-64 overflow-y-auto">
              {orderItems.length > 0 ? (
                <div className="space-y-3">
                  {orderItems.map((item, index) => (
                    <div key={index} className="bg-white p-3 rounded border border-gray-200">
                      <div className="flex justify-between items-start mb-2">
                        <div>
                          <p className="font-medium text-gray-800">{item.itemName}</p>
                          {item.variantName && (
                            <p className="text-sm text-gray-600">{item.variantName}</p>
                          )}
                        </div>
                        <p className="font-semibold text-blue-600">
                          ${(item.unitPrice * item.quantity).toFixed(2)}
                        </p>
                      </div>
                      <div className="flex items-center gap-2">
                        <button
                          onClick={() => updateItemQuantity(index, item.quantity - 1)}
                          className="bg-red-500 text-white px-2 py-1 rounded text-sm hover:bg-red-600"
                        >
                          -
                        </button>
                        <input
                          type="number"
                          value={item.quantity}
                          onChange={(e) =>
                            updateItemQuantity(index, parseInt(e.target.value) || 1)
                          }
                          className="w-12 px-2 py-1 border border-gray-300 rounded text-center"
                          min="1"
                        />
                        <button
                          onClick={() => updateItemQuantity(index, item.quantity + 1)}
                          className="bg-green-500 text-white px-2 py-1 rounded text-sm hover:bg-green-600"
                        >
                          +
                        </button>
                        <button
                          onClick={() => removeItem(index)}
                          className="ml-auto bg-gray-400 text-white px-2 py-1 rounded text-sm hover:bg-gray-500"
                        >
                          Delete
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-gray-500 text-center italic">No items added yet</p>
              )}
            </div>

            {/* Totals */}
            <div className="border-t-2 border-gray-300 pt-4 space-y-2 mb-4">
              <div className="flex justify-between text-gray-700">
                <span>Subtotal:</span>
                <span className="font-semibold">${subtotal.toFixed(2)}</span>
              </div>
              <div className="flex justify-between text-gray-700">
                <span>Tax (10%):</span>
                <span className="font-semibold">${tax.toFixed(2)}</span>
              </div>
              <div className="flex justify-between text-xl font-bold text-gray-900 bg-yellow-50 p-3 rounded">
                <span>Total:</span>
                <span>${total.toFixed(2)}</span>
              </div>
            </div>

            {/* Action Buttons */}
            <div className="space-y-2">
              <FormButton
                type="button"
                label="Complete Order"
                onClick={handleCompleteOrder}
                loading={isLoading}
                variant="primary"
                className="w-full"
              />
              <FormButton
                type="button"
                label="Clear Order"
                onClick={() => {
                  setOrderItems([]);
                  setCustomerName('');
                  setTableNumber('');
                  setNotes('');
                }}
                variant="secondary"
                className="w-full"
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default OrderScreen;
