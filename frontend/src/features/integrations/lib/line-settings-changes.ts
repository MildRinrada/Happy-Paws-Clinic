import type { CredentialDraft } from '../components/line-credential-fields';
import type { LineSettings, LineSettingsInput } from '../types/line.types';

/**
 * What a save sends. The switches and the quota go every time; the channel id only when changed;
 * a secret or token only when something was typed, so an empty field keeps the stored one.
 * Clearing sends empty strings, which the backend reads as "forget the stored value".
 */
export function lineSettingsChanges(
  settings: LineSettings,
  credentials: CredentialDraft,
  enabled: boolean,
  limit: number,
  types: Record<string, boolean>,
): LineSettingsInput {
  const input: LineSettingsInput = { enabled, broadcast_monthly_limit: limit, notification_types: types };

  if (credentials.channelId.trim() !== (settings.channel_id ?? '')) {
    input.channel_id = credentials.channelId.trim();
  }

  if (credentials.clearStored) {
    input.channel_secret = '';
    input.channel_access_token = '';
    return input;
  }

  if (credentials.channelSecret.trim()) input.channel_secret = credentials.channelSecret.trim();
  if (credentials.channelAccessToken.trim()) input.channel_access_token = credentials.channelAccessToken.trim();

  return input;
}
