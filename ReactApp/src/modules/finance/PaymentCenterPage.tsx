import React, { useCallback, useEffect, useMemo, useState } from 'react';
import DataTable, { type Action, type Column } from '../../components/DataTable';
import Badge from '../../components/Badge';
import PermissionGate from '../../components/PermissionGate';
import { useConfirmDialog } from '../../contexts/ConfirmDialogContext';
import { useFormModal } from '../../contexts/FormModalContext';
import { useBusinessCurrency } from '../../hooks/useBusinessCurrency';
import { useModuleCrudAccess } from '../../hooks/useModuleCrudAccess';
import { hasBranchContext } from '../../types/permissions';
import { getApiErrorMessage } from '../../services/api';
import { useBranchStore } from '../../stores/useBranchStore';
import { supplierService, type SupplierItem } from '../supplier/supplierService';
import { customerService, type CustomerListItem } from '../customer/customerService';
import { partyLedgerService } from '../ledger/partyLedgerService';
import type {
  InvoicePaymentCategory,
  PartyPaymentType,
} from '../../components/forms/ReceivePaymentForm';
import { paymentCenterService, type PaymentListItem, type PaymentModule } from './paymentCenterService';

type PaymentCenterMode = 'payable' | 'receivable';

const formatDate = (value: string) => {
  const d = new Date(value);
  return Number.isNaN(d.getTime())
    ? '—'
    : d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
};

interface PaymentCenterPageProps {
  mode: PaymentCenterMode;
}

