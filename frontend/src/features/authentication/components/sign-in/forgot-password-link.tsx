import Link from 'next/link';
import { signInPage } from '../../content/sign-in-page';

/** "ลืมรหัสผ่าน?", on the right under the password. */
export function ForgotPasswordLink() {
  const { label, href } = signInPage.forgotPassword;

  return (
    <div className="flex justify-end">
      <Link href={href} className="text-[15px] text-site-teal hover:underline">
        {label}
      </Link>
    </div>
  );
}
