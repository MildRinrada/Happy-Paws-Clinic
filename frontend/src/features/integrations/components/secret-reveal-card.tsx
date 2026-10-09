import type { ReactNode } from 'react';
import { Alert } from '@/components/ui/alert';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { copyLabels } from '../content/copy';
import { CopyValue } from './copy-value';

interface SecretRevealCardProps {
  heading: string;
  label: string;
  secret: string;
  warning: string;
  children?: ReactNode;
}

/** A secret shown this once, with the warning that it will not be shown again. */
export function SecretRevealCard({ heading, label, secret, warning, children }: SecretRevealCardProps) {
  return (
    <Card className="border-amber-300">
      <CardHeader title={heading} />
      <CardBody className="space-y-4">
        <Alert tone="error">{warning}</Alert>
        <CopyValue label={label} value={secret} copyLabel={copyLabels.copy} copiedLabel={copyLabels.copied} />
        {children}
      </CardBody>
    </Card>
  );
}