export default function PaymentCenterPage({ mode }: PaymentCenterPageProps) {
  const isPayable = mode === 'payable';
  const module: PaymentModule = isPayable ? 'Purchase' : 'Sale';
  const formType = isPayable ? 'paySupplier' : 'receivePayment';
  const title = isPayable ? 'Payables' : 'Receivables';
  const subtitle = isPayable
    ? 'Record and manage supplier payments'
    : 'Record and manage customer receipts';

  const { fmt } = useBusinessCurrency();
  const { openForm, isOpen } = useFormModal();
  const { showConfirm } = useConfirmDialog();
  const { canAdd, canModify, canRemove, getWriteBlockMessage } = useModuleCrudAccess('Party Ledger');
  const { selectedBranchId } = useBranchStore();
  const branchId = selectedBranchId ?? 0;
  const hasBranchSelection = hasBranchContext(selectedBranchId);

  const [items, setItems] = useState<PaymentListItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [totalRecords, setTotalRecords] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [partyId, setPartyId] = useState(0);
  const [suppliers, setSuppliers] = useState<SupplierItem[]>([]);
  const [customers, setCustomers] = useState<CustomerListItem[]>([]);
  const [notification, setNotification] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  const showNotification = useCallback((type: 'success' | 'error', message: string) => {
    setNotification({ type, message });
    setTimeout(() => setNotification(null), 4000);
  }, []);

  useEffect(() => {
    if (branchId <= 0) return;
    if (isPayable) {
      void supplierService.getAllActive(branchId).then((res) => {
        setSuppliers(Array.isArray(res.data) ? res.data : []);
      });
    } else {
      void customerService.getForLedgerFilter(branchId, '').then(setCustomers);
    }
  }, [branchId, isPayable]);

  const fetchPayments = useCallback(async () => {
    if (!hasBranchSelection || branchId <= 0) {
      setItems([]);
      setTotalRecords(0);
      setTotalPages(0);
      return;
    }

    setLoading(true);
    try {
      const res = await paymentCenterService.list(branchId, module, currentPage, pageSize, {
        supplierId: isPayable && partyId > 0 ? partyId : undefined,
        customerId: !isPayable && partyId > 0 ? partyId : undefined,
        fromDate: fromDate || undefined,
        toDate: toDate || undefined,
      });
      setItems(res.payments);
      setTotalRecords(res.totalRecords);
      setTotalPages(res.totalPages);
    } catch (err) {
      setItems([]);
      setTotalRecords(0);
      setTotalPages(0);
      showNotification('error', getApiErrorMessage(err, `Failed to load ${title.toLowerCase()}.`));
    } finally {
      setLoading(false);
    }
  }, [hasBranchSelection, branchId, module, currentPage, pageSize, isPayable, partyId, fromDate, toDate, title, showNotification]);

  useEffect(() => {
    void fetchPayments();
  }, [fetchPayments]);

  useEffect(() => {
    if (!isOpen) void fetchPayments();
  }, [isOpen, fetchPayments]);

  useEffect(() => {
    setCurrentPage(1);
  }, [partyId, fromDate, toDate, pageSize, branchId]);

  const openCreate = () => {
    const block = getWriteBlockMessage();
    if (!canAdd || block) {
      showNotification('error', block ?? 'You do not have permission to add payments.');
      return;
    }
    openForm(formType, isPayable
      ? (partyId > 0 ? { supplierId: partyId } : undefined)
      : (partyId > 0 ? { customerId: partyId } : undefined));
  };

  const openEdit = useCallback(async (item: PaymentListItem) => {
    const block = getWriteBlockMessage();
    if (!canModify || block) {
      showNotification('error', block ?? 'You do not have permission to edit payments.');
      return;
    }
    if (item.isReversed) {
      showNotification('error', 'Reversed payments cannot be edited.');
      return;
    }

    try {
      const res = await partyLedgerService.getPayment(branchId, item.id);
      const payment = res.data;
      if (isPayable) {
        openForm('paySupplier', {
          id: payment.id,
          supplierId: payment.supplierId ?? partyId,
          paymentMethod: payment.paymentType as PartyPaymentType,
          paymentCategory: (payment.category ?? 'AgainstInvoice') as InvoicePaymentCategory,
          amount: payment.amount,
          paymentDate: payment.paymentDate,
          referenceNo: payment.referenceNo,
          notes: payment.notes,
          allocationMode: payment.hasAllocations ? 'manual' : 'auto',
          allocations: payment.allocations.map((a) => ({
            invoiceId: a.invoiceId,
            appliedAmount: a.appliedAmount,
            invoiceNo: a.invoiceNo,
          })),
        });
      } else {
        openForm('receivePayment', {
          id: payment.id,
          customerId: payment.customerId ?? partyId,
          paymentType: payment.paymentType as PartyPaymentType,
          amount: payment.amount,
          paymentDate: payment.paymentDate,
          referenceNo: payment.referenceNo,
          notes: payment.notes,
          allocationMode: payment.hasAllocations ? 'manual' : 'auto',
          allocations: payment.allocations.map((a) => ({
            invoiceId: a.invoiceId,
            appliedAmount: a.appliedAmount,
            invoiceNo: a.invoiceNo,
          })),
        });
      }
    } catch (err) {
      showNotification('error', getApiErrorMessage(err, 'Failed to load payment for editing.'));
    }
  }, [branchId, canModify, getWriteBlockMessage, isPayable, openForm, partyId, showNotification]);

  const handleReverse = useCallback((item: PaymentListItem) => {
    const block = getWriteBlockMessage();
    if (!canRemove || block) {
      showNotification('error', block ?? 'You do not have permission to reverse payments.');
      return;
    }
    if (item.isReversed) return;

    showConfirm({
      title: isPayable ? 'Delete Payment?' : 'Delete Receipt?',
      message: 'This reverses the entry in the ledger and restores invoice balances.',
      highlightText: `#${item.id} — ${fmt(item.amount)}`,
      variant: 'danger',
      confirmLabel: 'Delete',
      onConfirm: async () => {
        try {
          await partyLedgerService.reversePayment(branchId, item.id);
          showNotification('success', isPayable ? 'Payment deleted successfully.' : 'Receipt deleted successfully.');
          void fetchPayments();
        } catch (err) {
          showNotification('error', getApiErrorMessage(err, 'Failed to reverse payment.'));
        }
      },
    });
  }, [branchId, canRemove, fetchPayments, fmt, getWriteBlockMessage, isPayable, showConfirm, showNotification]);

  const columns = useMemo<Column<PaymentListItem>[]>(() => {
    const base: Column<PaymentListItem>[] = [
      {
        key: 'referenceNo',
        header: isPayable ? 'Payment No' : 'Receipt No',
        sortable: false,
        render: (value: string, row) => (
          <span className="font-mono text-sm">
            {value?.trim() || (row.id > 0 ? `#${row.id}` : '—')}
          </span>
        ),
      },
      {
        key: 'paymentDate',
        header: 'Date',
        sortable: false,
        render: (value: string) => <span className="whitespace-nowrap">{formatDate(value)}</span>,
      },
    ];

    if (isPayable) {
      base.push({
        key: 'supplierName',
        header: 'Supplier',
        sortable: false,
        render: (value: string) => value || '—',
      });
    } else {
      base.push({
        key: 'customerName',
        header: 'Customer',
        sortable: false,
        render: (value: string) => value || '—',
      });
    }

    base.push(
      {
        key: 'notes',
        header: 'Description',
        sortable: false,
        render: (_: string, row) => row.notes?.trim() || row.invoiceNo || '—',
      },
      {
        key: 'paymentType',
        header: 'Payment Type',
        sortable: false,
        render: (value: string) => (
          <Badge variant={value === 'Bank' ? 'info' : 'success'} size="sm">{value}</Badge>
        ),
      },
      {
        key: 'amount',
        header: 'Amount',
        sortable: false,
        render: (value: number) => <span className="font-semibold tabular-nums">{fmt(Number(value))}</span>,
      },
      {
        key: 'isReversed',
        header: 'Status',
        sortable: false,
        render: (value: boolean) => (
          <Badge variant={value ? 'danger' : 'success'} size="sm" dot>
            {value ? 'Reversed' : 'Posted'}
          </Badge>
        ),
      },
    );

    return base;
  }, [fmt, isPayable]);

  const actions = useMemo<Action<PaymentListItem>[]>(() => {
    const list: Action<PaymentListItem>[] = [];
    if (canModify) {
      list.push({
        label: 'Edit',
        hidden: (row) => row.isReversed,
        onClick: (row) => void openEdit(row),
        variant: 'secondary',
      });
    }
    if (canRemove) {
      list.push({
        label: 'Delete',
        hidden: (row) => row.isReversed,
        onClick: handleReverse,
        variant: 'danger',
      });
    }
    return list;
  }, [canModify, canRemove, handleReverse, openEdit]);

  if (branchId <= 0) {
    return <div className="flex h-64 items-center justify-center text-gray-500">Please select a branch first.</div>;
  }

  return (
    <div className="space-y-4 p-4 md:p-6">
      {notification && (
        <div className={`rounded-lg px-4 py-3 text-sm ${notification.type === 'success' ? 'bg-green-50 text-green-800 border border-green-200' : 'bg-red-50 text-red-700 border border-red-200'}`}>
          {notification.message}
        </div>
      )}

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-800">{title}</h1>
          <p className="text-sm text-gray-500 mt-0.5">{subtitle}</p>
        </div>
        <PermissionGate module="Party Ledger" action="create">
          <button
            type="button"
            onClick={openCreate}
            disabled={!canAdd}
            className="px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 disabled:opacity-60"
          >
            {isPayable ? '+ Add Payment' : '+ Receive Payment'}
          </button>
        </PermissionGate>
      </div>

      <div className="bg-white rounded-xl border border-gray-100 p-4 grid grid-cols-1 md:grid-cols-4 gap-4">
        <div>
          <label className="text-xs text-gray-500 font-medium mb-1 block">{isPayable ? 'Supplier' : 'Customer'}</label>
          <select
            value={partyId || ''}
            onChange={(e) => setPartyId(Number(e.target.value))}
            className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg"
          >
            <option value="">All</option>
            {isPayable
              ? suppliers.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)
              : customers.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </div>
        <div>
          <label className="text-xs text-gray-500 font-medium mb-1 block">From Date</label>
          <input type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg" />
        </div>
        <div>
          <label className="text-xs text-gray-500 font-medium mb-1 block">To Date</label>
          <input type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg" />
        </div>
      </div>

      <DataTable
        columns={columns}
        data={items}
        actions={actions}
        loading={loading}
        searchable={false}
        serverSide
        currentPage={currentPage}
        pageSize={pageSize}
        pageSizeOptions={[10, 25, 50, 100]}
        totalRecords={totalRecords}
        totalPages={totalPages}
        onPageChange={setCurrentPage}
        onPageSizeChange={(size) => { setPageSize(size); setCurrentPage(1); }}
        emptyMessage={`No ${title.toLowerCase()} found.`}
      />
    </div>
  );
}
