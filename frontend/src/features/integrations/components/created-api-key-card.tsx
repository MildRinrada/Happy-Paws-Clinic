import { Button } from '@/components/ui/button';
import { formatDateTime } from '@/lib/format/datetime';
import { apiKeySettingsPage } from '../content/api-key-settings';
import type { CreatedApiKey } from '../types/integration.types';
import { SecretRevealCard } from './secret-reveal-card';

/** A key just made: the whole key this once, how to send it, and what it may do until when. */
export function CreatedApiKeyCard({ apiKey, onDone }: { apiKey: CreatedApiKey; onDone: () => void }) {
  const text = apiKeySettingsPage.created;

  return (
    <SecretRevealCard heading={text.heading} label={text.key} secret={apiKey.key} warning={text.warning}>
      <p className="text-sm text-ink-600">{text.usage}</p>
      <dl className="grid gap-x-6 gap-y-2 text-sm sm:grid-cols-[auto_1fr]">
        <dt className="text-ink-500">{text.prefix}</dt>
        <dd className="font-mono text-ink-800">{apiKey.prefix}</dd>
        <dt className="text-ink-500">{text.expires}</dt>
        <dd className="text-ink-800">{formatDateTime(apiKey.expires_at)}</dd>
        <dt className="text-ink-500">{text.scopes}</dt>
        <dd className="font-mono text-xs text-ink-800">{apiKey.scopes.join(', ')}</dd>
      </dl>
      <Button variant="ghost" size="sm" onClick={onDone}>
        {apiKeySettingsPage.form.heading}
      </Button>
    </SecretRevealCard>
  );
}
