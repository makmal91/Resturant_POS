import apiClient from '../../../services/api';

const cache = new Map<string, string>();
const inflight = new Map<string, Promise<string | null>>();

const cacheKey = (productId: number, branchId: number) => `${branchId}:${productId}`;

export function getCachedProductImageUrl(productId: number, branchId: number): string | null {
  return cache.get(cacheKey(productId, branchId)) ?? null;
}

export async function fetchProductImageUrl(productId: number, branchId: number): Promise<string | null> {
  const key = cacheKey(productId, branchId);
  const existing = cache.get(key);
  if (existing) return existing;

  const pending = inflight.get(key);
  if (pending) return pending;

  const request = apiClient
    .get(`/products/${productId}/image`, {
      params: { branchId },
      responseType: 'blob',
      headers: { 'X-Branch-Id': String(branchId) },
    })
    .then((response) => {
      const objectUrl = URL.createObjectURL(response.data);
      cache.set(key, objectUrl);
      return objectUrl;
    })
    .catch(() => null)
    .finally(() => {
      inflight.delete(key);
    });

  inflight.set(key, request);
  return request;
}
