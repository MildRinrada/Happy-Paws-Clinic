import type { Metadata } from 'next';
import type { ClinicIdentity } from '../types/clinic-identity.types';

/** A short stamp of the pictures in use, so the browser fetches the tab icon again when one changes. */
function stamp(...values: (string | undefined)[]): string {
  let hash = 5381;
  for (const char of values.join('|')) hash = ((hash * 33) ^ char.charCodeAt(0)) >>> 0;
  return hash.toString(36);
}

/**
 * The tab icon and the phone's home-screen icon (docs/contract-extensions.md § 50). With no logo
 * and no tab icon the page names the paw under /public, exactly as before; otherwise the two
 * routes under /brand draw the clinic's own.
 */
export function clinicIconsMetadata(identity: ClinicIdentity): Metadata['icons'] {
  if (!identity.logoSrc && !identity.tabIconSrc) {
    return { icon: [{ url: '/icon.svg', type: 'image/svg+xml' }] };
  }

  const v = stamp(identity.tabIconSrc, identity.logoSrc);

  return {
    icon: [{ url: `/brand/tab-icon?v=${v}` }],
    apple: [{ url: `/brand/touch-icon?v=${v}`, sizes: '180x180', type: 'image/png' }],
  };
}
