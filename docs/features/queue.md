# คิว

บัตรคิว (`queue_tickets`) คือที่ของสัตว์เลี้ยงหนึ่งตัวในคิววันนี้ของสาขาหนึ่ง บัตรเกิดได้สองทาง คือเช็กอินนัด ([การจองและนัดหมาย](booking-and-appointments.md)) หรือเคาน์เตอร์ออกบัตรให้ walk-in ที่ไม่มีนัด จากนั้นเคาน์เตอร์เรียก ข้าม หรือเอาออกจากคิว ส่วนการเข้าห้องตรวจและตรวจเสร็จเกิดจากการเปิดและปิดเคส ดู [การตรวจและเวชระเบียน](consultations-and-records.md)

คิวไม่แบ่งหน้า ต่างจากรายการอื่นทุกตัวในระบบ เพราะวันหนึ่งของสาขาหนึ่งมีไม่กี่สิบใบ และกระดานต้องอ่านทั้งหมดเพื่อตอบว่า "เหลืออีกกี่คิว"

## Endpoint

| Endpoint | สิทธิ์ | ทำอะไร |
|---|---|---|
| `GET /staff/queue?branch=&room=&date=&include_finished=` | `queue:read` | กระดานของวัน เรียงตามลำดับที่จะถูกเรียก |
| `POST /staff/queue/tickets` | `queue:write` | ออกบัตรให้ walk-in ต้องมี `Idempotency-Key` |
| `POST /staff/queue/{id}/assign` | `queue:write` | จ่ายหมอ ห้องตรวจ หรือทั้งสองอย่าง |
| `POST /staff/queue/{id}/call` | `queue:call` | เรียกคิว แล้วแจ้งเจ้าของ |
| `POST /staff/queue/{id}/skip` | `queue:call` | ข้ามคิวที่ยังไม่มา เลื่อนไปท้ายแถว |
| `POST /staff/queue/{id}/priority` | `queue:write` | เปลี่ยนความเร่งด่วน **ส่วนขยาย** |
| `POST /staff/queue/{id}/leave` | `queue:write` | เจ้าของกลับบ้านแล้ว **ส่วนขยาย** |
| `GET /portal/queue/{id}` | `portal` | บัตรของลูกค้าเอง และเหลืออีกกี่คิว |
| `GET /queue/stream?branch=` (SSE) | ทั้งสองฝั่ง | อัปเดตสด ดู [realtime](../realtime.md) |

สองเส้นที่ติด **ส่วนขยาย** ไม่อยู่ใน contract เหตุผลอยู่ใน [endpoint ส่วนขยาย](../contract-extensions.md) ทุก path อยู่ใต้ `/v1`

## สถานะ 6 แบบ

| ค่าที่เก็บ | กระดานบนเว็บเขียนว่า | ยังอยู่บนกระดาน | นับเป็นรอเรียก |
|---|---|---|---|
| `waiting` | รอเรียก | ใช่ | ใช่ |
| `called` | กำลังเรียก | ใช่ | ไม่ |
| `in_room` | อยู่ในห้องตรวจ | ใช่ | ไม่ |
| `skipped` | ข้ามแล้ว รอเรียกซ้ำ | ใช่ | ใช่ |
| `served` | ตรวจเสร็จ | ไม่ | ไม่ |
| `left` | ออกจากคิว | ไม่ | ไม่ |

- "ยังอยู่บนกระดาน" คือชุด `QueueTicketTransitions.LiveStatuses` กระดานปกติแสดงเฉพาะชุดนี้ ส่ง `include_finished=true` ถ้าต้องการรวม `served` กับ `left`
- "นับเป็นรอเรียก" คือ `AwaitingCallStatuses` ใช้ทั้ง `waiting_count` ของกระดานและ `ahead` ของลูกค้า คนที่อยู่ในห้องแล้วไม่นับว่าอยู่ข้างหน้า ไม่อย่างนั้นตัวเลขบนมือถือลูกค้าจะค้างอยู่ที่ 1 ตลอดการตรวจ

## ลำดับที่บัตรเดิน

