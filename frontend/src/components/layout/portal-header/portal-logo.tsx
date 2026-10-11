import Link from 'next/link';
import { ClinicLogo } from '../clinic-logo/clinic-logo';
import { ClinicNameSlot } from '../clinic-logo/clinic-name-slot';
import { ClinicNameText } from '../clinic-logo/clinic-name-text';

/**
 * The logo at the top left of the portal, the link back to the clinic's site. The short name
 * "Happy Paws", in the clinic's dark green, as the portal's design draws it: on a phone the full
 * name would crowd the bell and the account button. The clinic's own logo replaces the paw when it
 * has set one (docs/contract-extensions.md § 50), and the name is left out when the logo spells it.
 */
export function PortalLogo() {
  return (
    <Link href="/" className="flex shrink-0 items-center gap-2">
      <ClinicLogo
        markClassName="size-8"
        imageClassName="h-8 max-w-[88px] sm:max-w-[140px]"
        aloneImageClassName="h-8 max-w-[150px] sm:max-w-[220px]"
      />
      <ClinicNameSlot>
        <span className="font-display text-base font-semibold text-brand-800">
          <ClinicNameText short="Happy Paws" />
        </span>
      </ClinicNameSlot>
    </Link>
  );
}
