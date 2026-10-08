import { Button } from '@/components/ui/button';
import type { LineFollower } from '@/features/integrations/types/line.types';
import { formatDateTime } from '@/lib/format/datetime';
import { customerLine } from '../../content/customer-line';
import { LineAvatar } from './line-avatar';

/** One recent follower: picture, LINE name, when they were last seen, and the button to pick them. */
export function LineFollowerRow({ follower, onChoose }: { follower: LineFollower; onChoose: (follower: LineFollower) => void }) {
  const text = customerLine;

  return (
    <li className="flex items-center gap-3 py-2">
      <LineAvatar name={follower.display_name} pictureUrl={follower.picture_url} />
      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-medium text-ink-900">{follower.display_name ?? text.noName}</p>
        <p className="text-xs text-ink-500">{text.picker.followedAt(formatDateTime(follower.last_seen_at))}</p>
      </div>
      <Button variant="secondary" size="sm" onClick={() => onChoose(follower)}>
        {text.picker.choose}
      </Button>
    </li>
  );
}
