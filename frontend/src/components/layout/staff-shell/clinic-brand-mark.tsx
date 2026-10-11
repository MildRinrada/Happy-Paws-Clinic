import { staffShellText } from '@/features/staff-shell/content/staff-shell-text';
import type { ClinicBrand } from '@/features/staff-shell/lib/load-clinic-brand';
import { ClinicLogo } from '../clinic-logo/clinic-logo';
import { ClinicNameSlot } from '../clinic-logo/clinic-name-slot';

interface ClinicBrandMarkProps {
  brand: ClinicBrand;
  /** The phone bar has no room for the second line. */
  compact?: boolean;
}

/**
 * The clinic's logo and name at the top of the menu. The stand-in paw is drawn when the clinic has
 * set no logo (docs/contract-extensions.md § 50), and the name and tagline are left out when the
 * logo already spells the name. In a folded sidebar only the logo stays on screen, shrunk to fit.
 */
export function ClinicBrandMark({ brand, compact = false }: ClinicBrandMarkProps) {
  return (
    <div className="flex min-w-0 items-center gap-2.5 group-data-[collapsed=true]/sidebar:justify-center">
      <ClinicLogo
        markClassName="size-9 shrink-0"
        imageClassName="h-9 max-w-[96px] group-data-[collapsed=true]/sidebar:max-w-10"
        aloneImageClassName="h-9 max-w-[200px] group-data-[collapsed=true]/sidebar:max-w-10"
      />
      <ClinicNameSlot>
        <div className="min-w-0 group-data-[collapsed=true]/sidebar:sr-only">
          <p className="truncate font-display text-base font-semibold leading-tight text-brand-800">{brand.name}</p>
          {!compact && <p className="truncate text-xs text-ink-500">{staffShellText.brandTagline}</p>}
        </div>
      </ClinicNameSlot>
    </div>
  );
}
