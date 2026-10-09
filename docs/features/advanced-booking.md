# นัดขั้นสูง

สิ่งที่คลินิกต้องใช้นอกจากการจองทีละนัด คือบันทึกคนที่ไม่มาตามนัด นโยบายยกเลิกและค่าปรับ ประเภทนัดที่มีกติกาต่างกัน การตรวจนัดชนก่อนยืนยัน นัดซ้ำเป็นคอร์ส คิวรอคิวว่าง และแผนการรักษาหลายครั้ง

ส่วนที่ contract ไม่ได้เขียนไว้และเราตีความเอง อยู่ใน [ข้อสมมติ § 37 และ § 38](../assumptions.md) การจองทีละนัดอยู่ใน [การจองและนัดหมาย](booking-and-appointments.md)

## Endpoint

| Method | Endpoint | สิทธิ์ | ทำอะไร |
|---|---|---|---|
| POST | `/staff/appointments/{id}/no-show` | `appointment:write` | บันทึกว่าไม่มาตามนัด และคืนสถิติของลูกค้า |
| GET | `/staff/policies/cancellation` | `settings:read` | อ่านนโยบายยกเลิกตามที่เก็บ (ส่วนขยาย § 23) |
| PUT | `/staff/policies/cancellation` | `settings:write` | ตั้งนโยบายยกเลิกและค่าปรับ |
| GET | `/staff/appointment-types` | `appointment:read` | อ่านประเภทนัดทั้งชุดพร้อมกติกา (ส่วนขยาย § 23) |
| PUT | `/staff/appointment-types` | `settings:write` | ตั้งประเภทนัดและกติกาของแต่ละประเภท |
| GET | `/portal/appointment-types` | `portal` | ประเภทที่ลูกค้าจองออนไลน์ได้ (ส่วนขยาย § 23) |
| GET | `/staff/appointments/conflicts` | `appointment:read` | ตรวจว่าเวลาหรือวันนั้นมีปัญหาอะไร |
| POST | `/staff/appointments/recurring` | `appointment:write` | จองคอร์สหลายครั้งด้วย RRULE |
| POST | `/staff/waiting-list` | `appointment:write` | ใส่สัตว์ในคิวรอคิวว่าง |
| GET | `/staff/waiting-list` | `appointment:write` | ดูรายการคิวรอ (ส่วนขยาย § 23) |
| DELETE | `/staff/waiting-list/{id}` | `appointment:write` | ถอนออกจากคิวรอ (ส่วนขยาย § 23) |
| POST | `/staff/pets/{id}/treatment-plans` | `consult:write` | วางแผนการรักษาหลายครั้งและจองนัด |

## หน้าจอ

| เรื่อง | อยู่ที่ไหน | สิทธิ์ |
|---|---|---|
| ไม่มาตามนัด | ปฏิทินนัด เปิดนัดแล้วกด "ไม่มาตามนัด" | `appointment:write` |
| นโยบายยกเลิกและประเภทนัด | ตั้งค่า แท็บ "นโยบายยกเลิกและประเภทนัด" (`/settings/appointments`) | `settings:write` |
| ประเภทนัดตอนจอง | หน้าจองนัด (`/appointments/new`) ช่อง "ประเภทนัด" และหน้าจองของลูกค้า | `appointment:read` และ `portal` |
| ตรวจนัดชน | หน้าจองนัด (`/appointments/new`) ปุ่ม "ตรวจนัดชน" | `appointment:read` |
| ตรวจนัดชนทั้งวัน | ปฏิทินนัด ปุ่ม "ตรวจนัดชนทั้งวัน" | `appointment:read` |
| นัดซ้ำเป็นคอร์ส | หน้าจองนัด ติ๊ก "จองเป็นคอร์ส" | `appointment:write` |
| คิวรอ | ปฏิทินนัด ปุ่ม "คิวรอคิวว่าง" (`/appointments/waiting-list`) | `appointment:write` |
| แผนการรักษา | หน้าสัตว์เลี้ยง | `consult:write` |

เมนูหรือปุ่มที่บัญชีไม่มีสิทธิ์จะไม่แสดง ระบบหลังบ้านตรวจสิทธิ์ซ้ำทุกครั้ง

