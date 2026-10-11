import { loadClinicBrand } from '@/features/staff-shell/lib/load-clinic-brand';
import { ClinicBrandMark } from './clinic-brand-mark';

/** The brand once `GET /clinic` has answered. */
export async function LoadedClinicBrand({ compact }: { compact: boolean }) {
  return <ClinicBrandMark brand={await loadClinicBrand()} compact={compact} />;
}
