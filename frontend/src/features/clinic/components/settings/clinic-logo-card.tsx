import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { clinicSettings } from '../../content/clinic-settings';
import type { ClinicProfileInput } from '../../types/settings.types';
import { ClinicLogoSection } from './logo/clinic-logo-section';

interface ClinicLogoCardProps {
  values: Pick<ClinicProfileInput, 'name' | 'logo_url' | 'tab_icon_url' | 'show_name_with_logo'>;
  errors: Partial<Record<string, string>>;
  onChange: <K extends keyof ClinicProfileInput>(field: K, value: ClinicProfileInput[K]) => void;
}

/** "โลโก้และไอคอน": the logo, the name beside it and the browser-tab icon, with previews. */
export function ClinicLogoCard({ values, errors, onChange }: ClinicLogoCardProps) {
  return (
    <Card>
      <CardHeader title={clinicSettings.logoCard.heading} />
      <CardBody>
        <ClinicLogoSection values={values} onChange={onChange} errors={errors} />
      </CardBody>
    </Card>
  );
}
