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
    const response = await apiClient.get<{ code: string }>('/codes/preview', {
      params: { module, branchId: branchId && branchId > 0 ? branchId : undefined },
      ...branchRequestConfig(branchId),
    });
    return response.data.code;
  },

  generate: async (module: CodeModuleName, branchId?: number) => {
    const response = await apiClient.post<{ code: string }>(
      '/codes/generate',
      null,
      {
        params: { module, branchId: branchId && branchId > 0 ? branchId : undefined },
        ...branchRequestConfig(branchId),
      }
    );
    return response.data.code;
  },

  generateBarcode: async (branchId: number) => {
    const response = await apiClient.post<{ barcode: string }>(
      '/codes/barcode',
      null,
      {
        params: { branchId },
        headers: { 'X-Branch-Id': String(branchId) },
      }
    );
    return response.data.barcode;
  },
};
