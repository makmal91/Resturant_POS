import BranchMasterPage from './BranchMasterPage';

const ColorPage = () => (
  <BranchMasterPage
    type="color"
    title="Colors"
    subtitle="Manage product variant colors for the selected branch."
    entityLabel="Color"
    permissionModule="Colors"
    showHexCode
  />
);

export default ColorPage;
