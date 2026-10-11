'use client';

import { defaultClinicIdentity } from '@/features/clinic/lib/default-clinic-identity';
import { useClinicIdentity } from './clinic-identity-context';

/**
 * The clinic's name as written beside its mark, from the clinic's profile rather than fixed in
 * code. `short` is the design's shorter form of the stand-in name, kept where the full one would
 * crowd a phone's header; a clinic with a name of its own always gets that name.
 */
export function ClinicNameText({ short }: { short?: string }) {
  const { name } = useClinicIdentity();

  return short && name === defaultClinicIdentity.name ? short : name;
}
