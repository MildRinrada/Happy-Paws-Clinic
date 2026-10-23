import { DEMO_TOTP } from './demo-accounts';
import { minutesFromNow, nowIso, table, type Row } from './db';
import { send } from './outbox';

/**
 * One-time codes sent by SMS: registration, guest booking, a changed phone number. The code lands
 * in the demo's outbox instead of a phone. The guide's fixed code is accepted too, so a visitor
 * who missed the message is never stuck.
 */

interface Otp extends Row {
  phone: string;
  purpose: string;
  code: string;
  expires_at: string;
  used: boolean;
}

const codes = () => table<Otp>('demo_otps');

export function digitsOf(phone: string): string {
  return phone.replace(/\D/g, '').replace(/^66/, '0');
}

export function issueOtp(phone: string, purpose: string, what = 'รหัสยืนยัน'): { expires_at: string } {
  const code = String(Math.floor(100000 + Math.random() * 900000));
  const expires_at = minutesFromNow(5);
  codes().insert({ phone: digitsOf(phone), purpose, code, expires_at, used: false });
  send({ channel: 'sms', to: digitsOf(phone), body: `Happy Paws: ${what}ของคุณคือ ${code} (ใช้ได้ 5 นาที)`, code });
  return { expires_at };
}

/** True when the code is right for this number and purpose; the code is spent. */
export function checkOtp(phone: string, code: string, purpose: string): boolean {
  const digits = digitsOf(phone);
  if (code === DEMO_TOTP) return true;
  const match = codes().find((row) => row.phone === digits && row.purpose === purpose && row.code === code && !row.used && row.expires_at > nowIso());
  if (!match) return false;
  codes().update(match.id, { used: true });
  return true;
}