- **ไม่มาตามนัด** ปุ่มขึ้นเฉพาะนัดที่ยัง `booked` หรือ `confirmed` และเลยเวลาเริ่มแล้ว ก่อนถึงเวลามีข้อความบอกว่าบันทึกได้เมื่อไร กดแล้วต้องยืนยันอีกครั้งเพราะย้อนกลับไม่ได้ บันทึกแล้วหน้าต่างแสดงสถิติจาก `attendance` และค่าปรับจาก `fee_due_satang` ถ้ามี พร้อมบอกว่าระบบไม่ได้ออกบิลให้
- **นโยบายยกเลิกและประเภทนัด** ฟอร์มเริ่มจากค่าที่เก็บไว้ (`GET` ทั้งสองเส้น ส่วนขยาย § 23) และส่ง `version` ที่อ่านมาเป็น `If-Match` ถ้ามีคนบันทึกก่อน ระบบตอบ `409` และหน้าจอบอกให้โหลดหน้าใหม่เพื่อดูค่าล่าสุด หลังบันทึก ฟอร์มแสดงค่าที่ระบบตอบกลับและใช้ `version` ใหม่ในครั้งถัดไป การอ่านใช้ scope ของตัวเอง (`settings:read` สำหรับนโยบาย `appointment:read` สำหรับประเภทนัด) บัญชีที่อ่านไม่ได้เห็นกล่องสีเหลืองแทนฟอร์ม เพราะการบันทึกแทนที่ทั้งชุด จึงไม่ควรบันทึกโดยไม่เห็นค่าเดิม ฟอร์มตรวจเองแค่ว่าเป็นตัวเลขและรหัสประเภทถูกรูปแบบ ช่วงของค่าให้ระบบหลังบ้านตัดสิน แล้วแสดงข้อความของระบบตามที่ตอบมา แถว `regular` ลบไม่ได้และแก้รหัสไม่ได้
- **ประเภทนัดตอนจอง** หน้าจองของเคาน์เตอร์มีช่องเลือกประเภทจาก `GET /staff/appointment-types` ทุกประเภท รวมประเภทที่ลูกค้าจองเองไม่ได้ ไม่เลือกคือ `regular` ประเภทที่มีระยะเวลาของตัวเองบอกใต้ช่องว่าเวลาว่างที่แสดงคิดจากระยะเวลาของบริการ และระบบตรวจช่วงเวลาจริงอีกครั้งตอนจอง บัญชีที่อ่านประเภทไม่ได้ไม่เห็นช่องนี้ และจองเป็น `regular` หน้าจองของลูกค้าแสดงเฉพาะประเภทจาก `GET /portal/appointment-types` ใช้ชื่อที่คลินิกตั้ง ถ้าคลินิกไม่เปิดประเภทไหนให้จองออนไลน์เลย หน้าบอกให้โทรจอง
- **ตรวจนัดชน** ถามทีละเวลาด้วย `starts_at` พร้อมบริการ หมอ สัตว์ และสาขา ถ้ายังไม่เลือกหมอ endpoint ตรวจได้แค่วันหยุดและนัดซ้อนของสัตว์ หน้าจอบอกเรื่องนี้ไว้ ผลตรวจหายไปเมื่อแก้ฟอร์ม เพื่อไม่ให้เห็นผลของเวลาเก่า
- **นัดซ้ำเป็นคอร์ส** ตั้ง "ซ้ำทุก N สัปดาห์หรือวัน" และจบเมื่อครบจำนวนครั้งหรือถึงวันที่ ฟอร์มสร้าง RRULE (`FREQ`, `INTERVAL`, `COUNT` หรือ `UNTIL`) และแสดงวันที่ทุกครั้งก่อนจอง โดยคิดแบบเดียวกับระบบหลังบ้าน ฟอร์มเตือนเมื่อน้อยกว่า 2 ครั้ง เกิน 52 ครั้ง หรือไกลเกิน 366 วันจากครั้งแรก กด "ตรวจนัดชนทุกครั้ง" ได้ก่อนจอง ถ้าระบบปฏิเสธ (`422 slot_unavailable`) เหตุผลและเวลาว่างจาก `details.conflicts` แสดงที่แถวของวันนั้น
- **ตรวจนัดชนทั้งวัน** ปุ่มบนปฏิทินนัดถาม `GET /staff/appointments/conflicts?date=` ของวันและสาขาที่เปิดอยู่ แต่ละเรื่องแสดงเวลา ชื่อสัตว์ และหมอของนัดนั้น พร้อมข้อความของระบบ ผลเป็นของตอนที่กด และหายไปเมื่อเปลี่ยนวัน ปุ่มขึ้นเฉพาะวันที่มีนัด
- **คิวรอ** เลือกสัตว์ บริการ ช่วงวัน (ตั้งต้นวันนี้ถึงอีก 14 วัน) หมอ สาขา และหมายเหตุ ด้านล่างฟอร์มคือรายการคิวรอ (`GET /staff/waiting-list`) ของสาขาที่เปิดอยู่รวมรายการที่รอทุกสาขา เรียงตามลำดับที่เข้าคิว แยกแท็บ "กำลังรอ" กับ "หมดอายุแล้ว" ทีละ 50 รายการ แต่ละแถวบอกชื่อสัตว์ บริการ ช่วงวัน สาขา หมอ และแจ้งไปแล้วกี่ครั้ง ปุ่ม "ถอนออก" ถามยืนยันก่อนลบ ถ้ามีคนถอนไปก่อน หน้าจอบอกว่าถูกถอนไปแล้ว

