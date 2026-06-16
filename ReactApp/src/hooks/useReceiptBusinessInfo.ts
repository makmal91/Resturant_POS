import { useEffect, useState } from 'react';
import { receiptBusinessService } from '../services/receiptBusinessService';
import type { ReceiptBusinessInfo } from '../components/receipt/receiptUtils';

interface UseReceiptBusinessInfoResult {
  business: ReceiptBusinessInfo | null;
  logoUrl: string | null;
  loading: boolean;
  error: string | null;
}

export const useReceiptBusinessInfo = (businessId: number): UseReceiptBusinessInfoResult => {
  const [business, setBusiness] = useState<ReceiptBusinessInfo | null>(null);
  const [logoUrl, setLogoUrl] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    let objectUrl: string | null = null;

    const load = async () => {
      setLoading(true);
      setError(null);

      try {
        const info = await receiptBusinessService.getBusinessInfo(businessId);
        if (cancelled) return;

        if (!info) {
          setBusiness(null);
          setLogoUrl(null);
          setError('Business details could not be loaded.');
          return;
        }

        setBusiness(info);

        if (info.hasLogo) {
          objectUrl = await receiptBusinessService.getLogoObjectUrl(info.id);
          if (!cancelled) {
            setLogoUrl(objectUrl);
          }
        } else {
          setLogoUrl(null);
        }
      } catch {
        if (!cancelled) {
          setError('Failed to load business details.');
          setBusiness(null);
          setLogoUrl(null);
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    };

    void load();

    return () => {
      cancelled = true;
      if (objectUrl) {
        URL.revokeObjectURL(objectUrl);
      }
    };
  }, [businessId]);

  return { business, logoUrl, loading, error };
};
