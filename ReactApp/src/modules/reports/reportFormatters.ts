export const fmt = (n: number) =>
  new Intl.NumberFormat(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(n);

export const fmtQty = (n: number) => {
  const abs = Math.abs(n);
  const s = abs % 1 === 0 ? abs.toFixed(0) : abs.toFixed(4).replace(/\.?0+$/, '');
  return n < 0 ? `−${s}` : s;
};

export const formatDate = (value: string) => {
  if (!value) return '—';
  const d = new Date(value);
  return Number.isNaN(d.getTime())
    ? '—'
    : d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
};

export const monthStart = () => {
  const d = new Date();
  return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().slice(0, 10);
};

export const todayStr = () => new Date().toISOString().slice(0, 10);
