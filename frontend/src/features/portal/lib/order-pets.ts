import type { MyPet } from '../types/portal.types';

/** Pets still with the family first, in the account's own order, then the ones that have died. */
export function alivePetsFirst<T extends Pick<MyPet, 'is_deceased'>>(pets: T[]): T[] {
  return [...pets.filter((pet) => !pet.is_deceased), ...pets.filter((pet) => pet.is_deceased)];
}

/** The pet a page opens on when none is named: the first still alive, or the first of all. */
export function firstPet<T extends Pick<MyPet, 'is_deceased'>>(pets: T[]): T | undefined {
  return pets.find((pet) => !pet.is_deceased) ?? pets[0];
}
