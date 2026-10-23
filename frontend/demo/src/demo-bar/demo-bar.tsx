import { useEffect, useRef, useState, type ReactNode } from 'react';
import { on } from '../mock/core/bus';
import { DEMO_ACCOUNTS, DEMO_PASSWORD, DEMO_TOTP } from '../mock/core/demo-accounts';
import { listOutbox, type OutboxMessage } from '../mock/core/outbox';
import { currentRole, resetDemo, signInAs, type DemoRole } from '../mock/demo-session';
import { currentPlace, navigate, subscribe } from '../runtime/navigation';
import './demo-bar.css';

/**
 * A strip above the product that says what this is, switches between the people who use the
 * system, and holds the messages the real system would have sent by SMS, LINE and email.
 */

interface Perspective {
  role: DemoRole;
  label: string;
  home: string;
}

const PERSPECTIVES: Perspective[] = [
  { role: 'none', label: 'หน้าเว็บคลินิก', home: '/' },
  { role: 'customer', label: 'ลูกค้า', home: '/portal' },
  { role: 'reception', label: 'เคาน์เตอร์', home: '/queue' },
  { role: 'vet', label: 'สัตวแพทย์', home: '/dashboard' },
  { role: 'owner', label: 'เจ้าของคลินิก', home: '/dashboard' },
];

const SEEN_KEY = 'hp-demo-seen';
const GUIDED_KEY = 'hp-demo-guided';

function read(key: string): string | null {
  try {
    return localStorage.getItem(key);
  } catch {
    return null;
  }
}

function write(key: string, value: string) {
  try {
    localStorage.setItem(key, value);
  } catch {
    // Private windows may refuse storage; the bar still works for this visit.
  }
}

const CHANNEL_LABEL: Record<OutboxMessage['channel'], string> = { sms: 'SMS', email: 'อีเมล', line: 'LINE' };

function timeOf(iso: string) {
  return new Date(iso).toLocaleString('th-TH', { dateStyle: 'medium', timeStyle: 'short' });
}

/** Links in a message body, made clickable; a link to this site opens inside the demo. */
function Linked({ text, onFollow }: { text: string; onFollow: () => void }) {
  const parts = text.split(/(https?:\/\/[^\s]+)/g);
  return (
    <>
      {parts.map((part, index) =>
        /^https?:\/\//.test(part) ? (
          <a
            key={index}
            href={part}
            className="demo-link"
            onClick={(event) => {
              const url = new URL(part);
              if (url.origin !== location.origin) return;
              event.preventDefault();
              onFollow();
              navigate(url.pathname + url.search + url.hash);
            }}
          >
            {part.replace(location.origin, '')}
          </a>
        ) : (
          <span key={index}>{part}</span>
        ),
      )}
    </>
  );
}

function Dialog({ open, title, onClose, children }: { open: boolean; title: string; onClose: () => void; children: ReactNode }) {
  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    const dialog = ref.current;
    if (!dialog) return;
    if (open && !dialog.open) dialog.showModal();
    if (!open && dialog.open) dialog.close();
  }, [open]);
  return (
    <dialog ref={ref} className="demo-dialog" aria-labelledby="demo-dialog-title" onClose={onClose} onCancel={onClose}>
      <div className="demo-dialog__head">
        <h2 id="demo-dialog-title">{title}</h2>
        <button type="button" className="demo-dialog__close" onClick={onClose} aria-label="ปิด">
          <svg viewBox="0 0 20 20" width="18" height="18" aria-hidden="true">
            <path d="M5 5l10 10M15 5L5 15" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
          </svg>
        </button>
      </div>
      <div className="demo-dialog__body">{children}</div>
    </dialog>
  );
}

