import { clinicLogoText } from '../content/clinic-logo-settings';
import { BRAND_IMAGE_RECOMMENDED_PX } from './brand-image-rules';

type Size = { width?: number; height?: number } | undefined;

/** A logo may be any shape; it is only worth a word when it is small. */
export function logoWarning(size: Size): string | undefined {
  if (!size?.width || !size.height) return undefined;
  const longSide = Math.max(size.width, size.height);
  return longSide < BRAND_IMAGE_RECOMMENDED_PX ? clinicLogoText.small(longSide) : undefined;
}

/** A tab icon is drawn in a square, so a picture that is not one, or is small, is worth a word. */
export function tabIconWarning(size: Size): string | undefined {
  if (!size?.width || !size.height) return undefined;
  if (size.width !== size.height) return clinicLogoText.tabIcon.notSquare(size.width, size.height);
  return size.width < BRAND_IMAGE_RECOMMENDED_PX ? clinicLogoText.small(size.width) : undefined;
}
