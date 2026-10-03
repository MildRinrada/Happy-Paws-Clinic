import Link from 'next/link';
import { signInPage } from '../../content/sign-in-page';

/** "ยังไม่มีบัญชี? สมัครสมาชิก", under the form. */
export function SignUpPrompt() {
  const { signUpPrompt, signUp } = signInPage;

  return (
    <p className="mt-11 text-[15px] text-site-muted">
      {signUpPrompt}{' '}
      <Link href={signUp.href} className="font-bold text-site-teal hover:underline">
        {signUp.label}
      </Link>
    </p>
  );
}
