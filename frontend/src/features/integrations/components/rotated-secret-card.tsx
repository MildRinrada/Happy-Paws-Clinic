import { Button } from '@/components/ui/button';
import { webhookListText } from '../content/webhook-list';
import { webhookSettingsPage } from '../content/webhook-settings';
import type { RotatedWebhookSecret } from '../types/integration.types';
import { SecretRevealCard } from './secret-reveal-card';

/** A webhook's new signing secret, shown this once, with how a receiver checks it. */
export function RotatedSecretCard({ rotated, onDone }: { rotated: RotatedWebhookSecret; onDone: () => void }) {
  const text = webhookListText.rotated;

  return (
    <SecretRevealCard heading={text.heading} label={text.secret} secret={rotated.secret} warning={text.warning}>
      <p className="text-sm text-ink-600">{webhookSettingsPage.created.signing}</p>
      <Button variant="ghost" size="sm" onClick={onDone}>
        {text.done}
      </Button>
    </SecretRevealCard>
  );
}
