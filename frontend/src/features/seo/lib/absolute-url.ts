import { siteEnv } from '@/config/env';

/**
 * A full address on the public site, for the places that need one: structured data, the sitemap
 * and robots.txt. `metadata` resolves relative paths itself through `metadataBase`.
 *
 * A value that is already absolute (an `https://` logo link from the API) is returned unchanged.
 */
export function absoluteUrl(path: string): string {
  if (/^https?:\/\//i.test(path)) {
    return path;
  }

  return `${siteEnv.url()}${path.startsWith('/') ? path : `/${path}`}`;
}
