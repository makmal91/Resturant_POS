export interface PagedListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface PagedListMeta {
  totalRecords: number;
  totalPages: number;
  currentPage: number;
}

export const extractPagedMeta = (payload: Record<string, unknown>): PagedListMeta => ({
  totalRecords: Number(payload.totalRecords ?? 0),
  totalPages: Number(payload.totalPages ?? 0),
  currentPage: Number(payload.currentPage ?? 1),
});
