# การอัปเดตสด

กระดานคิว (`/queue`) เปลี่ยนเองเมื่อมีการเช็กอิน ออกบัตร walk-in เรียกคิว ข้ามคิว หรือปิดเคส โดยไม่ต้องกดรีเฟรช ระบบใช้ Server-Sent Events (SSE) ที่สัญญากำหนดไว้ว่า `SSE /queue/stream?branch=` ส่ง `ticket.called`, `ticket.updated` และ `board.refresh` ตอนนี้มีสตรีมนี้สตรีมเดียว และใช้เฉพาะกับคิว

## หลักการ

**สตรีมส่งสัญญาณ REST ส่งข้อมูล**

เหตุการณ์บอกแค่ว่าบัตรใบไหนเปลี่ยน ไม่ได้บอกสภาพของกระดาน เมื่อหน้าเว็บได้เหตุการณ์ จะ render server component ใหม่ ซึ่งอ่าน `GET /staff/queue?branch=` จาก API อีกครั้ง หน้าเว็บจึงไม่มีสำเนาคิวของตัวเองให้คลาดจาก API และกติกาลำดับคิวกับสิทธิ์อยู่ที่ API ที่เดียว ถ้าเหตุการณ์หายไประหว่างทาง หน้าจอก็ยังถูกเมื่อได้เหตุการณ์ถัดไป

## เส้นทางของสตรีม

```text
EventSource ในเบราว์เซอร์
   │  GET /api/queue/stream?branch=brn_x   (same origin, คุกกี้ hp_at)
   ▼
src/middleware.ts                       ต่ออายุ token ถ้า hp_at หมด
   ▼
app/api/queue/stream/route.ts           อ่าน hp_at แล้วเปิดสตรีมไปที่ API
   │  GET /v1/queue/stream?branch=brn_x
   │  Authorization: Bearer <access token>
   ▼
QueueStreamController (API)             ตรวจผู้เรียก แล้วเขียน event ทีละบรรทัด
   ▲
QueueEventBroker (singleton)            รับ Publish จาก handler หลัง commit
```

route handler ส่ง body ของ API ต่อไปทั้งก้อนโดยไม่แตะ และตั้ง `runtime = 'nodejs'` กับ `dynamic = 'force-dynamic'` เพื่อให้ถือ socket ได้นานและไม่ถูก cache

**ทำไมต้องผ่าน Next.js** `EventSource` ของเบราว์เซอร์ตั้ง header เองไม่ได้ และ token อยู่ในคุกกี้ที่ script อ่านไม่ได้ ทางเลือกคือใส่ token ใน query string ซึ่งจะไปโผล่ใน access log และ cache ของ proxy ทุกตัวระหว่างทาง จึงให้เบราว์เซอร์ต่อเข้า origin ของตัวเอง แล้วเซิร์ฟเวอร์ Next.js แนบ token ให้ เหมือนคำขออื่นทุกเส้น การตีความนี้บันทึกไว้ใน [จุดกำกวมของสัญญา](assumptions.md)

## การตรวจตอนเชื่อมต่อ

| จุดตรวจ | ไม่ผ่านได้ |
|---|---|
| middleware ของ Next.js: ไม่มีทั้ง `hp_at` และ `hp_rt` หรือ refresh ไม่สำเร็จ | 401 และล้างคุกกี้ |
| route handler: ไม่มี `branch` | 400 |
| route handler: ไม่มีคุกกี้ `hp_at` | 401 |
| API: ไม่มี token หรือ token ไม่ถูกต้อง (`FallbackPolicy`) | 401 |
| API: `branch` ว่าง | 422 `validation_failed` |
| API: พนักงานที่มี `queue:read` แต่ไม่ได้อยู่สาขานั้น (`BranchGuard`) | 403 |
| API: ไม่ใช่พนักงานที่มี `queue:read` และไม่ใช่ลูกค้า | 403 |

route handler ส่ง 401 และ 403 จาก API ต่อไปตรง ๆ เพื่อให้เบราว์เซอร์หยุด ไม่วนต่อใหม่ไม่รู้จบกับสตรีมที่เซสชันนี้ดูไม่ได้

endpoint นี้ไม่มี `[RequireScope]` เพราะสัญญาให้ทั้งพนักงานและลูกค้าใช้ การตัดสินจึงอยู่ใน `ResolveAudience` ของ controller และด้วยเหตุนี้กติกา 2FA ของ `[RequireScope]` จึงไม่ถูกตรวจที่เส้นนี้

## ใครเห็นอะไร

| ผู้ฟัง | เงื่อนไข | ได้รับ |
|---|---|---|
| พนักงาน | มี `queue:read` และเข้าสาขานั้นได้ | ทุกเหตุการณ์ของสาขา |
| ลูกค้า | token มี `customer_id` | เฉพาะเหตุการณ์ของบัตรที่เป็นของตัวเอง |

