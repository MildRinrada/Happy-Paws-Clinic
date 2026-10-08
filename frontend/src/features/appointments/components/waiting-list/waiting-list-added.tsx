import { Button } from '@/components/ui/button';
import { formatLongDate } from '@/lib/format/datetime';
import { waitingListText } from '../../content/advanced-booking';
import type { WaitingListEntry } from '../../types/advanced-booking.types';

interface WaitingListAddedProps {
  entry: WaitingListEntry;
  petName: string;
  onAnother: () => void;
}

/**
 * The entry as the backend stored it. This is the only time it is seen: the contract has no way
 * to list the waiting list afterwards.
 */
export function WaitingListAdded({ entry, petName, onAnother }: WaitingListAddedProps) {
  const text = waitingListText;
  // Dates are clinic days; noon keeps the long format on the same day in any viewer's zone.
  const day = (value: string) => formatLongDate(`${value}T12:00:00+07:00`);

  return (
    <div role="status" className="space-y-3 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3.5 text-sm text-emerald-900">
      <p className="font-semibold">
        {text.added}: {petName}
      </p>
      <p>{text.addedWindow(day(entry.earliest_on), day(entry.latest_on))}</p>
      {entry.note && <p className="text-emerald-800">{entry.note}</p>}
      <div className="flex justify-end">
        <Button variant="secondary" size="sm" onClick={onAnother}>
          {text.addAnother}
        </Button>
      </div>
    </div>
  );
}
