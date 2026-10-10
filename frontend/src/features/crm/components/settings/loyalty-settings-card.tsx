import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { loyaltySettingsText } from '../../content/loyalty-settings';
import type { LoyaltySettings } from '../../types/crm.types';
import { LoyaltySettingsForm } from './loyalty-settings-form';

/**
 * The points programme: how a change applies, and the form with the stored values. `settings` is
 * null when this session may not read them (`settings:read`); a blind replace is not offered then.
 */
export function LoyaltySettingsCard({ settings }: { settings: LoyaltySettings | null }) {
  const text = loyaltySettingsText;

  return (
    <Card>
      <CardHeader title={text.heading} />
      <CardBody className="space-y-4">
        <ul className="list-disc space-y-1 pl-5 text-sm text-ink-700">
          {text.how.map((line) => (
            <li key={line}>{line}</li>
          ))}
        </ul>
        {settings ? (
          <LoyaltySettingsForm settings={settings} />
        ) : (
          <p role="note" className="rounded-lg border border-amber-200 bg-amber-50 px-3.5 py-2.5 text-sm text-amber-900">
            {text.noRead}
          </p>
        )}
      </CardBody>
    </Card>
  );
}