ลูกค้าไม่ได้เหตุการณ์ของบัตรคนอื่นแม้แต่ id เพราะจำนวนเหตุการณ์ก็บอกได้ว่าคลินิกรับคนไข้เร็วแค่ไหน ฝั่ง API รองรับลูกค้าแล้ว แต่ยังไม่มีหน้าพอร์ทัลบนเว็บที่ใช้

## ข้อความจากเซิร์ฟเวอร์

```text
event: board.refresh
data: {"branch_id":"brn_main"}                          ส่งทันทีที่เชื่อมต่อ

event: ticket.updated
data: {"ticket_id":"tkt_...","branch_id":"brn_main"}

event: ticket.called
data: {"ticket_id":"tkt_...","branch_id":"brn_main"}

: ping                                                  comment ทุก 20 วินาที
```

`board.refresh` ที่ส่งตอนเปิดสตรีมทำให้หน้าเว็บที่ต่อใหม่หลังหลุดอ่านกระดานใหม่ ไม่ต้องเดาว่าระหว่างหลุดเกิดอะไรขึ้น ส่วน `: ping` เป็น comment ที่ client ไม่สนใจ แต่ทำให้ proxy ไม่ตัด socket ที่เงียบ

API ใส่ `Cache-Control: no-cache` และ `X-Accel-Buffering: no` และปิด buffer ของ response เพราะ nginx ที่ buffer ไว้จะทำให้กระดานขยับเป็นช่วง ๆ ช้าไปหลายนาที

## จุดที่ส่งเหตุการณ์

ทุกจุดส่งหลัง commit เท่านั้น การเปลี่ยนที่ rollback ไม่ส่งอะไร

| use case | endpoint | เหตุการณ์ |
|---|---|---|
| `CheckInAppointmentHandler` | `POST /appointments/{id}/check-in` | `ticket.updated` |
| `IssueWalkInTicketHandler` | `POST /staff/queue/tickets` | `ticket.updated` |
| `AssignTicketHandler` | `POST /staff/queue/{id}/assign` | `ticket.updated` |
| `CallTicketHandler` | `POST /staff/queue/{id}/call` | `ticket.called` |
| `SkipTicketHandler` | `POST /staff/queue/{id}/skip` | `board.refresh` |
| `SetTicketPriorityHandler` | `POST /staff/queue/{id}/priority` (ส่วนขยาย) | `board.refresh` |
| `LeaveQueueHandler` | `POST /staff/queue/{id}/leave` (ส่วนขยาย) | `board.refresh` |
| `OpenConsultationHandler` | `POST /staff/consultations` เมื่อเปิดเคสจากบัตรคิว | `ticket.updated` |
| `CloseConsultationHandler` | `POST /staff/consultations/{id}/close` เมื่อบัตรยังอยู่ในห้อง | `ticket.updated` |

การข้าม การเลื่อนความเร่งด่วน และการออกจากคิวส่ง `board.refresh` เพราะลำดับของบัตรใบอื่นเปลี่ยนด้วย

## ฝั่ง API

`QueueEventBroker` (`Infrastructure/Realtime/QueueEventBroker.cs`) เป็น singleton เก็บรายชื่อผู้ฟังในหน่วยความจำ

- ผู้ฟังแต่ละรายมี channel ขนาด 64 เหตุการณ์ เต็มแล้วทิ้งเหตุการณ์เก่าสุด (`DropOldest`) แทนการรอ จอที่เน็ตช้าจึงทำให้การกดเรียกคิวที่เคาน์เตอร์ช้าไม่ได้ และหน้าจอที่ตามไม่ทันขนาดนั้นควรอ่านกระดานใหม่อยู่แล้ว
- `Publish` ไม่ throw และไม่รอ
- เมื่อ client ตัดการเชื่อมต่อ ผู้ฟังถูกถอดใน `finally` ทุกกรณี ไม่มี channel ค้าง
- กรองตามสาขาที่ broker ส่วนการกรองตามลูกค้าอยู่ที่ controller

handler รู้จักแค่ interface `IQueueEventStream` (`Application/Queueing/IQueueEventStream.cs`) ถ้าวันหนึ่งต้องรัน API หลายตัว ต้องเพิ่มตัวกลางส่งข้อความหลัง interface เดิมนี้ ไม่ต้องแก้ handler

## ฝั่งหน้าเว็บ

