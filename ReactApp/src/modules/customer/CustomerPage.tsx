import React from 'react';
import CustomerForm from './CustomerForm';
import { customerService } from './customerService';
import ManagementPage from '../shared/ManagementPage';

const CustomerPage: React.FC = () => {
  return (
    <ManagementPage
      title="Customer Management"
      subtitle="Manage your customer master data and status."
      entityLabel="Customer"
      service={customerService}
      FormComponent={CustomerForm}
    />
  );
};

export default CustomerPage;
