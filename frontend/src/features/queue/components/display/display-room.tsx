import { queueDisplay } from '../../content/queue-display';
import type { DisplayRoom as Room } from '../../types/queue.types';

/** One exam room along the bottom of the screen: its name, its vet, and who is inside. */
export function DisplayRoom({ room }: { room: Room }) {
  return (
    <li className="min-w-0 rounded-[1.5vh] bg-ink-800 px-[2vh] py-[1.5vh]">
      <p className="truncate text-[clamp(1rem,3vh,2.75rem)] font-medium text-ink-200">{room.room}</p>
      {room.vetName && <p className="truncate text-[clamp(0.875rem,2.4vh,2rem)] text-ink-400">{room.vetName}</p>}
      <p className="mt-[0.5vh] truncate font-display text-[clamp(1.5rem,6vh,5rem)] font-semibold leading-tight">
        {room.inRoom.length > 0 ? room.inRoom.join(' · ') : <span className="text-ink-500">{queueDisplay.roomFree}</span>}
      </p>
    </li>
  );
}
