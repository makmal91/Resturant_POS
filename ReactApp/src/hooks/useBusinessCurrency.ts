import { useEffect, useState } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { masterDataService } from '../services/masterDataService';
import { BASE_CURRENCY, formatCurrency, getCurrencySymbol } from '../utils/currencyHelper';
import apiClient from '../services/api';

interface BusinessCurrencyState {
  currencyId: number;
  currencyCode: string;
  symbol: string;
  exchangeRateToPKR: number;
  loading: boolean;
}

const defaultState: BusinessCurrencyState = {
  currencyId: 1,
  currencyCode: BASE_CURRENCY,
  symbol: getCurrencySymbol(BASE_CURRENCY),
  exchangeRateToPKR: 1,
  loading: true,
};

let cachedBusinessId = 0;
let cachedState: BusinessCurrencyState | null = null;

export const useBusinessCurrency = () => {
  const { user } = useAuth();
  const businessId = user?.businessId && user.businessId > 0 ? user.businessId : 1;
  const [state, setState] = useState<BusinessCurrencyState>(() =>
    cachedBusinessId === businessId && cachedState ? cachedState : defaultState,
  );

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      if (cachedBusinessId === businessId && cachedState) {
        setState(cachedState);
        return;
      }

      setState((prev) => ({ ...prev, loading: true }));

      try {
        const [businessRes, currencies] = await Promise.all([
          apiClient.get(`/businesses/${businessId}`),
          masterDataService.getCurrencies(),
        ]);

        if (cancelled) return;

        const currencyId = Number(businessRes.data?.currencyId ?? 0);
        const currencyCode = String(businessRes.data?.currency ?? BASE_CURRENCY).toUpperCase();
        const currency = currencies.find((c) => c.id === currencyId)
          ?? currencies.find((c) => c.code === currencyCode)
          ?? currencies.find((c) => c.isBase)
          ?? { id: 1, code: BASE_CURRENCY, symbol: getCurrencySymbol(BASE_CURRENCY), exchangeRateToPKR: 1, name: 'Pakistani Rupee', isBase: true };

        const next: BusinessCurrencyState = {
          currencyId: currency.id,
          currencyCode: currency.code,
          symbol: currency.symbol || getCurrencySymbol(currency.code),
          exchangeRateToPKR: currency.exchangeRateToPKR || 1,
          loading: false,
        };

        cachedBusinessId = businessId;
        cachedState = next;
        setState(next);
      } catch {
        if (!cancelled) {
          const fallback = { ...defaultState, loading: false };
          setState(fallback);
        }
      }
    };

    void load();
    return () => { cancelled = true; };
  }, [businessId]);

  const fmt = (value: number) => formatCurrency(value, state.currencyCode);

  return {
    ...state,
    fmt,
    format: fmt,
    getSymbol: () => state.symbol,
  };
};
