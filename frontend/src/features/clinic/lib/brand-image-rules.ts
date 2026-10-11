import type { BrandImageType } from '../types/settings.types';

/**
 * The limits the API keeps on the logo and the tab icon (docs/contract-extensions.md § 50),
 * checked here first to save a trip. No SVG: an SVG can carry script.
 */
export const BRAND_IMAGE_TYPES: readonly BrandImageType[] = ['image/png', 'image/jpeg', 'image/webp'];
export const BRAND_IMAGE_MAX_BYTES = 5 * 1024 * 1024;

/** Smaller than this on its long side looks soft on a sharp screen. Advice, not a refusal. */
export const BRAND_IMAGE_RECOMMENDED_PX = 512;

export function brandImageTypeOf(file: File): BrandImageType | undefined {
  const type = file.type.toLowerCase();
  return BRAND_IMAGE_TYPES.find((allowed) => allowed === type);
}

/** The picture's size in pixels, read by the browser from the chosen file, or undefined if it cannot. */
export function readImageSize(src: string): Promise<{ width: number; height: number } | undefined> {
  return new Promise((resolve) => {
    const image = new Image();
    image.onload = () => resolve({ width: image.naturalWidth, height: image.naturalHeight });
    image.onerror = () => resolve(undefined);
    image.src = src;
  });
}
