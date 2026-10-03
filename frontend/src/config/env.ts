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
