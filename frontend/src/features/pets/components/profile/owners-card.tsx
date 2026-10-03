import Link from 'next/link';
import { Badge } from '@/components/ui/badge';
import { Card, CardHeader } from '@/components/ui/card';
import { customerPaths } from '@/features/customers/lib/customer-paths';
import { petProfile } from '../../content/pet-profile';
import type { PetOwner } from '../../lib/load-pet-profile';

/** Who brings this pet in. Each name opens that customer's page. */
export function OwnersCard({ owners }: { owners: PetOwner[] }) {
  const text = petProfile.owners;

  return (
    <Card>
      <CardHeader title={text.title} />
      <ul className="divide-y divide-ink-100">
        {owners.map((owner) => (
          <li key={owner.customerId} className="flex flex-wrap items-center justify-between gap-2 px-5 py-3">
            <div className="min-w-0">
              <Link href={customerPaths.detail(owner.customerId)} className="font-medium text-brand-700 hover:underline">
                {owner.name ?? text.unnamed}
              </Link>
              {owner.phone && <p className="text-sm text-ink-600">{owner.phone}</p>}
            </div>
            <div className="flex gap-1.5">
              <Badge tone={owner.isPrimary ? 'brand' : 'neutral'}>{owner.isPrimary ? text.primary : text.carer}</Badge>
              {!owner.mayViewHistory && <Badge tone="amber">{text.noHistory}</Badge>}
            </div>
          </li>
        ))}
      </ul>
    </Card>
  );
}
