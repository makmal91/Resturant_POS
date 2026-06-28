import React from 'react';
import BarcodeLabel, { BarcodeLabelProps } from './BarcodeLabel';

export interface BarcodeLabelGridProps {
  labels: Array<BarcodeLabelProps & { key?: React.Key }>;
  className?: string;
  /** Adds print-area class for in-page thermal printing. */
  printArea?: boolean;
}

/**
 * Renders multiple barcode labels in a flex wrap grid (2mm gap).
 *
 * @example
 * <BarcodeLabelGrid
 *   printArea
 *   labels={[
 *     { productName: 'Shirt', variantName: 'Red Large', unitName: 'BOX', barcode: 'AKH001', price: 1200, width: 50, height: 25 },
 *   ]}
 * />
 */
const BarcodeLabelGrid: React.FC<BarcodeLabelGridProps> = ({
  labels,
  className = '',
  printArea = false,
}) => (
  <div
    className={[
      'flex flex-wrap gap-[2mm]',
      printArea ? 'print-area' : '',
      className,
    ].filter(Boolean).join(' ')}
  >
    {labels.map((label, index) => {
      const { key, ...props } = label;
      return <BarcodeLabel key={key ?? `${props.barcode}-${index}`} {...props} />;
    })}
  </div>
);

export default BarcodeLabelGrid;
