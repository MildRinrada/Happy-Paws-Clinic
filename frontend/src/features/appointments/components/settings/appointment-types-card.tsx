import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { appointmentSettingsText } from '../../content/appointment-settings';
import { AppointmentTypesForm } from './appointment-types-form';
import { PutOnlyIntro } from './put-only-intro';

/** The appointment types: what each rule does, that the set cannot be read back, and the form. */
export function AppointmentTypesCard() {
  const text = appointmentSettingsText.types;

  return (
    <Card>
      <CardHeader title={text.heading} />
      <CardBody className="space-y-4">
        <PutOnlyIntro how={text.how} cannotRead={text.cannotRead} />
        <AppointmentTypesForm />
      </CardBody>
    </Card>
  );
}
