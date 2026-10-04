import { Alert } from '@/components/ui/alert';
import { signInPage } from '../../content/sign-in-page';

type NoticeKey = keyof typeof signInPage.notices;

function isNoticeKey(value: string | undefined): value is NoticeKey {
  return value !== undefined && Object.hasOwn(signInPage.notices, value);
}

/**
 * A short message left by the page that sent the visitor here (`/login?notice=registered`). The
 * query only picks one of the messages in the content file; an unknown value shows nothing.
 */
export function SignInNotice({ notice }: { notice?: string }) {
  if (!isNoticeKey(notice)) {
    return null;
  }

  return (
    <div className="mt-6">
      <Alert tone="success">{signInPage.notices[notice]}</Alert>
    </div>
  );
}
