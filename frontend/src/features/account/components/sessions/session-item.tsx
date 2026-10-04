import { Badge } from '@/components/ui/badge';
import { deviceLabel } from '@/features/portal/lib/device-label';
import { formatDateTime } from '@/lib/format/datetime';
import { sessionSettings } from '../../content/session-settings';
import type { AccountSession } from '../../types/account.types';
import { RevokeSessionButton } from './revoke-session-button';

/**
 * One signed-in device: a rough name from its browser, when it was last used, and the address it
 * came from. The device in use is marked and has no button; the header's sign-out ends it properly.
 */
export function SessionItem({ session }: { session: AccountSession }) {
  const text = sessionSettings;
  const details = [
    text.lastUsed(formatDateTime(session.last_used_at)),
    text.signedIn(formatDateTime(session.created_at)),
    session.ip_address ? text.ip(session.ip_address) : undefined,
  ].filter(Boolean);

  return (
    <li className="flex flex-col gap-3 py-3 sm:flex-row sm:items-start sm:justify-between">
      <div className="min-w-0 space-y-0.5">
        <p className="flex flex-wrap items-center gap-2 text-sm font-medium text-ink-900">
          <span className="break-words">{deviceLabel(session.user_agent) ?? text.unknownDevice}</span>
          {session.current && <Badge tone="brand">{text.current}</Badge>}
        </p>
        {details.map((line) => (
          <p key={line} className="break-words text-xs text-ink-500">
            {line}
          </p>
        ))}
        {session.current && <p className="text-xs text-ink-500">{text.currentNote}</p>}
      </div>

      {!session.current && <RevokeSessionButton sessionId={session.id} />}
    </li>
  );
}
