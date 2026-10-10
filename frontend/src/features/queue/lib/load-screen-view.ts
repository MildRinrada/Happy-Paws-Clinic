import { getDisplayBoard } from '../api/displayBoard';
import type { DisplayView } from '../types/queue.types';
import { buildDisplayView } from './build-display-view';

export interface ScreenView {
  branchId: string;
  branchName: string;
  view: DisplayView;
}

/**
 * The token screen's data (`/display`, docs/contract-extensions.md § 30): the display board, which
 * the backend has already cut down to numbers, rooms and vet names for the token's own branch. The
 * same layout as the staff-session screen, built by the same function.
 */
export async function loadScreenView(displayToken: string): Promise<ScreenView> {
  const board = await getDisplayBoard(displayToken);

  return {
    branchId: board.branch_id,
    branchName: board.branch_name,
    view: buildDisplayView(
      {
        service_date: board.service_date,
        waiting_count: board.waiting_count,
        tickets: board.tickets.map((ticket) => ({
          number: ticket.number,
          status: ticket.status,
          room_id: ticket.room_id,
          called_at: ticket.called_at,
          vetName: ticket.vet_name,
        })),
      },
      board.exam_rooms,
    ),
  };
}
