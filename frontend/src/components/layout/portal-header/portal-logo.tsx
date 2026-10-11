import Link from 'next/link';
import { LogoMark } from '../site-header/logo-mark';

/**
 * The logo at the top left of the portal, the link back to the clinic's site. The short name
 * "Happy Paws", in the clinic's dark green, as the portal's design draws it: on a phone the full
 * name would crowd the bell and the account button.
 */
export function PortalLogo() {
  return (
    <Link href="/" className="flex shrink-0 items-center gap-2">
      <LogoMark className="size-8" />
      <span className="font-display text-base font-semibold text-brand-800">Happy Paws</span>
    </Link>
  );
}
