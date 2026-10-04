import Link from 'next/link';
import { registerPage } from '../../content/register-page';

/** The line under the sign-up button that points to the privacy policy. */
export function PrivacyNote() {
  const { before, link, after } = registerPage.privacyNote;

  return (
    <p className="text-sm text-site-muted">
      {before}{' '}
      <Link href={link.href} className="font-semibold text-site-teal hover:underline">
        {link.label}
      </Link>{' '}
      {after}
    </p>
  );
}
