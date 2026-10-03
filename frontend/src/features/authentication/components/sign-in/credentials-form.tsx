import { Alert } from '@/components/ui/alert';
import { signInPage } from '../../content/sign-in-page';
import { ForgotPasswordLink } from './forgot-password-link';
import { SignInField } from './sign-in-field';
import { SignInSubmit } from './sign-in-submit';

interface CredentialsFormProps {
  action: (form: FormData) => void;
  pending: boolean;
  error?: string;
  /** What was typed last time, kept in the box after a refusal. The password is never kept. */
  identifier?: string;
}

/** Step one: phone number or email, and password. */
export function CredentialsForm({ action, pending, error, identifier }: CredentialsFormProps) {
  return (
    <form action={action} className="mt-10 space-y-5">
      {error && <Alert>{error}</Alert>}

      <SignInField
        id="sign-in-identifier"
        name="identifier"
        label={signInPage.identifierLabel}
        autoComplete="username"
        defaultValue={identifier}
        required
        autoFocus
      />

      <div className="space-y-4">
        <SignInField
          id="sign-in-password"
          name="password"
          type="password"
          label={signInPage.passwordLabel}
          autoComplete="current-password"
          required
        />
        <ForgotPasswordLink />
      </div>

      <SignInSubmit disabled={pending}>{pending ? signInPage.submitting : signInPage.submit}</SignInSubmit>
    </form>
  );
}
