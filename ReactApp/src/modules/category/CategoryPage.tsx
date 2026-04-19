import React from 'react';
import CategoryForm from './CategoryForm';
import { categoryService } from './categoryService';
import ManagementPage from '../shared/ManagementPage';

const CategoryPage: React.FC = () => {
  return (
    <ManagementPage
      title="Category Management"
      subtitle="Manage categories used across your POS and ERP modules."
      entityLabel="Category"
      service={categoryService}
      FormComponent={CategoryForm}
    />
  );
};

export default CategoryPage;
