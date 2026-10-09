import { staffApi } from '@/lib/api/server-client';
import type { LabSettings, LabSettingsInput, PaymentSettings, PaymentSettingsInput } from '../types/integration.types';

/**
 * `PUT /staff/integrations/payment` and `/lab` — need `integration:manage`. The contract has no
 * `GET`, so an empty body is how the settings are read: it writes nothing and returns them.
 */
export function readPaymentSettings(): Promise<PaymentSettings> {
  return staffApi().put<PaymentSettings>('/staff/integrations/payment', { body: {} });
}

export function configurePayment(version: number, input: PaymentSettingsInput): Promise<PaymentSettings> {
  return staffApi().put<PaymentSettings>('/staff/integrations/payment', { body: input, ifMatch: version });
}

export function readLabSettings(): Promise<LabSettings> {
  return staffApi().put<LabSettings>('/staff/integrations/lab', { body: {} });
}

export function configureLab(version: number, input: LabSettingsInput): Promise<LabSettings> {
  return staffApi().put<LabSettings>('/staff/integrations/lab', { body: input, ifMatch: version });
}
