import type { DisplayRoom, DisplayView, QueueBoard, QueueTicket } from '../types/queue.types';

/**
 * Reduces the board to what a public screen may show: numbers, rooms and vet names.
 *
 * Done on the server, so the ticket objects (pet, owner and service ids) never travel to the
 * browser on the waiting-room TV; the page sends only this.
 *
 * - "กำลังเรียก" is every `called` ticket, the most recent call first, since that is the one
 *   people in the room are looking up for.
 * - "คิวถัดไป" is the waiting and skipped tickets in the backend's own call order, untouched.
 * - Each exam room shows the numbers `in_room` there. Rooms come from the branch's register, in
 *   its order, plus any room a ticket names that the register no longer lists.
 */
export function buildDisplayView(
  board: QueueBoard,
  examRooms: string[],
  vetName: (staffId?: string) => string | undefined,
): DisplayView {
  const called = board.tickets
    .filter((ticket) => ticket.status === 'called')
    .sort((a, b) => Date.parse(b.called_at ?? b.issued_at) - Date.parse(a.called_at ?? a.issued_at));

  const roomNames = [...examRooms];
  for (const ticket of board.tickets) {
    if (ticket.room_id && !roomNames.includes(ticket.room_id)) {
      roomNames.push(ticket.room_id);
    }
  }

  const rooms: DisplayRoom[] = roomNames.map((room) => {
    const inRoom = board.tickets.filter((ticket) => ticket.status === 'in_room' && ticket.room_id === room);
    const withVet = [...inRoom, ...called.filter((ticket) => ticket.room_id === room)].find(
      (ticket: QueueTicket) => ticket.staff_id,
    );

    return { room, vetName: vetName(withVet?.staff_id), inRoom: inRoom.map((ticket) => ticket.number) };
  });

  return {
    serviceDate: board.service_date,
    waitingCount: board.waiting_count,
    calls: called.map((ticket) => ({
      number: ticket.number,
      room: ticket.room_id,
      vetName: vetName(ticket.staff_id),
    })),
    rooms,
    next: board.tickets
      .filter((ticket) => ticket.status === 'waiting' || ticket.status === 'skipped')
      .map((ticket) => ticket.number),
  };
}
