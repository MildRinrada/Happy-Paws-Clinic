import { publicApi, staffApi } from '@/lib/api/server-client';
import type { ClinicContact, ContactInput } from '../types/contact.types';

/**
 * The clinic's contact entries — a contract extension (docs/contract-extensions.md § 15). Read
 * publicly by the website; written under `settings:write`.
 */

/** `GET /clinic/contacts` — public, in display order. */
export async function listContacts(): Promise<ClinicContact[]> {
  const { data } = await publicApi().get<{ data: ClinicContact[] }>('/clinic/contacts');
  return data;
}

/** `POST /clinic/contacts`. Blank optional fields are left out. */
export function addContact(input: ContactInput): Promise<ClinicContact> {
  return staffApi().post<ClinicContact>('/clinic/contacts', {
    body: {
      kind: input.kind,
      label: input.label,
      value: input.value,
      url: input.url || undefined,
      embed_url: input.embed_url || undefined,
      branch_id: input.branch_id || undefined,
      sort_order: input.sort_order,
    },
  });
}

/** `PATCH /clinic/contacts/{id}` — every field sent; an empty string clears an optional one. */
export function updateContact(id: string, version: number, input: ContactInput): Promise<ClinicContact> {
  return staffApi().patch<ClinicContact>(`/clinic/contacts/${encodeURIComponent(id)}`, { body: input, ifMatch: version });
}

/** `DELETE /clinic/contacts/{id}`. */
export function removeContact(id: string, version: number): Promise<void> {
  return staffApi().delete<void>(`/clinic/contacts/${encodeURIComponent(id)}`, { ifMatch: version });
}
