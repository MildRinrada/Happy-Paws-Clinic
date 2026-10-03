'use client';

import { Button } from '@/components/ui/button';

/**
 * What the console shows when something actually broke — the API is unreachable, or answered 500.
 *
 * A refusal is not handled here. "You lack this scope" and "this bill is already paid" are answers
 * the pages render in place, in the backend's own words. This is for faults, and it says nothing
 * about what went wrong beyond the digest, because the detail is in the server log and an error
 * message on a clinic's screen is not the place for it.
 */
export default function StaffError({ error, reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return (
    <div role="alert" className="mx-auto max-w-md rounded-xl border border-red-200 bg-white p-6 text-center">
      <p className="font-display text-lg font-semibold text-ink-900">โหลดข้อมูลไม่สำเร็จ</p>
      <p className="mt-1 text-sm text-ink-600">ระบบขัดข้องชั่วคราว ข้อมูลที่บันทึกไปแล้วไม่ได้รับผลกระทบ</p>
      {error.digest && <p className="mt-2 font-mono text-xs text-ink-400">รหัสอ้างอิง {error.digest}</p>}
      <Button className="mt-4" onClick={reset}>
        ลองใหม่
      </Button>
    </div>
  );
}
