import { Badge } from '@/components/ui/badge';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { formatLongDate } from '@/lib/format/datetime';
import { petProfile } from '../../content/pet-profile';
import type { PetVaccinations } from '../../types/pet.types';
import { RecordVaccinationForm } from './record-vaccination-form';

interface VaccinesCardProps {
  petId: string;
  vaccinations: PetVaccinations;
  /** `record:write`, and the patient is alive: a closed record takes no new dose. */
  canRecord: boolean;
}

/** What is due next, overdue first, then what was given and from which lot. */
export function VaccinesCard({ petId, vaccinations, canRecord }: VaccinesCardProps) {
  const text = petProfile.vaccines;
  const { due, doses } = vaccinations;

  return (
    <Card>
      <CardHeader title={text.title} />
      <CardBody className="space-y-4">
        {due.length === 0 && doses.length === 0 && <p className="text-sm text-ink-500">{text.none}</p>}

        {due.length > 0 && (
          <section className="space-y-2">
            <h3 className="text-xs font-semibold text-ink-500">{text.due}</h3>
            <ul className="space-y-1.5">
              {due.map((entry) => {
                const status = text.status[entry.status];
                return (
                  <li key={entry.vaccine_name} className="flex flex-wrap items-center justify-between gap-2 text-sm">
                    <span className="font-medium text-ink-900">{entry.vaccine_name}</span>
                    <span className="flex items-center gap-2 text-ink-600">
                      {formatLongDate(entry.next_due_on)}
                      <Badge tone={status.tone}>{status.text}</Badge>
                    </span>
                  </li>
                );
              })}
            </ul>
          </section>
        )}

        {doses.length > 0 && (
          <section className="space-y-2">
            <h3 className="text-xs font-semibold text-ink-500">{text.given}</h3>
            <ul className="divide-y divide-ink-100 text-sm">
              {doses.map((dose) => (
                <li key={dose.id} className="flex flex-wrap justify-between gap-2 py-1.5">
                  <span className="text-ink-900">{dose.vaccine_name}</span>
                  <span className="text-ink-500">
                    {formatLongDate(dose.administered_at)} / {text.lot(dose.lot_number)}
                  </span>
                </li>
              ))}
            </ul>
          </section>
        )}

        {canRecord && <RecordVaccinationForm petId={petId} />}
      </CardBody>
    </Card>
  );
}
