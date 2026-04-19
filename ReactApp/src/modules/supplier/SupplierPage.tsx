import React from 'react';
import SupplierForm from './SupplierForm';
import { supplierService } from './supplierService';
import ManagementPage from '../shared/ManagementPage';

const SupplierPage: React.FC = () => {
  return (
    <ManagementPage
      title="Supplier Management"
      subtitle="Manage suppliers and procurement master records."
      entityLabel="Supplier"
      service={supplierService}
      FormComponent={SupplierForm}
    />
  );
};

export default SupplierPage;
