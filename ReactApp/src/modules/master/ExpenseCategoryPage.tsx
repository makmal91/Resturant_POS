import BranchMasterPage from './BranchMasterPage';

const ExpenseCategoryPage = () => (
  <BranchMasterPage
    type="expense-category"
    title="Expense Categories"
    subtitle="Manage expense categories used when recording branch expenses."
    entityLabel="Expense Category"
    permissionModule="Expense Categories"
    showDescription
  />
);

export default ExpenseCategoryPage;
