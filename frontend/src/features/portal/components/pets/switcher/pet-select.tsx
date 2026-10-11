'use client';

import { useRouter } from 'next/navigation';
import { useId } from 'react';
import { petDetail } from '../../../content/pet-detail';
import type { SwitcherPet } from '../../../lib/pet-switcher';
import { ChevronIcon } from '../chevron-icon';

/**
 * "ทั้งหมด N ตัว" for a dozen pets or fewer: the browser's own list, which every device knows how to
 * show. Choosing a pet opens its page. Living pets first, then the ones that have died, in their own group.
 */
export function PetSelect({ count, pets }: { count: number; pets: SwitcherPet[] }) {
  const router = useRouter();
  const id = useId();
  const text = petDetail.switcher;
  const alive = pets.filter((pet) => !pet.deceased);
  const gone = pets.filter((pet) => pet.deceased);

  return (
    <div className="relative">
      <label htmlFor={id} className="sr-only">
        {text.label}
      </label>
      <select
        id={id}
        value=""
        onChange={(event) => {
          const chosen = pets.find((pet) => pet.id === event.target.value);
          if (chosen) router.push(chosen.href);
        }}
        className="h-9 cursor-pointer appearance-none rounded-full border border-ink-200 bg-white pl-4 pr-9 font-display text-[14.5px] text-ink-800 hover:border-brand-300"
      >
        <option value="" disabled>
          {text.all(count)}
        </option>
        {alive.map((pet) => (
          <option key={pet.id} value={pet.id}>
            {pet.name}
          </option>
        ))}
        {gone.length > 0 && (
          <optgroup label={text.deceasedGroup}>
            {gone.map((pet) => (
              <option key={pet.id} value={pet.id}>
                {pet.name}
              </option>
            ))}
          </optgroup>
        )}
      </select>
      <span className="pointer-events-none absolute right-3.5 top-1/2 -translate-y-1/2 text-ink-600">
        <ChevronIcon direction="down" />
      </span>
    </div>
  );
}
