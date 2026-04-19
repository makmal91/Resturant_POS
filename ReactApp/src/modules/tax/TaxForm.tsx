import React from 'react';
import ManagementForm from '../shared/ManagementForm';
import { EntityFormProps } from '../shared/ManagementPage';

const TaxForm: React.FC<EntityFormProps> = (props) => {
  return <ManagementForm entityLabel="Tax" {...props} />;
};

export default TaxForm;