## ไม่มาตามนัด

`POST /staff/appointments/{id}/no-show` ไม่มี body

- บันทึกได้หลังถึงเวลานัดแล้วเท่านั้น ก่อนหน้านั้นตอบ `409 no_show_too_early` เพราะเจ้าของยังมาทันได้
- ได้เฉพาะนัดที่ยัง `booked` หรือ `confirmed` สัตว์ที่เช็กอินแล้วคือมาแล้ว (`409 state_conflict`)
- คำตอบมีนัดที่เปลี่ยนแล้ว และ `attendance` ของลูกค้า

```json
"attendance": {
  "customer_id": "cus_...", "no_shows": 2, "late_cancellations": 1,
  "strikes_in_window": 3, "window_days": 365, "free_allowance": 1, "fee_due_satang": 20000
}
```

ตัวเลขนับจากตารางนัดทุกครั้ง ไม่ได้เก็บเป็นตัวนับแยก จึงตรงกับปฏิทินเสมอ บันทึก audit `appointment.no_show`

## นโยบายยกเลิก

```http
PUT /v1/staff/policies/cancellation
{ "cutoff_hours": 24, "fee_satang": 20000, "free_allowance": 1, "window_days": 365 }
```

| ช่อง | ความหมาย | ค่าตั้งต้น |
|---|---|---|
| `cutoff_hours` | ลูกค้ายกเลิกเองกระชั้นกว่านี้ นับเป็นการยกเลิกช่วงกระชั้น | 24 |
| `fee_satang` | ค่าปรับต่อครั้งที่เกินจำนวนที่ยอมให้ | 0 |
| `free_allowance` | จำนวนครั้ง (ยกเลิกกระชั้นรวมไม่มาตามนัด) ที่ยอมให้ | 1 |
| `window_days` | นับย้อนหลังกี่วัน | 365 |

- เป็น `PUT` ต้องส่งครบทุกช่อง ใช้ `If-Match` ได้ ทุกการเปลี่ยนบันทึก audit พร้อมค่าก่อนและหลัง
- **การยกเลิกไม่เคยถูกห้าม** แค่ถูกนับ ลูกค้าที่ยกเลิกเองกระชั้นกว่า cutoff ได้ `late_cancellation: true` บนนัด พนักงานที่ยกเลิกให้ไม่ถูกนับ เพราะอาจเป็นคลินิกเองที่ยกเลิก
- cutoff นี้คนละตัวกับ "ยกเลิกล่วงหน้ากี่ชั่วโมง" ใน `PATCH /clinic/settings/booking` ตัวนั้นตัดสินว่าลูกค้า **เลื่อนนัด** เองได้ถึงเมื่อไร
- **ค่าปรับไม่ถูกเก็บอัตโนมัติ** ระบบคำนวณและบอกใน `fee_due_satang` ส่วนการออกบิลเป็นการตัดสินที่เคาน์เตอร์ ผ่าน `POST /staff/invoices`

