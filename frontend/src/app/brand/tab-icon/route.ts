import { brandIconResponse } from '@/features/clinic/lib/brand-icon-response';

// Read on each request; the profile behind it is shared for five minutes and the browser keeps the
// answer as long.
export const dynamic = 'force-dynamic';

/**
 * The browser tab's icon (docs/contract-extensions.md § 50): the clinic's square tab icon, else its
 * logo fitted into a square, else the paw. The root layout names it only when the clinic has a
 * logo or a tab icon; otherwise the page names `/icon.svg` itself.
 */
export function GET() {
  return brandIconResponse({ size: 64 });
}