export function DemoBar() {
  const [messages, setMessages] = useState(listOutbox);
  const [seen, setSeen] = useState(() => read(SEEN_KEY) ?? '');
  const [role, setRole] = useState<DemoRole>(currentRole);
  const [panel, setPanel] = useState<'guide' | 'messages' | null>(() => (read(GUIDED_KEY) ? null : 'guide'));
  const [toast, setToast] = useState<OutboxMessage | null>(null);

  useEffect(
    () =>
      on(({ topic, data }) => {
        if (topic !== 'outbox') return;
        setMessages(listOutbox());
        if (data) setToast(data as OutboxMessage);
      }),
    [],
  );

  // A sign-in or sign-out through the product's own forms changes who this browser is.
  useEffect(() => subscribe(() => setRole(currentRole())), []);

  useEffect(() => {
    if (!toast) return;
    const timer = setTimeout(() => setToast(null), toast.code ? 15_000 : 6_000);
    return () => clearTimeout(timer);
  }, [toast]);

  const unread = messages.filter((message) => message.created_at > seen).length;

  function close() {
    if (panel === 'guide') write(GUIDED_KEY, '1');
    setPanel(null);
  }

  function become(perspective: Perspective) {
    close();
    signInAs(perspective.role);
    setRole(perspective.role);
    navigate(perspective.home);
  }

  function openMessages() {
    const latest = messages[0]?.created_at ?? '';
    setSeen(latest);
    write(SEEN_KEY, latest);
    setToast(null);
    setPanel('messages');
  }

  function openScreen() {
    // The waiting-room screen runs on the staff session; it opens in a tab of its own so the
    // console and the screen can be watched side by side.
    if (role === 'none' || role === 'customer') {
      signInAs('reception');
      setRole('reception');
      if (currentPlace().pathname !== '/queue') navigate('/queue');
    }
    window.open('/queue/display', '_blank', 'noopener');
  }

  function startOver() {
    if (!window.confirm('ล้างทุกอย่างที่ทำในเดโม แล้วกลับไปใช้ข้อมูลตัวอย่างเริ่มต้น?')) return;
    resetDemo();
    write(SEEN_KEY, '');
    location.assign('/');
  }

  return (
    <>
      <div className="demobar" role="region" aria-label="แถบเดโม">
        <p className="demobar__what">
          <span className="demobar__tag">โหมดเดโม</span>
          <span className="demobar__note">ข้อมูลอยู่ในเบราว์เซอร์นี้เท่านั้น ไม่มี SMS อีเมล หรือ LINE จริงส่งออกไป</span>
        </p>
        <div className="demobar__actions">
          <span className="demobar__label">ดูในมุมของ</span>
          {PERSPECTIVES.map((perspective) => (
            <button
              key={perspective.role}
              type="button"
              aria-pressed={role === perspective.role}
              onClick={() => become(perspective)}
            >
              {perspective.label}
            </button>
          ))}
          <button type="button" onClick={openScreen}>
            จอทีวีคิว
          </button>
          <span className="demobar__divider" aria-hidden="true" />
          <button
            type="button"
            className="demobar__mail"
            onClick={openMessages}
            aria-label={`ข้อความจำลอง${unread ? ` ใหม่ ${unread} ข้อความ` : ''}`}
          >
            ข้อความจำลอง
            {unread > 0 && <b aria-hidden="true">{unread}</b>}
          </button>
          <button type="button" onClick={() => setPanel('guide')}>
            วิธีเดโม
          </button>
        </div>
      </div>

      {toast && panel !== 'messages' && (
        <div className="demo-toast" role="status">
          <span className="demo-toast__meta">
            {CHANNEL_LABEL[toast.channel]} ถึง {toast.to} (จำลอง)
          </span>
          {toast.code ? (
            <span className="demo-toast__code">
              รหัส <strong>{toast.code}</strong>
            </span>
          ) : (
            <span className="demo-toast__body">{toast.subject ?? toast.body}</span>
          )}
          <div className="demo-toast__actions">
            <button type="button" onClick={openMessages}>
              เปิดดู
            </button>
            <button type="button" onClick={() => setToast(null)}>
              ปิด
            </button>
          </div>
        </div>
      )}

      <Dialog open={panel === 'messages'} onClose={close} title="ข้อความที่ระบบส่ง (จำลอง)">
        <p className="demo-muted">
          ระบบจริงส่งข้อความเหล่านี้ทาง SMS อีเมล หรือ LINE ในเดโมเก็บไว้ที่นี่ รหัส OTP และลิงก์ในข้อความใช้งานได้จริงในเดโม
        </p>
        {messages.length === 0 ? (
          <p>ยังไม่มีข้อความ ลองจองนัดหมายจากหน้าเว็บคลินิก หรือเรียกคิวจากหน้าเคาน์เตอร์ดูก่อน</p>
        ) : (
          <ol className="demo-messages">
            {messages.map((message) => (
              <li key={message.id}>
                <div className="demo-messages__head">
                  <span className={`demo-chip demo-chip--${message.channel}`}>{CHANNEL_LABEL[message.channel]}</span>
                  <span className="demo-muted">
                    ถึง {message.to} · {timeOf(message.created_at)}
                  </span>
                </div>
                {message.subject && <strong>{message.subject}</strong>}
                <p className="demo-messages__body">
                  <Linked text={message.body} onFollow={close} />
                </p>
                {message.code && (
                  <p className="demo-messages__code">
                    รหัส <strong>{message.code}</strong>
                  </p>
                )}
              </li>
            ))}
          </ol>
        )}
      </Dialog>

      <Dialog open={panel === 'guide'} onClose={close} title="ลองใช้ Happy Paws Clinic ในราว 10 นาที">
        <p className="demo-muted">
          นี่คือระบบจริงทั้งระบบที่ทำงานในเบราว์เซอร์ของคุณ ข้อมูลเป็นข้อมูลตัวอย่าง ทุกอย่างที่คุณกดหรือแก้จะถูกเก็บไว้ในเบราว์เซอร์นี้เท่านั้น
          และเริ่มใหม่ทุกวัน
        </p>
        <ol className="demo-guide">
          <li>
            <strong>หน้าเว็บคลินิก</strong> ดูบริการ ราคา ทีมแพทย์ บทความ แล้วกด <em>จองนัดหมาย</em> จองได้โดยไม่ต้องมีบัญชี
            รหัส OTP จะเด้งขึ้นที่มุมจอแทน SMS
          </li>
          <li>
            <strong>ลูกค้า</strong> พอร์ทัลของเจ้าของสัตว์ ดูนัดหมาย ประวัติการรักษาและวัคซีน ใบแจ้งหนี้ คิวของวันนี้ และแชทกับคลินิก
          </li>
          <li>
            <strong>เคาน์เตอร์</strong> เช็คอินลูกค้าและเรียกคิว แล้วกด <em>จอทีวีคิว</em> เพื่อเปิดจอในห้องรอในแท็บใหม่
            คิวจะเปลี่ยนบนจอทันทีที่เรียก
          </li>
          <li>
            <strong>สัตวแพทย์</strong> เปิดเคสที่กำลังตรวจ บันทึกอาการแบบ SOAP สั่งยา สั่งแล็บ และดูประวัติของสัตว์
          </li>
          <li>
            <strong>เจ้าของคลินิก</strong> แดชบอร์ดรายได้ รายงาน สต็อกยา CRM ข้อความอัตโนมัติ ตั้งค่าคลินิก และสิทธิ์ของพนักงาน
          </li>
        </ol>
        <section className="demo-box" aria-label="บัญชีสำหรับเข้าสู่ระบบเอง">
          <strong>เข้าสู่ระบบเองด้วยฟอร์ม</strong>
          <table className="demo-accounts">
            <tbody>
              {Object.values(DEMO_ACCOUNTS).map((account) => (
                <tr key={account.key}>
                  <th scope="row">{account.label}</th>
                  <td className="demo-mono">{account.key === 'customer' ? account.phone : account.email}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <span>
            รหัสผ่านทุกบัญชี <span className="demo-mono">{DEMO_PASSWORD}</span> · รหัสยืนยันตัวตนสองชั้น (2FA){' '}
            <span className="demo-mono">{DEMO_TOTP}</span>
          </span>
        </section>
        <div className="demo-dialog__actions">
          <button type="button" className="demo-button demo-button--quiet" onClick={startOver}>
            ล้างข้อมูลเดโม
          </button>
          <button type="button" className="demo-button demo-button--primary" onClick={close}>
            เริ่มเลย
          </button>
        </div>
      </Dialog>
    </>
  );
}
