import type { TrialBalanceRow } from './trialBalanceService';

const money = (value: number) =>
  new Intl.NumberFormat(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value);

export function exportTrialBalanceExcel(
  filename: string,
  rows: TrialBalanceRow[],
  totalDebit: number,
  totalCredit: number,
) {
  const lines = [
    ['Account Code', 'Account Name', 'Debit', 'Credit'],
    ...rows.map((row) => [
      row.accountCode,
      `${'  '.repeat(row.level)}${row.accountName}`,
      row.debit > 0 ? money(row.debit) : '',
      row.credit > 0 ? money(row.credit) : '',
    ]),
    ['', 'Total', money(totalDebit), money(totalCredit)],
  ];

  const csv = lines
    .map((line) => line.map((cell) => `"${String(cell).replace(/"/g, '""')}"`).join(','))
    .join('\r\n');

  const blob = new Blob(['\uFEFF' + csv], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = `${filename}.csv`;
  link.click();
  URL.revokeObjectURL(url);
}

export function printTrialBalancePdf() {
  window.print();
}
