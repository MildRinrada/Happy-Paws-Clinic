import Link from 'next/link';
import { portalPaths } from '../../../content/portal-navigation';
import { petDetail } from '../../../content/pet-detail';
import type { PetVaccine } from '../../../lib/load-pet-vaccines';
import type { MyPet } from '../../../types/portal.types';
import { EmptyLine } from '../../shared/empty-line';
import { SectionCard } from '../../shared/section-card';
import { solidButton } from '../pet-button-styles';
import { VaccineItem } from './vaccine-item';

interface PetVaccinesCardProps {
  pet: MyPet;
  vaccines: PetVaccine[];
  /** False while only due vaccines can be read: a quiet line says the given ones come later. */
  complete: boolean;
}

/**
 * "วัคซีน": the pet's vaccines, most urgent first. On a phone a full-width button books the most
 * urgent one (wider screens have that link on the red tile above). Left out for a pet that has died
 * while only due vaccines can be read.
 */
export function PetVaccinesCard({ pet, vaccines, complete }: PetVaccinesCardProps) {
  const text = petDetail.vaccines;
  const urgent = vaccines.find((item) => item.status !== 'given');

  // Nothing falls due for a pet that has died; until given doses can be read, the card would only be empty.
  if (pet.is_deceased && !complete) return null;

  return (
    <SectionCard title={text.title} ruled="lg">
      {vaccines.length === 0 ? (
        <div className="lg:py-4">
          <EmptyLine>{text.empty}</EmptyLine>
        </div>
      ) : (
        <ul>
          {vaccines.map((vaccine, index) => (
            <VaccineItem key={`${vaccine.name}-${vaccine.date}`} vaccine={vaccine} last={index === vaccines.length - 1} />
          ))}
        </ul>
      )}

      {!complete && <p className="mt-3 text-xs leading-5 text-ink-500 lg:mt-0 lg:border-t lg:border-ink-100 lg:py-3">{text.partial}</p>}

      {urgent && !pet.is_deceased && (
        <Link href={portalPaths.bookFor(pet.id)} className={`${solidButton} mt-4 lg:hidden`}>
          {text.book(urgent.name)}
        </Link>
      )}
    </SectionCard>
  );
}
