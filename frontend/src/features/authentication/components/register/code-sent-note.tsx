import { formatTime } from '@/lib/format/datetime';
import { registerPage } from '../../content/register-page';

interface CodeSentNoteProps {
  phone: string;
  /** When the code stops working, from the backend. Shown in clinic time. */
  expiresAt: string;
  onChangeNumber: () => void;
}

/** "ส่งรหัสไปที่ 081... แล้ว", how long the code lasts, and a way back to fix a mistyped number. */
export function CodeSentNote({ phone, expiresAt, onChangeNumber }: CodeSentNoteProps) {
  const text = registerPage.codeSent;

  return (
    <div className="rounded-[10px] border border-ink-200 bg-ink-50 px-4 py-3 text-[15px] text-ink-700">
      <p className="break-words">
        {text.sentTo} <span className="font-semibold text-ink-900">{phone}</span>
      </p>
      <p className="mt-1 text-sm text-site-muted">{text.validUntil(formatTime(expiresAt))}</p>
      <button type="button" onClick={onChangeNumber} className="mt-2 text-sm font-bold text-site-teal hover:underline">
        {text.changeNumber}
      </button>
    </div>
  );
}
