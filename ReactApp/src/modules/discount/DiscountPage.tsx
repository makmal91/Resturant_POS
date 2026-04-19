import React from 'react';
import DiscountForm from './DiscountForm';
import { discountService } from './discountService';
import ManagementPage from '../shared/ManagementPage';

const DiscountPage: React.FC = () => {
  return (
    <ManagementPage
      title="Discount Management"
      subtitle="Manage discount rules and promotional entities."
      entityLabel="Discount"
      service={discountService}
      FormComponent={DiscountForm}
    />
  );
};

export default DiscountPage;
