import { portalPaths } from '../content/portal-navigation';
import type { MyPet } from '../types/portal.types';
import type { PetAvatarTone } from '../components/shared/pet-avatar';
import { alivePetsFirst } from './order-pets';
import { toneOfPet } from './pet-tone';

/** One pet in the switcher at the top right of a pet's page. */
export interface SwitcherPet {
  id: string;
  name: string;
  nickname?: string | null;
  photoUrl?: string | null;
  tone: PetAvatarTone;
  deceased: boolean;
  href: string;
  current: boolean;
}

/** What the switcher draws: a few chips, and, when there are more pets than fit, one control for all of them. */
export interface SwitcherView {
  chips: SwitcherPet[];
  all?: {
    count: number;
    pets: SwitcherPet[];
    /** A plain list to pick from, or one with a search box for a long list. */
    mode: 'select' | 'search';
  };
}

/** Up to this many pets, every one is a chip. */
const ALL_AS_CHIPS = 5;
/** Past that, this many chips (the current pet always among them), then "ทั้งหมด N ตัว". */
const CHIPS_BEFORE_MORE = 3;
/** From this many pets, the "ทั้งหมด" control has a search box. */
const SEARCH_FROM = 13;

/**
 * The switcher for an account's pets, the current one always visible. One pet needs no switcher.
 * Living pets come first; a pet that has died is still reachable, after them.
 */
export function buildSwitcher(pets: MyPet[], currentId: string): SwitcherView | undefined {
  if (pets.length < 2) return undefined;

  const ids = pets.map((pet) => pet.id);
  const items = alivePetsFirst(pets).map(
    (pet): SwitcherPet => ({
      id: pet.id,
      name: pet.name,
      nickname: pet.nickname,
      photoUrl: pet.photo_url,
      tone: toneOfPet(ids, pet.id),
      deceased: pet.is_deceased,
      href: portalPaths.pet(pet.id),
      current: pet.id === currentId,
    }),
  );

  if (items.length <= ALL_AS_CHIPS) return { chips: items };

  const chips = items.filter((item) => !item.deceased).slice(0, CHIPS_BEFORE_MORE);
  const current = items.find((item) => item.current);
  if (current && !chips.includes(current)) chips.push(current);

  return {
    chips,
    all: { count: items.length, pets: items, mode: items.length >= SEARCH_FROM ? 'search' : 'select' },
  };
}
