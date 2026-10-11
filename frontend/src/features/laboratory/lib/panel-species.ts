import type { PanelSpecies } from '../types/result-panel.types';

const DOG = ['สุนัข', 'หมา', 'dog', 'canine'];
const CAT = ['แมว', 'cat', 'feline'];

/**
 * The species a panel has ranges for, from the pet's species as the counter typed it. The API takes
 * any text ("สุนัข", "Dog"), so the common spellings are matched; anything else has no ranges.
 */
export function panelSpeciesOf(species: string | undefined): PanelSpecies | undefined {
  const value = species?.trim().toLowerCase();
  if (!value) return undefined;
  if (DOG.includes(value)) return 'dog';
  if (CAT.includes(value)) return 'cat';

  return undefined;
}
