import Link from 'next/link';
import { ClinicLogo } from '@/components/layout/clinic-logo/clinic-logo';
import { ClinicNameSlot } from '@/components/layout/clinic-logo/clinic-name-slot';
import { ClinicNameText } from '@/components/layout/clinic-logo/clinic-name-text';

/**
 * The logo on a white card at the top of the slideshow, so it reads on any photo. Links home. The
 * clinic's own logo replaces the paw when it has set one (docs/contract-extensions.md § 50).
 */
export function ShowcaseBadge() {
  return (
    <Link href="/" className="flex w-fit items-center gap-2.5 rounded-2xl bg-white/95 px-4 py-2.5 shadow-sm">
      <ClinicLogo markClassName="size-9" imageClassName="h-9 max-w-[180px]" aloneImageClassName="h-9 max-w-[320px]" />
      <ClinicNameSlot>
        <span className="font-wordmark text-lg font-bold tracking-tight text-ink-900">
          <ClinicNameText />
        </span>
      </ClinicNameSlot>
    </Link>
  );
}
