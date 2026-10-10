import type { DisplayRoom, DisplayView, TicketStatus } from '../types/queue.types';

/**
 * One ticket as the screen needs it, whichever way it was read: from the staff board (the old
 * `/queue/display`, with the vet's name looked up) or from the display board a screen's own token
 * reads (`/display`, docs/contract-extensions.md § 30), which already carries it.
 */
export interface DisplaySourceTicket {
  number: string;
  status: TicketStatus;
  room_id?: string;
  called_at?: string;
  vetName?: string;
}

export interface DisplaySource {
  service_date: string;
  waiting_count: number;
  tickets: DisplaySourceTicket[];
}

const calledTime = (ticket: DisplaySourceTicket) => (ticket.called_at ? Date.parse(ticket.called_at) : 0);

/**
 * Reduces the board to what a public screen may show: numbers, rooms and vet names.
 *
 * Done on the server, so whatever else the board carried never travels to the browser on the
 * waiting-room TV; the page sends only this.
 *
 * - "กำลังเรียก" is every `called` ticket, the most recent call first, since that is the one
 *   people in the room are looking up for.
 * - "คิวถัดไป" is the waiting and skipped tickets in the backend's own call order, untouched.
 * - Each exam room shows the numbers `in_room` there. Rooms come from the branch's register, in
 *   its order, plus any room a ticket names that the register no longer lists.
 */
export function buildDisplayView(board: DisplaySource, examRooms: string[]): DisplayView {
  const called = board.tickets.filter((ticket) => ticket.status === 'called').sort((a, b) => calledTime(b) - calledTime(a));

  const roomNames = [...examRooms];
  for (const ticket of board.tickets) {
    if (ticket.room_id && !roomNames.includes(ticket.room_id)) {
      roomNames.push(ticket.room_id);
    }
  }

  const rooms: DisplayRoom[] = roomNames.map((room) => {
    const inRoom = board.tickets.filter((ticket) => ticket.status === 'in_room' && ticket.room_id === room);
    const withVet = [...inRoom, ...called.filter((ticket) => ticket.room_id === room)].find((ticket) => ticket.vetName);

    return { room, vetName: withVet?.vetName, inRoom: inRoom.map((ticket) => ticket.number) };
  });

  return {
    serviceDate: board.service_date,
    waitingCount: board.waiting_count,
    calls: called.map((ticket) => ({ number: ticket.number, room: ticket.room_id, vetName: ticket.vetName })),
    rooms,
    next: board.tickets
      .filter((ticket) => ticket.status === 'waiting' || ticket.status === 'skipped')
      .map((ticket) => ticket.number),
  };
}
