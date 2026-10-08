import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { appointmentSettingsText } from '../../content/appointment-settings';
import { CancellationPolicyForm } from './cancellation-policy-form';
import { PutOnlyIntro } from './put-only-intro';

/** The cancellation policy: how it counts, that it cannot be read back, and the form. */
export function CancellationPolicyCard() {
  const text = appointmentSettingsText.policy;

  return (
    <Card>
      <CardHeader title={text.heading} />
      <CardBody className="space-y-4">
        <PutOnlyIntro how={text.how} cannotRead={text.cannotRead} />
        <CancellationPolicyForm />
      </CardBody>
    </Card>
  );
}
