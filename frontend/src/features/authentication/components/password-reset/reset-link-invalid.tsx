import Link from 'next/link';
import { Alert } from '@/components/ui/alert';
import { resetPasswordPage } from '../../content/password-reset-page';

/**
 * A link with no token, or one the backend refused. Wrong, used and expired all read the same,
 * because the backend gives one answer for the three.
 */
export function ResetLinkInvalid() {
  const text = resetPasswordPage.invalid;

  return (
    <div className="mt-10 space-y-4">
      <Alert>
        <p className="font-semibold">{text.title}</p>
        <p className="mt-1">{text.body}</p>
      </Alert>
      <Link href={text.again.href} className="inline-block text-[15px] font-bold text-site-teal hover:underline">
        {text.again.label}
      </Link>
    </div>
  );
}
