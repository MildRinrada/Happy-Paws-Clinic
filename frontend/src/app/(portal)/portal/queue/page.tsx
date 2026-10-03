import { redirect } from 'next/navigation';

/** The visit card now lives at the top of the portal's home. Kept so an old link still lands. */
export default function PortalQueuePage() {
  redirect('/portal');
}