## ประเภทนัด

ค่าตั้งต้นคือสี่ประเภทที่ contract เขียนไว้

| `code` | ชื่อ | กติกาตั้งต้น |
|---|---|---|
| `regular` | นัดปกติ | จองออนไลน์ได้ |
| `emergency` | ฉุกเฉิน | จองออนไลน์ไม่ได้ เข้าคิวเป็น `emergency` |
| `follow_up` | ติดตามผล | จองออนไลน์ได้ |
| `surgery` | ผ่าตัด | จองออนไลน์ไม่ได้ |

```http
PUT /v1/staff/appointment-types
{ "types": [ { "code": "regular", "name": "นัดปกติ" }, { "code": "dental", "name": "ทันตกรรม", "duration_minutes": 60, "online_booking": false, "queue_priority": "urgent" } ] }
```

| กติกา | มีผลที่ |
|---|---|
| `duration_minutes` | ทุกทางจอง ใช้แทนระยะเวลาของบริการ |
| `online_booking` | `POST /portal/appointments` ประเภทที่ปิดไว้ตอบ `422` ที่ช่อง `type` |
| `lead_time_minutes` | การจองของลูกค้า ใช้แทนเวลาจองล่วงหน้าของคลินิก |
| `queue_priority` | การเช็กอิน บัตรคิวเริ่มที่ความเร่งด่วนนี้ (`normal`, `urgent`, `emergency`) |

- ส่งทั้งชุดทุกครั้ง ต้องมี `regular` เสมอ ไม่เกิน 20 ประเภท รหัสเป็นตัวพิมพ์เล็ก ตัวเลข หรือ `_`
- การจองส่ง `type` ได้ทั้ง `POST /staff/appointments` และ `POST /portal/appointments` ไม่ส่งคือ `regular` ทุกนัดตอบ `type` กลับมา
- นัดที่จองไว้แล้วเก็บรหัสประเภทของตัวเองไว้ ลบประเภทแล้วนัดเดิมยังอยู่ และเช็กอินแบบนัดปกติ
- อ่านทั้งชุดได้ที่ `GET /staff/appointment-types` (`appointment:read` รูปเดียวกับคำตอบของ `PUT` พร้อม `version`) และนโยบายที่ `GET /staff/policies/cancellation` (`settings:read`) ลูกค้าอ่านเฉพาะประเภทที่จองออนไลน์ได้ที่ `GET /portal/appointment-types` ทั้งสามเส้นเป็น [ส่วนขยาย § 23](../contract-extensions.md)

## ตรวจนัดชน

ถามได้สองแบบ

**ก่อนจอง** ส่งเวลาที่จะจอง

```http
GET /v1/staff/appointments/conflicts?starts_at=2026-10-05T10:00:00%2B07:00&service_id=svc_...&vet_id=stf_...&pet_id=pet_...&branch=brn_...
```

ไม่ส่ง `ends_at` ระบบใช้ระยะเวลาของบริการ ตอนเลื่อนนัดส่ง `exclude` เป็น id ของนัดที่กำลังเลื่อน

**ทั้งวัน** ส่ง `date` เพื่อตรวจนัดที่จองไว้แล้วในวันนั้น เช่น หลังบันทึกวันลาของหมอ

```http
GET /v1/staff/appointments/conflicts?date=2026-10-05&branch=brn_...
```

คำตอบ

```json
{ "clear": false, "conflicts": [ { "kind": "staff_on_time_off", "message": "หมอลาในช่วงนี้: ป่วย", "starts_at": "...", "ends_at": "...", "appointment_id": "apt_...", "staff_id": "stf_..." } ] }
```

| `kind` | แปลว่า |
|---|---|
| `clinic_closed` | วันนั้นเป็นวันหยุดของสาขา |
| `staff_off_shift` | หมอไม่มีเวรครอบช่วงนั้น |
| `staff_on_time_off` | หมอลาในช่วงนั้น |
| `staff_double_booked` | หมอมีนัดอื่นทับ |
| `pet_double_booked` | สัตว์มีนัดอื่นทับ |

endpoint นี้แค่รายงาน ไม่ห้ามอะไร **ไม่ตรวจห้อง** เพราะนัดในระบบนี้ไม่ได้จองห้อง ห้องถูกจ่ายบนกระดานคิวตอนเรียก

