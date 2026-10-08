import type { ReactNode } from 'react';
import { Card, CardBody, CardHeader } from '@/components/ui/card';

/** One report on a tab: a title, an optional line on how it is counted, and its body. */
export function ReportCard({ title, note, children }: { title: string; note?: string; children: ReactNode }) {
  return (
    <Card>
      <CardHeader title={title} />
      <CardBody className="space-y-4">
        {note && <p className="text-xs text-ink-500">{note}</p>}
        {children}
      </CardBody>
    </Card>
  );
}
