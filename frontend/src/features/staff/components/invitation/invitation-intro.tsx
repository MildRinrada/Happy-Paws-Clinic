import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { invitationPage } from '../../content/invitation-page';

interface InvitationIntroProps {
  onStart: () => void;
  starting: boolean;
  error?: string;
}

/** What is about to happen, and the button that starts it. */
export function InvitationIntro({ onStart, starting, error }: InvitationIntroProps) {
  const text = invitationPage.intro;

  return (
    <div className="space-y-4">
      <h2 className="text-base font-semibold text-ink-900">{text.title}</h2>
      <p className="text-sm text-ink-600">{text.body}</p>

      <ol className="list-decimal space-y-1 pl-5 text-sm text-ink-700">
        {text.steps.map((step) => (
          <li key={step}>{step}</li>
        ))}
      </ol>

      {error && <Alert>{error}</Alert>}

      <Button className="w-full" onClick={onStart} disabled={starting}>
        {starting ? text.starting : text.start}
      </Button>
    </div>
  );
}
