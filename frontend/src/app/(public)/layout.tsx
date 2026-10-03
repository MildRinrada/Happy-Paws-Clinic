import type { ReactNode } from 'react';
import { SiteFooter } from '@/components/layout/site-footer/site-footer';
import { SiteHeader } from '@/components/layout/site-header/site-header';
import { AssistantWidget } from '@/features/assistant/components/assistant-widget';

/** Chrome for the public site: what anyone sees before signing in. */
export default function PublicLayout({ children }: { children: ReactNode }) {
  return (
    <div className="min-h-screen bg-white font-display text-ink-900">
      <SiteHeader />
      {children}
      <SiteFooter />
      <AssistantWidget />
    </div>
  );
}
