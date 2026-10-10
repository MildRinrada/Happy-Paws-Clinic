import { listBranches } from '@/features/clinic/api/listBranches';
import { vetNames } from '@/features/portal/lib/display-names';
import { loadPortalHome } from '@/features/portal/lib/load-portal-home';
import { listServices } from '@/features/service-catalog/api/listServices';
import { listOnlineAppointmentTypes } from '../api/listOnlineAppointmentTypes';
import type { BookingDetails, BookingOptions } from '../types/booking.types';
import { preselect } from './preselect-booking';

/**
 * Everything the booking form chooses from, read once on the server: the owner's living pets, the
 * open branches, each branch's online services, the types the clinic lets customers book, and the
 * vets' names.
 *
 * `petId` preselects a pet when the page is opened from that pet's card (`?pet=`), and `service`
 * and `branch` a service when it comes from the public booking page or the services page.
 */
export async function loadBookingOptions(
  petId?: string,
  service?: string,
  branch?: string,
): Promise<{ options: BookingOptions; initial: BookingDetails }> {
  const [home, branches, vets, types] = await Promise.all([loadPortalHome(), listBranches(), vetNames(), listOnlineAppointmentTypes()]);

  const services = await Promise.all(branches.map((branch) => listServices(branch.id)));
  const pets = home.pets.filter((pet) => !pet.is_deceased);

  const options: BookingOptions = {
    pets: pets.map(({ id, name, species }) => ({ id, name, species })),
    branches: branches.map(({ id, name }) => ({ id, name })),
    servicesByBranch: Object.fromEntries(
      branches.map((branch, index) => [
        branch.id,
        services[index]!.map(({ id, name, duration_minutes, price_satang }) => ({ id, name, duration_minutes, price_satang })),
      ]),
    ),
    types,
    vets: Object.fromEntries(vets),
  };

  const firstPet = pets.find((pet) => pet.id === petId) ?? (pets.length === 1 ? pets[0] : undefined);

  return {
    options,
    initial: {
      petId: firstPet?.id ?? '',
      ...preselect(options, service, branch),
      // `regular` when the clinic offers it online, as every booking that names no type is.
      type: types.some((type) => type.code === 'regular') ? 'regular' : (types[0]?.code ?? 'regular'),
      reason: '',
    },
  };
}
