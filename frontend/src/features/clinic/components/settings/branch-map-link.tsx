import { clinicSettings } from '../../content/clinic-settings';
import { mapLink } from '../../lib/map-link';

/** "เปิดในแผนที่" at the branch's pin, in a new tab. */
export function BranchMapLink({ latitude, longitude }: { latitude: number; longitude: number }) {
  const text = clinicSettings.branches;

  return (
    <a
      href={mapLink(latitude, longitude)}
      target="_blank"
      rel="noopener noreferrer"
      className="text-sm font-medium text-brand-700 hover:underline"
    >
      {text.openMap}
      <span className="sr-only"> {text.newTab}</span>
    </a>
  );
}
