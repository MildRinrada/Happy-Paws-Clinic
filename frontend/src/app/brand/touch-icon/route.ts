import { brandIconResponse } from '@/features/clinic/lib/brand-icon-response';

// Read on each request; the profile behind it is shared for five minutes and the phone keeps the
// answer as long.
export const dynamic = 'force-dynamic';

/**
 * The icon a phone puts on its home screen (`apple-touch-icon`, docs/contract-extensions.md § 50):
 * the clinic's tab icon or logo drawn on white in a 180 px square, since the phone turns
 * transparency black and does not round a picture that is not square.
 */
export function GET() {
  return brandIconResponse({ size: 180, background: '#ffffff', padding: 0.1, drawTabIcon: true });
}
