import { staffApi } from '@/lib/api/server-client';
import type { CalendarLink } from '../types/integration.types';

/**
 * `GET /staff/integrations/calendar/ics` asked for JSON: the signed-in person's subscription link,
 * made the first time it is asked for. Any staff member; the feed itself is fetched by the
 * calendar app with the token in the link.
 */
export function getCalendarLink(): Promise<CalendarLink> {
  return staffApi().get<CalendarLink>('/staff/integrations/calendar/ics');
}
