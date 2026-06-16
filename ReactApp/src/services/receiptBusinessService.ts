import apiClient from './api';
import { BusinessService } from './apiService';
import type { ReceiptBusinessInfo } from '../components/receipt/receiptUtils';

const normalizeBusiness = (raw: Record<string, unknown>): ReceiptBusinessInfo => ({
  id: Number(raw.id ?? raw.Id ?? 0),
  name: String(raw.name ?? raw.Name ?? ''),
  legalName: String(raw.legalName ?? raw.LegalName ?? ''),
  address: String(raw.address ?? raw.Address ?? ''),
  phone: String(raw.phone ?? raw.Phone ?? ''),
  email: String(raw.email ?? raw.Email ?? ''),
  currency: String(raw.currency ?? raw.Currency ?? 'USD') || 'USD',
  taxNumber: String(raw.taxNumber ?? raw.TaxNumber ?? ''),
  hasLogo: Boolean(raw.hasLogo ?? raw.HasLogo ?? false),
  slogan: raw.slogan != null || raw.Slogan != null
    ? String(raw.slogan ?? raw.Slogan ?? '')
    : null,
  website: raw.website != null || raw.Website != null
    ? String(raw.website ?? raw.Website ?? '')
    : null,
});

export const resolveSessionBusinessId = (userBusinessId?: number | null): number => {
  if (userBusinessId && userBusinessId > 0) return userBusinessId;
  const stored = Number(localStorage.getItem('businessId') ?? 0);
  return stored > 0 ? stored : 0;
};

export const receiptBusinessService = {
  async getMyBusinessInfo(): Promise<ReceiptBusinessInfo | null> {
    try {
      const response = await apiClient.get('/businesses/my');
      const payload = response.data as Record<string, unknown>;
      const detail = (payload.data ?? payload) as Record<string, unknown>;
      const normalized = normalizeBusiness(detail);
      return normalized.id > 0 ? normalized : null;
    } catch {
      return null;
    }
  },

  async getBusinessInfo(businessId: number): Promise<ReceiptBusinessInfo | null> {
    const fromMy = await this.getMyBusinessInfo();
    if (fromMy) return fromMy;

    if (businessId <= 0) return null;
    try {
      const response = await BusinessService.getById(businessId);
      const payload = response.data as Record<string, unknown>;
      const detail = (payload.data ?? payload) as Record<string, unknown>;
      const normalized = normalizeBusiness(detail);
      return normalized.id > 0 ? normalized : null;
    } catch {
      return null;
    }
  },

  async getLogoObjectUrl(businessId: number): Promise<string | null> {
    try {
      const response = await apiClient.get('/businesses/my/logo', {
        responseType: 'blob',
      });
      return URL.createObjectURL(response.data as Blob);
    } catch {
      if (businessId <= 0) return null;
      try {
        const response = await apiClient.get(`/businesses/${businessId}/logo`, {
          responseType: 'blob',
        });
        return URL.createObjectURL(response.data as Blob);
      } catch {
        return null;
      }
    }
  },

  getLogoUrl(businessId: number): string {
    return BusinessService.getLogoUrl(businessId);
  },
};
