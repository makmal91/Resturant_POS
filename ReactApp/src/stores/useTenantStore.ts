import { create } from 'zustand'

const TENANT_SESSION_KEY = 'tenantSession'

export type TenantRole = 'SuperAdmin' | 'BusinessAdmin' | 'BranchUser'

export interface TenantSession {
  businessId: number
  branchId: number
  role: TenantRole
}

interface TenantState {
  session: TenantSession
  setBusinessId: (businessId: number) => void
  setBranchId: (branchId: number) => void
  setRole: (role: TenantRole) => void
  hydrate: () => void
}

const defaultSession: TenantSession = {
  businessId: 1,
  branchId: 1,
  role: 'BranchUser',
}

const safeNumber = (value: unknown, fallback: number): number => {
  const parsed = Number(value)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback
}

const safeBranchNumber = (value: unknown, fallback: number): number => {
  const parsed = Number(value)
  return Number.isFinite(parsed) && parsed >= 0 ? parsed : fallback
}

const saveSession = (session: TenantSession) => {
  localStorage.setItem(TENANT_SESSION_KEY, JSON.stringify(session))
  localStorage.setItem('businessId', String(session.businessId))
  localStorage.setItem('branchId', String(session.branchId))
}

const readSession = (): TenantSession => {
  const raw = localStorage.getItem(TENANT_SESSION_KEY)
  if (!raw) {
    return defaultSession
  }

  try {
    const parsed = JSON.parse(raw) as Partial<TenantSession>
    return {
      businessId: safeNumber(parsed.businessId, defaultSession.businessId),
      branchId: safeBranchNumber(parsed.branchId, defaultSession.branchId),
      role: parsed.role ?? defaultSession.role,
    }
  } catch {
    return defaultSession
  }
}

export const useTenantStore = create<TenantState>((set, get) => ({
  session: defaultSession,

  setBusinessId: (businessId) => {
    const next = {
      ...get().session,
      businessId: safeNumber(businessId, defaultSession.businessId),
    }
    saveSession(next)
    set({ session: next })
  },

  setBranchId: (branchId) => {
    const next = {
      ...get().session,
      branchId: safeBranchNumber(branchId, defaultSession.branchId),
    }
    saveSession(next)
    set({ session: next })
  },

  setRole: (role) => {
    const next = {
      ...get().session,
      role,
    }
    saveSession(next)
    set({ session: next })
  },

  hydrate: () => {
    const session = readSession()
    saveSession(session)
    set({ session })
  },
}))