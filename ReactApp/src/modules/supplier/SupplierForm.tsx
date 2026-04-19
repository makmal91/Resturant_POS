import React from 'react';
import ManagementForm from '../shared/ManagementForm';
import { EntityFormProps } from '../shared/ManagementPage';

const SupplierForm: React.FC<EntityFormProps> = (props) => {
  return <ManagementForm entityLabel="Supplier" {...props} />;
};

export default SupplierForm;
