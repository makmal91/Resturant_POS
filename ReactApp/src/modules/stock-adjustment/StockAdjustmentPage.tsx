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
import { stockAdjustmentService, type StockAdjustmentDto } from './stockAdjustmentService';

const formatDate = (value: string) => {
  if (!value) return '—';
  const d = new Date(value);
  return Number.isNaN(d.getTime())
    ? '—'
    : d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
};

const formatCurrency = (value: unknown) => {
  const n = Number(value);
  if (!Number.isFinite(n) || n === 0) return '—';
  return new Intl.NumberFormat(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(n);
};

const mapAdjustmentRow = (row: Record<string, unknown>): StockAdjustmentDto => ({
  id: Number(row.id ?? row.Id ?? 0),
  adjustmentNo: safeString(row.adjustmentNo ?? row.AdjustmentNo),
  adjustmentDate: safeString(row.adjustmentDate ?? row.AdjustmentDate),
  warehouseId: Number(row.warehouseId ?? row.WarehouseId ?? 0),
  warehouseName: safeString(row.warehouseName ?? row.WarehouseName),
  adjustmentTypeId: Number(row.adjustmentTypeId ?? row.AdjustmentTypeId ?? 0),
  adjustmentTypeName: safeString(row.adjustmentTypeName ?? row.AdjustmentTypeName),
  remarks: safeString(row.remarks ?? row.Remarks),
  totalAmount: Number(row.totalAmount ?? row.TotalAmount ?? 0),
  gainAmount: Number(row.gainAmount ?? row.GainAmount ?? 0),
  lossAmount: Number(row.lossAmount ?? row.LossAmount ?? 0),
  lineCount: Number(row.lineCount ?? row.LineCount ?? 0),
  branchId: Number(row.branchId ?? row.BranchId ?? 0),
  branchName: safeString(row.branchName ?? row.BranchName),
  createdAt: safeString(row.createdAt ?? row.CreatedAt),
  isReversed: Boolean(row.isReversed ?? row.IsReversed ?? false),
  reversedAt: safeString(row.reversedAt ?? row.ReversedAt) || null,
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
    adjustmentNo: safeString(data.adjustmentNo ?? data.AdjustmentNo),
    adjustmentDate: toDateInputValue(data.adjustmentDate ?? data.AdjustmentDate),
    remarks: safeString(data.remarks ?? data.Remarks),
    warehouseId: Number(data.warehouseId ?? data.WarehouseId ?? 0),
    adjustmentTypeId: Number(data.adjustmentTypeId ?? data.AdjustmentTypeId ?? 0),
    branchId: Number(data.branchId ?? data.BranchId ?? 0),
    lines: lines.map((line) => {
      const unitQty = Number(line.unitQuantity ?? line.UnitQuantity ?? line.quantity ?? line.Quantity ?? 0);
      return {
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
        direction: unitQty < 0 ? ('decrease' as const) : ('increase' as const),
        quantity: unitQty,
        conversionFactor: Number(line.conversionFactor ?? line.ConversionFactor ?? 1),
        convertedQuantity: Number(line.baseQuantity ?? line.BaseQuantity ?? 0),
        costPrice: Number(line.costPrice ?? line.CostPrice ?? 0),
      };
    }),
  };
};

const StockAdjustmentPage: React.FC = () => {
  const { openForm, isOpen } = useFormModal();
  const { showConfirm } = useConfirmDialog();

  const [items, setItems] = useState<StockAdjustmentDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [searchTerm, setSearchTerm] = useState('');
  const [sortColumn, setSortColumn] = useState<string>('adjustmentDate');
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
  } = useModuleCrudAccess('Stock Adjustment');

  const hasBranchSelection = hasBranchContext(selectedBranchId);

  const showNotification = useCallback((type: 'success' | 'error', message: string) => {
    setNotification({ type, message });
    setTimeout(() => setNotification(null), 4000);
  }, []);

  const fetchAdjustments = useCallback(async () => {
    if (!hasBranchSelection || selectedBranchId === null) {
      setItems([]);
      setTotalRecords(0);
      setTotalPages(0);
      return;
    }
    setLoading(true);
    try {
      const response = await stockAdjustmentService.getAll(
        selectedBranchId,
        currentPage,
        pageSize,
        searchTerm.trim() || undefined,
      );
      const payload = response.data as Record<string, unknown> | undefined;
      const rows = Array.isArray(payload?.adjustments)
        ? payload.adjustments
        : Array.isArray(payload?.data)
          ? payload.data
          : [];
      setItems(rows.map((r) => mapAdjustmentRow(r as Record<string, unknown>)));
      setTotalRecords(Number(payload?.totalRecords ?? payload?.TotalRecords ?? 0));
      setTotalPages(Number(payload?.totalPages ?? payload?.TotalPages ?? 0));
    } catch (error) {
      showNotification('error', getApiErrorMessage(error, 'Failed to load stock adjustments.'));
      setItems([]);
      setTotalRecords(0);
      setTotalPages(0);
    } finally {
      setLoading(false);
    }
  }, [currentPage, hasBranchSelection, pageSize, searchTerm, selectedBranchId, showNotification]);

  useEffect(() => {
    if (!isOpen) void fetchAdjustments();
  }, [fetchAdjustments, isOpen]);

  const loadAdjustmentDetail = useCallback(
    async (id: number) => {
      if (!selectedBranchId) throw new Error('Branch is required.');
      const response = await stockAdjustmentService.getById(id, selectedBranchId);
      return response.data as Record<string, unknown>;
    },
    [selectedBranchId],
  );

  const handleView = useCallback(
    async (row: StockAdjustmentDto) => {
      if (!selectedBranchId) return;
      try {
        const data = await loadAdjustmentDetail(row.id);
        openForm('stockAdjustment', {
          ...mapDetailToFormData(data),
          id: Number(data.id ?? data.Id ?? row.id),
          readOnly: true,
        });
      } catch (error) {
        showNotification('error', getApiErrorMessage(error, 'Failed to load adjustment details.'));
      }
    },
    [loadAdjustmentDetail, openForm, selectedBranchId, showNotification],
  );

  const handleEdit = useCallback(
    async (row: StockAdjustmentDto) => {
      const block = getWriteBlockMessage();
      if (!canModify || block) {
        showNotification('error', block ?? 'You do not have permission to edit stock adjustments.');
        return;
      }
      if (row.isReversed) {
        showNotification('error', 'Reversed adjustments cannot be edited.');
        return;
      }
      if (!selectedBranchId) return;

      try {
        const data = await loadAdjustmentDetail(row.id);
        openForm('stockAdjustment', {
          ...mapDetailToFormData(data),
          id: row.id,
          readOnly: false,
        });
      } catch (error) {
        showNotification('error', getApiErrorMessage(error, 'Failed to load adjustment for editing.'));
      }
    },
    [canModify, getWriteBlockMessage, loadAdjustmentDetail, openForm, selectedBranchId, showNotification],
  );

  const handleReverse = useCallback(
    (row: StockAdjustmentDto) => {
      const block = getWriteBlockMessage();
      if (!canRemove || block) {
        showNotification('error', block ?? 'You do not have permission to reverse stock adjustments.');
        return;
      }
      if (row.isReversed) return;
      if (!selectedBranchId) return;

      showConfirm({
        title: 'Reverse Stock Adjustment?',
        message:
          'This reverses inventory and accounting entries for this adjustment. The adjustment will be marked as reversed.',
        highlightText: row.adjustmentNo,
        variant: 'danger',
        confirmLabel: 'Reverse',
        cancelLabel: 'Cancel',
        onConfirm: async () => {
          try {
            await stockAdjustmentService.reverse(row.id, selectedBranchId);
            showNotification('success', `Adjustment "${row.adjustmentNo}" reversed successfully.`);
            void fetchAdjustments();
          } catch (error) {
            showNotification('error', getApiErrorMessage(error, 'Failed to reverse adjustment.'));
          }
        },
      });
    },
    [canRemove, fetchAdjustments, getWriteBlockMessage, selectedBranchId, showConfirm, showNotification],
  );

  const handleDelete = useCallback(
    (row: StockAdjustmentDto) => {
      const block = getWriteBlockMessage();
      if (!canRemove || block) {
        showNotification('error', block ?? 'You do not have permission to delete stock adjustments.');
        return;
      }
      if (row.isReversed) return;
      if (!selectedBranchId) return;

      showConfirm({
        title: 'Delete Stock Adjustment?',
        message:
          'This permanently removes the adjustment and reverses its stock and accounting impact.',
        highlightText: row.adjustmentNo,
        variant: 'danger',
        confirmLabel: 'Delete',
        cancelLabel: 'Cancel',
        onConfirm: async () => {
          try {
            await stockAdjustmentService.delete(row.id, selectedBranchId);
            showNotification('success', `Adjustment "${row.adjustmentNo}" deleted successfully.`);
            void fetchAdjustments();
          } catch (error) {
            showNotification('error', getApiErrorMessage(error, 'Failed to delete adjustment.'));
          }
        },
      });
    },
    [canRemove, fetchAdjustments, getWriteBlockMessage, selectedBranchId, showConfirm, showNotification],
  );

  const columns: Column<StockAdjustmentDto>[] = useMemo(
    () => [
      {
        key: 'adjustmentNo',
        header: 'Adjustment No',
        sortable: true,
        render: (value) => <span className="font-medium text-gray-900">{safeString(value)}</span>,
      },
      {
        key: 'adjustmentDate',
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
        key: 'adjustmentTypeName',
        header: 'Type',
        sortable: true,
        render: (value) => safeString(value) || '—',
      },
      {
        key: 'gainAmount',
        header: 'Gain',
        sortable: true,
        render: (value) => <span className="text-emerald-700">{formatCurrency(value)}</span>,
      },
      {
        key: 'lossAmount',
        header: 'Loss',
        sortable: true,
        render: (value) => <span className="text-red-700">{formatCurrency(value)}</span>,
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

  const actions: Action<StockAdjustmentDto>[] = useMemo(() => {
    const list: Action<StockAdjustmentDto>[] = [
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
      list.push({
        label: 'Delete',
        onClick: handleDelete,
        variant: 'danger',
        hidden: (item) => item.isReversed,
        icon: (
          <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
            />
          </svg>
        ),
      });
    }

    return list;
  }, [canModify, canRemove, handleDelete, handleEdit, handleReverse, handleView]);

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-gray-900">Stock Adjustments</h1>
          <p className="text-sm text-gray-500">
            Record inventory gains and losses with accounting impact per adjustment type.
          </p>
        </div>
        <PermissionGate module="Stock Adjustment" action="Create">
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
              openForm('stockAdjustment', { branchId: selectedBranchId ?? 0 });
            }}
          >
            New Adjustment
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
          {isGlobalMode ? 'Select a branch to manage stock adjustments.' : 'Branch context is required.'}
        </div>
      )}

      <DataTable
        data={items}
        columns={columns}
        actions={actions}
        loading={loading}
        searchable
        searchPlaceholder="Search by adjustment no or remarks…"
        pagination
        pageSize={pageSize}
        pageSizeOptions={[5, 10, 25, 50]}
        onPageSizeChange={(n) => {
          setPageSize(n);
          setCurrentPage(1);
        }}
        emptyMessage={
          !hasBranchSelection
            ? 'Select a branch to load stock adjustments.'
            : searchTerm
              ? 'No adjustments match your search.'
              : 'No stock adjustments found.'
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

export default StockAdjustmentPage;
