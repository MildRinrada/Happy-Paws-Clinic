import type { SlotImage } from '@/features/home/types/home.types';
import heroDogCat from './hero-dog-cat.png';

/**
 * The home page's words as the product has them, with the banner picture filled in: the product
 * ships the slot empty for each clinic to fill, and a demo with an empty slot looks unfinished.
 */
export * from '@/features/home/content/hero.ts?original';

export const heroImage: SlotImage = { src: heroDogCat, alt: 'สุนัขโกลเด้นรีทรีฟเวอร์และแมวสีเทา' };
