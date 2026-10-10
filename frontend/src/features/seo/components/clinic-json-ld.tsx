import { loadSiteInfo } from '@/features/clinic/lib/load-site-info';
import { clinicSchema } from '../lib/clinic-schema';
import { loadClinicProfile } from '../lib/load-clinic-profile';
import { JsonLd } from './json-ld';

/**
 * The clinic as a local business, for the home and contact pages. Reads the same contact list,
 * hours and branches the footer already loaded for this request, so it costs one extra call.
 */
export async function ClinicJsonLd() {
  const [profile, site] = await Promise.all([loadClinicProfile(), loadSiteInfo()]);

  return <JsonLd data={clinicSchema(profile, site)} />;
}
