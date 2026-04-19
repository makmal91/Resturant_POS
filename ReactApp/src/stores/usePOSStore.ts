import { create } from 'zustand'
import api, { getApiErrorMessage } from '../services/api'

export interface MenuCategory {
  id: number
  name: string
  description: string
  categoryType?: 'Sale' | 'Inventory' | string
  items: MenuItem[]
}

export interface MenuItem {
  id: number
  name: string
  price: number
  tax: number
  preparationTime: number
  productType?: 'RawMaterial' | 'FinishedGood' | 'SemiFinished' | 'Service' | string
  isSaleable?: boolean
  isInventoryItem?: boolean
  isRecipeItem?: boolean
  isPurchasable?: boolean
  variants: MenuItemVariant[]
  addons: MenuItemAddon[]
}

export interface MenuItemVariant {
  id: number
  name: string
  price: number
}

export interface MenuItemAddon {
  id: number
  name: string
  price: number
}

export interface CartItem {
  id: string
  menuItemId: number
  name: string
  quantity: number
  price: number
  tax: number
  total: number
  addons: MenuItemAddon[]
  variant?: MenuItemVariant
}

interface POSState {
  menu: MenuCategory[]
  isMenuLoading: boolean
  menuError: string | null
  selectedCategory: number | null
  cart: CartItem[]
  discount: number
  taxRate: number // overall tax rate if needed, but tax is per item
  selectedItem: MenuItem | null
  showItemModal: boolean
  fetchMenu: (branchId: number) => Promise<void>
  selectCategory: (categoryId: number) => void
  openItemModal: (item: MenuItem) => void
  closeItemModal: () => void
  addToCart: (item: MenuItem, quantity?: number, addons?: MenuItemAddon[], variant?: MenuItemVariant) => void
  updateCartItem: (id: string, quantity: number) => void
  removeFromCart: (id: string) => void
  clearCart: () => void
  setDiscount: (discount: number) => void
  getSubtotal: () => number
  getTax: () => number
  getGrandTotal: () => number
}

export const usePOSStore = create<POSState>((set, get) => ({
  menu: [],
  isMenuLoading: false,
  menuError: null,
  selectedCategory: null,
  cart: [],
  discount: 0,
  taxRate: 0,
  selectedItem: null,
  showItemModal: false,

  fetchMenu: async (branchId: number) => {
    set({ isMenuLoading: true, menuError: null })

    try {
      console.log(`[POS] Fetching menu for branch ${branchId}`)

      const response = await api.get('/menu', {
        params: { branchId }
      })

      const categories = (response.data?.categories ?? [])
        .filter((category: MenuCategory) => (category?.categoryType ?? 'Sale') === 'Sale')
        .map((category: MenuCategory) => ({
          ...category,
          items: (category?.items ?? []).filter((item: MenuItem) =>
            (item?.isSaleable ?? false) && (item?.productType ?? '') === 'FinishedGood'
          )
        }))

      set({
        menu: categories,
        selectedCategory: categories[0]?.id ?? null,
        isMenuLoading: false,
        menuError: null,
      })
    } catch (error) {
      const message = getApiErrorMessage(error, 'Failed to fetch menu.')
      console.error('Failed to fetch menu:', error)

      set({
        menu: [],
        selectedCategory: null,
        isMenuLoading: false,
        menuError: message,
      })
    }
  },

  selectCategory: (categoryId: number) => {
    set({ selectedCategory: categoryId })
  },

  openItemModal: (item: MenuItem) => {
    set({ selectedItem: item, showItemModal: true })
  },

  closeItemModal: () => {
    set({ selectedItem: null, showItemModal: false })
  },

  addToCart: (item: MenuItem, quantity = 1, addons = [], variant) => {
    const { cart } = get()
    const existingItem = cart.find(ci => ci.menuItemId === item.id && JSON.stringify(ci.addons) === JSON.stringify(addons) && ci.variant?.id === variant?.id)

    if (existingItem) {
      get().updateCartItem(existingItem.id, existingItem.quantity + quantity)
    } else {
      const basePrice = variant ? variant.price : item.price
      const addonsPrice = addons.reduce((sum, addon) => sum + addon.price, 0)
      const totalPrice = (basePrice + addonsPrice) * quantity
      const tax = (basePrice + addonsPrice) * item.tax / 100 * quantity

      const newItem: CartItem = {
        id: `${item.id}-${Date.now()}`,
        menuItemId: item.id,
        name: item.name,
        quantity,
        price: basePrice + addonsPrice,
        tax,
        total: totalPrice + tax,
        addons,
        variant
      }

      set({ cart: [...cart, newItem] })
    }
  },

  updateCartItem: (id: string, quantity: number) => {
    set(state => ({
      cart: state.cart.map(item =>
        item.id === id
          ? {
              ...item,
              quantity,
              total: (item.price * quantity) + (item.tax / item.quantity * quantity)
            }
          : item
      )
    }))
  },

  removeFromCart: (id: string) => {
    set(state => ({
      cart: state.cart.filter(item => item.id !== id)
    }))
  },

  clearCart: () => {
    set({ cart: [] })
  },

  setDiscount: (discount: number) => {
    set({ discount })
  },

  getSubtotal: () => {
    const { cart } = get()
    return cart.reduce((sum, item) => sum + item.total - item.tax, 0)
  },

  getTax: () => {
    const { cart } = get()
    return cart.reduce((sum, item) => sum + item.tax, 0)
  },

  getGrandTotal: () => {
    const { getSubtotal, getTax, discount } = get()
    return getSubtotal() + getTax() - discount
  }
}))