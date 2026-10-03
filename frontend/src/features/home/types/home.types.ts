/** A picture the page has a place for. Without `src` the place is drawn as an empty slot. */
export interface SlotImage {
  src?: string;
  alt: string;
}

export interface HeroAction {
  label: string;
  href: string;
}

export type StatTone = 'green' | 'orange' | 'purple' | 'gray';

export interface Stat {
  value: string;
  label: string;
  tone: StatTone;
}

export type ServiceTagTone = 'neutral' | 'warning' | 'notice';

export interface ServiceTag {
  label: string;
  tone: ServiceTagTone;
}

export interface FeaturedService {
  id: string;
  name: string;
  price: string;
  /** One entry per line as it should break on a wide screen. */
  description: string[];
  tags: ServiceTag[];
  image: SlotImage;
}

export type ServiceIconName = 'surgery' | 'lab' | 'grooming';

export interface MoreService {
  id: string;
  name: string;
  detail: string;
  icon: ServiceIconName;
}

export interface CarePromise {
  title: string;
  detail: string;
}

export interface Testimonial {
  id: string;
  /** One entry per line as it should break on a wide screen. The quote marks are added for you. */
  quote: string[];
  author: string;
  /** Whose owner they are, e.g. "เจ้าของไมโล". */
  relation: string;
}

export interface ContactRow {
  label: string;
  value: string;
  /** Makes the value a link — a call, a chat. */
  href?: string;
}

export interface LineBenefit {
  text: string;
}
