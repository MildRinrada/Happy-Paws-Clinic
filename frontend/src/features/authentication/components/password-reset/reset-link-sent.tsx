import { Alert } from '@/components/ui/alert';
import { forgotPasswordPage } from '../../content/password-reset-page';

/**
 * The confirmation after asking for a link. It reads the same whether or not the email or number
 * has an account, as the backend's answer does, so this page cannot be used to find out who is a
 * customer here.
 */
export function ResetLinkSent({ onAgain }: { onAgain: () => void }) {
  const text = forgotPasswordPage.sent;

  return (
    <div className="mt-10 space-y-4">
      <Alert tone="success">
        <p className="font-semibold">{text.title}</p>
        <p className="mt-1">{text.body}</p>
      </Alert>
      <p className="text-sm text-site-muted">{text.expiry}</p>
      <button type="button" onClick={onAgain} className="text-[15px] font-bold text-site-teal hover:underline">
        {text.again}
      </button>
    </div>
  );
}
