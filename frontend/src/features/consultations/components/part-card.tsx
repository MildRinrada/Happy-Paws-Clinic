import type { ReactNode } from 'react';
import { Card, CardBody, CardHeader } from '@/components/ui/card';

/** One section of the case: a titled card. `id` lets "ยังขาด" link straight to it. */
export function PartCard({ id, title, aside, children }: { id: string; title: string; aside?: ReactNode; children: ReactNode }) {
  return (
    <div id={id} className="scroll-mt-6">
      <Card>
        <CardHeader title={title} aside={aside} />
        <CardBody className="space-y-4">{children}</CardBody>
      </Card>
    </div>
  );
}