## นัดซ้ำเป็นคอร์ส

```http
POST /v1/staff/appointments/recurring
Idempotency-Key: ...
{ "pet_id": "pet_...", "service_id": "svc_...", "first_starts_at": "2026-10-05T10:00:00+07:00", "rrule": "FREQ=WEEKLY;INTERVAL=3;COUNT=6" }
```

- RRULE ชุดเดียวกับตารางเวร (`FREQ=DAILY` หรือ `WEEKLY`, `INTERVAL`, `BYDAY`, `COUNT` หรือ `UNTIL`) คิดตามเวลาคลินิก 10:00 คือ 10:00 ทุกครั้ง 2 ถึง 52 ครั้ง
- **จองทั้งชุดหรือไม่จองเลย** ถ้าบางครั้งจองไม่ได้ ตอบ `422 slot_unavailable` และ `details.conflicts` บอกทีละวันพร้อมเหตุผลและเวลาว่างของวันนั้น
- **หมอคนเดียวตลอดคอร์ส** ไม่ระบุ `staff_id` ระบบเลือกหมอให้ครั้งแรก แล้วใช้หมอคนนั้นทุกครั้ง วันที่หมอคนนั้นไม่ว่างกลายเป็นวันที่ชน ไม่ถูกเปลี่ยนหมอเงียบ ๆ
- ไม่ติดเพดาน "จองล่วงหน้าได้กี่วัน" ของหน้าจอง เพราะคอร์ส 6 ครั้งทุก 3 สัปดาห์ยาวเกิน 60 วัน
- ทุกนัดในคอร์สมี `series_id` เดียวกัน

## คิวรอคิวว่าง

```http
POST /v1/staff/waiting-list
{ "pet_id": "pet_...", "service_id": "svc_...", "earliest_on": "2026-10-05", "latest_on": "2026-10-12", "staff_id": "stf_...", "note": "ช่วงเช้าเท่านั้น" }
```

- ไม่ส่งวันคือวันนี้ถึงอีก 14 วัน รอได้ไกลสุด 90 วัน
- ข้อความไปหาเจ้าของหลักของสัตว์
- สัตว์หนึ่งตัวรอบริการเดียวได้ครั้งเดียว ซ้ำตอบ `409 already_waiting`
- **เมื่อมีคนยกเลิกนัด** ที่ตรงบริการ วัน สาขา และหมอที่ระบุ ระบบแจ้งคนที่รอไม่เกินสามคนตามลำดับที่เข้าคิว (`waiting_list.slot_available`) **ไม่ได้จองให้** ใครจองก่อนได้ก่อนผ่านการจองปกติ จึงไม่มีทางจองซ้อน
- รายการหมดอายุเองหลัง `latest_on` และถอนออกก่อนได้ด้วย `DELETE /staff/waiting-list/{id}` ซึ่งลบแถวและบันทึก audit `waiting_list.removed` ถอนซ้ำตอบ `404` ถอนแล้วใส่ใหม่ได้ทันที
- `GET /staff/waiting-list?branch=&service_id=&status=` เรียงเก่าสุดก่อน `status` คือ `waiting` หรือ `lapsed` คิดจาก `latest_on` แต่ละแถวมี `pet_name` และ `service_name` `branch` คือรายการของสาขานั้นรวมรายการที่รอทุกสาขา พนักงานที่ผูกสาขาเห็นและถอนได้แค่รายการของสาขาตัวเองและรายการที่รอทุกสาขา (ส่วนขยาย § 23)
- ข้อความผ่านด่านความยินยอมเหมือนทุกข้อความ และเข้ากล่องแจ้งเตือนในเว็บ ยังไม่มี LINE จริง

## แผนการรักษา

```http
POST /v1/staff/pets/pet_.../treatment-plans
Idempotency-Key: ...
{ "template_id": "tpl_...", "service_id": "svc_...", "first_starts_at": "2026-10-05T10:00:00+07:00" }
```

