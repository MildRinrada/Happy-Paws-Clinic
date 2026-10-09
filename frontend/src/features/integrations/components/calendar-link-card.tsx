import { Alert } from '@/components/ui/alert';
import { Card, CardBody } from '@/components/ui/card';
import { calendarFeedPage } from '../content/calendar-feed';
import { copyLabels } from '../content/copy';
import { CopyValue } from './copy-value';

interface CalendarLinkCardProps {
  mineUrl: string;
  /** Present when the session may see the whole branch (`appointment:read`). */
  branchUrl?: string;
}

/** The subscription links, how to add one to a calendar app, and why the link must stay private. */
export function CalendarLinkCard({ mineUrl, branchUrl }: CalendarLinkCardProps) {
  const text = calendarFeedPage;

  return (
    <Card>
      <CardBody className="space-y-5">
        <Alert tone="info">{text.privacy}</Alert>

        <div className="space-y-1">
          <CopyValue label={text.mine} value={mineUrl} copyLabel={copyLabels.copy} copiedLabel={copyLabels.copied} />
          <p className="text-sm text-ink-500">{text.mineHint}</p>
        </div>

        {branchUrl && (
          <div className="space-y-1">
            <CopyValue label={text.branch} value={branchUrl} copyLabel={copyLabels.copy} copiedLabel={copyLabels.copied} />
            <p className="text-sm text-ink-500">{text.branchHint}</p>
          </div>
        )}

        <p className="text-sm text-ink-600">{text.howTo}</p>
        <p className="text-sm text-ink-500">{text.window}</p>
      </CardBody>
    </Card>
  );
}
