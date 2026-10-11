'use client';

import { createContext, useContext, type ReactNode } from 'react';
import { defaultClinicIdentity } from '@/features/clinic/lib/default-clinic-identity';
import type { ClinicIdentity } from '@/features/clinic/types/clinic-identity.types';

const ClinicIdentityContext = createContext<ClinicIdentity>(defaultClinicIdentity);

/**
 * Hands the clinic's logo to every mark on the page (docs/contract-extensions.md § 50). The root
 * layout reads it once on the server; the marks sit in server and client components alike, so a
 * context reaches all of them without each one reading the API.
 */
export function ClinicIdentityProvider({ identity, children }: { identity: ClinicIdentity; children: ReactNode }) {
  return <ClinicIdentityContext.Provider value={identity}>{children}</ClinicIdentityContext.Provider>;
}

export function useClinicIdentity(): ClinicIdentity {
  return useContext(ClinicIdentityContext);
}

/** True when the name is written beside the mark: always for the paw, and for a logo unless switched off. */
export function useClinicNameShown(): boolean {
  const identity = useClinicIdentity();
  return identity.showName || !identity.logoSrc;
}
