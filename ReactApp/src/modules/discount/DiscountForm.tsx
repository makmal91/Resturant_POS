import React from 'react';
import ManagementForm from '../shared/ManagementForm';
import { EntityFormProps } from '../shared/ManagementPage';

const DiscountForm: React.FC<EntityFormProps> = (props) => {
  return <ManagementForm entityLabel="Discount" {...props} />;
};

export default DiscountForm;
