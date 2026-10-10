import Link from 'next/link';
import { emergencyLink } from '@/config/site-navigation';

/**
 * "ฉุกเฉิน" at the header's right edge, on every screen: a small red-outlined button with a phone
 * icon, the word shown from a small tablet up and the icon alone on a phone, where it sits beside
 * the menu button. Kept out of the menu row, which is full at desktop width.
 */
export function EmergencyLink() {
  return (
    <Link
      href={emergencyLink.href}
      aria-label={emergencyLink.ariaLabel}
      className="inline-flex h-11 shrink-0 items-center justify-center gap-1.5 rounded-full border border-site-alert/40 px-3 text-sm font-bold text-site-alert transition hover:bg-site-alert hover:text-white max-sm:w-11 max-sm:px-0 xl:h-10"
    >
      <svg viewBox="0 0 24 24" aria-hidden="true" className="size-[18px] shrink-0" fill="currentColor">
        <path d="M6.6 10.8a15.1 15.1 0 0 0 6.6 6.6l2.2-2.2a1 1 0 0 1 1-.25 11.4 11.4 0 0 0 3.6.57 1 1 0 0 1 1 1V20a1 1 0 0 1-1 1A17 17 0 0 1 3 4a1 1 0 0 1 1-1h3.5a1 1 0 0 1 1 1c0 1.25.2 2.45.57 3.57a1 1 0 0 1-.25 1L6.6 10.8Z" />
      </svg>
      <span className="max-sm:sr-only">{emergencyLink.label}</span>
    </Link>
  );
}
