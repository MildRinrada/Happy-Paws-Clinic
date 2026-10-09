/** Where the calendar screens live in the console. */
export const appointmentPaths = {
  calendar: (day?: string) => (day ? `/appointments?date=${day}` : '/appointments'),
  book: (day?: string) => (day ? `/appointments/new?date=${day}` : '/appointments/new'),
  waitingList: '/appointments/waiting-list',
  /** The waiting list showing one status, from a page of it when `cursor` is given. */
  waitingListOf: (status: 'waiting' | 'lapsed', cursor?: string) =>
    `/appointments/waiting-list?status=${status}${cursor ? `&cursor=${encodeURIComponent(cursor)}` : ''}`,
};
