import type { PriceTier, PublicService } from '../types/service.types';

/**
 * Prices for the public menu. Whole baht drop the ".00" the counter screens show: "฿500", but
 * "฿1,250.50" keeps its satang.
 */
const wholeBaht = new Intl.NumberFormat('th-TH', { style: 'currency', currency: 'THB', maximumFractionDigits: 0 });
const bahtAndSatang = new Intl.NumberFormat('th-TH', { style: 'currency', currency: 'THB', minimumFractionDigits: 2 });

export const formatMenuPrice = (satang: number) =>
  (satang % 100 === 0 ? wholeBaht : bahtAndSatang).format(satang / 100);

/**
 * The lowest and highest price a service can cost. The base price applies to an animal no tier
 * matches, so it counts alongside the tiers. Equal when the service has one price.
 */
export function priceRange(service: Pick<PublicService, 'price_satang' | 'price_tiers'>): { min: number; max: number } {
  const prices = [service.price_satang, ...service.price_tiers.map((tier) => tier.price_satang)];

  return { min: Math.min(...prices), max: Math.max(...prices) };
}

/** "30 นาที", "1 ชม.", "1 ชม. 30 นาที". */
export function formatDuration(minutes: number): string {
  const hours = Math.floor(minutes / 60);
  const rest = minutes % 60;

  if (hours === 0) {
    return `${rest} นาที`;
  }

  return rest === 0 ? `${hours} ชม.` : `${hours} ชม. ${rest} นาที`;
}

/**
 * Who a tier is for, in words: "สุนัข 10 ถึงไม่ถึง 20 กก.", "แมว", "ไม่ถึง 5 กก.". The maximum
 * weight is not included, as the staff screen says when the price is set.
 */
export function describeTier(tier: PriceTier): string {
  // A bound the clinic left blank may arrive as null as well as absent.
  const min = typeof tier.min_weight_kg === 'number' ? tier.min_weight_kg : undefined;
  const max = typeof tier.max_weight_kg === 'number' ? tier.max_weight_kg : undefined;
  let weight = '';

  if (min !== undefined && max !== undefined) {
    weight = `${min} ถึงไม่ถึง ${max} กก.`;
  } else if (min !== undefined) {
    weight = `${min} กก. ขึ้นไป`;
  } else if (max !== undefined) {
    weight = `ไม่ถึง ${max} กก.`;
  }

  return [tier.species, weight].filter(Boolean).join(' ');
}
