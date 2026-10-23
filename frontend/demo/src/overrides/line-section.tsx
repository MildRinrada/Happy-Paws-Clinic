import type { ReactNode } from 'react';
import { SiteContainer } from '@/components/layout/site-container';
import { SectionHeading } from '@/components/site/section-heading';
import { CtaLink } from '@/components/ui/cta-link';
import { line } from '@/features/home/content/line';
import { LineBenefit } from '@/features/home/components/line/line-benefit';

/**
 * The product's LINE section, with the right half filled: the product leaves it open for each
 * clinic's own picture, and the demo shows what the owner would actually receive, one message per
 * benefit listed on the left. The left half is the product's markup unchanged.
 */
export function LineSection() {
  return (
    <section aria-labelledby="line-heading" className="overflow-hidden bg-line-green pb-14 pt-[58px] text-white">
      <SiteContainer className="flex items-center justify-between gap-12">
        <div className="max-w-[563px]">
          <p className="inline-flex min-h-10 items-center rounded-full bg-white/30 px-6 py-1.5 font-light">{line.pill}</p>

          <div className="mt-7">
            <SectionHeading id="line-heading">
              {line.headingLines.map((headingLine) => (
                <span key={headingLine} className="block">
                  {headingLine}
                </span>
              ))}
            </SectionHeading>
          </div>

          <p className="mt-6 max-w-[450px] text-[15px] font-semibold leading-[19px]">{line.lead}</p>

          <ol className="mt-5 space-y-[17px]">
            {line.benefits.map((benefit, index) => (
              <LineBenefit key={benefit.text} number={index + 1} benefit={benefit} />
            ))}
          </ol>

          <div className="mt-9 flex justify-center">
            <CtaLink href={line.action.href} variant="dark" size="lg" className="w-[212px]">
              {line.action.label}
            </CtaLink>
          </div>
        </div>

        <LineChatPhone />
      </SiteContainer>
    </section>
  );
}

function Paw({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} aria-hidden="true">
      <g fill="currentColor">
        <ellipse cx="6.5" cy="9" rx="2" ry="2.6" />
        <ellipse cx="10.2" cy="5.6" rx="2" ry="2.7" />
        <ellipse cx="14.2" cy="5.6" rx="2" ry="2.7" />
        <ellipse cx="17.8" cy="9" rx="2" ry="2.6" />
        <path d="M12 11.2c-3.2 0-6 3.4-6 6 0 1.8 1.4 2.8 3 2.8 1.1 0 2-.6 3-.6s1.9.6 3 .6c1.6 0 3-1 3-2.8 0-2.6-2.8-6-6-6z" />
      </g>
    </svg>
  );
}

/** One message from the clinic: its avatar beside the first bubble of a run, the time under it. */
function Incoming({ children, time, first = false }: { children: ReactNode; time: string; first?: boolean }) {
  return (
    <div className="flex items-end gap-2">
      <span className={`grid size-7 shrink-0 place-items-center self-start rounded-full bg-line-green text-white ${first ? '' : 'invisible'}`}>
        <Paw className="size-4" />
      </span>
      <div className="min-w-0 max-w-[208px]">{children}</div>
      <span className="shrink-0 pb-0.5 text-[10px] text-white/90">{time}</span>
    </div>
  );
}

/** A LINE "Flex" card: a coloured title band, a body and buttons, as the clinic's reminders look. */
function Card({ title, tone, children, actions }: { title: string; tone: string; children: ReactNode; actions?: string[] }) {
  return (
    <div className="overflow-hidden rounded-2xl bg-white text-ink-900 shadow-sm">
      <p className={`px-3.5 py-2 text-[12px] font-semibold text-white ${tone}`}>{title}</p>
      <div className="space-y-0.5 px-3.5 py-2.5 text-[12px] leading-snug">{children}</div>
      {actions && (
        <div className="flex border-t border-ink-100">
          {actions.map((action) => (
            <span key={action} className="flex-1 py-2 text-center text-[12px] font-semibold text-[#06a14a] [&+&]:border-l [&+&]:border-ink-100">
              {action}
            </span>
          ))}
        </div>
      )}
    </div>
  );
}

