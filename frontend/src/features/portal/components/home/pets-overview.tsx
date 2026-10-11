import { portalHome } from '../../content/portal-home';
import { portalPaths } from '../../content/portal-navigation';
import { alivePetsFirst } from '../../lib/order-pets';
import { toneOfPet } from '../../lib/pet-tone';
import type { MyPet } from '../../types/portal.types';
import { EmptyLine } from '../shared/empty-line';
import { MoreLink } from '../shared/more-link';
import { PetTile } from './pet-tile';

/** How many pets the home page shows. An account with more has them all one tap away on the pets page. */
const SHOWN = 6;

/**
 * The owner's pets, two to a row, living ones first, and the way to the pets page. The circles take
 * turns in yellow and mint (`toneOfPet`), so two pets side by side are told apart at a glance, and
 * each pet keeps its colour on its own page.
 */
export function PetsOverview({ pets }: { pets: MyPet[] }) {
  const text = portalHome.pets;
  const ids = pets.map((pet) => pet.id);
  const shown = alivePetsFirst(pets).slice(0, SHOWN);

  return (
    <section>
      <div className="mb-2.5 flex items-center justify-between gap-3">
        <h2 className="font-display text-base font-semibold leading-6 text-ink-900">{text.title}</h2>
        {pets.length > 0 && <MoreLink label={text.more} href={portalPaths.pets} />}
      </div>

      {pets.length === 0 ? (
        <div className="rounded-2xl border border-ink-200 bg-white p-4">
          <EmptyLine>{text.empty}</EmptyLine>
        </div>
      ) : (
        <ul className="grid grid-cols-2 gap-2.5">
          {shown.map((pet) => (
            <li key={pet.id}>
              <PetTile pet={pet} tone={toneOfPet(ids, pet.id)} />
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
