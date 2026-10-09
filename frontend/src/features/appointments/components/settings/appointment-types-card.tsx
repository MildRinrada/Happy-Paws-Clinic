import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { appointmentSettingsText } from '../../content/appointment-settings';
import type { AppointmentTypes } from '../../types/appointment-settings.types';
import { AppointmentTypesForm } from './appointment-types-form';
import { RulesIntro } from './rules-intro';

/**
 * The appointment types: what each rule does, and the form with the stored set. `types` is null
 * when this session may not read it (`appointment:read`); a blind replace is not offered then.
 */
export function AppointmentTypesCard({ types }: { types: AppointmentTypes | null }) {
  const text = appointmentSettingsText.types;

  return (
    <Card>
      <CardHeader title={text.heading} />
      <CardBody className="space-y-4">
        <RulesIntro how={text.how} />
        {types ? (
          <AppointmentTypesForm stored={types} />
        ) : (
          <p role="note" className="rounded-lg border border-amber-200 bg-amber-50 px-3.5 py-2.5 text-sm text-amber-900">
            {text.noRead}
          </p>
        )}
      </CardBody>
    </Card>
  );
}
