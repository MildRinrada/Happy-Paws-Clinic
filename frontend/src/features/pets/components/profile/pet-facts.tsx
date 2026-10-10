import { Card, CardBody } from '@/components/ui/card';
import { petProfile } from '../../content/pet-profile';
import { identityFacts } from '../../lib/describe-pet';
import type { PetSummary } from '../../types/pet.types';

/**
 * The identity card: chip, sex, birth date and age, neuter status (unknown ones left out), and
 * what the owner keeps up to date from the portal.
 */
export function PetFacts({ pet }: { pet: PetSummary }) {
  const text = petProfile.facts;
  const rows = [
    { label: text.microchip, value: pet.microchip_number ?? text.noMicrochip },
    ...identityFacts(pet),
    pet.nickname ? { label: text.nickname, value: pet.nickname } : null,
    pet.diet ? { label: text.diet, value: pet.diet } : null,
    pet.behaviour ? { label: text.behaviour, value: pet.behaviour } : null,
  ].filter((row): row is { label: string; value: string } => row !== null);

  return (
    <Card>
      <CardBody>
        <dl className="space-y-2.5 text-sm">
          {rows.map((row) => (
            <div key={row.label} className="flex justify-between gap-4">
              <dt className="shrink-0 text-ink-500">{row.label}</dt>
              <dd className="text-right font-medium text-ink-900">{row.value}</dd>
            </div>
          ))}
        </dl>
      </CardBody>
    </Card>
  );
}
