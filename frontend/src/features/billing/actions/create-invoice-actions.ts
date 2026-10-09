'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { getCustomer } from '@/features/customers/api/getCustomer';
import { searchCustomers } from '@/features/customers/api/searchCustomers';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { createInvoice } from '../api/createInvoice';
import type { Invoice } from '../types/billing.types';

/**
 * The three calls behind `/billing/new`: finding the customer (`customer:read`), listing their pets
 * (`customer:read`), and raising the bill (`invoice:write`). The backend checks each scope again.
 */

export interface CustomerChoice {
  id: string;
  name: string;
  phone: string;
}

export interface PetChoice {
  id: string;
  name: string;
  species: string;
}

const SEARCH_LIMIT = 8;

const searchInput = z.object({ q: z.string().trim().min(1).max(100) });

export async function findCustomersAction(values: z.infer<typeof searchInput>): Promise<ActionResult<CustomerChoice[]>> {
  const { q } = searchInput.parse(values);
  const result = await runAction(() => searchCustomers({ q, limit: SEARCH_LIMIT }));

  return result.ok
    ? { ok: true, data: result.data.data.map((customer) => ({ id: customer.id, name: customer.display_name, phone: customer.phone })) }
    : result;
}

const petsInput = z.object({ customerId: z.string().trim().min(1).max(40) });

/** The customer's living pets, for the optional "which pet" on the bill. */
export async function customerPetsAction(values: z.infer<typeof petsInput>): Promise<ActionResult<PetChoice[]>> {
  const { customerId } = petsInput.parse(values);
  const result = await runAction(() => getCustomer(customerId));

  return result.ok
    ? {
        ok: true,
        data: result.data.pets
          .filter((pet) => !pet.is_deceased)
          .map((pet) => ({ id: pet.id, name: pet.name, species: pet.species })),
      }
    : result;
}

const line = z.object({
  description: z.string().trim().min(1).max(300),
  quantity: z.number().int().positive().max(100_000),
  // Never negative: a negative line is a discount that skipped its approval.
  unit_price_satang: z.number().int().nonnegative(),
  kind: z.enum(['service', 'treatment', 'medicine', 'other']).optional(),
  source_id: z.string().trim().min(1).max(40).optional(),
});

const createInput = z.object({
  customerId: z.string().trim().min(1).max(40),
  branchId: z.string().trim().min(1).max(40).optional(),
  petId: z.string().trim().min(1).max(40).optional(),
  note: z.string().trim().max(1000).optional(),
  lines: z.array(line).min(1).max(100),
  idempotencyKey: z.uuid(),
});

export async function createInvoiceAction(values: z.infer<typeof createInput>): Promise<ActionResult<Invoice>> {
  const input = createInput.parse(values);
  const result = await runAction(() => createInvoice(input));

  if (result.ok) {
    revalidatePath('/billing');
  }

  return result;
}
