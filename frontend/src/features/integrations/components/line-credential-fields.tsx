import { Checkbox } from '@/components/ui/checkbox';
import { Field, Input } from '@/components/ui/field';
import { lineSettingsPage } from '../content/line-settings';

export interface CredentialDraft {
  channelId: string;
  channelSecret: string;
  channelAccessToken: string;
  /** Sends empty strings for both, handing them back to the server's environment. */
  clearStored: boolean;
}

interface LineCredentialFieldsProps {
  draft: CredentialDraft;
  hasStored: boolean;
  errors: Record<string, string>;
  onChange: (draft: CredentialDraft) => void;
}

/**
 * The channel id, secret and token. The secret and token are never shown back: an empty field
 * keeps what is stored, and only something typed replaces it.
 */
export function LineCredentialFields({ draft, hasStored, errors, onChange }: LineCredentialFieldsProps) {
  const text = lineSettingsPage.credentials;
  const placeholder = hasStored ? text.storedPlaceholder : text.emptyPlaceholder;
  const set = (patch: Partial<CredentialDraft>) => onChange({ ...draft, ...patch });

  return (
    <div className="space-y-4">
      <div className="grid gap-4 sm:grid-cols-3">
        <Field label={text.channelId} htmlFor="line-channel-id" hint={text.channelIdHint} error={errors.channel_id}>
          <Input
            id="line-channel-id"
            inputMode="numeric"
            maxLength={20}
            value={draft.channelId}
            className="font-mono"
            onChange={(event) => set({ channelId: event.target.value })}
          />
        </Field>
        <Field label={text.channelSecret} htmlFor="line-channel-secret" error={errors.channel_secret}>
          <Input
            id="line-channel-secret"
            type="password"
            autoComplete="off"
            maxLength={200}
            placeholder={placeholder}
            value={draft.channelSecret}
            disabled={draft.clearStored}
            onChange={(event) => set({ channelSecret: event.target.value })}
          />
        </Field>
        <Field label={text.channelAccessToken} htmlFor="line-access-token" error={errors.channel_access_token}>
          <Input
            id="line-access-token"
            type="password"
            autoComplete="off"
            maxLength={1000}
            placeholder={placeholder}
            value={draft.channelAccessToken}
            disabled={draft.clearStored}
            onChange={(event) => set({ channelAccessToken: event.target.value })}
          />
        </Field>
      </div>

      <p className="text-sm text-ink-500">{text.secretHint}</p>

      {hasStored && (
        <Checkbox
          label={text.clearStored}
          checked={draft.clearStored}
          onChange={(event) => set({ clearStored: event.target.checked, channelSecret: '', channelAccessToken: '' })}
        />
      )}
    </div>
  );
}
