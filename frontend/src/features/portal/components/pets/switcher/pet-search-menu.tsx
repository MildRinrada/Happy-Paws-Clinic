'use client';

import { useEffect, useId, useRef, useState } from 'react';
import { petDetail } from '../../../content/pet-detail';
import { matchesPet } from '../../../lib/match-pet';
import type { SwitcherPet } from '../../../lib/pet-switcher';
import { ChevronIcon } from '../chevron-icon';
import { PetMenuLink } from './pet-menu-link';

/**
 * "ทั้งหมด N ตัว" for a long list: a button that opens a panel with a search box over a scrolling
 * list of every pet, living ones first. Typing narrows the list by name or nickname. Escape or a
 * click outside closes it, and focus goes back to the button.
 */
export function PetSearchMenu({ count, pets }: { count: number; pets: SwitcherPet[] }) {
  const text = petDetail.switcher;
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState('');
  const panelId = useId();
  const searchId = useId();
  const root = useRef<HTMLDivElement>(null);
  const button = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    if (!open) return;

    const closeOutside = (event: MouseEvent) => {
      if (!root.current?.contains(event.target as Node)) setOpen(false);
    };
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key !== 'Escape') return;
      setOpen(false);
      button.current?.focus();
    };

    document.addEventListener('mousedown', closeOutside);
    document.addEventListener('keydown', closeOnEscape);
    return () => {
      document.removeEventListener('mousedown', closeOutside);
      document.removeEventListener('keydown', closeOnEscape);
    };
  }, [open]);

  const shown = pets.filter((pet) => matchesPet(pet, query));
  const alive = shown.filter((pet) => !pet.deceased);
  const gone = shown.filter((pet) => pet.deceased);

  return (
    <div ref={root} className="relative">
      <button
        ref={button}
        type="button"
        aria-expanded={open}
        aria-controls={panelId}
        onClick={() => setOpen((value) => !value)}
        className="flex h-9 items-center gap-2 rounded-full border border-ink-200 bg-white pl-4 pr-3.5 font-display text-[14.5px] text-ink-800 hover:border-brand-300"
      >
        {text.all(count)}
        <ChevronIcon direction="down" className="size-4 text-ink-600" />
      </button>

      {open && (
        <div
          id={panelId}
          className="absolute right-0 top-full z-30 mt-2 w-72 rounded-xl border border-ink-200 bg-white p-2 shadow-lg"
        >
          <label htmlFor={searchId} className="sr-only">
            {text.searchLabel}
          </label>
          <input
            id={searchId}
            type="search"
            autoFocus
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder={text.searchPlaceholder}
            className="h-10 w-full rounded-lg border border-ink-200 px-3 text-sm text-ink-900 placeholder:text-ink-400 focus:border-brand-500"
          />

          <div className="mt-2 max-h-80 overflow-y-auto">
            {shown.length === 0 && <p className="px-2 py-3 text-sm text-ink-600">{text.noMatch}</p>}
            <ul>
              {alive.map((pet) => (
                <PetMenuLink key={pet.id} pet={pet} />
              ))}
            </ul>
            {gone.length > 0 && (
              <>
                <p className="px-2 pb-1 pt-3 text-xs text-ink-500">{text.deceasedGroup}</p>
                <ul>
                  {gone.map((pet) => (
                    <PetMenuLink key={pet.id} pet={pet} />
                  ))}
                </ul>
              </>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
