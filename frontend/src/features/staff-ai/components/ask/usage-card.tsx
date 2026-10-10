import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { askPage } from '../../content/ask-page';
import type { AiUsage } from '../../types/staff-ai.types';
import { UsageMeter } from './usage-meter';

/** This month's allowance and what each feature has used of it. */
export function UsageCard({ usage }: { usage: AiUsage }) {
  const text = askPage.usage;

  return (
    <Card>
      <CardHeader title={text.title} aside={<span className="text-xs text-ink-500">{text.month(usage.month)}</span>} />
      <CardBody className="space-y-3">
        <UsageMeter used={usage.used} limit={usage.limit} />
        <dl className="space-y-1 text-sm">
          {Object.entries(usage.by_feature).map(([feature, count]) => (
            <div key={feature} className="flex justify-between gap-3">
              <dt className="text-ink-600">{text.features[feature] ?? feature}</dt>
              <dd className="tabular-nums text-ink-900">{count.toLocaleString('th-TH')}</dd>
            </div>
          ))}
        </dl>
        <p className="text-xs text-ink-500">{text.note}</p>
      </CardBody>
    </Card>
  );
}
