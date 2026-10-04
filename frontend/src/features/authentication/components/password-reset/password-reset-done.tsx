import Link from 'next/link';
import { Alert } from '@/components/ui/alert';
import { resetPasswordPage } from '../../content/password-reset-page';

/** The password is set. No session is made here: the person signs in with the new password. */
export function PasswordResetDone() {
  const text = resetPasswordPage.done;

  return (
    <div className="mt-10 space-y-5">
      <Alert tone="success">
        <p className="font-semibold">{text.title}</p>
        <p className="mt-1">{text.body}</p>
      </Alert>
      <Link
        href={text.signIn.href}
        className="flex h-12 w-full items-center justify-center rounded-[10px] bg-site-orange text-[15px] font-bold text-white transition hover:brightness-95"
      >
        {text.signIn.label}
      </Link>
    </div>
  );
}
