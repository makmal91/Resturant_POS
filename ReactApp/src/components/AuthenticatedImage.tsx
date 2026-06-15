import React, { useEffect, useState } from 'react';
import apiClient from '../services/api';

interface AuthenticatedImageProps {
  endpoint: string;
  params?: Record<string, string | number | boolean | undefined>;
  alt: string;
  className?: string;
  fallback?: React.ReactNode;
}

const AuthenticatedImage: React.FC<AuthenticatedImageProps> = ({
  endpoint,
  params,
  alt,
  className,
  fallback = null,
}) => {
  const [src, setSrc] = useState<string | null>(null);
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    let cancelled = false;
    let objectUrl: string | null = null;

    const loadImage = async () => {
      setFailed(false);
      setSrc(null);

      try {
        const branchId = params?.branchId;
        const response = await apiClient.get(endpoint, {
          params,
          responseType: 'blob',
          headers:
            branchId !== undefined && branchId !== null
              ? { 'X-Branch-Id': String(branchId) }
              : undefined,
        });

        objectUrl = URL.createObjectURL(response.data);
        if (!cancelled) {
          setSrc(objectUrl);
        }
      } catch {
        if (!cancelled) {
          setFailed(true);
        }
      }
    };

    void loadImage();

    return () => {
      cancelled = true;
      if (objectUrl) {
        URL.revokeObjectURL(objectUrl);
      }
    };
  }, [endpoint, JSON.stringify(params)]);

  if (failed || !src) {
    return <>{fallback}</>;
  }

  return <img src={src} alt={alt} className={className} />;
};

export default AuthenticatedImage;
