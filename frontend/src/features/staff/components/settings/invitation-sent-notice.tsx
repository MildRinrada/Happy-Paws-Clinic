import { Button } from '@/components/ui/button';
import { formatDateTime } from '@/lib/format/datetime';
import { staffSettings } from '../../content/staff-settings';

interface InvitationSentNoticeProps {
  email: string;
  expiresAt?: string;
  onDismiss: () => void;
}

/**
 * Where the link went. The page never sees the link itself (the API does not return it), so the
 * notice says where to look: the invitee's inbox, or the API log on a server with no email set up.
 */
export function InvitationSentNotice({ email, expiresAt, onDismiss }: InvitationSentNoticeProps) {
  const text = staffSettings.sent;

  return (
    <div role="status" className="space-y-1.5 rounded-lg border border-emerald-200 bg-emerald-50 px-3.5 py-2.5 text-sm text-emerald-900">
      <p className="font-medium">{text.title(email)}</p>
      {expiresAt && <p>{text.expires(formatDateTime(expiresAt))}</p>}
      <p>{text.delivery}</p>
      <div className="flex justify-end">
        <Button variant="ghost" size="sm" onClick={onDismiss}>
          {text.dismiss}
        </Button>
      </div>
    </div>
  );
}
