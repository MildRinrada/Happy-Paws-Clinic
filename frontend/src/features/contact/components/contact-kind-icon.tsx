import { FacebookIcon } from '@/components/layout/site-footer/icons/facebook-icon';
import { InstagramIcon } from '@/components/layout/site-footer/icons/instagram-icon';
import { LineIcon } from '@/components/layout/site-footer/icons/line-icon';
import type { ContactKind } from '@/features/clinic/types/contact.types';

/** Simple line glyphs for the kinds without a brand icon, drawn in the current text colour. */
const glyphs: Partial<Record<ContactKind, string>> = {
  phone: 'M6.6 10.8a15.1 15.1 0 0 0 6.6 6.6l2.2-2.2a1 1 0 0 1 1-.25 11.4 11.4 0 0 0 3.6.57 1 1 0 0 1 1 1V20a1 1 0 0 1-1 1A17 17 0 0 1 3 4a1 1 0 0 1 1-1h3.5a1 1 0 0 1 1 1c0 1.25.2 2.45.57 3.57a1 1 0 0 1-.25 1L6.6 10.8Z',
  emergency: 'M12 2 1 21h22L12 2Zm1 15h-2v-2h2v2Zm0-4h-2V9h2v4Z',
  email: 'M3 5h18a1 1 0 0 1 1 1v12a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1V6a1 1 0 0 1 1-1Zm9 7.5L3.6 7v.1l8.4 5.6 8.4-5.6V7L12 12.5Z',
  website: 'M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20Zm6.9 6h-3a15.7 15.7 0 0 0-1.4-3.6A8 8 0 0 1 18.9 8ZM12 4c.8 1.2 1.5 2.5 1.9 4h-3.8c.4-1.5 1.1-2.8 1.9-4ZM4.3 14a7.9 7.9 0 0 1 0-4h3.4a16 16 0 0 0 0 4H4.3Zm.8 2h3a15.7 15.7 0 0 0 1.4 3.6A8 8 0 0 1 5.1 16Zm3-8h-3a8 8 0 0 1 4.4-3.6A15.7 15.7 0 0 0 8.1 8ZM12 20c-.8-1.2-1.5-2.5-1.9-4h3.8c-.4 1.5-1.1 2.8-1.9 4Zm2.3-6H9.7a14.7 14.7 0 0 1 0-4h4.6a14.7 14.7 0 0 1 0 4Zm.3 5.6c.6-1.1 1.1-2.3 1.4-3.6h3a8 8 0 0 1-4.4 3.6Zm1.7-5.6a16 16 0 0 0 0-4h3.4a7.9 7.9 0 0 1 0 4h-3.4Z',
  map: 'M12 2a7 7 0 0 0-7 7c0 5.2 7 13 7 13s7-7.8 7-13a7 7 0 0 0-7-7Zm0 9.5A2.5 2.5 0 1 1 12 6.5a2.5 2.5 0 0 1 0 5Z',
  other: 'M4 10a2 2 0 1 0 0 4 2 2 0 0 0 0-4Zm8 0a2 2 0 1 0 0 4 2 2 0 0 0 0-4Zm8 0a2 2 0 1 0 0 4 2 2 0 0 0 0-4Z',
};

/** The icon for one kind of contact entry: the app's own for LINE, Facebook and Instagram. */
export function ContactKindIcon({ kind, className = 'size-10' }: { kind: ContactKind; className?: string }) {
  if (kind === 'line') return <LineIcon className={className} />;
  if (kind === 'facebook') return <FacebookIcon className={className} />;
  if (kind === 'instagram') return <InstagramIcon className={className} />;

  return (
    <span aria-hidden="true" className={`inline-flex items-center justify-center rounded-xl bg-site-teal/10 text-site-teal ${className}`}>
      <svg viewBox="0 0 24 24" className="size-5" fill="currentColor">
        <path d={glyphs[kind] ?? glyphs.other} />
      </svg>
    </span>
  );
}
