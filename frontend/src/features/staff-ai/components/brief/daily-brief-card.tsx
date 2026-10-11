import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { dailyBriefText } from '../../content/daily-brief';
import type { DailyBrief } from '../../types/staff-ai.types';
import { AiDraftNotice } from '../common/ai-draft-notice';
import { BriefStatusNote } from './brief-status-note';
import { BriefText } from './brief-text';

/**
 * "สรุปเช้านี้" on the dashboard: the few sentences the AI wrote at seven, cut to three lines with
 * "ดูเพิ่ม". The figures the brief was written from are not repeated here: the page's own numbers
 * of today sit above it and are newer. The page leaves the card out when the assistant is off.
 */
export function DailyBriefCard({ brief }: { brief: DailyBrief }) {
  const text = dailyBriefText;

  return (
    <Card>
      <CardHeader title={text.title} />
      <CardBody className="space-y-2">
        {brief.status === 'ready' && brief.text ? (
          <>
            <AiDraftNotice />
            <BriefText text={brief.text} more={text.more} less={text.less} />
          </>
        ) : (
          <BriefStatusNote brief={brief} />
        )}
      </CardBody>
    </Card>
  );
}
