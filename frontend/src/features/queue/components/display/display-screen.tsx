import type { DisplayView } from '../../types/queue.types';
import { DisplayCalls } from './display-calls';
import { DisplayHeader } from './display-header';
import { DisplayNext } from './display-next';
import { DisplayRooms } from './display-rooms';

interface Props {
  view: DisplayView;
  branchId: string;
  branchName: string;
}

/**
 * The waiting-room TV, laid out for a 16:9 screen read from across a room: sizes follow the
 * viewport height, the newest call takes most of the screen, the next numbers sit to its right,
 * and the exam rooms run along the bottom. Numbers, rooms and vet names only.
 */
export function DisplayScreen({ view, branchId, branchName }: Props) {
  return (
    <div className="flex h-dvh flex-col gap-[3vh] overflow-hidden p-[3vh]">
      <DisplayHeader branchId={branchId} branchName={branchName} serviceDate={view.serviceDate} />

      <div className="grid min-h-0 flex-1 grid-cols-[2fr_1fr] gap-[3vh]">
        <DisplayCalls calls={view.calls} />
        <DisplayNext next={view.next} waitingCount={view.waitingCount} />
      </div>

      <DisplayRooms rooms={view.rooms} />
    </div>
  );
}
