import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { Field, Input } from '@/components/ui/field';
import { clinicSettings } from '../../content/clinic-settings';
import type { ClinicProfileInput } from '../../types/settings.types';

interface ClinicDetailsCardProps {
  values: Pick<ClinicProfileInput, 'name' | 'license_no' | 'operator_name'>;
  errors: Partial<Record<string, string>>;
  onChange: <K extends keyof ClinicProfileInput>(field: K, value: ClinicProfileInput[K]) => void;
}

/** "ข้อมูลคลินิก": the name, the licence and its operator, each saying where it shows. */
export function ClinicDetailsCard({ values, errors, onChange }: ClinicDetailsCardProps) {
  const text = clinicSettings.profile;

  return (
    <Card>
      <CardHeader title={text.heading} />
      <CardBody className="space-y-4">
        <Field label={text.name} htmlFor="clinic-name" hint={text.nameHint} error={errors.name}>
          <Input id="clinic-name" value={values.name} maxLength={200} required className="sm:max-w-md" onChange={(event) => onChange('name', event.target.value)} />
        </Field>

        <div className="grid gap-4 sm:grid-cols-2">
          <Field label={text.licenseNo} htmlFor="clinic-license" hint={text.licenseNoHint} error={errors.license_no}>
            <Input id="clinic-license" value={values.license_no} maxLength={64} onChange={(event) => onChange('license_no', event.target.value)} />
          </Field>

          <Field label={text.operatorName} htmlFor="clinic-operator" hint={text.operatorNameHint} error={errors.operator_name}>
            <Input id="clinic-operator" value={values.operator_name} maxLength={200} onChange={(event) => onChange('operator_name', event.target.value)} />
          </Field>
        </div>
      </CardBody>
    </Card>
  );
}
