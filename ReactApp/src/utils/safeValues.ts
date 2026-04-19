export const safeString = (value: unknown, fallback = ''): string => {
  if (value == null) {
    return fallback;
  }

  return String(value);
};
