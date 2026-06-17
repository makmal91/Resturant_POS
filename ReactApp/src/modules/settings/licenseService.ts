import apiClient from '../../services/api';

export interface LicenseStatus {
  isValid: boolean;
  isExpired: boolean;
  message?: string | null;
  licenseId?: string | null;
  customerName?: string | null;
  issuedAt?: string | null;
  expiresAt?: string | null;
  maxBusinesses: number;
  maxBranchesPerBusiness: number;
  maxUsers: number;
  loadedAt?: string | null;
}

export interface LicenseBranchUsage {
  businessId: number;
  businessName: string;
  currentBranches: number;
  maxBranchesPerBusiness: number;
}

export interface LicenseUsage {
  currentBusinesses: number;
  maxBusinesses: number;
  totalUsers: number;
  maxUsers: number;
  branchUsageByBusiness: LicenseBranchUsage[];
}

export interface LicenseUploadResponse {
  message: string;
  licenseStatus: LicenseStatus;
  usage: LicenseUsage;
}

const normalizeStatus = (value: Record<string, unknown>): LicenseStatus => ({
  isValid: Boolean(value.isValid ?? value.IsValid),
  isExpired: Boolean(value.isExpired ?? value.IsExpired),
  message: (value.message ?? value.Message) as string | null | undefined,
  licenseId: (value.licenseId ?? value.LicenseId) as string | null | undefined,
  customerName: (value.customerName ?? value.CustomerName) as string | null | undefined,
  issuedAt: (value.issuedAt ?? value.IssuedAt) as string | null | undefined,
  expiresAt: (value.expiresAt ?? value.ExpiresAt) as string | null | undefined,
  maxBusinesses: Number(value.maxBusinesses ?? value.MaxBusinesses ?? 0),
  maxBranchesPerBusiness: Number(value.maxBranchesPerBusiness ?? value.MaxBranchesPerBusiness ?? 0),
  maxUsers: Number(value.maxUsers ?? value.MaxUsers ?? 0),
  loadedAt: (value.loadedAt ?? value.LoadedAt) as string | null | undefined,
});

const normalizeUsage = (value: Record<string, unknown>): LicenseUsage => {
  const branchesRaw = (value.branchUsageByBusiness ?? value.BranchUsageByBusiness) as
    | Record<string, unknown>[]
    | undefined;

  return {
    currentBusinesses: Number(value.currentBusinesses ?? value.CurrentBusinesses ?? 0),
    maxBusinesses: Number(value.maxBusinesses ?? value.MaxBusinesses ?? 0),
    totalUsers: Number(value.totalUsers ?? value.TotalUsers ?? 0),
    maxUsers: Number(value.maxUsers ?? value.MaxUsers ?? 0),
    branchUsageByBusiness: Array.isArray(branchesRaw)
      ? branchesRaw.map((item) => ({
          businessId: Number(item.businessId ?? item.BusinessId ?? 0),
          businessName: String(item.businessName ?? item.BusinessName ?? ''),
          currentBranches: Number(item.currentBranches ?? item.CurrentBranches ?? 0),
          maxBranchesPerBusiness: Number(item.maxBranchesPerBusiness ?? item.MaxBranchesPerBusiness ?? 0),
        }))
      : [],
  };
};

export const licenseService = {
  getStatus: async () => {
    const response = await apiClient.get('/licenses/status');
    return normalizeStatus((response.data ?? {}) as Record<string, unknown>);
  },

  getUsage: async () => {
    const response = await apiClient.get('/licenses/usage');
    return normalizeUsage((response.data ?? {}) as Record<string, unknown>);
  },

  uploadLicense: async (file: File) => {
    const formData = new FormData();
    formData.append('file', file);

    const response = await apiClient.post('/licenses/upload', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });

    const data = (response.data ?? {}) as Record<string, unknown>;
    return {
      message: String(data.message ?? data.Message ?? 'License installed successfully.'),
      licenseStatus: normalizeStatus((data.licenseStatus ?? data.LicenseStatus ?? {}) as Record<string, unknown>),
      usage: normalizeUsage((data.usage ?? data.Usage ?? {}) as Record<string, unknown>),
    } satisfies LicenseUploadResponse;
  },

  reloadLicense: async () => {
    const response = await apiClient.post('/licenses/reload');
    const data = (response.data ?? {}) as Record<string, unknown>;
    return {
      message: String(data.message ?? data.Message ?? 'License cache reloaded.'),
      licenseStatus: normalizeStatus((data.licenseStatus ?? data.LicenseStatus ?? {}) as Record<string, unknown>),
    };
  },
};
