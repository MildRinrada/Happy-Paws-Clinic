/**
 * Environment access, validated once at module load so a missing variable fails at boot
 * rather than at the first request.
 *
 * `API_BASE_URL` is server-only — it is used by server components and route handlers that
 * attach the session cookie. `NEXT_PUBLIC_API_BASE_URL` is the browser-visible base used
 * only for the contract's public reads (`/clinic`, `/services`, `/booking/availability`).
 */

function required(name: string, value: string | undefined): string {
  if (!value) {
    throw new Error(`Missing environment variable: ${name}`);
  }
  return value;
}

export const serverEnv = {
  apiBaseUrl: () => required('API_BASE_URL', process.env.API_BASE_URL),
} as const;

export const publicEnv = {
  apiBaseUrl: () => required('NEXT_PUBLIC_API_BASE_URL', process.env.NEXT_PUBLIC_API_BASE_URL),
} as const;

/**
 * The public website's own address, such as `https://www.happypaws.example`, with no trailing
 * slash. Canonical links, Open Graph, the sitemap and structured data are built on it.
 *
 * Optional on purpose: a missing value must not take the site down, so it falls back to the local
 * dev server. Next inlines `NEXT_PUBLIC_*` at build time, so set it before `next build`.
 */
export const siteEnv = {
  url: () => (process.env.NEXT_PUBLIC_SITE_URL || 'http://localhost:3000').replace(/\/+$/, ''),
} as const;
