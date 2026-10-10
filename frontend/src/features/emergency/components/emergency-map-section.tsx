import Link from 'next/link';
import { MapPanel } from '@/features/contact/components/map-panel';
import { emergencyPage } from '../content/emergency-page';
import type { MapEntry } from '../lib/map-entries';

/**
 * How to get there: the clinic's map entries, drawn by the contact page's own map panel (a live
 * map when the entry has an embed, the address, and the link out to Google Maps). Nothing when the
 * clinic has set no map.
 */
export function EmergencyMapSection({ maps }: { maps: MapEntry[] }) {
  const text = emergencyPage.map;

  if (maps.length === 0) return null;

  return (
    <section aria-labelledby="emergency-map-heading" className="space-y-4">
      <h2 id="emergency-map-heading" className="text-2xl font-semibold leading-tight sm:text-[30px]">
        {text.heading}
      </h2>

      <div className={`grid gap-4 ${maps.length > 1 ? 'lg:grid-cols-2' : ''}`}>
        {maps.map((entry) => (
          <MapPanel key={entry.map.id} map={entry.map} title={entry.title} />
        ))}
      </div>

      <Link href={text.more.href} className="inline-block text-[15px] font-semibold text-site-teal hover:underline">
        {text.more.label}
      </Link>
    </section>
  );
}