```text
 ออกบัตร ──► waiting ──── เรียก ────► called ──── เปิดเคส ────► in_room ──── ปิดเคส ────► served
               │                      │   ▲
               │ ข้าม            ข้าม │   │ เรียกซ้ำ
               │                      ▼   │
               └────────────────► skipped ┘

 waiting, called, skipped ──── ออกจากคิว ────► left
```

ระบบบังคับลำดับนี้ ทางทั้งหมดอยู่ใน `QueueTicketTransitions` การเปลี่ยนที่ไม่มีในแผนภาพได้ `409 state_conflict`

- **`skipped` เป็นสถานะรอ ไม่ใช่ทางออก** แผนภาพใน contract วาด `skipped` เป็นทางออก แต่คำอธิบายของ endpoint บอกว่า "เลื่อนไปท้ายแถว" เราถือตามคำอธิบาย บัตรที่ถูกข้ามยังอยู่และเรียกซ้ำได้ ดู [จุดที่ contract กำกวม](../assumptions.md)
- **ข้ามซ้ำไม่ได้** `skipped` ไป `skipped` ได้ `409` บัตรที่อยู่ท้ายแถวแล้วต้องถูกเรียกก่อนจึงข้ามได้อีก การกดสองครั้งจึงไม่ทำให้ `skip_count` เพิ่มเกินจริง
- **เปิดเคสคือการเรียก** ถ้าหมอเดินไปรับสัตว์เองโดยไม่มีใครกดเรียก การเปิดเคสจะเรียกให้ก่อน (บันทึกว่าหมอคนนั้นเรียก) แล้วจึงพาเข้าห้อง `called_at` จึงไม่ว่างและสถิติเวลารอยังใช้ได้ บัตรที่อยู่ในห้องอยู่แล้วไม่ถูกแตะ
- `in_room` ไปได้ทางเดียวคือ `served` ส่วน `served` กับ `left` จบแล้ว

## ใครเปลี่ยนสถานะได้

| การเปลี่ยน | ทางเข้า | สิทธิ์ |
|---|---|---|
| เกิดเป็น `waiting` จากนัด | `POST /appointments/{id}/check-in` | `appointment:checkin` หรือเจ้าของสัตว์ |
| เกิดเป็น `waiting` แบบ walk-in | `POST /staff/queue/tickets` | `queue:write` |
| เรียก | `POST /staff/queue/{id}/call` | `queue:call` |
| ข้าม | `POST /staff/queue/{id}/skip` | `queue:call` |
| ออกจากคิว | `POST /staff/queue/{id}/leave` | `queue:write` |
| เข้าห้อง | เปิดเคส `POST /staff/consultations` | `consult:write` |
| ตรวจเสร็จ | ปิดเคส `POST /staff/consultations/{id}/close` | `record:sign` |

- contract แยกสิทธิ์ไว้ชัด จัดกระดาน (จ่ายห้อง ความเร่งด่วน ออกจากคิว) เป็น `queue:write` ของเคาน์เตอร์ ส่วนเรียกและข้ามเป็น `queue:call`
- พนักงานทำได้เฉพาะบัตรของสาขาที่ตัวเองเข้าถึงได้ บัตรที่ไม่มีได้ `404`
- role ตั้งต้น `receptionist` ได้ `queue:*` ส่วน `veterinarian` ได้แค่ `queue:call` ตามตารางใน contract จึงเรียกคิวได้ แต่เปิดกระดาน (`queue:read`) ไม่ได้จนกว่าเจ้าของคลินิกจะเพิ่มให้ `vet_assistant` ได้ `queue:read` ดู [พนักงานและสิทธิ์](staff-and-roles.md)

## ลำดับการเรียก

กระดานเรียงตามนี้ และหน้าเว็บแสดงตามลำดับที่ API ส่งมาโดยไม่เรียงใหม่

1. ความเร่งด่วน มากไปน้อย (`emergency`, `urgent`, `normal`)
2. บัตรที่ไม่เคยถูกข้าม มาก่อนบัตรที่ถูกข้าม
3. บัตรที่ถูกข้าม เรียงตามเวลาที่ถูกข้ามครั้งล่าสุด ข้ามสองครั้งจึงไปอยู่หลังคนที่ถูกข้ามครั้งเดียว
4. เลขคิว

