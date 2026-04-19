import React from 'react';
import TaxForm from './TaxForm';
import { taxService } from './taxService';
import ManagementPage from '../shared/ManagementPage';

const TaxPage: React.FC = () => {
  return (
    <ManagementPage
      title="Tax Management"
      subtitle="Manage tax setup used in pricing and checkout calculations."
      entityLabel="Tax"
      service={taxService}
      FormComponent={TaxForm}
    />
  );
};

export default TaxPage;
