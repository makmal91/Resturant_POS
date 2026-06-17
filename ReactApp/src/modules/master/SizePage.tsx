import BranchMasterPage from './BranchMasterPage';

const SizePage = () => (
  <BranchMasterPage
    type="size"
    title="Sizes"
    subtitle="Manage product variant sizes for the selected branch."
    entityLabel="Size"
    permissionModule="Sizes"
    showSortOrder
  />
);

export default SizePage;
