import { signInPage } from '../../content/sign-in-page';

/** "ยินดีต้อนรับกลับมา" and the line under it. */
export function SignInHeading() {
  return (
    <div>
      <h1 className="font-display text-[30px] font-bold leading-tight text-ink-900">{signInPage.heading}</h1>
      <p className="mt-2 text-lg text-site-muted">{signInPage.subheading}</p>
    </div>
  );
}
