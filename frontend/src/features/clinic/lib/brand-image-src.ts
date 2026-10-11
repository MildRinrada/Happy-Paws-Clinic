import type { BrandImageKind } from '../types/settings.types';

/**
 * Where a browser loads the clinic's logo and tab icon from (docs/contract-extensions.md § 50).
 *
 * An uploaded picture is stored as `storage:clinic-brand/…`, which is a name, not an address. The
 * API serves it at a fixed public address, `GET /clinic/logo` or `GET /clinic/tab-icon`, that
 * redirects to a fresh signed link. `?v=` is the stored file's name, so a new upload is a new
 * address and no browser keeps showing the old picture.
 */

const UPLOADED = 'storage:';

export function isUploadedImage(value?: string): boolean {
  return Boolean(value?.startsWith(UPLOADED));
}

const isWebAddress = (value?: string): value is string => Boolean(value && /^https?:\/\//i.test(value));

/** The browser-visible API base, or undefined when it is not configured (the page then keeps the paw). */
function apiBase(): string | undefined {
  const base = process.env.NEXT_PUBLIC_API_BASE_URL;
  return base ? base.replace(/\/$/, '') : undefined;
}

/** The fixed public address of an uploaded picture. */
function uploadedSrc(kind: BrandImageKind, value: string): string | undefined {
  const base = apiBase();
  const file = value.slice(value.lastIndexOf('/') + 1);
  return base ? `${base}/clinic/${kind}?v=${encodeURIComponent(file)}` : undefined;
}

/** The logo as an image address: the uploaded one, an `https://` address as typed, or nothing (the paw). */
export function logoSrcOf(logoUrl?: string): string | undefined {
  if (isUploadedImage(logoUrl)) return uploadedSrc('logo', logoUrl!);
  return isWebAddress(logoUrl) ? logoUrl : undefined;
}

/** The uploaded tab icon as an image address, or nothing. */
export function tabIconSrcOf(tabIconUrl?: string): string | undefined {
  return isUploadedImage(tabIconUrl) ? uploadedSrc('tab-icon', tabIconUrl!) : undefined;
}
