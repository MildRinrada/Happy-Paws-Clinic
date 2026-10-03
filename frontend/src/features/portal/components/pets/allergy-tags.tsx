import { portalPets } from '../../content/portal-pets';
import type { MyPet } from '../../types/portal.types';

/** What the pet is allergic to, each with how bad, so an owner can tell another vet. */
export function AllergyTags({ allergies }: { allergies: MyPet['allergies'] }) {
  return (
    <div>
      <p className="text-xs text-site-muted">{portalPets.allergiesTitle}</p>
      {allergies.length === 0 ? (
        <p className="text-sm text-ink-900">{portalPets.noAllergies}</p>
      ) : (
        <ul className="mt-1 flex flex-wrap gap-2">
          {allergies.map((allergy) => (
            <li key={allergy.substance} className="rounded-full bg-red-50 px-2.5 py-1 text-xs font-medium text-red-800">
              {allergy.substance} ({portalPets.severity[allergy.severity] ?? allergy.severity})
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
