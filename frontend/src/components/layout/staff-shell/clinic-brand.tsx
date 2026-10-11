import { Suspense } from 'react';
import { staffShellText } from '@/features/staff-shell/content/staff-shell-text';
import { ClinicBrandMark } from './clinic-brand-mark';
import { LoadedClinicBrand } from './loaded-clinic-brand';

/**
 * The clinic's name and logo, streamed in: the frame is drawn at once with the name it always had,
 * and the clinic's own replaces it when `GET /clinic` answers.
 */
export function ClinicBrand({ compact = false }: { compact?: boolean }) {
  return (
    <Suspense fallback={<ClinicBrandMark brand={{ name: staffShellText.brandFallback }} compact={compact} />}>
      <LoadedClinicBrand compact={compact} />
    </Suspense>
  );
}
