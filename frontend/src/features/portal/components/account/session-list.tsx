import type { AccountSession } from '@/features/account/types/account.types';
import { formatDateTime } from '@/lib/format/datetime';
import { portalAccount } from '../../content/portal-account';
import { deviceLabel } from '../../lib/device-label';
import { RevokeSessionButton } from './revoke-session-button';

/**
 * Every device signed in to the account. This one is marked and cannot be signed out from here;
 * "ออกจากระบบ" in the header does that.
 */
export function SessionList({ sessions }: { sessions: AccountSession[] }) {
  const text = portalAccount.sessions;
  const ordered = [...sessions].sort((a, b) => Number(b.current) - Number(a.current) || b.last_used_at.localeCompare(a.last_used_at));

  return (
    <ul className="divide-y divide-ink-100">
      {ordered.map((session) => (
        <li key={session.id} className="flex items-start justify-between gap-4 py-3">
          <div className="min-w-0">
            <p className="text-sm font-medium text-ink-900">
              {deviceLabel(session.user_agent) ?? text.unknownDevice}
              {session.current && (
                <span className="ml-2 rounded-full bg-brand-50 px-2 py-0.5 text-xs font-semibold text-site-teal">{text.current}</span>
              )}
            </p>
            <p className="text-xs text-site-muted">
              {text.lastUsed(formatDateTime(session.last_used_at))}
              {session.ip_address && ` (${session.ip_address})`}
            </p>
          </div>
          {!session.current && <RevokeSessionButton sessionId={session.id} />}
        </li>
      ))}
    </ul>
  );
}
