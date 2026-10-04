import Link from 'next/link';
import { registerPage } from '../../content/register-page';

/** "มีบัญชีอยู่แล้ว? เข้าสู่ระบบ", under the sign-up form. */
export function SignInPrompt() {
  const { signInPrompt, signIn } = registerPage;

  return (
    <p className="mt-11 text-[15px] text-site-muted">
      {signInPrompt}{' '}
      <Link href={signIn.href} className="font-bold text-site-teal hover:underline">
        {signIn.label}
      </Link>
    </p>
  );
}
