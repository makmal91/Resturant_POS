import apiClient from './api';

export interface CurrencyDto {
  id: number;
  code: string;
  name: string;
  symbol: string;
  exchangeRateToPKR: number;
  isBase: boolean;
}

export interface ExpenseCategoryDto {
  id: number;
  name: string;
  description?: string | null;
}

export type MasterType =
  | 'size'
  | 'color'
  | 'expense-category'
  | 'country'
  | 'city'
  | 'currency';

export interface MasterItemDto {
  id: number;
  name: string;
  hexCode?: string | null;
}

const FALLBACK_MASTERS: Record<MasterType, string[]> = {
  size: ['S', 'M', 'L', 'XL', 'XXL'],
  color: ['Black', 'White', 'Blue', 'Red', 'Green'],
  'expense-category': ['Utilities', 'Rent', 'Salary', 'Supplies', 'Maintenance', 'Other'],
  country: [],
  city: [],
  currency: [],
};

let currencyCache: CurrencyDto[] | null = null;
let currencyCacheAt = 0;
const CACHE_TTL_MS = 5 * 60 * 1000;

const categoryCache = new Map<string, { data: ExpenseCategoryDto[]; at: number }>();
const masterCache = new Map<string, { data: MasterItemDto[]; at: number }>();

const masterCacheKey = (
  type: MasterType,
  branchId?: number,
  countryId?: number,
) => `${type}:${branchId ?? 0}:${countryId ?? 0}`;

export const masterDataService = {
  async getMasterData(
    type: MasterType,
    options?: { branchId?: number; countryId?: number; force?: boolean },
  ): Promise<MasterItemDto[]> {
    const branchId = options?.branchId;
    const countryId = options?.countryId;
    const key = masterCacheKey(type, branchId, countryId);
    const cached = masterCache.get(key);
    const now = Date.now();

    if (!options?.force && cached && now - cached.at < CACHE_TTL_MS) {
      return cached.data;
    }

    try {
      const res = await apiClient.get<MasterItemDto[]>(`/masters/${type}`, {
        params: {
          branchId: branchId && branchId > 0 ? branchId : undefined,
          countryId: countryId && countryId > 0 ? countryId : undefined,
        },
        headers: branchId && branchId > 0 ? { 'X-Branch-Id': String(branchId) } : undefined,
      });

      const rows = Array.isArray(res.data) ? res.data : [];
      const data = rows.map((item) => ({
        id: Number(item.id ?? 0),
        name: String(item.name ?? ''),
        hexCode: item.hexCode ? String(item.hexCode) : null,
      })).filter((item) => item.id > 0 && item.name);

      if (data.length > 0) {
        masterCache.set(key, { data, at: now });
        return data;
      }
    } catch {
      // fall through to static fallback
    }

    const fallback = this.getFallbackMasterData(type).map((name, index) => ({
      id: index + 1,
      name,
      hexCode: null,
    }));

    masterCache.set(key, { data: fallback, at: now });
    return fallback;
  },

  getFallbackMasterData(type: MasterType): string[] {
    return [...(FALLBACK_MASTERS[type] ?? [])];
  },

  async getSizes(branchId: number, force = false): Promise<MasterItemDto[]> {
    return this.getMasterData('size', { branchId, force });
  },

  async getColors(branchId: number, force = false): Promise<MasterItemDto[]> {
    return this.getMasterData('color', { branchId, force });
  },

  async getExpenseCategories(branchId: number, search?: string): Promise<ExpenseCategoryDto[]> {
    const rows = await this.getMasterData('expense-category', { branchId });
    const term = search?.trim().toLowerCase();
    const filtered = term
      ? rows.filter((c) => c.name.toLowerCase().includes(term))
      : rows;

    return filtered.map((c) => ({
      id: c.id,
      name: c.name,
      description: null,
    }));
  },

  async getCurrencies(force = false): Promise<CurrencyDto[]> {
    const now = Date.now();
    if (!force && currencyCache && now - currencyCacheAt < CACHE_TTL_MS) {
      return currencyCache;
    }

    const res = await apiClient.get<CurrencyDto[]>('/currencies');
    const rows = Array.isArray(res.data) ? res.data : [];
    currencyCache = rows.map((c) => ({
      id: Number(c.id ?? 0),
      code: String(c.code ?? ''),
      name: String(c.name ?? ''),
      symbol: String(c.symbol ?? ''),
      exchangeRateToPKR: Number(c.exchangeRateToPKR ?? 1),
      isBase: Boolean(c.isBase),
    })).filter((c) => c.id > 0);

    currencyCacheAt = now;
    return currencyCache;
  },

  clearMasterCache(type?: MasterType, branchId?: number) {
    if (!type) {
      masterCache.clear();
      categoryCache.clear();
      return;
    }

    const prefix = `${type}:${branchId ?? ''}`;
    for (const key of masterCache.keys()) {
      if (key.startsWith(prefix) || key.startsWith(`${type}:`)) {
        masterCache.delete(key);
      }
    }

    if (type === 'expense-category') {
      this.clearExpenseCategoryCache(branchId);
    }
  },

  clearExpenseCategoryCache(branchId?: number) {
    if (branchId == null) {
      categoryCache.clear();
      return;
    }
    for (const key of categoryCache.keys()) {
      if (key.startsWith(`${branchId}:`)) {
        categoryCache.delete(key);
      }
    }
  },
};
