import { publicEnv, serverEnv } from '@/config/env';
import { fetchImageBytes, redirectLocation, type ImageBytes } from '@/lib/api/image-fetch';
import type { BrandImageKind } from '../types/settings.types';
import { isUploadedImage } from './brand-image-src';

/** The same limit the API keeps on an upload. Anything larger is not read into memory. */
const MAX_BYTES = 5 * 1024 * 1024;

/**
 * Where the server itself fetches an uploaded picture. `GET /clinic/{kind}` redirects to a signed
 * link on the browser's API address (`NEXT_PUBLIC_API_BASE_URL`); in Docker the server reaches the
 * same API under another name (`API_BASE_URL`), so a link on the browser's address is rewritten to
 * the server's. A link elsewhere, such as a cloud bucket, is used as it is.
 */
async function uploadedAddress(kind: BrandImageKind): Promise<string | undefined> {
  const serverBase = serverEnv.apiBaseUrl().replace(/\/$/, '');
  const location = await redirectLocation(`${serverBase}/clinic/${kind}`);
  if (!location) return undefined;

  const browserBase = publicEnv.apiBaseUrl().replace(/\/$/, '');
  return location.startsWith(browserBase) ? serverBase + location.slice(browserBase.length) : location;
}

/**
 * The bytes of the clinic's logo or tab icon, for drawing it into a square on the server
 * (docs/contract-extensions.md § 50). Undefined when there is none or it cannot be read.
 */
export async function readBrandImage(kind: BrandImageKind, value?: string): Promise<ImageBytes | undefined> {
  const address = isUploadedImage(value) ? await uploadedAddress(kind) : value && /^https:\/\//i.test(value) ? value : undefined;

  return address ? fetchImageBytes(address, MAX_BYTES) : undefined;
}
