import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.trim() || '/api';
const API_TIMEOUT_MS = Number(import.meta.env.VITE_API_TIMEOUT_MS ?? 15000);

const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: API_TIMEOUT_MS,
  headers: {
    'Content-Type': 'application/json',
  },
});

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = localStorage.getItem('authToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

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
    return 'Backend server is not running.';
  }

  const responseData = error.response?.data as
    | { message?: string; title?: string; error?: string }
    | undefined;

  return (
    responseData?.message ||
    responseData?.title ||
    responseData?.error ||
    error.message ||
    fallbackMessage
  );
};

export default api;
