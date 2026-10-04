import type { PriceTier } from './service.types';

/** A weekly window for online booking: `{ day: 'monday', from: '09:00', to: '12:00' }`. */
export interface BookingWindow {
  day: string;
  from: string;
  to: string;
}

/** A price list and the day it takes effect. No `effective_from` on the price a service was created with. */
export interface ServicePricing {
  effective_from?: string;
  base_price_satang: number;
  tiers: PriceTier[];
}

/** A service as staff manage it — `GET /staff/services` (docs/contract-extensions.md § 17). */
export interface ManagedService {
  id: string;
  version: number;
  code: string;
  name: string;
  category_code: string;
  duration_minutes: number;
  /** The base price in force today. */
  price_satang: number;
  online_booking_enabled: boolean;
  is_active: boolean;
  /** Empty: any vet on shift. */
  eligible_staff_ids: string[];
  /** Empty: every branch. */
  branch_ids: string[];
  /** Empty: any time a vet is on shift. */
  booking_windows: BookingWindow[];
  pricing: ServicePricing;
  /** Price lists entered for a later day, soonest first. */
  upcoming_pricing: ServicePricing[];
}

/** `GET /services/categories` — the contract's six headings. */
export interface ServiceCategory {
  code: string;
  name: string;
  name_en: string;
}

/** `POST /staff/services`. */
export interface NewServiceInput {
  name: string;
  category_code: string;
  duration_minutes: number;
  price_satang: number;
  code?: string;
  online_booking_enabled: boolean;
}

/** `PATCH /staff/services/{id}` (docs/contract-extensions.md § 17). */
export interface ServiceDetailsInput {
  name: string;
  category_code: string;
  duration_minutes: number;
  eligible_staff_ids: string[];
  is_active: boolean;
}

/** `PATCH /staff/services/{id}/pricing`. */
export interface PricingInput {
  base_price_satang: number;
  tiers: PriceTier[];
  effective_from: string;
}

/** `PATCH /staff/services/{id}/availability`. Every part sent; an empty list lifts that limit. */
export interface AvailabilityInput {
  online_booking_enabled: boolean;
  branch_ids: string[];
  booking_windows: BookingWindow[];
}
