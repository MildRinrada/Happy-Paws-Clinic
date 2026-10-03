import { cache } from 'react';
import { authed } from '@/lib/auth/session';
import { getPortalHome } from '../api/getPortalHome';

/**
 * `GET /portal/home`, once per request. The layout needs the unread count for the bell and the
 * home page needs the rest; both read this and the backend is asked once.
 */
export const loadPortalHome = cache(() => authed(getPortalHome));
