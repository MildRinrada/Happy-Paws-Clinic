import { publicEnv } from '@/config/env';

/**
 * The address a calendar app subscribes to: the API's public base URL (the one ending in `/v1`)
 * and the path the backend gave, which already carries the token. The app fetches it from outside
 * the clinic, so it has to be the public base, not the server-to-server one.
 */
export function feedUrl(path: string, view?: 'branch'): string {
  const url = publicEnv.apiBaseUrl().replace(/\/$/, '') + path;
  return view ? `${url}&view=${view}` : url;
}
