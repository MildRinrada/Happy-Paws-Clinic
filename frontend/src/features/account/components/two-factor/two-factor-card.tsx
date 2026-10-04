import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { twoFactorSettings } from '../../content/two-factor-settings';
import type { Me } from '../../types/account.types';
import { TwoFactorSetup } from './two-factor-setup';
import { TwoFactorStatus } from './two-factor-status';

/** Where the second factor stands now, and the way to set it up or replace it. */
export function TwoFactorCard({ twoFactor }: { twoFactor: Me['two_factor'] }) {
  return (
    <Card>
      <CardHeader title={twoFactorSettings.heading} />
      <CardBody className="space-y-4">
        <TwoFactorStatus twoFactor={twoFactor} />
        <TwoFactorSetup enabled={twoFactor.enabled} />
      </CardBody>
    </Card>
  );
}
