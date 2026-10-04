import Link from 'next/link';
import { Badge } from '@/components/ui/badge';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { customerPaths } from '@/features/customers/lib/customer-paths';
import { petProfile } from '../../content/pet-profile';
import type { PetOwner } from '../../lib/load-pet-profile';
import { AddCarerForm } from './add-carer-form';
import { RemoveCarerButton } from './remove-carer-button';

interface OwnersCardProps {
  petId: string;
  owners: PetOwner[];
  /** `patient:write`: add a carer, or withdraw one. The primary owner is never removable here. */
  canManage: boolean;
}

/** Who brings this pet in. Each name opens that customer's page. */
export function OwnersCard({ petId, owners, canManage }: OwnersCardProps) {
  const text = petProfile.owners;

  return (
    <Card>
      <CardHeader title={text.title} />
      <ul className="divide-y divide-ink-100">
        {owners.map((owner) => (
          <li key={owner.customerId} className="flex flex-wrap items-center gap-2 px-5 py-3">
            <div className="min-w-0 flex-1">
              <Link href={customerPaths.detail(owner.customerId)} className="font-medium text-brand-700 hover:underline">
                {owner.name ?? text.unnamed}
              </Link>
              {owner.phone && <p className="text-sm text-ink-600">{owner.phone}</p>}
            </div>
            <div className="flex flex-wrap items-center gap-1.5">
              <Badge tone={owner.isPrimary ? 'brand' : 'neutral'}>{owner.isPrimary ? text.primary : text.carer}</Badge>
              {!owner.mayViewHistory && <Badge tone="amber">{text.noHistory}</Badge>}
            </div>
            {canManage && !owner.isPrimary && <RemoveCarerButton petId={petId} customerId={owner.customerId} />}
          </li>
        ))}
      </ul>
      {canManage && (
        <CardBody className="border-t border-ink-100">
          <AddCarerForm petId={petId} linkedIds={owners.map((owner) => owner.customerId)} />
        </CardBody>
      )}
    </Card>
  );
}
