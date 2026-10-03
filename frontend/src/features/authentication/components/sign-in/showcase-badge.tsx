import Link from 'next/link';
import { LogoMark } from '@/components/layout/site-header/logo-mark';

/** The logo on a white card at the top of the slideshow, so it reads on any photo. Links home. */
export function ShowcaseBadge() {
  return (
    <Link href="/" className="flex w-fit items-center gap-2.5 rounded-2xl bg-white/95 px-4 py-2.5 shadow-sm">
      <LogoMark className="size-9" />
      <span className="font-wordmark text-lg font-bold tracking-tight text-ink-900">Happy Paws Clinic</span>
    </Link>
  );
}
