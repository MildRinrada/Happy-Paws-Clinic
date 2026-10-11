import { cache } from 'react';
import { getCachedClinicProfile } from '../api/getCachedClinicProfile';
import type { ClinicIdentity } from '../types/clinic-identity.types';
import { logoSrcOf, tabIconSrcOf } from './brand-image-src';
import { defaultClinicIdentity } from './default-clinic-identity';

/**
 * The clinic's logo, its name and the switch between them, read once per request for the root
 * layout's frame and the tab icon (docs/contract-extensions.md § 50). Never throws: when the API is
 * away every page keeps the paw it always had.
 */
export const loadClinicIdentity = cache(async (): Promise<ClinicIdentity> => {
  try {
    const profile = await getCachedClinicProfile();

    return {
      name: profile.name.trim() || defaultClinicIdentity.name,
      logoSrc: logoSrcOf(profile.logo_url),
      tabIconSrc: tabIconSrcOf(profile.tab_icon_url),
      showName: profile.show_name_with_logo ?? true,
    };
  } catch {
    return defaultClinicIdentity;
  }
});
