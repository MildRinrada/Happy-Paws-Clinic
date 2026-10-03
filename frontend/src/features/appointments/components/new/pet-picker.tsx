'use client';

import { useEffect, useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input } from '@/components/ui/field';
import type { PetSummary } from '@/features/pets/types/pet.types';
import { searchPetsAction } from '@/features/queue/actions/queue-actions';
import { newAppointment } from '../../content/appointment-calendar';

interface PetPickerProps {
  pet: PetSummary | null;
  onPick: (pet: PetSummary | null) => void;
}

/** Find the patient by name or HN. Searches once the counter stops typing. */
export function PetPicker({ pet, onPick }: PetPickerProps) {
  const text = newAppointment.pet;
  const [query, setQuery] = useState('');
  const [matches, setMatches] = useState<PetSummary[] | null>(null);
  const [searching, startSearch] = useTransition();

  useEffect(() => {
    if (pet || query.trim().length < 2) {
      setMatches(null);
      return;
    }

    const timer = setTimeout(() => {
      startSearch(async () => {
        const result = await searchPetsAction(query);
        setMatches(result.ok ? result.data.filter((match) => !match.is_deceased) : []);
      });
    }, 250);

    return () => clearTimeout(timer);
  }, [query, pet]);

  if (pet) {
    return (
      <div className="flex items-center justify-between rounded-lg border border-brand-200 bg-brand-50 px-3.5 py-2.5">
        <div>
          <p className="font-medium text-ink-900">{pet.name}</p>
          <p className="text-sm text-ink-600">{[pet.species, pet.breed, pet.hospital_number].filter(Boolean).join(' / ')}</p>
        </div>
        <Button size="sm" variant="ghost" onClick={() => onPick(null)}>
          {text.change}
        </Button>
      </div>
    );
  }

  return (
    <Field label={text.label} htmlFor="book-pet" hint={text.hint}>
      <Input id="book-pet" value={query} autoComplete="off" autoFocus onChange={(event) => setQuery(event.target.value)} />
      {searching && <p className="text-sm text-ink-500">{text.searching}</p>}
      {matches?.length === 0 && !searching && <p className="text-sm text-ink-500">{text.none}</p>}
      {matches && matches.length > 0 && (
        <ul className="max-h-56 divide-y divide-ink-100 overflow-auto rounded-lg border border-ink-200">
          {matches.map((match) => (
            <li key={match.id}>
              <button type="button" className="w-full px-3.5 py-2 text-left hover:bg-ink-50" onClick={() => onPick(match)}>
                <span className="font-medium text-ink-900">{match.name}</span>{' '}
                <span className="text-sm text-ink-500">
                  {match.species} / {match.hospital_number}
                </span>
              </button>
            </li>
          ))}
        </ul>
      )}
    </Field>
  );
}
