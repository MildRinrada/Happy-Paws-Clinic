import { portalHome } from '../../content/portal-home';
import { portalNavLinks } from '../../content/portal-navigation';
import type { MyPet } from '../../types/portal.types';
import { EmptyLine } from '../shared/empty-line';
import { PetAvatar } from '../shared/pet-avatar';
import { SectionCard } from '../shared/section-card';

const petsHref = portalNavLinks.find((link) => link.icon === 'paw')?.href ?? '/portal/pets';

/** The owner's pets in one line each: photo, name, species and the latest weight. */
export function PetsOverview({ pets }: { pets: MyPet[] }) {
  const text = portalHome.pets;

  return (
    <SectionCard title={text.title} more={pets.length > 0 ? { label: text.more, href: petsHref } : undefined}>
      {pets.length === 0 ? (
        <EmptyLine>{text.empty}</EmptyLine>
      ) : (
        <ul className="space-y-3">
          {pets.map((pet) => (
            <li key={pet.id} className="flex items-center gap-3">
              <PetAvatar name={pet.name} photoUrl={pet.photo_url} />
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-semibold text-ink-900">{pet.name}</p>
                <p className="truncate text-xs text-site-muted">
                  {[pet.species, pet.breed].filter(Boolean).join(' ')}
                  {pet.is_deceased && ` (${text.deceased})`}
                </p>
              </div>
              {pet.latest_weight && (
                <span className="shrink-0 text-sm text-ink-700">{text.weight(pet.latest_weight.weight_kg)}</span>
              )}
            </li>
          ))}
        </ul>
      )}
    </SectionCard>
  );
}
