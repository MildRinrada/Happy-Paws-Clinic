import type { BookingDetails, BookingOptions } from '../types/booking.types';

/**
 * Whether the details step has what the next step needs: a branch and a service, and a pet and a
 * type when the page asks for them. The summary card's button and the step's own both use it.
 */
export function detailsReady(options: BookingOptions, value: BookingDetails, askPet: boolean, askType: boolean): boolean {
  return Boolean(
    (!askPet || value.petId) && value.branchId && value.serviceId && (!askType || options.types.some((type) => type.code === value.type)),
  );
}
