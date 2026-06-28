import axios, { AxiosError, AxiosHeaders, InternalAxiosRequestConfig } from 'axios';
import { authStorage } from '../utils/storage';
import { useRouteContextStore } from '../stores/useRouteContextStore';

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.trim() || '/api';
const API_TIMEOUT_MS = Number(import.meta.env.VITE_API_TIMEOUT_MS ?? 15000);

const methodToAction = (method?: string): string | null => {
  switch (method?.toUpperCase()) {
    case 'GET':
      return 'view';
    case 'POST':
      return 'create';
    case 'PUT':
    case 'PATCH':
      return 'edit';
    case 'DELETE':
      return 'delete';
    default:
      return null;
  }
};

const setHeaderIfMissing = (
  headers: InternalAxiosRequestConfig['headers'],
  name: string,
  value: string | null | undefined,
) => {
  if (!value) {
    return;
  }

  if (headers instanceof AxiosHeaders) {
    if (!headers.has(name)) {
      headers.set(name, value);
    }
    return;
  }

  const record = headers as Record<string, string>;
  if (!record[name]) {
    record[name] = value;
  }
};

const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: API_TIMEOUT_MS,
  headers: {
    'Content-Type': 'application/json',
  },
});

const dispatchAuthLogout = () => {
  window.dispatchEvent(new Event('auth:logout'));
};

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = authStorage.getToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  if (config.data instanceof FormData) {
    if (config.headers instanceof AxiosHeaders) {
      config.headers.setContentType(false);
    }
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const hdrs = config.headers as any;
    if (typeof hdrs.delete === 'function') {
      hdrs.delete('Content-Type');
      hdrs.delete('content-type');
    }
    delete hdrs['Content-Type'];
    delete hdrs['content-type'];
  }

  const user = authStorage.getUser();
  let selectedBranchId = authStorage.getSelectedBranchId();
  const isGlobalAdmin =
    user?.isGlobalAdmin === true ||
    user?.roleName === 'System Admin' ||
    user?.roleName === 'Super Admin' ||
    user?.roleName === 'SuperAdmin';

  if (selectedBranchId === null) {
    const authBranches = authStorage.getBranches();
    if (authBranches.length === 1) {
      selectedBranchId = authBranches[0].id;
    } else if (isGlobalAdmin && authBranches.length > 1) {
      selectedBranchId = 0;
    } else if (authBranches.length > 0) {
      selectedBranchId = authBranches[0].id;
    }
  }

  const businessId = user?.businessId ?? Number(localStorage.getItem('businessId') ?? 0);

  if (businessId > 0) {
    config.headers['X-Business-Id'] = String(businessId);
  }

  const headers = config.headers;
  const hasExplicitBranchHeader =
    (headers instanceof AxiosHeaders
      ? headers.has('X-Branch-Id') || headers.has('x-branch-id')
      : Boolean(
          (headers as Record<string, unknown>)?.['X-Branch-Id'] ??
            (headers as Record<string, unknown>)?.['x-branch-id'],
        ));

  if (selectedBranchId !== null && !hasExplicitBranchHeader) {
    if (headers instanceof AxiosHeaders) {
      headers.set('X-Branch-Id', String(selectedBranchId));
    } else {
      (config.headers as Record<string, string>)['X-Branch-Id'] = String(selectedBranchId);
    }
  }

  if (user?.roleName) {
    config.headers['X-User-Role'] = user.roleName;
  }

  const routeContext = useRouteContextStore.getState();
  setHeaderIfMissing(config.headers, 'x-module', routeContext.module);
  setHeaderIfMissing(config.headers, 'x-form', routeContext.form);

  const explicitAction =
    (config.headers instanceof AxiosHeaders
      ? config.headers.get('x-action')
      : (config.headers as Record<string, unknown>)?.['x-action']) ?? null;
  const action =
    typeof explicitAction === 'string' && explicitAction.trim().length > 0
      ? explicitAction
      : methodToAction(config.method);
  setHeaderIfMissing(config.headers, 'x-action', action);

  const method = config.method?.toUpperCase() ?? 'GET';
  const requestUrl = `${config.baseURL ?? ''}${config.url ?? ''}`;
  console.log(`[API] ${method} ${requestUrl}`, config.params ?? config.data ?? '');

  return config;
});

api.interceptors.response.use(
  (response) => {
    const method = response.config.method?.toUpperCase() ?? 'GET';
    const requestUrl = `${response.config.baseURL ?? ''}${response.config.url ?? ''}`;
    console.log(`[API] ${response.status} ${method} ${requestUrl}`, response.data);
    return response;
  },
  (error: AxiosError) => {
    const method = error.config?.method?.toUpperCase() ?? 'GET';
    const requestUrl = `${error.config?.baseURL ?? ''}${error.config?.url ?? ''}`;
    console.error(`[API] ERROR ${method} ${requestUrl}`, {
      code: error.code,
      status: error.response?.status,
      data: error.response?.data,
      message: error.message,
    });

    const status = error.response?.status;
    const responseData = error.response?.data as { message?: string } | undefined;
    const message = responseData?.message?.toLowerCase() ?? '';

    const isBranchAccessDenied =
      status === 403 &&
      (message.includes('access to the selected branch') ||
        message.includes('x-branch-id header is required') ||
        message.includes('you do not have access to the selected branch'))

    if (status === 401) {
      dispatchAuthLogout();
      if (window.location.pathname !== '/login') {
        window.location.href = '/login';
      }
    } else if (isBranchAccessDenied) {
      authStorage.setSelectedBranchId(null);
      if (window.location.pathname !== '/select-branch') {
        window.location.href = '/select-branch';
      }
    }

    return Promise.reject(error);
  }
);

export const isBackendUnavailableError = (error: unknown): boolean => {
  if (!axios.isAxiosError(error)) {
    return false;
  }

  return (
    error.code === 'ECONNREFUSED' ||
    error.code === 'ERR_NETWORK' ||
    error.message.toLowerCase().includes('network error')
  );
};

export const getApiErrorMessage = (
  error: unknown,
  fallbackMessage = 'Something went wrong. Please try again.'
): string => {
  if (!axios.isAxiosError(error)) {
    return fallbackMessage;
  }

  if (isBackendUnavailableError(error)) {
    return 'Backend server is not running. Start the API with: dotnet run (in the API folder).'
  }

  if (error.code === 'ECONNABORTED' || error.message.toLowerCase().includes('timeout')) {
    return 'Request timed out. Ensure the API is running at http://localhost:5226 and try again.'
  }

  const responseData = error.response?.data as
    | { message?: string; title?: string; error?: string; errors?: Record<string, string[]> }
    | undefined;

  if (responseData?.errors) {
    const validationMessages = Object.values(responseData.errors).flat().filter(Boolean);
    if (validationMessages.length > 0) {
      return validationMessages.join(' ');
    }
  }

  return (
    responseData?.message ||
    responseData?.title ||
    responseData?.error ||
    error.message ||
    fallbackMessage
  );
};

export default api;
