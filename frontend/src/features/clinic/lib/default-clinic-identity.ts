import type { ClinicIdentity } from '../types/clinic-identity.types';

/** What every page draws when the API cannot be read: the paw and the name, as before § 50. */
export const defaultClinicIdentity: ClinicIdentity = { name: 'Happy Paws Clinic', showName: true };
