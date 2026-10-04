import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { sessionSettings } from '../../content/session-settings';
import type { AccountSession } from '../../types/account.types';
import { SessionItem } from './session-item';

/** Every device signed in to this account, this one first, then the most recently used. */
export function SessionsCard({ sessions }: { sessions: AccountSession[] }) {
  const text = sessionSettings;
  const ordered = [...sessions].sort(
    (a, b) => Number(b.current) - Number(a.current) || Date.parse(b.last_used_at) - Date.parse(a.last_used_at),
  );
  const othersSignedIn = sessions.some((session) => !session.current);

  return (
    <Card>
      <CardHeader title={text.heading} />
      <CardBody className="space-y-2">
        <p className="text-sm text-ink-600">{text.intro}</p>
        <ul className="divide-y divide-ink-100">
          {ordered.map((session) => (
            <SessionItem key={session.id} session={session} />
          ))}
        </ul>
        {!othersSignedIn && <p className="text-sm text-ink-500">{text.empty}</p>}
      </CardBody>
    </Card>
  );
}
