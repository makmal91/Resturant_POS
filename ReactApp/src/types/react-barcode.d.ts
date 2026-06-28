declare module 'react-barcode' {
  import { Component } from 'react';

  export interface BarcodeProps {
    value: string;
    format?: string;
    width?: number;
    height?: number;
    fontSize?: number;
    margin?: number;
    displayValue?: boolean;
    lineColor?: string;
    background?: string;
  }

  export default class Barcode extends Component<BarcodeProps> {}
}
