import { Badge } from '@/components/ui/badge';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { lineSettingsPage } from '../content/line-settings';
import type { LineSettings } from '../types/line.types';

/** Whether LINE works right now, where its credentials come from, and the webhook address. */
export function LineStatusCard({ settings }: { settings: LineSettings }) {
  const text = lineSettingsPage.status;

  return (
    <Card>
      <CardHeader
        title={text.heading}
        aside={
          <span className="flex flex-wrap gap-1.5">
            <Badge tone={settings.can_push ? 'green' : 'amber'}>{settings.can_push ? text.canPush : text.cannotPush}</Badge>
            <Badge tone={settings.can_verify ? 'green' : 'amber'}>{settings.can_verify ? text.canVerify : text.cannotVerify}</Badge>
          </span>
        }
      />
      <CardBody>
        <dl className="grid gap-3 text-sm sm:grid-cols-3">
          <div>
            <dt className="text-ink-500">{text.token}</dt>
            <dd className="text-ink-900">{text.sources[settings.token_source] ?? settings.token_source}</dd>
          </div>
          <div>
            <dt className="text-ink-500">{text.secret}</dt>
            <dd className="text-ink-900">{text.sources[settings.secret_source] ?? settings.secret_source}</dd>
          </div>
          <div>
            <dt className="text-ink-500">{text.webhook}</dt>
            <dd className="font-mono text-ink-900">{settings.webhook_path}</dd>
            <dd className="text-xs text-ink-500">{text.webhookHint}</dd>
          </div>
        </dl>
      </CardBody>
    </Card>
  );
}
