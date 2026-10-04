import { registerPage } from '../../content/register-page';

interface ResendCodeButtonProps {
  /** Seconds left before another code may be asked for. Zero means now. */
  remaining: number;
  sending: boolean;
  onResend: () => void;
}

/**
 * "ส่งรหัสอีกครั้ง", which waits a minute between presses. The backend allows three codes per
 * number in ten minutes; the wait keeps an impatient double press from spending two of them.
 */
export function ResendCodeButton({ remaining, sending, onResend }: ResendCodeButtonProps) {
  const text = registerPage.codeSent;
  const waiting = remaining > 0;

  return (
    <button
      type="button"
      onClick={onResend}
      disabled={waiting || sending}
      className="text-sm font-bold text-site-teal hover:underline disabled:cursor-not-allowed disabled:font-normal disabled:text-site-muted disabled:no-underline"
    >
      {sending ? text.resending : waiting ? text.resendIn(remaining) : text.resend}
    </button>
  );
}
