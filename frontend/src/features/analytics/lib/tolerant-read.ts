import { unstable_rethrow } from 'next/navigation';
import { authed } from '@/lib/auth/session';

/**
 * Runs a read the overview can do without, answering `null` on any failure: a refusal, a missing
 * row, a 500 or a dropped connection. Each box on the page is a convenience next to the screen
 * where the work is done, so one that cannot be read is left out rather than taking the page down.
 *
 * A lapsed session still goes to sign in (`authed`), and Next's own control flow is never swallowed.
 */
export async function tolerant<T>(read: () => Promise<T>): Promise<T | null> {
  try {
    return await authed(read);
  } catch (error) {
    unstable_rethrow(error);
    return null;
  }
}
