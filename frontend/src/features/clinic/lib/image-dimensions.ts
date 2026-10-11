/**
 * The pixel size of a PNG or JPEG from its first bytes, or undefined for anything else. Enough to
 * fit a logo into a square without an image library: the PNG header holds it at a fixed place, and
 * a JPEG holds it in its first start-of-frame marker.
 */
export function imageDimensions(bytes: Uint8Array): { width: number; height: number } | undefined {
  const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);

  if (bytes.length >= 24 && bytes[0] === 0x89 && bytes[1] === 0x50 && bytes[2] === 0x4e && bytes[3] === 0x47) {
    return { width: view.getUint32(16), height: view.getUint32(20) };
  }

  if (bytes.length >= 4 && bytes[0] === 0xff && bytes[1] === 0xd8) {
    let offset = 2;

    while (offset + 9 < bytes.length) {
      if (bytes[offset] !== 0xff) return undefined;
      const marker = bytes[offset + 1]!;
      const length = view.getUint16(offset + 2);

      // SOF0 to SOF15, except DHT (C4), JPG (C8) and DAC (CC), carry the frame's size.
      if (marker >= 0xc0 && marker <= 0xcf && marker !== 0xc4 && marker !== 0xc8 && marker !== 0xcc) {
        return { width: view.getUint16(offset + 7), height: view.getUint16(offset + 5) };
      }

      offset += 2 + length;
    }
  }

  return undefined;
}
