import Link from 'next/link';
import { backToSignIn } from '../../content/password-reset-page';

/** "กลับไปหน้าเข้าสู่ระบบ", under the password pages' forms. */
export function BackToSignInLink() {
  return (
    <p className="mt-11 text-[15px]">
      <Link href={backToSignIn.href} className="font-bold text-site-teal hover:underline">
        {backToSignIn.label}
      </Link>
    </p>
  );
}
