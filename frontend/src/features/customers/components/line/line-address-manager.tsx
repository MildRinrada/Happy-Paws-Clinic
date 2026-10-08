'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { customerLine } from '../../content/customer-line';
import type { CustomerLine } from '../../types/customer.types';
import { LineFollowerPicker } from './line-follower-picker';
import { UnlinkLineButton } from './unlink-line-button';

/** The buttons under the LINE card: pick or change the address, or take it off. */
export function LineAddressManager({ line, customerName }: { line: CustomerLine; customerName: string }) {
  const [picking, setPicking] = useState(false);

  if (picking) {
    return <LineFollowerPicker customerId={line.customer_id} customerName={customerName} version={line.version} onClose={() => setPicking(false)} />;
  }

  return (
    <div className="flex flex-wrap items-start gap-2">
      <Button variant="secondary" size="sm" onClick={() => setPicking(true)}>
        {line.linked ? customerLine.change : customerLine.pick}
      </Button>
      {line.linked && <UnlinkLineButton customerId={line.customer_id} version={line.version} />}
    </div>
  );
}
