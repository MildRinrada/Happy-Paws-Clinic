import type { SlotImage } from '@/features/home/types/home.types';

/**
 * The home page's care section as the product has it, with its picture filled in from the photos
 * the product already ships (the slot is empty in the product, for each clinic to fill).
 */
export * from '@/features/home/content/care.ts?original';

export const careImage: SlotImage = {
  // The injection photo is the vaccine card's, a few cards above; this one is calmer.
  src: '/images/sign-in/vet-exam-dog.jpg',
  alt: 'สัตวแพทย์ประคองลูกสุนัขชิวาวาบนโต๊ะตรวจ',
};
