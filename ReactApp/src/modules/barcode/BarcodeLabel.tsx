import React, { useMemo } from 'react';
import Barcode from 'react-barcode';
import { buildVariantUnitLine, computeBarcodeLabelScale, truncateBarcodeForLabel } from './barcodeLabelHelpers';
import { formatLabelPrice } from './barcodeUtils';

export interface BarcodeLabelProps {
  productName: string;
  variantName?: string | null;
  unitName?: string | null;
  barcode: string;
  price?: number;
  showPrice?: boolean;
  showVariant?: boolean;
  showUnit?: boolean;
  priceFormatted?: string;
  width: number;
  height: number;
  companyName?: string;
  showBorder?: boolean;
  currencySymbol?: string;
  currencyCode?: string;
  className?: string;
}

const BarcodeLabel: React.FC<BarcodeLabelProps> = ({
  productName,
  variantName,
  unitName,
  barcode,
  price = 0,
  showPrice = true,
  showVariant = true,
  showUnit = true,
  priceFormatted,
  width,
  height,
  companyName,
  showBorder = false,
  currencySymbol = 'Rs.',
  currencyCode = 'PKR',
  className = '',
}) => {
  const variantLine = buildVariantUnitLine(variantName, unitName, { showVariant, showUnit });
  const hasCompany = Boolean(companyName?.trim());

  const scale = useMemo(
    () => computeBarcodeLabelScale(
      { labelWidth: width, labelHeight: height },
      { showVariantLine: Boolean(variantLine), showPrice, showCompany: hasCompany },
    ),
    [width, height, variantLine, showPrice, hasCompany],
  );

  const displayPrice = priceFormatted
    ?? (showPrice ? formatLabelPrice(price, currencySymbol, currencyCode) : '');

  const barcodeValue = barcode.trim();
  const displayBarcode = truncateBarcodeForLabel(barcodeValue);

  return (
    <div
      className={[
        'barcode-label',
        'box-border flex h-full w-full flex-col items-center overflow-hidden bg-white text-center font-sans text-black',
        showBorder ? 'border border-dashed border-gray-400' : 'border border-transparent',
        className,
      ].filter(Boolean).join(' ')}
      style={{
        width: `${width}mm`,
        height: `${height}mm`,
        padding: `${scale.paddingMm}mm`,
        fontFamily: 'Arial, sans-serif',
      }}
    >
      <header
        className="w-full shrink-0 text-center"
        style={{ marginBottom: `${scale.sectionGapMm}mm` }}
      >
        {hasCompany && (
          <p
            className="w-full truncate text-black"
            style={{
              fontSize: `${scale.companyPx}px`,
              lineHeight: 1.1,
              marginBottom: `${scale.headerGapMm}mm`,
            }}
          >
            {companyName!.trim()}
          </p>
        )}
        <p
          className="w-full truncate font-bold text-black"
          style={{ fontSize: `${scale.productNamePx}px`, lineHeight: 1.15 }}
          title={productName}
        >
          {productName}
        </p>
        {variantLine && (
          <p
            className="w-full truncate text-black"
            style={{
              fontSize: `${scale.variantPx}px`,
              lineHeight: 1.15,
              marginTop: `${scale.headerGapMm}mm`,
            }}
            title={variantLine}
          >
            {variantLine}
          </p>
        )}
      </header>

      <section
        className="flex w-full min-h-0 flex-1 flex-col items-center justify-center"
        style={{ marginBottom: `${scale.sectionGapMm}mm` }}
      >
        {barcodeValue && (
          <div
            className="flex w-full items-center justify-center overflow-hidden"
            style={{ maxHeight: `${scale.barcodeMaxHeightMm}mm` }}
          >
            <Barcode
              value={barcodeValue}
              format="CODE128"
              width={scale.barcodeWidth}
              height={scale.barcodeHeight}
              fontSize={12}
              margin={0}
              displayValue={false}
              lineColor="#000000"
              background="#ffffff"
            />
          </div>
        )}
        <p
          className="w-full shrink-0 truncate font-mono text-black"
          style={{
            fontSize: `${scale.barcodeTextPx}px`,
            letterSpacing: '0.8px',
            lineHeight: 1.2,
            marginTop: `${scale.barcodeTextGapMm}mm`,
          }}
          title={barcodeValue}
        >
          {displayBarcode}
        </p>
      </section>

      {showPrice && displayPrice && (
        <footer
          className="w-full shrink-0 truncate text-center font-bold text-black"
          style={{
            fontSize: `${scale.pricePx}px`,
            lineHeight: 1.1,
            marginTop: `${scale.priceGapMm}mm`,
          }}
        >
          {displayPrice}
        </footer>
      )}
    </div>
  );
};

export default BarcodeLabel;
