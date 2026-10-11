import { cache } from 'react';
import { getClinicProfile } from '@/features/clinic/api/getClinicProfile';
import { staffShellText } from '../content/staff-shell-text';

export interface ClinicBrand {
  name: string;
}

/**
 * The clinic's own name for the console's frame, from the public `GET /clinic`. Read once per
 * request though the sidebar and the phone bar both draw it. When the read fails the frame keeps
 * the name it always had, rather than a page of errors over a label. The logo comes from the root
 * layout's `ClinicIdentityProvider`, as on every other page (docs/contract-extensions.md § 50).
 */
export const loadClinicBrand = cache(async (): Promise<ClinicBrand> => {
  try {
    const profile = await getClinicProfile();
    return { name: profile.name.trim() || staffShellText.brandFallback };
  } catch {
    return { name: staffShellText.brandFallback };
  }
});