/** What adding the clinic on LINE looks like on the owner's phone. A picture: nothing in it is a control. */
function LineChatPhone() {
  return (
    <figure className="relative hidden shrink-0 lg:block" aria-label="ตัวอย่างข้อความจากคลินิกใน LINE">
      <div aria-hidden="true" className="absolute -inset-10 rounded-full bg-white/15 blur-3xl" />
      <div aria-hidden="true" className="relative w-[300px] rotate-[2deg] rounded-[46px] bg-[#141414] p-[10px] shadow-[0_30px_60px_rgba(0,0,0,0.35)]">
        <div className="overflow-hidden rounded-[37px] bg-[#8fadd3] font-sans">
          {/* Status bar and the notch. */}
          <div className="relative flex h-8 items-center justify-between bg-white px-6 text-[11px] font-semibold text-ink-900">
            <span>9:41</span>
            <span className="absolute left-1/2 top-1.5 h-5 w-20 -translate-x-1/2 rounded-full bg-[#141414]" />
            <span className="tracking-tight">5G ▮▮▮</span>
          </div>

          {/* The chat's header. */}
          <div className="flex items-center gap-2.5 border-b border-ink-100 bg-white px-4 pb-2.5 pt-1.5 text-ink-900">
            <svg viewBox="0 0 24 24" className="size-4 text-ink-700" aria-hidden="true">
              <path d="M15 5l-7 7 7 7" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
            <span className="grid size-8 place-items-center rounded-full bg-line-green text-white">
              <Paw className="size-5" />
            </span>
            <span className="min-w-0">
              <span className="flex items-center gap-1 text-[13px] font-semibold leading-tight">
                Happy Paws Clinic
                <svg viewBox="0 0 16 16" className="size-3.5 text-line-green" aria-hidden="true">
                  <path fill="currentColor" d="M8 0l2 1.6 2.5-.2.7 2.4 2.2 1.3-.9 2.4.9 2.4-2.2 1.3-.7 2.4-2.5-.2L8 16l-2-1.6-2.5.2-.7-2.4L.6 10.9l.9-2.4L.6 6.1l2.2-1.3.7-2.4 2.5.2z" />
                  <path d="M5 8.2l2 2 4-4" fill="none" stroke="#fff" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" />
                </svg>
              </span>
              <span className="block text-[10px] leading-tight text-ink-500">@happypaws</span>
            </span>
          </div>

          <div className="space-y-2.5 px-3 pb-4 pt-3">
            <p className="mx-auto w-fit rounded-full bg-black/15 px-2.5 py-0.5 text-[10px] text-white">วันนี้</p>

            <Incoming time="09:00" first>
              <Card title="เตือนนัดพรุ่งนี้" tone="bg-[#06c755]" actions={['ยืนยัน', 'เลื่อนนัด']}>
                <p className="font-semibold">น้องโมจิ · ตรวจสุขภาพประจำปี</p>
                <p className="text-ink-600">พรุ่งนี้ 10:30 น. · สาขาสุขุมวิท</p>
                <p className="text-ink-600">สพ.ญ. มินท์ ศรีสุข</p>
              </Card>
            </Incoming>

            <Incoming time="10:42">
              <div className="rounded-2xl rounded-tl-md bg-white px-3.5 py-2.5 text-[12px] leading-snug text-ink-900 shadow-sm">
                อีก <span className="font-semibold">1 คิว</span> จะถึงน้องโมจิ (A015) เชิญที่หน้าห้องตรวจ 2 ได้เลยค่ะ
              </div>
            </Incoming>

            <Incoming time="11:20">
              <Card title="สรุปการตรวจและใบเสร็จ" tone="bg-[#217a71]" actions={['ดูสรุปการตรวจ']}>
                <p className="font-semibold">โมจิแข็งแรงดี หนัก 10.4 กก.</p>
                <p className="text-ink-600">ฉีดวัคซีนรวม DHPPi+L แล้ว</p>
                <p className="text-ink-600">ชำระแล้ว ฿918.00</p>
              </Card>
            </Incoming>

            <Incoming time="11:21">
              <div className="rounded-2xl rounded-tl-md bg-white px-3.5 py-2.5 text-[12px] leading-snug text-ink-900 shadow-sm">
                เข็มต่อไปครบกำหนด 12 พ.ย. ระบบจะเตือนล่วงหน้า 7 วันนะคะ
              </div>
            </Incoming>
          </div>

          {/* The message box. */}
          <div className="flex items-center gap-2 bg-white px-3 pb-5 pt-2.5">
            <span className="text-lg leading-none text-ink-400">+</span>
            <span className="h-8 flex-1 rounded-full bg-ink-100" />
            <span className="grid size-6 place-items-center rounded-full text-ink-400">
              <svg viewBox="0 0 24 24" className="size-5" aria-hidden="true">
                <path d="M12 3a3 3 0 00-3 3v6a3 3 0 006 0V6a3 3 0 00-3-3zM6 11a6 6 0 0012 0M12 17v4" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
              </svg>
            </span>
          </div>
        </div>
      </div>
      <figcaption className="sr-only">ตัวอย่างข้อความที่เจ้าของสัตว์ได้รับจากคลินิกทาง LINE: เตือนนัด แจ้งคิว สรุปการตรวจ และเตือนวัคซีน</figcaption>
    </figure>
  );
}
