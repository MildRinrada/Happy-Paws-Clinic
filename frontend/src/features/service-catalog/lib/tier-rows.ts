import type { PriceTier } from '../types/service.types';
import { inputToSatang, satangToInput } from './baht-input';

/** One price-tier row as the form edits it: every box is text until it is saved. */
export interface TierRow {
  key: number;
  species: string;
  minKg: string;
  maxKg: string;
  price: string;
}

let nextKey = 0;

export function tierRow(tier?: PriceTier): TierRow {
  nextKey += 1;

  return {
    key: nextKey,
    species: tier?.species ?? '',
    minKg: tier?.min_weight_kg?.toString() ?? '',
    maxKg: tier?.max_weight_kg?.toString() ?? '',
    price: tier ? satangToInput(tier.price_satang) : '',
  };
}

const kilograms = (input: string) => {
  const trimmed = input.trim();
  return trimmed === '' || !Number.isFinite(Number(trimmed)) ? undefined : Number(trimmed);
};

/**
 * The rows as `PATCH …/pricing` takes them, or the index of the first row whose price is not a
 * number. Whether a row names enough to be a tier is the backend's rule and its refusal.
 */
export function tiersPayload(rows: TierRow[]): { tiers: PriceTier[] } | { badRow: number } {
  const tiers: PriceTier[] = [];

  for (const [index, row] of rows.entries()) {
    const price = inputToSatang(row.price);
    if (price === null) return { badRow: index };

    tiers.push({
      price_satang: price,
      species: row.species.trim() || undefined,
      min_weight_kg: kilograms(row.minKg),
      max_weight_kg: kilograms(row.maxKg),
    });
  }

  return { tiers };
}
