import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { dailyBriefText } from '../../content/daily-brief';
import type { DailyBrief } from '../../types/staff-ai.types';
import { AiDraftNotice } from '../common/ai-draft-notice';
import { BriefNumbers } from './brief-numbers';
import { BriefStatusNote } from './brief-status-note';

/**
 * "สรุปเช้านี้" on the dashboard: a few sentences the AI wrote at seven, and the numbers they are
 * about. The page leaves the card out when the assistant is off.
 */
export function DailyBriefCard({ brief }: { brief: DailyBrief }) {
  return (
    <Card>
      <CardHeader title={dailyBriefText.title} />
      <CardBody className="space-y-3">
        {brief.status === 'ready' && brief.text ? (
          <div className="space-y-2">
            <AiDraftNotice />
            <p className="whitespace-pre-line text-sm leading-relaxed text-ink-900">{brief.text}</p>
          </div>
        ) : (
          <BriefStatusNote brief={brief} />
        )}
        {brief.numbers && <BriefNumbers numbers={brief.numbers} />}
      </CardBody>
    </Card>
  );
}
