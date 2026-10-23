import type { ReactNode } from 'react';
import { ClinicIdentityProvider } from '@/components/layout/clinic-logo/clinic-identity-context';
import { loadClinicIdentity } from '@/features/clinic/lib/load-clinic-identity';
import { asyncComponent } from './runtime/async-component';

/**
 * The product's app/layout.tsx without what only a server can do: `<html>` and `<body>` are
 * index.html's, and the fonts load from there. What is left is the clinic's mark, handed to every
 * header, footer and sidebar below.
 */
async function DemoRootLayout({ children }: { children: ReactNode }) {
  const identity = await loadClinicIdentity();
  return <ClinicIdentityProvider identity={identity}>{children}</ClinicIdentityProvider>;
}

export default asyncComponent(DemoRootLayout, 'demo/root-layout');
