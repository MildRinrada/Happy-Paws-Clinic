import Link from 'next/link';
import { Alert } from '@/components/ui/alert';
import { formatDateTime } from '@/lib/format/datetime';
import { portalReschedule } from '../../content/portal-reschedule';
import { portalPaths } from '../../content/portal-navigation';

interface RescheduleRefusedProps {
  /** The backend's own message on `cutoff_passed`, which names the cut-off in hours. */
  message: string;
  /** `details.cutoff_at`: when online changes closed for this appointment. */
  closedAt?: string;
}

/** Too late to move it online: what the backend said, and the way forward, which is the clinic. */
export function RescheduleRefused({ message, closedAt }: RescheduleRefusedProps) {
  const text = portalReschedule.refused;

  return (
    <div className="space-y-3">
      <Alert>
        <p>{message}</p>
        {closedAt && <p className="mt-1 text-xs">{text.closedAt(formatDateTime(closedAt))}</p>}
      </Alert>
      <div className="flex flex-wrap gap-4 text-sm font-semibold">
        <Link href={text.contact.href} className="text-site-teal hover:underline">
          {text.contact.label}
        </Link>
        <Link href={portalPaths.appointments} className="text-site-teal hover:underline">
          {text.back}
        </Link>
      </div>
    </div>
  );
}
