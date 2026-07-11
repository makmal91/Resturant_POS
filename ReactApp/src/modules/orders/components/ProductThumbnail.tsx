import React, { useEffect, useRef, useState } from 'react';
import { fetchProductImageUrl, getCachedProductImageUrl } from '../utils/productImageCache';

export interface ProductThumbnailProps {
  productId: number;
  branchId: number;
  hasImage: boolean;
  alt: string;
}

const PLACEHOLDER = (
  <div className="flex h-[88px] w-full items-center justify-center bg-gray-100" aria-hidden>
    <svg className="h-8 w-8 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth={1.5}
        d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"
      />
    </svg>
  </div>
);

const ProductThumbnail: React.FC<ProductThumbnailProps> = React.memo(
  ({ productId, branchId, hasImage, alt }) => {
    const containerRef = useRef<HTMLDivElement>(null);
    const [src, setSrc] = useState<string | null>(() =>
      hasImage ? getCachedProductImageUrl(productId, branchId) : null,
    );
    const [failed, setFailed] = useState(false);

    useEffect(() => {
      if (!hasImage || src) return undefined;

      const node = containerRef.current;
      if (!node) return undefined;

      let cancelled = false;

      const load = () => {
        void fetchProductImageUrl(productId, branchId).then((url) => {
          if (cancelled) return;
          if (url) {
            setSrc(url);
          } else {
            setFailed(true);
          }
        });
      };

      if (typeof IntersectionObserver === 'undefined') {
        load();
        return () => {
          cancelled = true;
        };
      }

      const observer = new IntersectionObserver(
        (entries) => {
          if (entries.some((entry) => entry.isIntersecting)) {
            observer.disconnect();
            load();
          }
        },
        { rootMargin: '120px' },
      );

      observer.observe(node);

      return () => {
        cancelled = true;
        observer.disconnect();
      };
    }, [branchId, hasImage, productId, src]);

    if (!hasImage || failed) {
      return PLACEHOLDER;
    }

    return (
      <div ref={containerRef} className="h-[88px] w-full shrink-0 overflow-hidden bg-gray-100">
        {src ? (
          <img
            src={src}
            alt={alt}
            loading="lazy"
            decoding="async"
            className="h-full w-full object-cover"
            onError={() => setFailed(true)}
          />
        ) : (
          PLACEHOLDER
        )}
      </div>
    );
  },
);

ProductThumbnail.displayName = 'ProductThumbnail';

export default ProductThumbnail;
