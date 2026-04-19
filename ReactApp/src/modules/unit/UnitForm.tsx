import React from 'react';
import ManagementForm from '../shared/ManagementForm';
import { EntityFormProps } from '../shared/ManagementPage';

const UnitForm: React.FC<EntityFormProps> = (props) => {
  return <ManagementForm entityLabel="Unit" {...props} />;
};

export default UnitForm;
