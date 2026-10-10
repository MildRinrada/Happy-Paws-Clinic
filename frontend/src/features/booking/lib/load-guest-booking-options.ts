import { listBranches } from '@/features/clinic/api/listBranches';
import { vetNames } from '@/features/portal/lib/display-names';
import { listServices } from '@/features/service-catalog/api/listServices';
import type { BookingDetails, BookingOptions } from '../types/booking.types';
import { preselect } from './preselect-booking';

/**
 * What the public booking page (`/book`) chooses from, read on the server from the public lists
 * only: the open branches, each branch's online services, and the vets' names. No pets and no
 * types: those need a customer's session, which the visitor gets at the last step.
 *
 * `service` and `branch` come from the address (`/book?service=svc_...`, as the services page
 * links) and preselect the form when they name something on offer.
 */
export async function loadGuestBookingOptions(
  service?: string,
  branch?: string,
): Promise<{ options: BookingOptions; initial: BookingDetails }> {
  const [branches, vets] = await Promise.all([listBranches(), vetNames()]);
  const services = await Promise.all(branches.map((item) => listServices(item.id)));

  const options: BookingOptions = {
    pets: [],
    branches: branches.map(({ id, name }) => ({ id, name })),
    servicesByBranch: Object.fromEntries(
      branches.map((item, index) => [
        item.id,
        services[index]!.map(({ id, name, duration_minutes, price_satang }) => ({ id, name, duration_minutes, price_satang })),
      ]),
    ),
    types: [],
    vets: Object.fromEntries(vets),
  };

  return {
    options,
    initial: { petId: '', ...preselect(options, service, branch), type: 'regular', reason: '' },
  };
}
