import { portalPets } from '../../../content/portal-pets';
import type { MyPet } from '../../../types/portal.types';

/** "การแพ้" and a red tag for each thing the pet reacts to, with how badly: "ไก่ ปานกลาง". */
export function AllergyRow({ allergies }: { allergies: MyPet['allergies'] }) {
  return (
    <div className="flex flex-wrap items-center gap-x-3 gap-y-2 px-4 py-3.5 lg:flex-nowrap lg:justify-between lg:px-5 lg:py-2.5">
      <p className="shrink-0 text-[12.5px] leading-5 text-ink-600 lg:text-[13px]">{portalPets.allergiesTitle}</p>

      {allergies.length === 0 ? (
        <p className="text-sm text-ink-900 lg:text-[13.5px]">{portalPets.noAllergies}</p>
      ) : (
        <ul className="flex min-w-0 flex-wrap gap-1.5 lg:justify-end">
          {allergies.map((allergy) => (
            <li
              key={allergy.substance}
              className="max-w-full break-words rounded-full bg-red-100 px-3 text-[13.5px] font-medium leading-6 text-red-800 lg:px-2.5 lg:text-xs"
            >
              {portalPets.allergy(allergy.substance, portalPets.severity[allergy.severity] ?? allergy.severity)}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