ลำดับนี้เขียนไว้ครั้งเดียวใน `QueueTicketRepository.InCallOrder` และการนับ `ahead` ใช้การเปรียบเทียบเดียวกัน กระดานกับตัวเลขบนมือถือลูกค้าจึงไม่ขัดกัน ความเร่งด่วนเก็บเป็นตัวเลข เพราะถ้าเก็บเป็นข้อความ PostgreSQL จะเรียงตามตัวอักษรและได้ลำดับผิด

## ความเร่งด่วน (ส่วนขยาย)

contract มีแค่ "มาก่อนได้ก่อน" กับการข้าม สัตว์ที่ถูกรถชนจึงแซงคิวตัดเล็บไม่ได้ เราเพิ่มความเร่งด่วน 3 ระดับ คือ `normal`, `urgent`, `emergency`

- ตั้งได้ตอนออกบัตร walk-in (`priority`, `priority_reason`) หรือเปลี่ยนภายหลังด้วย `POST /staff/queue/{id}/priority` (`priority`, `reason`)
- สูงกว่า `normal` ต้องมีเหตุผล ตอนออกบัตรไม่มีเหตุผลได้ `422` ตอนเปลี่ยนภายหลังได้ `409 invalid_priority` ค่าที่ไม่รู้จักได้ `422` ไม่ถูกตีเป็น `normal` เงียบ ๆ
- ไม่ใช่การเปลี่ยนสถานะ บัตรคงเลขและสถานะเดิม ทำได้เฉพาะบัตรที่ยังอยู่บนกระดาน
- บัตรเก็บ `priority_reason`, ผู้ตั้ง และเวลาที่ตั้ง การเปลี่ยนผ่าน endpoint `priority` เขียน audit log พร้อมค่าก่อน ค่าหลัง และเหตุผล
- เช็กอินไม่เดาความเร่งด่วนจากประเภทนัด เพราะประเภทนัดยังไม่มี schema ใน contract

## เลขคิว

- รูปแบบมาจากการตั้งค่าคิว ค่าเริ่มต้นคือ `A` ตามด้วยเลข 3 หลัก (`A001`) และเริ่มใหม่ทุกวันต่อสาขา ปิดการเริ่มใหม่รายวันได้ ดู [ตั้งค่าคลินิก](clinic-setup.md)
- ออกเลขภายใต้ `pg_advisory_xact_lock` ที่ผูกกับสาขาและวัน (หรือสาขาอย่างเดียวถ้าไม่เริ่มใหม่รายวัน) ใน transaction เดียวกับการเช็กอินหรือออกบัตร ใช้ค่ามากสุดบวกหนึ่ง การเช็กอินพร้อมกันสองคนจึงไม่ได้เลขซ้ำ และ transaction ที่ rollback ไม่ทำให้เลขหาย
- unique index บน `(branch_id, service_date, number)` เป็นตัวกันชั้นสุดท้าย และนัดหนึ่งมีบัตรได้ใบเดียว (partial unique index บน `appointment_id`)
- `service_date` เก็บเป็นวันตามเวลาคลินิก กระดานที่ไม่ส่ง `date` ใช้วันนี้ตามเวลาคลินิก ไม่ใช่ตาม UTC

## การกระทำบนบัตร

