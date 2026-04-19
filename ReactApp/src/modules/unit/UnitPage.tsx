import React from 'react';
import UnitForm from './UnitForm';
import { unitService } from './unitService';
import ManagementPage from '../shared/ManagementPage';

const UnitPage: React.FC = () => {
  return (
    <ManagementPage
      title="Unit Management"
      subtitle="Manage measurement units used by product and inventory flows."
      entityLabel="Unit"
      service={unitService}
      FormComponent={UnitForm}
    />
  );
};

export default UnitPage;
