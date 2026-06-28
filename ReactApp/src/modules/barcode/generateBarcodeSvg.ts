import JsBarcode from 'jsbarcode';

export interface BarcodeSvgOptions {
  width?: number;
  height?: number;
}

const sanitizeBarcodeValue = (value: string): string => value.replace(/\s+/g, '').trim();

export const generateBarcodeSvg = (value: string, options: BarcodeSvgOptions = {}): string => {
  const sanitized = sanitizeBarcodeValue(value);
  if (!sanitized) return '';

  const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
  JsBarcode(svg, sanitized, {
    format: 'CODE128',
    width: options.width ?? 1.5,
    height: options.height ?? 40,
    fontSize: 12,
    margin: 0,
    displayValue: false,
    lineColor: '#000000',
    background: '#ffffff',
  });

  return svg.outerHTML;
};
