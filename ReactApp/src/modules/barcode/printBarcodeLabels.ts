import { buildVariantUnitLine, computeBarcodeLabelScale, truncateBarcodeForLabel } from './barcodeLabelHelpers';
import { generateBarcodeSvg } from './generateBarcodeSvg';
import { formatLabelPrice } from './barcodeUtils';
import { LabelDimensions } from './labelSize';

export interface BarcodeLabelData {
  productName: string;
  variantName?: string | null;
  unitName?: string | null;
  barcode: string;
  price?: number;
  showPrice?: boolean;
  priceFormatted?: string;
  qty: number;
  companyName?: string;
}

export interface PrintBarcodeLabelsOptions {
  labelSize: LabelDimensions;
  showPrice?: boolean;
  showVariant?: boolean;
  showUnit?: boolean;
  currencySymbol?: string;
  currencyCode?: string;
  companyName?: string;
}

const escapeHtml = (value: string): string =>
  value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');

const buildLabelHtml = (
  label: BarcodeLabelData,
  labelSize: LabelDimensions,
  options: PrintBarcodeLabelsOptions,
): string => {
  const showPrice = label.showPrice ?? options.showPrice ?? true;
  const showVariant = options.showVariant ?? true;
  const showUnit = options.showUnit ?? true;
  const variantLine = buildVariantUnitLine(label.variantName, label.unitName, { showVariant, showUnit });
  const companyName = label.companyName ?? options.companyName;
  const hasCompany = Boolean(companyName?.trim());

  const scale = computeBarcodeLabelScale(labelSize, {
    showVariantLine: Boolean(variantLine),
    showPrice,
    showCompany: hasCompany,
  });

  const barcodeValue = label.barcode.trim();
  const barcodeSvg = generateBarcodeSvg(barcodeValue, {
    width: scale.barcodeWidth,
    height: scale.barcodeHeight,
  });
  const displayBarcode = truncateBarcodeForLabel(barcodeValue);
  const priceText = label.priceFormatted
    ?? (showPrice && label.price != null
      ? formatLabelPrice(label.price, options.currencySymbol ?? 'Rs.', options.currencyCode ?? 'PKR')
      : '');

  const companyHtml = hasCompany
    ? `<p style="font-size:${scale.companyPx}px;line-height:1.1;margin:0 0 ${scale.headerGapMm}mm;overflow:hidden;text-overflow:ellipsis;white-space:nowrap">${escapeHtml(companyName!.trim())}</p>`
    : '';

  const variantHtml = variantLine
    ? `<p style="font-size:${scale.variantPx}px;line-height:1.15;margin:${scale.headerGapMm}mm 0 0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap">${escapeHtml(variantLine)}</p>`
    : '';

  const priceHtml = showPrice && priceText
    ? `<footer style="font-size:${scale.pricePx}px;line-height:1.1;font-weight:bold;margin:${scale.priceGapMm}mm 0 0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap">${escapeHtml(priceText)}</footer>`
    : '';

  const { labelWidth, labelHeight } = labelSize;

  return `
    <div class="label" style="width:${labelWidth}mm;height:${labelHeight}mm;padding:${scale.paddingMm}mm">
      <header style="margin-bottom:${scale.sectionGapMm}mm;text-align:center">
        ${companyHtml}
        <p style="font-size:${scale.productNamePx}px;line-height:1.15;font-weight:bold;margin:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap">${escapeHtml(label.productName.trim())}</p>
        ${variantHtml}
      </header>
      <section style="flex:1;display:flex;flex-direction:column;align-items:center;justify-content:center;min-height:0;margin-bottom:${scale.sectionGapMm}mm;text-align:center">
        <div style="max-height:${scale.barcodeMaxHeightMm}mm;max-width:100%;overflow:hidden;display:flex;align-items:center;justify-content:center">${barcodeSvg}</div>
        <p style="font-size:${scale.barcodeTextPx}px;line-height:1.2;letter-spacing:0.8px;font-family:Consolas,'Courier New',monospace;margin:${scale.barcodeTextGapMm}mm 0 0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;max-width:100%">${escapeHtml(displayBarcode)}</p>
      </section>
      ${priceHtml}
    </div>`;
};

const buildPrintHtml = (
  labels: BarcodeLabelData[],
  labelSize: LabelDimensions,
  options: PrintBarcodeLabelsOptions,
): string => {
  const sheets = labels.flatMap((label) =>
    Array.from({ length: label.qty }, () => label),
  );

  const labelHtml = sheets.map((label) => buildLabelHtml(label, labelSize, options)).join('');

  return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <title>Barcode Labels</title>
  <style>
    * { box-sizing: border-box; margin: 0; padding: 0; }
    @page { size: auto; margin: 8mm; }
    body {
      font-family: Arial, sans-serif;
      background: #fff;
      color: #000;
      padding: 4mm;
    }
    .print-area {
      display: flex;
      flex-wrap: wrap;
      gap: 2mm;
      align-items: flex-start;
    }
    .label {
      display: flex;
      flex-direction: column;
      align-items: stretch;
      text-align: center;
      overflow: hidden;
      background: #fff;
      color: #000;
      page-break-inside: avoid;
      break-inside: avoid;
    }
    .label svg {
      max-width: 100%;
      height: auto;
    }
  </style>
</head>
<body>
  <div class="print-area">${labelHtml}</div>
  <script>
    window.onload = function () {
      setTimeout(function () { window.print(); }, 200);
    };
  </script>
</body>
</html>`;
};

export const printBarcodeLabels = (
  labels: BarcodeLabelData[],
  options: PrintBarcodeLabelsOptions,
): boolean => {
  if (labels.length === 0) return false;

  const printWindow = window.open('', '_blank', 'width=800,height=600');
  if (!printWindow) return false;

  printWindow.document.open();
  printWindow.document.write(buildPrintHtml(labels, options.labelSize, options));
  printWindow.document.close();

  printWindow.onafterprint = () => {
    printWindow.close();
  };

  return true;
};
