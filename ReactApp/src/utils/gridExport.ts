export interface GridExportColumn<T extends Record<string, unknown> = Record<string, unknown>> {
  key: string;
  header: string;
  format?: (value: unknown, row: T) => string;
}

function escapeCsvCell(value: string): string {
  if (/[",\n\r]/.test(value)) return `"${value.replace(/"/g, '""')}"`;
  return value;
}

export function buildGridCsv<T extends Record<string, unknown>>(
  columns: GridExportColumn<T>[],
  rows: T[],
): string {
  const header = columns.map((col) => escapeCsvCell(col.header)).join(',');
  const body = rows
    .map((row) => columns.map((col) => {
      const raw = row[col.key];
      const text = col.format ? col.format(raw, row) : String(raw ?? '');
      return escapeCsvCell(text);
    }).join(','))
    .join('\n');
  return `${header}\n${body}`;
}

export function downloadGridCsv(filename: string, csv: string): void {
  const blob = new Blob([`\uFEFF${csv}`], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename.endsWith('.csv') ? filename : `${filename}.csv`;
  link.click();
  URL.revokeObjectURL(url);
}

export async function fetchAllGridPages<T>(
  fetchPage: (pageNumber: number, pageSize: number) => Promise<{ data: T[]; totalRecords: number }>,
  pageSize = 100,
  maxPages = 200,
): Promise<T[]> {
  const all: T[] = [];
  let pageNumber = 1;
  let totalRecords = Number.POSITIVE_INFINITY;

  while (pageNumber <= maxPages && all.length < totalRecords) {
    const result = await fetchPage(pageNumber, pageSize);
    totalRecords = result.totalRecords;
    if (!result.data.length) break;
    all.push(...result.data);
    if (all.length >= totalRecords) break;
    pageNumber += 1;
  }

  return all;
}

export function exportGridData<T extends Record<string, unknown>>(
  filename: string,
  columns: GridExportColumn<T>[],
  rows: T[],
): void {
  const csv = buildGridCsv(columns, rows);
  downloadGridCsv(filename, csv);
}

export async function exportGridListing<T extends Record<string, unknown>>(
  filename: string,
  columns: GridExportColumn<T>[],
  fetchPage: (pageNumber: number, pageSize: number) => Promise<{ data: T[]; totalRecords: number }>,
): Promise<void> {
  const rows = await fetchAllGridPages(fetchPage);
  const csv = buildGridCsv(columns, rows);
  downloadGridCsv(filename, csv);
}

export function exportStatementCsv(
  filename: string,
  lines: { section: string; label: string; amount: string }[],
): void {
  const header = 'Section,Line Item,Amount';
  const body = lines
    .map((line) => [line.section, line.label, line.amount]
      .map((cell) => {
        const value = String(cell ?? '');
        return /[",\n\r]/.test(value) ? `"${value.replace(/"/g, '""')}"` : value;
      })
      .join(','))
    .join('\n');
  downloadGridCsv(filename, `${header}\n${body}`);
}
