import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { appointmentSettingsText } from '../../content/appointment-settings';
import type { CancellationPolicy } from '../../types/appointment-settings.types';
import { CancellationPolicyForm } from './cancellation-policy-form';
import { RulesIntro } from './rules-intro';

/**
 * The cancellation policy: how it counts, and the form with the stored values. `policy` is null
 * when this session may not read it (`settings:read`); a blind replace is not offered then.
 */
export function CancellationPolicyCard({ policy }: { policy: CancellationPolicy | null }) {
  const text = appointmentSettingsText.policy;

  return (
    <Card>
      <CardHeader title={text.heading} />
      <CardBody className="space-y-4">
        <RulesIntro how={text.how} />
        {policy ? (
          <CancellationPolicyForm policy={policy} />
        ) : (
          <p role="note" className="rounded-lg border border-amber-200 bg-amber-50 px-3.5 py-2.5 text-sm text-amber-900">
            {text.noRead}
          </p>
        )}
      </CardBody>
    </Card>
  );
}
