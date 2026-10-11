import type { Metadata } from 'next';
import { IBM_Plex_Sans_Thai, Inter, Prompt } from 'next/font/google';
import type { ReactNode } from 'react';
import { ClinicIdentityProvider } from '@/components/layout/clinic-logo/clinic-identity-context';
import { siteEnv } from '@/config/env';
import { clinicIconsMetadata } from '@/features/clinic/lib/clinic-icons-metadata';
import { loadClinicIdentity } from '@/features/clinic/lib/load-clinic-identity';
import './globals.css';

// Self-hosted at build time by next/font, so the console makes no request to a font CDN from
// inside a clinic's network and text never flashes in a fallback face.
const plexThai = IBM_Plex_Sans_Thai({
  subsets: ['thai', 'latin'],
  weight: ['400', '500', '600', '700'],
  variable: '--font-plex-thai',
  display: 'swap',
});

const prompt = Prompt({
  subsets: ['thai', 'latin'],
  // 300/400 are the public site's body text; the console only uses Prompt for headings.
  weight: ['300', '400', '500', '600', '700'],
  variable: '--font-prompt',
  display: 'swap',
});

// One weight, Latin only: it sets the wordmark beside the logo and nothing else.
const inter = Inter({
  subsets: ['latin'],
  weight: ['700'],
  variable: '--font-inter',
  display: 'swap',
});

/**
 * The tab icon follows the clinic's own logo or tab icon (docs/contract-extensions.md § 50), read
 * from the profile every visitor shares for five minutes; with neither, or with the API away, it
 * is the paw under /public.
 */
export async function generateMetadata(): Promise<Metadata> {
  return {
    // Canonical links and Open Graph pictures are written as paths and resolved against this.
    metadataBase: new URL(siteEnv.url()),
    title: 'Happy Paws Clinic',
    description: 'ระบบจัดการคลินิกสัตวแพทย์',
    icons: clinicIconsMetadata(await loadClinicIdentity()),
  };
}

/**
 * Root layout shared by every route group. Group-specific chrome — the staff console shell,
 * the customer portal shell, the LIFF shell — belongs in each group's own layout.tsx, not here.
 *
 * The one thing every group shares is the clinic's mark: its logo, or the paw when it has none
 * (docs/contract-extensions.md § 50), handed to every header, footer and sidebar from here.
 */
export default async function RootLayout({ children }: { children: ReactNode }) {
  const identity = await loadClinicIdentity();

  return (
    // data-scroll-behavior: globals.css scrolls smoothly for in-page anchors; this tells Next to
    // switch it off while changing route, so a new page opens at its top rather than gliding there.
    <html
      lang="th"
      data-scroll-behavior="smooth"
      className={`${plexThai.variable} ${prompt.variable} ${inter.variable}`}
    >
      <body>
        <ClinicIdentityProvider identity={identity}>{children}</ClinicIdentityProvider>
      </body>
    </html>
  );
}
