import { useNavigate } from 'react-router-dom';
import { useFormModal } from '../../contexts/FormModalContext';
import type { FinanceSourceTarget } from './financeVoucherNav';

export function useFinanceSourceNav(branchId: number) {
  const navigate = useNavigate();
  const { openForm } = useFormModal();

  const openSource = (target: FinanceSourceTarget) => {
    if (!target) return;

    if (target.path === '/purchase' && target.purchaseId) {
      openForm('purchase', { id: target.purchaseId, branchId });
      return;
    }

    if (target.path === '/sales-invoices' && target.invoiceId) {
      navigate('/sales-invoices', { state: { viewInvoiceId: target.invoiceId, branchId } });
      return;
    }

    if (target.path === '/finance/expenses' && target.expenseId) {
      navigate('/finance/expenses', { state: { expenseId: target.expenseId, branchId } });
      return;
    }

    navigate(target.path, {
      state: {
        branchId,
        paymentId: target.paymentId,
      },
    });
  };

  return { openSource };
}
