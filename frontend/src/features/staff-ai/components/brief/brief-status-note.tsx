import { dailyBriefText } from '../../content/daily-brief';
import type { DailyBrief } from '../../types/staff-ai.types';

/** Why there are no words yet, or none today: not written yet, the model failed, or the month's allowance ran out. */
export function BriefStatusNote({ brief }: { brief: DailyBrief }) {
  const text = dailyBriefText;

  if (brief.status === 'pending') {
    return <p className="text-sm text-ink-600">{text.pending(brief.generates_at)}</p>;
  }

  if (brief.status === 'failed') {
    return <p className="text-sm text-ink-600">{brief.reason === 'quota_exceeded' ? text.quota : text.failed}</p>;
  }

  return null;
}
