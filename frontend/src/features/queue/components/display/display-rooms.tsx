import { queueDisplay } from '../../content/queue-display';
import type { DisplayRoom as Room } from '../../types/queue.types';
import { DisplayRoom } from './display-room';

/** "กำลังตรวจ": a strip of the branch's exam rooms. Not drawn at a clinic with no room register. */
export function DisplayRooms({ rooms }: { rooms: Room[] }) {
  if (rooms.length === 0) {
    return null;
  }

  return (
    <section aria-labelledby="display-rooms" className="space-y-[1.5vh]">
      <h2 id="display-rooms" className="font-display text-[clamp(1.25rem,3.5vh,3rem)] font-semibold text-brand-300">
        {queueDisplay.inRoom}
      </h2>
      <ul className="grid gap-[2vh]" style={{ gridTemplateColumns: `repeat(${Math.min(rooms.length, 6)}, minmax(0, 1fr))` }}>
        {rooms.map((room) => (
          <DisplayRoom key={room.room} room={room} />
        ))}
      </ul>
    </section>
  );
}
