'use client';

import Link from 'next/link';
import { useId, useState } from 'react';
import { Alert } from '@/components/ui/alert';
import { guestBookingPage } from '../../content/guest-booking-page';
import type { GuestPet, NewPet } from '../../types/booking.types';
import { ChoiceCard } from '../choice-card';
import { NewPetFields } from './new-pet-fields';
import { PickedTimeSummary, type PickedSummary } from './picked-time-summary';

const NEW = 'new';

/** Which pet the booking is for: one the visitor has, or one the clinic has not met yet. */
export type PetChoice = { petId: string } | { newPet: NewPet };

interface GuestPetStepProps {
  summary: PickedSummary;
  pets: GuestPet[];
  onBook: (choice: PetChoice) => void;
  submitting: boolean;
  error?: string;
  /** Where to pick another time when this one was lost: the signed-in booking page. */
  pickAgainHref?: string;
}

/**
 * Step 5 of `/book`, once the number is proved and the visitor is signed in: pick one of their
 * living pets or add a new one, then book.
 */
export function GuestPetStep({ summary, pets, onBook, submitting, error, pickAgainHref }: GuestPetStepProps) {
  const text = guestBookingPage.pet;
  const groupName = useId();
  const [choice, setChoice] = useState(pets.length === 1 ? pets[0]!.id : pets.length === 0 ? NEW : '');
  const [newPet, setNewPet] = useState<NewPet>({ name: '', species: text.speciesOptions[0]! });
  const [speciesChoice, setSpeciesChoice] = useState(text.speciesOptions[0]!);
  const [missing, setMissing] = useState(false);

  function book() {
    if (choice === NEW) {
      const name = newPet.name.trim();
      const species = newPet.species.trim();

      if (!name || !species) {
        setMissing(true);
        return;
      }

      setMissing(false);
      onBook({ newPet: { name, species } });
    } else if (choice) {
      setMissing(false);
      onBook({ petId: choice });
    } else {
      setMissing(true);
    }
  }

  return (
    <div className="space-y-5">
      <div className="space-y-1">
        <h2 className="text-base font-semibold text-ink-900">{text.title}</h2>
        <p className="text-sm text-site-muted">{pets.length > 0 ? text.welcomeBack : text.welcomeNew}</p>
      </div>

      <Alert tone="success">{text.signedIn}</Alert>

      <PickedTimeSummary summary={summary} />

      {pets.length > 0 && (
        <div className="grid gap-2 sm:grid-cols-2">
          {pets.map((pet) => (
            <ChoiceCard
              key={pet.id}
              name={groupName}
              value={pet.id}
              checked={choice === pet.id}
              onChange={setChoice}
              title={pet.name}
              detail={pet.species}
            />
          ))}
          <ChoiceCard name={groupName} value={NEW} checked={choice === NEW} onChange={setChoice} title={text.newPet} detail={text.newPetDetail} />
        </div>
      )}

      {choice === NEW && (
        <NewPetFields
          value={newPet}
          speciesChoice={speciesChoice}
          onChange={(value, species) => {
            setNewPet(value);
            setSpeciesChoice(species);
          }}
        />
      )}

      {missing && <Alert>{text.missingPet}</Alert>}

      {error && (
        <Alert>
          {error}
          {pickAgainHref && (
            <>
              {' '}
              <Link href={pickAgainHref} className="font-semibold underline">
                {text.pickAgain}
              </Link>
            </>
          )}
        </Alert>
      )}

      <button
        type="button"
        onClick={book}
        disabled={submitting}
        className="h-12 w-full rounded-xl bg-site-orange text-sm font-bold text-white transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-50"
      >
        {submitting ? text.submitting : text.submit}
      </button>
    </div>
  );
}
