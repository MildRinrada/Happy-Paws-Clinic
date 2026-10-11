import { Badge } from '@/components/ui/badge';
import { clinicSettings } from '../../content/clinic-settings';

/** A branch's exam rooms as small labels, or a line saying there are none yet. */
export function ExamRoomBadges({ rooms }: { rooms: string[] }) {
  const text = clinicSettings.branches;

  if (rooms.length === 0) {
    return <p className="text-sm text-ink-500">{text.noRooms}</p>;
  }

  return (
    <ul aria-label={text.roomsLabel} className="flex flex-wrap gap-1.5">
      {rooms.map((room) => (
        <li key={room}>
          <Badge tone="blue">{room}</Badge>
        </li>
      ))}
    </ul>
  );
}
