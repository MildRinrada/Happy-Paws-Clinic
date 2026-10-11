'use client';

import type { ReactNode } from 'react';
import { useClinicNameShown } from './clinic-identity-context';

/**
 * The name written beside the clinic's mark, left out when the clinic's logo already spells it
 * (docs/contract-extensions.md § 50). The logo then carries the name as its alternative text.
 * Adds no element of its own, so a page without a logo is drawn exactly as before.
 */
export function ClinicNameSlot({ children }: { children: ReactNode }) {
  return useClinicNameShown() ? children : null;
}
