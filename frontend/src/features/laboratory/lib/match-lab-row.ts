import type { LabRequest } from '../types/lab.types';
import type { LabFilters } from './lab-filters';

interface Matchable {
  request: LabRequest;
  pet?: { name: string; hospitalNumber: string };
}

/**
 * The search box and the destination filter. The list endpoint has neither a text search nor a
 * destination filter, so both are applied here to the page that was loaded. A pet the session may
 * not read (no `patient:read`) is matched by its id, which is what its row shows.
 */
export function matchesLabFilters(row: Matchable, filters: LabFilters): boolean {
  if (filters.destination && row.request.destination !== filters.destination) return false;
  if (!filters.query) return true;

  const needle = filters.query.toLocaleLowerCase('th');
  const haystack = row.pet ? [row.pet.name, row.pet.hospitalNumber] : [row.request.pet_id];

  return haystack.some((value) => value.toLocaleLowerCase('th').includes(needle));
}
