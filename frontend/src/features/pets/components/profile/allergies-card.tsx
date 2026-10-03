import { Badge } from '@/components/ui/badge';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { petProfile } from '../../content/pet-profile';
import type { PetAllergy } from '../../types/pet.types';
import { AddAllergyForm } from './add-allergy-form';

/**
 * Every recorded allergy, red when there is one. "แสดงเป็นแถบเตือนแดงทุกหน้าที่เกี่ยวข้อง" is the
 * contract's line for this, so it sits at the top of the page, not in a tab.
 */
export function AllergiesCard({ petId, allergies, canRecord }: { petId: string; allergies: PetAllergy[]; canRecord: boolean }) {
  const text = petProfile.allergies;
  const any = allergies.length > 0;

  return (
    <Card className={any ? 'border-red-300' : ''}>
      <CardHeader title={text.title} />
      <CardBody className="space-y-3">
        {any ? (
          <>
            <p role="note" className="rounded-lg bg-red-50 px-3 py-2 text-sm font-semibold text-red-800">
              {text.banner}
            </p>
            <ul className="space-y-2">
              {allergies.map((allergy) => (
                <li key={allergy.id} className="flex flex-wrap items-start justify-between gap-2 text-sm">
                  <div>
                    <p className="font-semibold text-ink-900">{allergy.substance}</p>
                    {allergy.reaction && <p className="text-ink-600">{allergy.reaction}</p>}
                  </div>
                  <Badge tone={allergy.severity === 'severe' ? 'red' : 'amber'}>
                    {text.severity[allergy.severity] ?? allergy.severity}
                  </Badge>
                </li>
              ))}
            </ul>
          </>
        ) : (
          <p className="text-sm text-ink-500">{text.none}</p>
        )}

        {canRecord && <AddAllergyForm petId={petId} />}
      </CardBody>
    </Card>
  );
}
