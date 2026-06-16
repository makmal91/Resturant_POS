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

let currencyCache: CurrencyDto[] | null = null;
let currencyCacheAt = 0;
const CACHE_TTL_MS = 5 * 60 * 1000;

const categoryCache = new Map<string, { data: ExpenseCategoryDto[]; at: number }>();

export const masterDataService = {
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

  async getExpenseCategories(branchId: number, search?: string): Promise<ExpenseCategoryDto[]> {
    const key = `${branchId}:${search?.trim().toLowerCase() ?? ''}`;
    const cached = categoryCache.get(key);
    const now = Date.now();
    if (cached && now - cached.at < CACHE_TTL_MS) {
      return cached.data;
    }

    const res = await apiClient.get<ExpenseCategoryDto[]>('/expense-categories', {
      params: { branchId, search: search?.trim() || undefined },
      headers: { 'X-Branch-Id': String(branchId) },
    });

    const rows = Array.isArray(res.data) ? res.data : [];
    const data = rows.map((c) => ({
      id: Number(c.id ?? 0),
      name: String(c.name ?? ''),
      description: c.description ? String(c.description) : null,
    })).filter((c) => c.id > 0);

    categoryCache.set(key, { data, at: now });
    return data;
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