- จำนวนครั้งมาจาก `rrule` หรือ `sessions` คู่กับ `interval_days` หรือคอร์สในเทมเพลต ไม่มีเลยตอบ `422`
- `title` ไม่ส่งใช้ชื่อเทมเพลต
- ปกติจองนัดทุกครั้งด้วยตัวจองเดียวกับนัดซ้ำ ทั้งหมดหรือไม่เลย แต่ละครั้งในแผนชี้ไปที่นัดของตัวเอง (`sessions[].appointment_id`)
- `book: false` วางแผนวันไว้โดยยังไม่จอง ใช้เมื่อเจ้าของยังไม่ยืนยันวัน
- "การเตือน" ทำแล้วใน [เตือนอัตโนมัติ](automation.md) กติกา `appointment_upcoming` เตือนนัดของแผนเหมือนนัดอื่น และกติกา `treatment_plan_session` เตือนแต่ละรอบของแผน รวมรอบที่ยังไม่ได้จองนัด
- บันทึก audit `pet.treatment_plan_created`

## ข้อจำกัดที่รู้อยู่

- หน้าจองคอร์สยังไม่รองรับ `BYDAY` (ซ้ำหลายวันในสัปดาห์) ซ้ำได้แค่วันเดียวกับครั้งแรก
- เวลาว่างในหน้าจองคิดจากระยะเวลาของบริการ ไม่ใช่ของประเภทนัด ระบบตรวจช่วงเวลาจริงตามประเภทตอนจอง
- แก้รายการคิวรอไม่ได้ ต้องถอนแล้วใส่ใหม่ และรายการที่หมดอายุไม่ถูกลบเอง
- ค่าปรับคำนวณแต่ไม่ออกบิลเอง
- ตรวจนัดชนไม่ตรวจห้อง
- คิวรอไม่จองให้ และแจ้งเฉพาะตอนมีคนยกเลิก การเลื่อนนัดที่ปล่อยช่วงเดิมว่างยังไม่แจ้ง

## โค้ด

| ส่วน | ไฟล์ |
|---|---|
| domain | `backend/src/HappyPaws.Domain/Appointments/` (`Appointment.cs`, `AppointmentTypeCatalog.cs`, `CancellationPolicy.cs`, `WaitingListEntry.cs`), `backend/src/HappyPaws.Domain/Patients/TreatmentPlan.cs` |
| หน้าจอ | `frontend/src/features/appointments/` (`components/detail/no-show-*`, `components/new/` ส่วนคอร์ส ตรวจนัดชน และ `type-select.tsx`, `components/calendar/day-conflict-check.tsx`, `components/settings/`, `components/waiting-list/`), `frontend/src/features/booking/api/listOnlineAppointmentTypes.ts`, `frontend/src/app/(staff)/settings/appointments/`, `frontend/src/app/(staff)/appointments/waiting-list/` |
| use case | `backend/src/HappyPaws.Application/Appointments/` (`MarkNoShow`, `ReplaceCancellationPolicy`, `ReplaceAppointmentTypes`, `DetectConflicts`, `CreateRecurringSeries`, `AddToWaitingList`, `GetAppointmentRules`, `ListWaitingList`, `RemoveFromWaitingList`, `AppointmentRules.cs`), `backend/src/HappyPaws.Application/Patients/CreateTreatmentPlan/` |
| ตัวจองคอร์ส | `backend/src/HappyPaws.Application/Appointments/CreateRecurringSeries/SeriesBooker.cs` |
| แจ้งคิวรอ | `backend/src/HappyPaws.Application/Appointments/AddToWaitingList/WaitingListNotifier.cs` |
| endpoint | `backend/src/HappyPaws.Api/Controllers/Staff/` (`AppointmentsController`, `CancellationPolicyController`, `AppointmentTypesController`, `AppointmentConflictsController`, `RecurringAppointmentsController`, `WaitingListController`, `TreatmentPlansController`), `Portal/PortalAppointmentTypesController` |
| เทสต์ | `backend/tests/HappyPaws.IntegrationTests/Api/Appointments/AdvancedAppointmentTests.cs`, `AppointmentRulesReadTests.cs`, `WaitingListManagementTests.cs`, `Api/Patients/TreatmentPlanTests.cs`, `backend/tests/HappyPaws.UnitTests/Domain/Appointments/AdvancedBookingRulesTests.cs`, `Application/Appointments/WaitingListReadTests.cs` |
