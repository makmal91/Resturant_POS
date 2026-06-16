export const BASE_CURRENCY = 'PKR';

const SYMBOLS: Record<string, string> = {
  PKR: '₨',
  USD: '$',
  GBP: '£',
  EUR: '€',
  AED: 'د.إ',
  SAR: '﷼',
};

export const getCurrencySymbol = (currencyCode = BASE_CURRENCY): string => {
  const code = (currencyCode || BASE_CURRENCY).trim().toUpperCase();
  return SYMBOLS[code] ?? code;
};

export const formatCurrency = (
  value: number,
  currencyCode = BASE_CURRENCY,
  locale?: string,
): string => {
  const code = (currencyCode || BASE_CURRENCY).trim().toUpperCase();
  try {
    return new Intl.NumberFormat(locale, {
      style: 'currency',
      currency: code,
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(value);
  } catch {
    return `${getCurrencySymbol(code)} ${new Intl.NumberFormat(locale, {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(value)}`;
  }
};

export const toPKR = (amount: number, exchangeRateToPKR: number): number => {
  if (exchangeRateToPKR <= 0) return amount;
  return Math.round(amount * exchangeRateToPKR * 100) / 100;
};

export const fromPKR = (amountInPKR: number, exchangeRateToPKR: number): number => {
  if (exchangeRateToPKR <= 0) return amountInPKR;
  return Math.round((amountInPKR / exchangeRateToPKR) * 100) / 100;
};