- **ออกบัตร walk-in** ไม่สร้างนัด ถ้าต้องการให้มีในตารางนัดด้วย ให้จองผ่าน `POST /staff/appointments` พร้อม `walk_in` แล้วเช็กอิน สัตว์ที่เสียชีวิตแล้วออกบัตรไม่ได้ ลูกค้าบนบัตรคือเจ้าของหลักของสัตว์
- **เรียก** บันทึกบัตรก่อน แล้วจึงส่ง `ticket.called` และส่งแจ้งเตือน `queue.called` (เลขคิวและห้อง) ให้เจ้าของ นอก transaction ช่องทางแจ้งเตือนที่ค้างหรือล้มจึงไม่ทำให้การเรียกล้ม ถ้า process ตายระหว่างนั้น เจ้าของไม่ได้ข้อความแต่กระดานยังถูกต้อง
- **ข้าม** เขียนทับ `skipped_at` และเพิ่ม `skip_count`
- **ออกจากคิว** มี `reason` ได้ แยกจาก `served` เพื่อไม่ให้การมาที่ไม่ได้ตรวจเข้าไปอยู่ในตัวเลขของวัน
- **จ่ายหมอและห้อง** ต้องส่งอย่างน้อยหนึ่งอย่าง ห้องต้องอยู่ในทะเบียนห้องตรวจของสาขา (ไม่อย่างนั้น `422`) ไม่ใช่การเปลี่ยนสถานะ ทำได้แม้อยู่ในห้องแล้ว จึงย้ายห้องกลางการตรวจได้
- **`If-Match`** ทุกการกระทำบนบัตรรับ `If-Match` ถ้าส่งมา version ไม่ตรงได้ `409 version_conflict` หน้าเว็บส่งทุกครั้ง

## หน้าของลูกค้า

`GET /portal/queue/{id}` ตอบเลขคิว สถานะ ความเร่งด่วน ห้อง `ahead` และ `called_at` ไม่มีข้อมูลของบัตรอื่น ลูกค้าเห็นเฉพาะบัตรที่ `customer_id` ตรงกับตัวเอง (คือเจ้าของหลักที่อยู่บนบัตร) ที่เหลือได้ `404` ยังไม่มีหน้าเว็บของลูกค้า

## เหตุการณ์ realtime

| การกระทำ | event |
|---|---|
| เช็กอิน, ออกบัตร walk-in, จ่ายหมอหรือห้อง, เปิดเคส, ปิดเคส | `ticket.updated` |
| เรียก | `ticket.called` |
| ข้าม, ออกจากคิว, เปลี่ยนความเร่งด่วน | `board.refresh` เพราะลำดับของทั้งกระดานเปลี่ยน |
| เปิดการเชื่อมต่อ | `board.refresh` ทันที ให้ client อ่านกระดานใหม่ |

- ส่งหลัง commit เสมอ กระดานจึงไม่ประกาศสิ่งที่ยังไม่เกิด
- ข้อมูลใน event มีแค่ `ticket_id` และ `branch_id` เป็นสัญญาณว่า "มีอะไรเปลี่ยน" ไม่ใช่ตัวข้อมูล
- พนักงานต้องมี `queue:read` และอยู่ในสาขานั้น ลูกค้าได้เฉพาะ event ของบัตรตัวเอง
- ตัวกระจาย event อยู่ใน process เดียว (`QueueEventBroker`) ใช้ได้กับ API instance เดียว รายละเอียดอยู่ใน [realtime](../realtime.md)

## กระดานคิวบนเว็บ

หน้า `/queue` สำหรับพนักงาน (`frontend/src/app/(staff)/queue/page.tsx`)

- เป็น server component อ่านกระดานจาก backend ทุกครั้งที่ render ไม่เก็บสำเนาไว้ในเบราว์เซอร์
- ใช้สาขาจากบัญชีที่เข้าสู่ระบบ ไม่มีสาขาแสดงหน้าว่าง ไม่มี `queue:read` แสดงข้อความบอกสิทธิ์ที่ขาด
- ชื่อสัตว์ บริการ และหมอ ดึงเพิ่มฝั่ง server พร้อมกัน ทุกตัวเป็นแบบไม่บังคับ ผู้ช่วยสัตวแพทย์ที่ไม่มี `staff:read` จะเห็นหมอเป็นรหัสแทนชื่อ

### หน้าตา

