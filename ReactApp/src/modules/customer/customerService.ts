import apiClient from '../../services/api';

export type CustomerType = 'Retail' | 'Wholesale' | 'VIP';

export interface CustomerListItem {
  id: number;
  customerCode: string;
  name: string;
  phone: string | null;
  email: string | null;
  cityName: string | null;
  countryId?: number | null;
  cityId?: number | null;
  customerType: CustomerType;
  status: boolean;
  creditLimit: number;
  isWalkIn: boolean;
  createdDate: string;
}

export interface CustomerDetail extends CustomerListItem {
  address: string | null;
  cnic: string | null;
  openingBalance: number;
  loyaltyPoints: number;
  updatedDate: string | null;
}

export interface CreateCustomerPayload {
  name: string;
  customerCode?: string;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  countryId?: number | null;
  cityId?: number | null;
  cnic?: string | null;
  customerType: number;
  status: boolean;
  openingBalance: number;
  creditLimit: number;
  businessId?: number;
  branchId?: number;
}

export interface QuickCreatePayload {
  name: string;
  phone?: string | null;
  businessId?: number;
  branchId?: number;
}

const bh = (branchId?: number) =>
  branchId ? { headers: { 'X-Branch-Id': String(branchId) } } : {};

const normalizeCustomerListItem = (raw: Record<string, unknown>): CustomerListItem => ({
  id: Number(raw.id ?? raw.Id ?? 0),
  customerCode: String(raw.customerCode ?? raw.CustomerCode ?? ''),
  name: String(raw.name ?? raw.Name ?? ''),
  phone: (raw.phone ?? raw.Phone ?? null) as string | null,
  email: (raw.email ?? raw.Email ?? null) as string | null,
  cityName: (raw.cityName ?? raw.CityName ?? null) as string | null,
  countryId: raw.countryId != null || raw.CountryId != null
    ? Number(raw.countryId ?? raw.CountryId)
    : null,
  cityId: raw.cityId != null || raw.CityId != null
    ? Number(raw.cityId ?? raw.CityId)
    : null,
  customerType: (raw.customerType ?? raw.CustomerType ?? 'Retail') as CustomerType,
  status: Boolean(raw.status ?? raw.Status ?? true),
  creditLimit: Number(raw.creditLimit ?? raw.CreditLimit ?? 0),
  isWalkIn: Boolean(raw.isWalkIn ?? raw.IsWalkIn ?? false),
  createdDate: String(raw.createdDate ?? raw.CreatedAt ?? raw.createdAt ?? ''),
});

const mergeWalkInCustomer = (
  customers: CustomerListItem[],
  walkIn: CustomerListItem | null
): CustomerListItem[] => {
  if (!walkIn?.id) return customers;
  const others = customers.filter((c) => c.id !== walkIn.id);
  return [walkIn, ...others];
};

const matchesLedgerCustomerSearch = (customer: CustomerListItem, search?: string): boolean => {
  const term = search?.trim();
  if (!term) return true;

  const q = term.toLowerCase();
  if (
    customer.name.toLowerCase().includes(q) ||
    customer.customerCode.toLowerCase().includes(q) ||
    (customer.phone?.includes(term) ?? false)
  ) {
    return true;
  }

  return customer.isWalkIn && q.includes('walk');
};

export const customerService = {
  getAll: (params: {
    branchId: number;
    page?: number;
    pageSize?: number;
    search?: string;
    type?: number | null;
    isActive?: boolean | null;
  }) =>
    apiClient.get('/customers', {
      params: {
        branchId: params.branchId,
        page: params.page ?? 1,
        pageSize: params.pageSize ?? 25,
        ...(params.search ? { search: params.search } : {}),
        ...(params.type != null ? { type: params.type } : {}),
        ...(params.isActive != null ? { isActive: params.isActive } : {}),
      },
      ...bh(params.branchId),
    }),

  getForLedgerFilter: async (branchId: number, search?: string) => {
    const [listRes, walkInRes] = await Promise.all([
      apiClient.get('/customers', {
        params: {
          branchId,
          page: 1,
          pageSize: 100,
          ...(search?.trim() ? { search: search.trim() } : {}),
        },
        ...bh(branchId),
      }),
      apiClient.get('/customers/walk-in', {
        params: { branchId },
        ...bh(branchId),
      }).catch(() => null),
    ]);

    const payload = listRes.data as { customers?: Record<string, unknown>[] };
    const customers = (payload.customers ?? []).map(normalizeCustomerListItem);
    const walkInFromApi = walkInRes?.data
      ? normalizeCustomerListItem(walkInRes.data as Record<string, unknown>)
      : null;
    const walkIn = walkInFromApi ?? customers.find((c) => c.isWalkIn) ?? null;

    const filtered = customers.filter((c) => matchesLedgerCustomerSearch(c, search));
    if (walkIn && matchesLedgerCustomerSearch(walkIn, search)) {
      return mergeWalkInCustomer(filtered, walkIn);
    }

    return filtered;
  },

  getById: (id: number, branchId: number) =>
    apiClient.get<CustomerDetail>(`/customers/${id}`, {
      params: { branchId },
      ...bh(branchId),
    }),

  getWalkIn: (branchId: number) =>
    apiClient.get<CustomerDetail>('/customers/walk-in', {
      params: { branchId },
      ...bh(branchId),
    }),

  search: (q: string, branchId: number) =>
    apiClient.get<CustomerListItem[]>('/customers/search', {
      params: { q, branchId },
      ...bh(branchId),
    }),

  create: (data: CreateCustomerPayload) =>
    apiClient.post<CustomerDetail>('/customers', data, bh(data.branchId)),

  quickCreate: (data: QuickCreatePayload) =>
    apiClient.post<CustomerDetail>('/customers/quick-create', data, bh(data.branchId)),

  update: (id: number, data: CreateCustomerPayload) =>
    apiClient.put<CustomerDetail>(`/customers/${id}`, data, bh(data.branchId)),

  delete: (id: number, branchId: number) =>
    apiClient.delete(`/customers/${id}`, { params: { branchId }, ...bh(branchId) }),
};
