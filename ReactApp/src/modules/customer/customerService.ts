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
