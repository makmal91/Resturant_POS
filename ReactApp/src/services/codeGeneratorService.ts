import apiClient from './api';

export const CODE_MODULES = {
  Branch: 'Branch',
  Category: 'Category',
  SubCategory: 'SubCategory',
  Product: 'Product',
  Customer: 'Customer',
  Supplier: 'Supplier',
  Purchase: 'Purchase',
  SalesInvoice: 'SalesInvoice',
} as const;

export type CodeModuleName = (typeof CODE_MODULES)[keyof typeof CODE_MODULES];

const branchRequestConfig = (branchId?: number) =>
  branchId && branchId > 0
    ? { headers: { 'X-Branch-Id': String(branchId) } }
    : {};

export const codeGeneratorService = {
  preview: async (module: CodeModuleName, branchId?: number) => {
    const scopedBranchId = branchId && branchId > 0 ? branchId : undefined;
    const response = await apiClient.get<{ code: string }>('/codes/preview', {
      params: { module, ...(scopedBranchId ? { branchId: scopedBranchId } : {}) },
      ...(scopedBranchId ? branchRequestConfig(scopedBranchId) : {}),
    });
    return response.data.code;
  },

  generateBarcode: async (branchId: number) => {
    const scopedBranchId = branchId > 0 ? branchId : undefined;
    if (!scopedBranchId) {
      throw new Error('BranchId is required.');
    }

    const response = await apiClient.post<{ barcode: string }>(
      '/codes/barcode',
      null,
      {
        params: { branchId: scopedBranchId },
        ...branchRequestConfig(scopedBranchId),
      },
    );
    return response.data.barcode;
  },
};
