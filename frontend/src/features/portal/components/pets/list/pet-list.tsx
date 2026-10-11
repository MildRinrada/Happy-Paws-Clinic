'use client';

import { useState } from 'react';
import { portalPets } from '../../../content/portal-pets';
import { matchesPet } from '../../../lib/match-pet';
import type { PetListItem } from '../../../lib/pet-list-items';
import { EmptyLine } from '../../shared/empty-line';
import { DeceasedPets } from './deceased-pets';
import { PetCardGrid } from './pet-card-grid';
import { PetSearchField } from './pet-search-field';

/** From this many pets, the list gets a search box. */
const SEARCH_FROM = 9;

/**
 * Every pet on the account, living ones first and those that have died folded away below. A long
 * list (more than eight) gets a search box by name or nickname, which also opens the folded group
 * when it finds a pet there.
 */
export function PetList({ pets }: { pets: PetListItem[] }) {
  const [query, setQuery] = useState('');
  const text = portalPets.list;
  const searching = query.trim().length > 0;

  const shown = pets.filter((pet) => matchesPet(pet, query));
  const alive = shown.filter((pet) => !pet.deceased);
  const gone = shown.filter((pet) => pet.deceased);

  return (
    <div className="space-y-4">
      {pets.length >= SEARCH_FROM && (
        <div className="space-y-1.5">
          <PetSearchField value={query} onChange={setQuery} />
          {searching && (
            <p aria-live="polite" className="text-[13px] text-ink-600">
              {shown.length > 0 ? text.found(shown.length) : text.noMatch}
            </p>
          )}
        </div>
      )}

      {alive.length > 0 && <PetCardGrid pets={alive} />}
      {/* A new key when a search starts or ends, so the group opens for a search and folds again after. */}
      <DeceasedPets key={searching ? 'search' : 'browse'} pets={gone} open={searching} />
      {!searching && shown.length === 0 && <EmptyLine>{portalPets.empty}</EmptyLine>}
    </div>
  );
}
