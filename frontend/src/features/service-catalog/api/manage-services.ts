import { publicApi, staffApi } from '@/lib/api/server-client';
import type {
  AvailabilityInput,
  ManagedService,
  NewServiceInput,
  PricingInput,
  ServiceCategory,
  ServiceDetailsInput,
} from '../types/managed-service.types';

/**
 * The service catalogue as staff manage it, all on `service:manage`. The list and the details edit
 * are an extension (docs/contract-extensions.md § 17); the rest are the contract's own.
 */

/** `GET /staff/services` — every service, counter-only and off-sale ones included. */
export async function listManagedServices(): Promise<ManagedService[]> {
  const { data } = await staffApi().get<{ data: ManagedService[] }>('/staff/services');
  return data;
}

/** `GET /services/categories` — public. */
export async function listServiceCategories(): Promise<ServiceCategory[]> {
  const { data } = await publicApi().get<{ data: ServiceCategory[] }>('/services/categories');
  return data;
}

/** `POST /staff/services`. */
export function createService(input: NewServiceInput): Promise<ManagedService> {
  return staffApi().post<ManagedService>('/staff/services', { body: input });
}

/** `PATCH /staff/services/{id}` — name, category, duration, who performs it, on sale or not. */
export function updateService(id: string, version: number, input: ServiceDetailsInput): Promise<ManagedService> {
  return staffApi().patch<ManagedService>(`/staff/services/${encodeURIComponent(id)}`, { body: input, ifMatch: version });
}

/** `PATCH /staff/services/{id}/pricing` — a dated price list; resending a date replaces that day's list. */
export function updateServicePricing(id: string, version: number, input: PricingInput): Promise<ManagedService> {
  return staffApi().patch<ManagedService>(`/staff/services/${encodeURIComponent(id)}/pricing`, { body: input, ifMatch: version });
}

/** `PATCH /staff/services/{id}/availability` — online booking, branches, weekly windows. */
export function updateServiceAvailability(id: string, version: number, input: AvailabilityInput): Promise<ManagedService> {
  return staffApi().patch<ManagedService>(`/staff/services/${encodeURIComponent(id)}/availability`, {
    body: input,
    ifMatch: version,
  });
}
