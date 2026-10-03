import Link from 'next/link';
import { LogoMark } from './logo-mark';

const wordmarkTones = {
  // On the white header.
  dark: 'text-ink-900',
  // On the dark footer.
  light: 'text-white',
};

/** The logo and the clinic's name, together the link home. */
export function SiteLogo({ tone = 'dark' }: { tone?: keyof typeof wordmarkTones }) {
  return (
    <Link href="/" className="flex shrink-0 items-center gap-2.5 xl:gap-3">
      <LogoMark className="size-10" />
      <span className={`font-wordmark text-lg font-bold tracking-tight xl:text-[19px] ${wordmarkTones[tone]}`}>
        Happy Paws Clinic
      </span>
    </Link>
  );
}
