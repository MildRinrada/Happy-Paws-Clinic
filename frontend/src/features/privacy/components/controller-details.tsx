import { controllerSection } from '../content/privacy-policy';
import type { ControllerInfo } from '../lib/load-controller';
import { PolicyDefinitions } from './policy-definitions';

/** The clinic's name, operator, licence and address, leaving out whatever the clinic has not set. */
export function ControllerDetails({ info }: { info: ControllerInfo }) {
  const { labels } = controllerSection;
  const rows = [
    { term: labels.clinic, detail: info.clinicName },
    { term: labels.operator, detail: info.operatorName },
    { term: labels.license, detail: info.licenseNo },
    { term: labels.address, detail: info.address },
  ].filter((row): row is { term: string; detail: string } => Boolean(row.detail));

  if (rows.length === 0) {
    return null;
  }

  return <PolicyDefinitions items={rows} />;
}
