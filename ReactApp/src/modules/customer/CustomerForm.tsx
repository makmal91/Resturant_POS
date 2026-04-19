import React from 'react';
import ManagementForm from '../shared/ManagementForm';
import { EntityFormProps } from '../shared/ManagementPage';

const CustomerForm: React.FC<EntityFormProps> = (props) => {
  return <ManagementForm entityLabel="Customer" {...props} />;
};

export default CustomerForm;