- `features/queue/hooks/use-queue-stream.ts` เปิด `EventSource` หนึ่งตัวต่อสาขา ฟังสามเหตุการณ์ แล้วรวมเหตุการณ์ที่มาติดกันภายใน 150 มิลลิวินาทีเป็นการเรียก callback ครั้งเดียว เพราะการปิดเคสหนึ่งครั้งส่งหลายเหตุการณ์ต่อกัน
- `features/queue/components/queue-live-indicator.tsx` ส่ง `router.refresh()` เป็น callback และแสดงสถานะ "กำลังเชื่อมต่อ" "อัปเดตสด" หรือข้อความว่าหลุดและกำลังต่อใหม่ สถานะนี้สำคัญเท่ากับการอัปเดต เพราะกระดานที่หยุดอัปเดตเงียบ ๆ ดูเหมือนช่วงเช้าที่คนไข้น้อย
- สายที่หลุดกลางทาง `EventSource` ต่อใหม่เอง ถ้าการต่อถูกปฏิเสธ (401, 403, 502) เบราว์เซอร์ปิด `EventSource` ถาวร hook จึงเปิดตัวใหม่เองหลังรอ 2 วินาที เพิ่มเป็นเท่าตัวจนถึง 1 นาที จอเรียกคิวในห้องรอ (`/queue/display`) ใช้ hook เดียวกัน ดู [คิว](features/queue.md#จอเรียกคิวในห้องรอ)
- เมื่อ access token หมดอายุแล้วสตรีมหลุด การต่อใหม่ผ่าน middleware ซึ่ง refresh token ให้ก่อน

## ข้อจำกัด

- broker อยู่ใน process เดียว รัน API สองตัวแล้วผู้ฟังของตัวหนึ่งจะไม่เห็นเหตุการณ์ของอีกตัว
- token ถูกตรวจตอนเปิดสตรีมครั้งเดียว สตรีมที่เปิดอยู่จะไม่ถูกตัดเมื่อ access token หมดอายุหรือเซสชันถูกเพิกถอน จนกว่าการเชื่อมต่อจะหลุดเอง
- ถ้า API ตอบ 401 หรือ 403 hook ลองต่อใหม่ไปเรื่อย ๆ ทุก 1 นาที ป้ายบนหน้าจอขึ้นว่ากำลังต่อใหม่ ไม่ได้แยกว่าเซสชันหมดหรือไม่มีสิทธิ์ เพราะ `EventSource` ไม่บอก status code
- heartbeat กับเหตุการณ์เขียนลง response จากสอง task โดยไม่มีตัวล็อก ถ้าเกิดตรงกันพอดีมีโอกาสที่บรรทัดจะปนกัน ยังไม่มีเทสต์ครอบกรณีนี้
- ไม่มี id ของเหตุการณ์ (`id:`) จึงใช้ `Last-Event-ID` ขอเหตุการณ์ที่พลาดไม่ได้ ระบบพึ่ง `board.refresh` ตอนต่อใหม่แทน

## เทสต์

`backend/tests/HappyPaws.IntegrationTests/Api/Queueing/QueueStreamTests.cs` มี 9 ข้อ ครอบ: เปิดสตรีมแล้วได้ `board.refresh` ก่อน, เรียกคิวแล้วพนักงานได้ `ticket.called`, ข้ามคิวได้ `board.refresh`, ผู้ฟังต่างสาขาไม่เห็นกัน, ลูกค้าเห็นเฉพาะบัตรตัวเอง, เจ้าของเห็นบัตรตัวเองถูกเรียก, ต้องเข้าสู่ระบบ, พนักงานดูสาขาที่ไม่ได้อยู่ไม่ได้ และ client ที่ตัดการเชื่อมต่อไม่ทิ้งผู้ฟังค้าง ฝั่งหน้าเว็บยังไม่มีเทสต์

## โค้ดที่เกี่ยวข้อง

| ไฟล์ | หน้าที่ |
|---|---|
| `backend/src/HappyPaws.Domain/Queueing/QueueEventTypes.cs` | ชื่อเหตุการณ์ทั้งสามตามสัญญา |
| `backend/src/HappyPaws.Application/Queueing/IQueueEventStream.cs` | interface และ record `QueueEvent` |
| `backend/src/HappyPaws.Infrastructure/Realtime/QueueEventBroker.cs` | กระจายเหตุการณ์ใน process |
| `backend/src/HappyPaws.Api/Controllers/Realtime/QueueStreamController.cs` | ตรวจผู้เรียก เขียน event และ heartbeat |
| `frontend/src/app/api/queue/stream/route.ts` | ส่งต่อสตรีมพร้อมแนบ token |
| `frontend/src/lib/api/event-stream.ts` | เปิดสตรีมไปที่ API (ที่เดียวที่เรียก `fetch` แบบสตรีม) |
| `frontend/src/features/queue/hooks/use-queue-stream.ts` | `EventSource` และการรวมเหตุการณ์ |
| `frontend/src/features/queue/components/queue-live-indicator.tsx` | สั่งอ่านใหม่และแสดงสถานะการเชื่อมต่อ |
