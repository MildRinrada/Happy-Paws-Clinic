import QRCode from 'qrcode';

/**
 * Draws an `otpauth://` URI as a QR code image, on the server.
 *
 * Done here rather than with a third-party image service: the URI holds the second-factor secret,
 * and it must not leave the clinic's own servers to be drawn.
 */
export function qrCodeDataUrl(text: string): Promise<string> {
  return QRCode.toDataURL(text, { margin: 1, width: 224, errorCorrectionLevel: 'M' });
}
