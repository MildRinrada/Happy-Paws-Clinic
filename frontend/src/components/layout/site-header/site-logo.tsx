import Link from 'next/link';
import { ClinicLogo } from '../clinic-logo/clinic-logo';
import { ClinicNameSlot } from '../clinic-logo/clinic-name-slot';
import { ClinicNameText } from '../clinic-logo/clinic-name-text';

const wordmarkTones = {
  // On the white header.
  dark: 'text-ink-900',
  // On the dark footer.
  light: 'text-white',
};

/**
 * The logo and the clinic's name, together the link home. The clinic's own logo replaces the paw
 * when it has set one (docs/contract-extensions.md § 50), kept narrow enough for a phone's header,
 * and the name is left out when the logo already spells it.
 */
export function SiteLogo({ tone = 'dark' }: { tone?: keyof typeof wordmarkTones }) {
  return (
    <Link href="/" className="flex shrink-0 items-center gap-2.5 xl:gap-3">
      <ClinicLogo
        markClassName="size-10"
        imageClassName="h-10 max-w-[96px] sm:max-w-[160px]"
        aloneImageClassName="h-10 max-w-[160px] sm:max-w-[240px]"
      />
      <ClinicNameSlot>
        <span className={`font-wordmark text-lg font-bold tracking-tight xl:text-[19px] ${wordmarkTones[tone]}`}>
          <ClinicNameText />
        </span>
      </ClinicNameSlot>
    </Link>
  );
}
