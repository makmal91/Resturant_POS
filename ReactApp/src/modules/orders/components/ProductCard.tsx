import React from 'react';
import { POS_INTERACTION, POS_THEME } from '../theme';
import ProductThumbnail from './ProductThumbnail';

export interface ProductCardProps {
  productId: number;
  name: string;
  priceLabel: string;
  branchId: number;
  hasImage: boolean;
  onSelect: (productId: number) => void;
  disabled?: boolean;
}

const ProductCard: React.FC<ProductCardProps> = React.memo(
  ({ productId, name, priceLabel, branchId, hasImage, onSelect, disabled = false }) => (
    <button
      type="button"
      onClick={() => onSelect(productId)}
      disabled={disabled}
      className={`relative flex flex-col min-h-[148px] rounded-lg border border-gray-200 bg-white text-left overflow-hidden disabled:cursor-not-allowed disabled:opacity-50 ${POS_INTERACTION.card}`}
    >
      <ProductThumbnail
        productId={productId}
        branchId={branchId}
        hasImage={hasImage}
        alt={name}
      />

      <div className="flex flex-col flex-1 p-3">
        <p className="font-semibold text-gray-900 text-sm leading-snug line-clamp-2">{name}</p>

        <div className="mt-auto pt-2 flex items-end justify-between gap-2">
          <p className="text-sm font-medium text-gray-700 tabular-nums">{priceLabel}</p>
          <span className="shrink-0 text-[10px] font-medium uppercase tracking-wide text-gray-400 bg-gray-50 border border-gray-200 rounded px-1.5 py-0.5">
            Tap
          </span>
        </div>
      </div>

      {disabled && (
        <span className="absolute inset-0 flex items-center justify-center bg-white/80">
          <span
            className="w-5 h-5 border-2 border-t-transparent rounded-full animate-spin"
            style={{ borderColor: POS_THEME.primary, borderTopColor: 'transparent' }}
          />
        </span>
      )}
    </button>
  ),
);

ProductCard.displayName = 'ProductCard';

export default ProductCard;
