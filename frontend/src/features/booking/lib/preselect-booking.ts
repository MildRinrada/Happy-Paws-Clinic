import type { BookingOptions } from '../types/booking.types';

/**
 * The branch and service a booking form opens on, from `?branch=` and `?service=` in the address.
 *
 * Each is used only when it names something on offer, and a service without a branch picks the
 * first branch that has it. Anything else falls back to the first branch and no service, which is
 * how the form opens without them.
 */
export function preselect(options: BookingOptions, service?: string, branch?: string): { branchId: string; serviceId: string } {
  const offers = (branchId: string) => Boolean(service && options.servicesByBranch[branchId]?.some((item) => item.id === service));
  const named = options.branches.find((item) => item.id === branch);
  const chosen = named ?? options.branches.find((item) => offers(item.id)) ?? options.branches[0];

  return {
    branchId: chosen?.id ?? '',
    serviceId: chosen && offers(chosen.id) ? service! : '',
  };
}
