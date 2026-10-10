import { loadSiteInfo } from '@/features/clinic/lib/load-site-info';
import { emergencySchema } from '../lib/emergency-schema';
import { loadClinicProfile } from '../lib/load-clinic-profile';
import { JsonLd } from './json-ld';

/**
 * The emergency page's line for search engines, from the same contact list, hours and branches
 * the page already loaded for this request. `telephone` is the number the page shows first.
 */
export async function EmergencyJsonLd({ telephone }: { telephone?: string }) {
  const [profile, site] = await Promise.all([loadClinicProfile(), loadSiteInfo()]);

  return <JsonLd data={emergencySchema(profile, site, telephone)} />;
}