- ส่วนหัวของหน้าแสดงสาขา วันที่ และจำนวนที่รอเรียก
- สามคอลัมน์ตามทางที่สัตว์เดินผ่านคลินิก คือ รอเรียก (`waiting` และ `skipped`) กำลังเรียก (`called`) และในห้องตรวจ (`in_room`) บัตรที่ตรวจเสร็จหรือออกจากคิวไม่แสดง
- การ์ดแต่ละใบมีเลขคิวตัวใหญ่ ชื่อและข้อมูลสัตว์ สถานะ ความเร่งด่วน ป้ายแดง "มีประวัติแพ้ยา" เมื่อสัตว์มีบันทึกแพ้ยา บริการ เวลาออกบัตรและนาทีที่รอ หมอ ห้อง และจำนวนครั้งที่ถูกข้าม บัตรฉุกเฉินมีขอบแดง

### ปุ่ม

| ปุ่ม | แสดงเมื่อ |
|---|---|
| เรียกคิว | มี `queue:call` และบัตรเป็น `waiting` หรือ `skipped` |
| ข้าม | มี `queue:call` และบัตรเป็น `waiting` หรือ `called` |
| เลือกห้อง | มี `queue:write` สาขามีห้องตรวจ และบัตรยังไม่อยู่ในห้อง |
| ออกจากคิว | มี `queue:write` และบัตรยังไม่อยู่ในห้อง |
| ออกบัตรคิว walk-in | มี `queue:write` |

- การซ่อนปุ่มเป็นแค่ UX ตัวที่ปฏิเสธจริงคือ backend
- ทุกปุ่มส่ง `version` ของบัตรไปด้วย ถ้ามีคนอื่นเปลี่ยนบัตรไปก่อนจะได้ข้อความปฏิเสธ และกระดานโหลดใหม่ แม้คำขอถูกปฏิเสธก็ `revalidatePath('/queue')`
- ฟอร์ม walk-in ค้นสัตว์จากชื่อหรือเลข HN (อย่างน้อย 2 ตัวอักษร รอหยุดพิมพ์ 250 ms) เลือกบริการ ความเร่งด่วน และเหตุผลเมื่อไม่ใช่ `normal` สร้าง `Idempotency-Key` ใหม่ทุกครั้งที่เปิดฟอร์ม กดซ้ำหรือส่งใหม่หลังเน็ตหลุดจึงได้บัตรใบเดิม
- การกระทำทั้งหมดผ่าน server action ใน `queue-actions.ts` ซึ่งตรวจ input ด้วย zod แล้วเรียก endpoint เดียว ไม่มีกฎของคิวอยู่ฝั่ง frontend

### อัปเดตสด

```text
 เบราว์เซอร์ EventSource
      │  GET /api/queue/stream?branch=   (origin เดียวกัน คุกกี้ hp_at ติดไปเอง)
      ▼
 Next.js route handler  app/api/queue/stream/route.ts
      │  อ่าน access token จากคุกกี้ แล้วต่อ backend พร้อม Authorization: Bearer
      ▼
 ASP.NET  GET /v1/queue/stream?branch=   ส่ง body กลับผ่าน Next.js ตรง ๆ
```

- `EventSource` ส่ง header ไม่ได้ และ token อยู่ในคุกกี้ httpOnly ที่ script อ่านไม่ได้ เบราว์เซอร์จึงต่อกับ Next.js แทน token ไม่เคยอยู่ใน URL
- middleware ครอบ `/api/queue/:path*` จึงต่ออายุ token ก่อน route handler ทำงาน การต่อใหม่หลัง token หมดอายุจึงได้ token ใหม่
- backend ตอบ `401` หรือ `403` route handler ส่งต่อตามนั้น
- ทุก event ที่ได้ ทำให้ `router.refresh()` อ่านกระดานใหม่ทั้งหน้า ไม่แก้ข้อมูลบนจอจาก event เอง event หลายตัวในช่วง 150 ms (เช่นตอนปิดเคส) รวมเป็นการอ่านครั้งเดียว
- ตัวบอกสถานะการเชื่อมต่อที่ส่วนหัวของหน้ามีสามแบบ กำลังเชื่อมต่อ อัปเดตสด และหลุดการเชื่อมต่อกำลังต่อใหม่ เพราะกระดานที่หยุดอัปเดตเงียบ ๆ ดูเหมือนเช้าที่ไม่มีคนไข้
- `EventSource` ต่อใหม่เอง และ backend ส่ง `board.refresh` ทุกครั้งที่ต่อ สิ่งที่พลาดไประหว่างหลุดจึงถูกเก็บทันที

