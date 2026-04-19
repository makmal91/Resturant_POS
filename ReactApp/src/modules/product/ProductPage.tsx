import React from 'react';
import ProductForm from './ProductForm';
import { productService } from './productService';
import ManagementPage from '../shared/ManagementPage';

const ProductPage: React.FC = () => {
  return (
    <ManagementPage
      title="Product Management"
      subtitle="Manage products, lifecycle, and core metadata."
      entityLabel="Product"
      service={productService}
      FormComponent={ProductForm}
    />
  );
};

export default ProductPage;
