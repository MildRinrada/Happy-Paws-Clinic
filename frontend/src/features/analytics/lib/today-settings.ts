/**
 * The fixed numbers behind the overview page. The clinic has no setting for any of them (the queue
 * settings hold only the ticket numbering), so they are named here, in one place, rather than
 * spread through the components.
 */

/** A ticket waiting at least this long to be called is "รอนาน" and goes in the to-do box. */
export const LONG_WAIT_MINUTES = 30;

/** How far ahead "นัดถัดไป" looks. */
export const UPCOMING_HOURS = 2;

/** At most this many upcoming appointments are listed; the calendar has the rest. */
export const UPCOMING_LIMIT = 8;

/** How many customers to call the page previews; the CRM page has the whole list. */
export const DUE_BACK_PREVIEW = 5;

/** The page reads itself again this often while it is on screen. */
export const REFRESH_SECONDS = 60;

/** The largest page a list read may ask for. A count that reaches it is shown as "100+". */
export const LIST_CEILING = 100;
