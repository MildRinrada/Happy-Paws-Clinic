/** Where the calendar screens live in the console. */
export const appointmentPaths = {
  calendar: (day?: string) => (day ? `/appointments?date=${day}` : '/appointments'),
  book: (day?: string) => (day ? `/appointments/new?date=${day}` : '/appointments/new'),
};