### ยังไม่มีบนเว็บ

เปลี่ยนความเร่งด่วนหลังออกบัตร จ่ายหมอ ใส่เหตุผลตอนออกจากคิว ดูบัตรที่จบแล้วหรือวันอื่น จอเรียกคิวสำหรับห้องรอ และหน้าคิวของลูกค้า

## ข้อจำกัด

- **แจ้งเตือน LINE ยังเป็น stub** การเรียกคิวผ่านประตูความยินยอมแล้วไปจบที่ `LoggingNotificationChannel` ซึ่งเขียน log ระดับ warning ว่าไม่ได้ส่ง ไม่มีข้อความถึงเจ้าของจริง ดู [การแจ้งเตือน](notifications.md)
- **ความเร่งด่วนตอนออกบัตร walk-in ไม่เขียน audit log** เก็บเหตุผลและผู้ตั้งไว้บนบัตรเท่านั้น แต่ข้อความใต้ช่องเหตุผลในฟอร์มบอกว่าบันทึกใน audit log การเปลี่ยนผ่าน endpoint `priority` เท่านั้นที่เขียน audit log
- **ยกเลิกนัดที่เช็กอินแล้ว บัตรยังอยู่บนกระดาน** ต้องกดออกจากคิวเอง
- **ออกบัตร walk-in ไม่ตรวจว่าบริการเปิดอยู่หรือเปิดที่สาขานั้น** ตรวจแค่ว่ามีบริการนี้
- **SSE ใช้ได้กับ instance เดียว** ขยายเป็นหลาย instance ต้องเพิ่ม backplane หลัง `IQueueEventStream`

## โค้ด

| ส่วน | ไฟล์ |
|---|---|
| สถานะและทางเปลี่ยน | `backend/src/HappyPaws.Domain/Queueing/QueueTicketStatus.cs`, `QueueTicketTransitions.cs`, `QueueTicket.cs` |
| ความเร่งด่วน | `backend/src/HappyPaws.Domain/Queueing/QueuePriority.cs`, `backend/src/HappyPaws.Application/Queueing/QueuePriorityParser.cs`, `SetTicketPriority/` |
| use case | `backend/src/HappyPaws.Application/Queueing/` |
| สิทธิ์ | `backend/src/HappyPaws.Application/Queueing/QueueAccess.cs` |
| ลำดับการเรียกและเลขคิว | `backend/src/HappyPaws.Infrastructure/Persistence/Repositories/Queueing/QueueRepositories.cs` |
| index | `backend/src/HappyPaws.Infrastructure/Persistence/Configurations/Queueing/QueueTicketConfiguration.cs` |
| ตั้งค่าเลขคิว | `backend/src/HappyPaws.Domain/Clinics/QueueSettings.cs` |
| controller | `backend/src/HappyPaws.Api/Controllers/Staff/QueueController.cs`, `Staff/QueueTicketActionsController.cs`, `Portal/PortalQueueController.cs`, `Realtime/QueueStreamController.cs` |
| ตัวกระจาย event | `backend/src/HappyPaws.Infrastructure/Realtime/QueueEventBroker.cs`, `backend/src/HappyPaws.Domain/Queueing/QueueEventTypes.cs` |
| หน้ากระดาน | `frontend/src/app/(staff)/queue/page.tsx`, `frontend/src/features/queue/` |
| ส่งต่อ SSE | `frontend/src/app/api/queue/stream/route.ts`, `frontend/src/lib/api/event-stream.ts`, `frontend/src/features/queue/hooks/use-queue-stream.ts` |
| คำภาษาไทยของสถานะ | `frontend/src/features/queue/lib/ticket-labels.ts` |
| เทสต์ | `backend/tests/HappyPaws.UnitTests/Domain/Queueing/QueueTicketStateMachineTests.cs`, `backend/tests/HappyPaws.IntegrationTests/Api/Queueing/` |
