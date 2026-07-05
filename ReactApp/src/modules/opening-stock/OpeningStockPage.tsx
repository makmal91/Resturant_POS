import React, { useCallback, useEffect, useMemo, useState } from 'react';
import Badge from '../../components/Badge';
import DataTable, { type Action, type Column } from '../../components/DataTable';
import PermissionGate from '../../components/PermissionGate';
import { useConfirmDialog } from '../../contexts/ConfirmDialogContext';
import { useFormModal } from '../../contexts/FormModalContext';
import { useModuleCrudAccess } from '../../hooks/useModuleCrudAccess';
import { hasBranchContext } from '../../types/permissions';
import { getApiErrorMessage } from '../../services/api';
import { safeString } from '../../utils/safeValues';
import { openingStockService, type OpeningStockVoucherDto } from './openingStockService';

const formatDate = (value: string) => {
  if (!value) return '—';
  const d = new Date(value);
  return Number.isNaN(d.getTime())
    ? '—'
    : d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
};

const formatCurrency = (value: unknown) => {
  const n = Number(value);
  if (!Number.isFinite(n)) return '—';
  return new Intl.NumberFormat(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(n);
};

const mapVoucherRow = (row: Record<string, unknown>): OpeningStockVoucherDto => ({
  id: Number(row.id ?? row.Id ?? 0),
  voucherNo: safeString(row.voucherNo ?? row.VoucherNo),
  voucherDate: safeString(row.voucherDate ?? row.VoucherDate),
  description: safeString(row.description ?? row.Description),
  warehouseId: Number(row.warehouseId ?? row.WarehouseId ?? 0),
  warehouseName: safeString(row.warehouseName ?? row.WarehouseName),
  totalAmount: Number(row.totalAmount ?? row.TotalAmount ?? 0),
  branchId: Number(row.branchId ?? row.BranchId ?? 0),
  branchName: safeString(row.branchName ?? row.BranchName),
  createdAt: safeString(row.createdAt ?? row.CreatedAt),
  isReversed: Boolean(row.isReversed ?? row.IsReversed ?? false),
  reversedAt: safeString(row.reversedAt ?? row.ReversedAt) || null,
  referenceVoucherId:
    row.referenceVoucherId != null || row.ReferenceVoucherId != null
      ? Number(row.referenceVoucherId ?? row.ReferenceVoucherId)
      : null,
  reversalVoucherId:
    row.reversalVoucherId != null || row.ReversalVoucherId != null
      ? Number(row.reversalVoucherId ?? row.ReversalVoucherId)
      : null,
});

const toDateInputValue = (value: unknown) => {
  const raw = safeString(value);
  if (!raw) return '';
  if (raw.includes('T')) return raw.split('T')[0].slice(0, 10);
  return raw.slice(0, 10);
};

const mapDetailToFormData = (data: Record<string, unknown>) => {
  const lines = (data.lines ?? data.Lines ?? []) as Record<string, unknown>[];
  return {
    voucherNo: safeString(data.voucherNo ?? data.VoucherNo),
    voucherDate: toDateInputValue(data.voucherDate ?? data.VoucherDate),
    description: safeString(data.description ?? data.Description),
    warehouseId: Number(data.warehouseId ?? data.WarehouseId ?? 0),
    branchId: Number(data.branchId ?? data.BranchId ?? 0),
    lines: lines.map((line) => ({
      productId: Number(line.productId ?? line.ProductId ?? 0),
      productName: safeString(line.productName ?? line.ProductName),
      productCode: safeString(line.productCode ?? line.ProductCode),
      baseUnitName: safeString(line.baseUnitName ?? line.BaseUnitName),
      variantId:
        line.variantId != null || line.VariantId != null
          ? Number(line.variantId ?? line.VariantId)
          : null,
      variantName: safeString(line.variantName ?? line.VariantName),
      unitId: Number(line.unitId ?? line.UnitId ?? 0),
      unitName: safeString(line.unitName ?? line.UnitName),
      quantity: Number(line.unitQuantity ?? line.UnitQuantity ?? line.quantity ?? line.Quantity ?? 0),
      conversionFactor: Number(line.conversionFactor ?? line.ConversionFactor ?? 1),
      convertedQuantity: Number(line.quantity ?? line.Quantity ?? 0),
      costPrice: Number(line.costPrice ?? line.CostPrice ?? 0),
    })),
  };
};

const OpeningStockPage: React.FC = () => {
  const { openForm, isOpen } = useFormModal();
  const { showConfirm } = useConfirmDialog();

  const [items, setItems] = useState<OpeningStockVoucherDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [searchTerm, setSearchTerm] = useState('');
  const [sortColumn, setSortColumn] = useState<string>('voucherDate');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('desc');
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [notification, setNotification] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  const {
    canAdd,
    canModify,
    canRemove,
    selectedBranchId,
    isGlobalMode,
    canWriteInView,
    getWriteBlockMessage,
  } = useModuleCrudAccess('Opening Stock');

  const hasBranchSelection = hasBranchContext(selectedBranchId);

  const showNotification = useCallback((type: 'success' | 'error', message: string) => {
    setNotification({ type, message });
    setTimeout(() => setNotification(null), 4000);
  }, []);

  const fetchVouchers = useCallback(async () => {
    if (!hasBranchSelection || selectedBranchId === null) {
      setItems([]);
      setTotalRecords(0);
      setTotalPages(0);
      return;
    }
    setLoading(true);
    try {
      const response = await openingStockService.getAll(
        selectedBranchId,
        currentPage,
        pageSize,
        searchTerm.trim() || undefined,
      );
      const payload = response.data as Record<string, unknown> | undefined;
      const rows = Array.isArray(payload?.vouchers)
        ? payload.vouchers
        : Array.isArray(payload?.data)
          ? payload.data
          : [];
      setItems(rows.map((r) => mapVoucherRow(r as Record<string, unknown>)));
      setTotalRecords(Number(payload?.totalRecords ?? payload?.TotalRecords ?? 0));
      setTotalPages(Number(payload?.totalPages ?? payload?.TotalPages ?? 0));
    } catch (error) {
      showNotification('error', getApiErrorMessage(error, 'Failed to load opening stock vouchers.'));
      setItems([]);
      setTotalRecords(0);
      setTotalPages(0);
    } finally {
      setLoading(false);
    }
  }, [currentPage, hasBranchSelection, pageSize, searchTerm, selectedBranchId, showNotification]);

  useEffect(() => {
    if (!isOpen) void fetchVouchers();
  }, [fetchVouchers, isOpen]);

  const loadVoucherDetail = useCallback(
    async (id: number) => {
      if (!selectedBranchId) throw new Error('Branch is required.');
      const response = await openingStockService.getById(id, selectedBranchId);
      return response.data as Record<string, unknown>;
    },
    [selectedBranchId],
  );

  const handleView = useCallback(
    async (row: OpeningStockVoucherDto) => {
      if (!selectedBranchId) return;
      try {
        const data = await loadVoucherDetail(row.id);
        openForm('openingStock', {
          ...mapDetailToFormData(data),
          id: Number(data.id ?? data.Id ?? row.id),
          readOnly: true,
        });
      } catch (error) {
        showNotification('error', getApiErrorMessage(error, 'Failed to load voucher details.'));
      }
    },
    [loadVoucherDetail, openForm, selectedBranchId, showNotification],
  );

  const handleEdit = useCallback(
    async (row: OpeningStockVoucherDto) => {
      const block = getWriteBlockMessage();
      if (!canModify || block) {
        showNotification('error', block ?? 'You do not have permission to edit opening stock vouchers.');
        return;
      }
      if (row.isReversed) {
        showNotification('error', 'Reversed vouchers cannot be edited.');
        return;
      }
      if (!selectedBranchId) return;

      try {
        const data = await loadVoucherDetail(row.id);
        openForm('openingStock', {
          ...mapDetailToFormData(data),
          id: row.id,
          readOnly: false,
        });
      } catch (error) {
        showNotification('error', getApiErrorMessage(error, 'Failed to load voucher for editing.'));
      }
    },
    [canModify, getWriteBlockMessage, loadVoucherDetail, openForm, selectedBranchId, showNotification],
  );

  const handleReverse = useCallback(
    (row: OpeningStockVoucherDto) => {
      const block = getWriteBlockMessage();
      if (!canRemove || block) {
        showNotification('error', block ?? 'You do not have permission to reverse opening stock vouchers.');
        return;
      }
      if (row.isReversed) return;
      if (!selectedBranchId) return;

      showConfirm({
        title: 'Reverse Opening Stock?',
        message:
          'This reverses stock ledger and accounting entries for this voucher. The voucher will be marked as reversed.',
        highlightText: row.voucherNo,
        variant: 'danger',
        confirmLabel: 'Reverse',
        cancelLabel: 'Cancel',
        onConfirm: async () => {
          try {
            await openingStockService.reverse(row.id, selectedBranchId);
            showNotification('success', `Voucher "${row.voucherNo}" reversed successfully.`);
            void fetchVouchers();
          } catch (error) {
            showNotification('error', getApiErrorMessage(error, 'Failed to reverse voucher.'));
          }
        },
      });
    },
    [canRemove, fetchVouchers, getWriteBlockMessage, selectedBranchId, showConfirm, showNotification],
  );

  const columns: Column<OpeningStockVoucherDto>[] = useMemo(
    () => [
      {
        key: 'voucherNo',
        header: 'Voucher No',
        sortable: true,
        render: (value) => <span className="font-medium text-gray-900">{safeString(value)}</span>,
      },
      {
        key: 'voucherDate',
        header: 'Date',
        sortable: true,
        render: (value) => formatDate(safeString(value)),
      },
      {
        key: 'warehouseName',
        header: 'Warehouse',
        sortable: true,
        render: (value) => safeString(value) || '—',
      },
      {
        key: 'totalAmount',
        header: 'Amount',
        sortable: true,
        render: (value) => <span className="font-semibold text-gray-900">{formatCurrency(value)}</span>,
      },
      {
        key: 'isReversed',
        header: 'Status',
        sortable: true,
        render: (value) =>
          value ? (
            <Badge variant="danger" size="sm" dot>
              Reversed
            </Badge>
          ) : (
            <Badge variant="success" size="sm" dot>
              Active
            </Badge>
          ),
      },
    ],
    [],
  );

  const actions: Action<OpeningStockVoucherDto>[] = useMemo(() => {
    const list: Action<OpeningStockVoucherDto>[] = [
      {
        label: 'View',
        onClick: (item) => {
          void handleView(item);
        },
        variant: 'secondary',
        icon: (
          <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
            />
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"
            />
          </svg>
        ),
      },
    ];

    if (canModify) {
      list.push({
        label: 'Edit',
        onClick: (item) => {
          void handleEdit(item);
        },
        variant: 'secondary',
        hidden: (item) => item.isReversed,
        icon: (
          <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
            />
          </svg>
        ),
      });
    }

    if (canRemove) {
      list.push({
        label: 'Reverse',
        onClick: handleReverse,
        variant: 'danger',
        hidden: (item) => item.isReversed,
        icon: (
          <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M3 10h10a8 8 0 018 8v2M3 10l6 6m-6-6l6-6"
            />
          </svg>
        ),
      });
    }

    return list;
  }, [canModify, canRemove, handleEdit, handleReverse, handleView]);

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-gray-900">Opening Stock Vouchers</h1>
          <p className="text-sm text-gray-500">Record opening inventory and post accounting entries in one voucher.</p>
        </div>
        <PermissionGate module="Opening Stock" action="Create">
          <button
            type="button"
            className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
            disabled={!canAdd || !canWriteInView}
            title={!canWriteInView ? getWriteBlockMessage() : undefined}
            onClick={() => {
              if (!canWriteInView) {
                showNotification('error', getWriteBlockMessage());
                return;
              }
              openForm('openingStock', { branchId: selectedBranchId ?? 0 });
            }}
          >
            New Opening Stock
          </button>
        </PermissionGate>
      </div>

      {notification && (
        <div
          className={`rounded-lg px-4 py-3 text-sm ${
            notification.type === 'success' ? 'bg-green-50 text-green-700' : 'bg-red-50 text-red-700'
          }`}
        >
          {notification.message}
        </div>
      )}

      {!hasBranchSelection && (
        <div className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
          {isGlobalMode ? 'Select a branch to manage opening stock vouchers.' : 'Branch context is required.'}
        </div>
      )}

      <DataTable
        data={items}
        columns={columns}
        actions={actions}
        loading={loading}
        searchable
        searchPlaceholder="Search by voucher no or remarks…"
        pagination
        pageSize={pageSize}
        pageSizeOptions={[5, 10, 25, 50]}
        onPageSizeChange={(n) => {
          setPageSize(n);
          setCurrentPage(1);
        }}
        emptyMessage={
          !hasBranchSelection
            ? 'Select a branch to load opening stock vouchers.'
            : searchTerm
              ? 'No vouchers match your search.'
              : 'No opening stock vouchers found.'
        }
        serverSide
        totalRecords={totalRecords}
        totalPages={totalPages}
        currentPage={currentPage}
        onPageChange={setCurrentPage}
        searchTerm={searchTerm}
        onSearchChange={(v) => {
          setSearchTerm(v);
          setCurrentPage(1);
        }}
        sortColumn={sortColumn}
        sortDirection={sortDirection}
        onSortChange={(col, dir) => {
          setSortColumn(col);
          setSortDirection(dir);
          setCurrentPage(1);
        }}
      />
    </div>
  );
};

export default OpeningStockPage;
