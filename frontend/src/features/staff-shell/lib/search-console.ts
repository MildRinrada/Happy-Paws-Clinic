import { searchCustomers } from '@/features/customers/api/searchCustomers';
import { searchPets } from '@/features/pets/api/searchPets';
import { ApiError } from '@/lib/api/api-error';
import type { ConsoleSearchResults } from '../types/staff-shell.types';

/** At most this many of each kind; the customers page has the rest. */
const LIMIT = 5;

/**
 * The top bar's search. Customers come from `GET /staff/customers?q=`, which matches a person's
 * name and phone and the name and HN of every pet they care for. Patients come from
 * `GET /staff/pets?q=` (name, HN or microchip), asked only when the session can open a pet's page;
 * a refusal there just leaves the patients out, since the customers are the part this box is for.
 */
export async function searchConsole(q: string, withPets: boolean): Promise<ConsoleSearchResults> {
  const [customers, pets] = await Promise.all([
    searchCustomers({ q, limit: LIMIT }),
    withPets
      ? searchPets(q, LIMIT).catch((error: unknown) => {
          if (error instanceof ApiError && error.status !== 401) return [];
          throw error;
        })
      : Promise.resolve([]),
  ]);

  return {
    customers: customers.data.map((customer) => ({ id: customer.id, name: customer.display_name, phone: customer.phone })),
    pets: pets.map((pet) => ({
      id: pet.id,
      name: pet.name,
      hospitalNumber: pet.hospital_number,
      species: pet.species,
      microchip: pet.microchip_number,
      deceased: pet.is_deceased,
    })),
  };
}
