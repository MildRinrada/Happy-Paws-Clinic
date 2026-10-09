import type { Metadata } from 'next';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { DisplayMessage } from '@/features/queue/components/display/display-message';
import { DisplayScreen } from '@/features/queue/components/display/display-screen';
import { queueDisplay } from '@/features/queue/content/queue-display';
import { loadDisplayView } from '@/features/queue/lib/load-display-view';
import type { DisplayView } from '@/features/queue/types/queue.types';
import { ApiError } from '@/lib/api/api-error';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: queueDisplay.metaTitle };

interface Props {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}

/**
 * The waiting-room screen: who is being called and where to, who is next, who is in each room.
 *
 * `?branch=` pins a TV to one branch; without it the screen follows the branch the console last
 * chose on this browser. A branch this session cannot reach is ignored, not trusted.
 *
 * Shows ticket numbers, rooms and vet names only. The reasons, and how the screen stays signed
 * in, are in docs/features/queue.md under "จอเรียกคิวในห้องรอ".
 */
export default async function QueueDisplayPage({ searchParams }: Props) {
  const context = await loadStaffContext();
  const requested = (await searchParams).branch;
  const branch = context?.branches.find((item) => item.id === requested) ?? context?.branch;

  if (!context || !branch) {
    return <DisplayMessage title={queueDisplay.noBranch} />;
  }

  if (!context.can(Scope.QueueRead)) {
    return <DisplayMessage title={queueDisplay.noAccess} />;
  }

  let view: DisplayView;

  try {
    view = await authed(() => loadDisplayView(branch.id, branch.exam_rooms));
  } catch (error) {
    if (error instanceof ApiError && error.status === 403) {
      return <DisplayMessage title={error.message} />;
    }
    throw error;
  }

  return <DisplayScreen view={view} branchId={branch.id} branchName={branch.name} />;
}
