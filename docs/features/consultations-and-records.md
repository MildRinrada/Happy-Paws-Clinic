# การตรวจและเวชระเบียน

การตรวจ (`consultations`) คือพื้นที่ทำงานที่หมอบันทึกระหว่างตรวจ เปิดจากบัตรคิวหรือจากนัด แก้ได้ตลอดจนกว่าจะปิดเคส การปิดเคสลงนามเวชระเบียน (`medical_records`) ซึ่งเป็นเอกสารถาวร แก้ในที่ไม่ได้ ต้องแก้ผ่าน amendment ที่เก็บฉบับเดิมไว้เสมอ

เหตุที่แยกเป็นสองอย่าง คือ contract ขอทั้ง "autosave ฉบับร่างทุก 10 วินาที" และ "เวชระเบียนเป็น append-only หลัง sign" สองข้อนี้จริงพร้อมกันได้เมื่อของที่แก้ได้กับของที่ลงนามแล้วเป็นคนละแถว

ตอนนี้มีแค่ API ยังไม่มีหน้าตรวจหรือหน้าเวชระเบียนบนเว็บ ทุก path อยู่ใต้ `/v1`

## Endpoint

| Endpoint | สิทธิ์ | ทำอะไร |
|---|---|---|
| `POST /staff/consultations` | `consult:write` | เปิดเคสจากคิวหรือนัด ต้องมี `Idempotency-Key` |
| `PATCH /staff/consultations/{id}` | `consult:write` | autosave ช่องข้อความของเคส |
| `POST /staff/consultations/{id}/vitals` | `vitals:write` | น้ำหนัก อุณหภูมิ ชีพจร การหายใจ |
| `POST /staff/consultations/{id}/examination` | `consult:write` | ผลตรวจรายระบบ |
| `POST /staff/consultations/{id}/diagnoses` | `consult:write` | คำวินิจฉัยหลักหรือรอง |
| `POST /staff/consultations/{id}/treatments` | `consult:write` | หัตถการ เข้าบิลตอนปิดเคส ต้องมี `Idempotency-Key` |
| `POST /staff/consultations/{id}/prescriptions` | `rx:write` | สั่งยา ตรวจแพ้ยาก่อน ต้องมี `Idempotency-Key` |
| `POST /staff/consultations/{id}/close` | `record:sign` | ปิดเคส ลงนาม ออกบิล นัดติดตาม ต้องมี `Idempotency-Key` |
| `GET /staff/medical-records/{id}` | `record:read` | อ่านเวชระเบียน ทุกการอ่านถูกบันทึก |
| `POST /staff/medical-records/{id}/amendments` | `record:amend` | แก้เวชระเบียนแบบมีร่องรอย |
| `GET /staff/medical-records/{id}/audit` | `record:audit` | ใครแก้อะไรเมื่อไร และใครเปิดอ่าน |

- scope แยกตามระดับความรับผิดชอบ ผู้ช่วยที่มี `vitals:write` ชั่งน้ำหนักได้อย่างเดียว `consult:write` คือพื้นที่ทำงานของหมอ `rx:write` คือการสั่งยา และ `record:sign` คือการลงนามบนเอกสารถาวร
- บัญชีที่ถือ `consult:write`, `rx:write` หรือ `record:*` ต้องผ่าน 2FA ในเซสชันนั้น ดู [การเข้าสู่ระบบ](../authentication.md)
- ทุกเส้นใช้กับเคสหรือเวชระเบียนในสาขาที่ตัวเองเข้าถึงได้เท่านั้น ไม่ได้จำกัดเฉพาะหมอเจ้าของเคส เพราะการส่งเวร ขอความเห็นหมอคนที่สอง และผู้ช่วยกรอก vitals ให้เคสของหมอคนอื่น เป็นเรื่องปกติ
- ลูกค้าไม่เข้าเส้นเหล่านี้เลย ส่วนที่เจ้าของเห็นจะผ่าน `GET /portal/pets/{id}/records` ซึ่งยังไม่ได้ทำ

## สถานะของการตรวจ

| ค่าที่เก็บ | ความหมาย | แก้ได้ |
|---|---|---|
| `draft` | เปิดอยู่ autosave ได้ ยังไม่อยู่ในประวัติถาวรของสัตว์ | ใช่ |
| `closed` | ปิดแล้ว มีเวชระเบียนที่ลงนาม (`medical_record_id`) | ไม่ ได้ `423 record_locked` |

```text
 เปิดเคส ──────► draft ──────── ปิดเคส ────────► closed
                 │   ▲                            │
                 └───┘                            └── แก้ต่อได้ทาง amendment ของเวชระเบียนเท่านั้น
     autosave, vitals, ผลตรวจ, วินิจฉัย,
     หัตถการ, ใบสั่งยา
```

ทุกการเขียนลงเคสผ่านจุดเดียวใน `Consultation.RequireOpen` เคสที่ปิดแล้วจึงไม่มีทางไหนลืมตรวจ คำตอบเป็น `423 record_locked` ไม่ใช่ `409` เพราะ `409` ชวนให้ลองใหม่ ส่วน `423` บอกว่าต้องทำ amendment

## สถานะของเวชระเบียน

| ค่าที่เก็บ | ความหมาย |
|---|---|
| `draft` | อยู่ใน contract แต่ไม่เกิดจริง เวชระเบียนในระบบนี้มีได้แค่แบบที่ลงนามแล้ว |
| `signed` | ลงนามตอนปิดเคส |
| `amended` | มี amendment อย่างน้อยหนึ่งครั้ง แก้ครั้งที่สองก็ยังเป็น `amended` จำนวนครั้งดูจากรายการ |

```text
 ปิดเคส ──► signed ──── amendment ────► amended ──┐
                                         ▲        │ amendment
                                         └────────┘
```

## เปิดเคส

`POST /staff/consultations` รับ `appointment_id` หรือ `queue_ticket_id` อย่างใดอย่างหนึ่ง (ไม่มีทั้งสองได้ `422`) และ `chief_complaint` ถ้ามี

- ส่งบัตรคิวมา ระบบหานัดของบัตรเอง ส่งนัดมา ระบบหาบัตรของนัดเอง ส่งทั้งสองแต่ไม่ใช่คู่เดียวกันได้ `422`
- ใน transaction เดียว สร้างเคส เปลี่ยนนัดเป็น `in_consultation` และพาบัตรเข้า `in_room` ถ้าบัตรยังไม่ถูกเรียก ระบบเรียกให้ก่อนในชื่อหมอที่เปิดเคส ดู [คิว](queue.md)
- นัดต้องเช็กอินแล้ว นัดที่ยัง `booked` หรือ `confirmed` ได้ `409 state_conflict` บัตรที่ตรวจเสร็จหรือออกจากคิวแล้วก็เช่นกัน
- เปิดซ้ำได้เคสเดิมคืน ไม่ใช่ `409` เพราะเหตุที่หมอกดซ้ำมักเป็นคำตอบแรกไม่มาถึง ฐานข้อมูลกันอีกชั้นด้วย partial unique index บน `appointment_id` และ `queue_ticket_id` ของเคส
- หมอเจ้าของเคส (`vet_staff_id`) คือคนที่เปิด `chief_complaint` ใช้ค่าที่ส่งมา หรือ "อาการที่แจ้ง" จากนัด
- เขียน audit log และส่ง `ticket.updated` หลัง commit

## ระหว่างตรวจ

| ส่วน | กติกา |
|---|---|
| ช่องข้อความ (`PATCH`) | `chief_complaint`, `subjective`, `objective`, `assessment`, `plan` ช่องที่ไม่ส่งคือไม่แตะ ส่งสตริงว่างคือล้าง เพราะ autosave ส่งเฉพาะช่องที่หมอแก้ `If-Match` ไม่บังคับ ไม่อย่างนั้น autosave จะชนกับตัวเองทุกไม่กี่วินาที |
| vitals | เพิ่มต่อท้าย ไม่เขียนทับ ค่าล่าสุดคือค่าปัจจุบัน น้ำหนักต้องมากกว่า 0 และไม่เกิน 500 kg อุณหภูมิ 20 ถึง 46 องศา นอกช่วงได้ `409 invalid_vitals` บันทึกว่าใครวัด และเป็นข้อมูลของกราฟน้ำหนักด้วย |
| ผลตรวจรายระบบ | ส่งทั้งฟอร์มแทนของเดิม (`findings` แต่ละแถวมี `body_system`, `is_normal`, `note`) ต้องมีอย่างน้อยหนึ่งระบบ ห้ามซ้ำ ผลผิดปกติต้องมีบันทึก ระบบที่ไม่ได้ตรวจคือไม่มีแถว ถ้าคลินิกตั้งรายการระบบไว้ ระบบนอกรายการได้ `422` |
| คำวินิจฉัย | คำวินิจฉัยแรกเป็นหลักเสมอ ตั้งตัวใหม่เป็นหลัก ตัวเดิมถูกลดเป็นรอง เคสจึงมีคำวินิจฉัยหลักหนึ่งเดียว `code` เป็นรหัสโรคของคลินิก ไม่บังคับ |
| หัตถการ | ชื่อ จำนวนมากกว่า 0 และราคาต่อหน่วยเป็นสตางค์ไม่ติดลบ ราคาเก็บไว้บนแถวตอนบันทึก ราคาบริการเปลี่ยนภายหลังจึงไม่กระทบ ส่วนลดไม่ใส่ที่นี่ ต้องไปทำที่ใบแจ้งหนี้ |
| ใบสั่งยา | ชื่อยา ขนาด ความถี่ จำนวนวัน และจำนวนยา ต้องครบ ตรวจแพ้ยาก่อนบันทึก ตัดสต๊อกและคิดเงินตอนจ่ายยา ไม่ใช่ตอนเขียน |

- ทุกส่วนเขียน audit log
- ราคาหัตถการเป็นค่าที่ผู้เรียกส่งมา (`unit_price_satang`) ไม่ได้ดึงจากรายการบริการ แม้จะส่ง `service_id` มาด้วยก็ตาม
- ทุกคำตอบคืนเคสทั้งก้อนพร้อม `ETag`

## ตรวจแพ้ยาก่อนสั่งยา

contract บอกให้ "ตรวจแพ้ยา" แต่ไม่บอกว่าเจอแล้วทำอะไร ห้ามเลยคือระบบไปค้านการตัดสินใจของหมอ เตือนเฉย ๆ คือการตรวจที่ไม่มีผล เราเลือกทางกลาง

```text
 สั่งยา ──► มีบันทึกแพ้ที่ชื่อตรงกัน?
              ไม่มี ──────────────────────────────► บันทึกใบสั่งยา
              มี และรุนแรง (severe) ────────────────► 409 allergy_conflict  overridable = false
              มี ไม่รุนแรง ไม่ส่ง acknowledge_allergy ──► 409 allergy_conflict  overridable = true
              มี ไม่รุนแรง ส่ง acknowledge_allergy ─────► บันทึกใบสั่งยา พร้อมข้อความยอมรับ
```

- `details` บอก `drug_name`, `allergies` (สารที่แพ้) และ `overridable`
- ข้อความยอมรับเก็บใน `allergy_acknowledgement` ของใบสั่งยา และเขียน audit log แถวแยกสำหรับการสั่งทับบันทึกแพ้ยา คนตรวจสอบค้นแถวนี้ได้โดยตรง
- แพ้รุนแรงสั่งทับผ่าน API ไม่ได้เลย เรื่องนี้ต้องคุยกัน ไม่ใช่กดซ้ำ
- ใช้เฉพาะบันทึกแพ้ที่ยังไม่ถูกถอน
- **ข้อจำกัด** จับคู่ด้วยชื่อแบบมีคำนี้อยู่ในอีกคำ (ไม่สนตัวพิมพ์) ไม่รู้จักกลุ่มยา แพ้ penicillin จะไม่เตือน amoxicillin ต้องมีฐานข้อมูลยาก่อนจึงทำได้ ถือเป็นตัวช่วย ไม่ใช่การรับประกัน
- การบันทึกแพ้ยาของสัตว์อยู่ใน [ลูกค้าและสัตว์เลี้ยง](customers-and-pets.md)

## ปิดเคส

`POST /staff/consultations/{id}/close` รับ `follow_up` (`starts_at`, `service_id`, `staff_id`) ถ้าต้องการนัดติดตาม

### ตรวจก่อนลงนาม

| กติกา | ไม่ผ่านได้ |
|---|---|
| ต้องมีคำวินิจฉัยอย่างน้อยหนึ่งรายการ ใช้กับทุกคลินิก | `409 incomplete_consultation` |
| ช่องที่คลินิกตั้งให้บังคับ (`chief_complaint`, `subjective`, `objective`, `assessment`, `plan`, `vitals`, `examination`) | `409 incomplete_consultation` พร้อม `details.missing_fields` |
| คลินิกตั้งให้ผู้ลงนามต้องเป็นหมอเจ้าของเคส | `403 forbidden` |
| เคสยังเปิดอยู่ | `423 record_locked` |

กติกาของคลินิกอ่านใหม่ทุกครั้งที่ปิดเคส กติกาที่เข้มขึ้นเช้านี้จึงมีผลกับเคสถัดไป ไม่ย้อนไปเคสที่ลงนามแล้ว ตั้งค่าได้ที่การตั้งค่าเวชระเบียน ดู [ตั้งค่าคลินิก](clinic-setup.md)

### สิ่งที่เกิดใน transaction เดียว

```text
 ปิดเคส
   ├─ ถ่ายเคสทั้งก้อนเป็น JSON แล้วลงนาม ──► medical_records (signed)
   ├─ เคส ──► closed
   ├─ บัตรคิวที่อยู่ในห้อง ──► served
   ├─ นัด ──► completed
   ├─ นัดติดตาม (ถ้าขอ) ──► appointments (booked)
   ├─ ใบแจ้งหนี้จากหัตถการ (ถ้ามีหัตถการ) ──► invoices (issued)
   └─ audit log: ปิดเคส, ลงนามเวชระเบียน, ออกใบแจ้งหนี้
 หลัง commit
   ├─ ticket.updated ไปที่กระดานคิว
   └─ แจ้งสรุปการตรวจให้เจ้าของ (consultation.summary)
```

ทุกอย่างเกิดพร้อมกันหรือไม่เกิดเลย เวชระเบียนที่ลงนามแล้วแต่สัตว์ยังค้างอยู่ในห้องตรวจ นัดที่เสร็จแต่ไม่มีเวชระเบียน หรือนัดติดตามของเคสที่ปิดไม่สำเร็จ ล้วนเป็นข้อมูลที่ขัดกัน

- **ใบแจ้งหนี้** ออกผ่าน `ConsultationBilling` คิดหัตถการทุกรายการ และยาที่จ่ายไปแล้วระหว่างเคสเปิด ตามราคาที่จับไว้ตอนจ่าย ใบสั่งยาที่ยังไม่จ่ายไม่ถูกคิดเงิน ยาที่จ่ายหลังปิดเคสถูกเพิ่มทีหลัง ดู [สต๊อกยาและเวชภัณฑ์](inventory.md#ค่ายาขึ้นบิลเดียวกับการรักษา) เคสที่ไม่มีหัตถการและไม่มีค่ายาไม่ออกใบแจ้งหนี้ เพราะบิล 0 บาทเป็นเอกสารที่เคาน์เตอร์ต้องดูโดยไม่มีอะไรให้เก็บ ถ้า transaction ถูกลองใหม่ ระบบหาใบที่ออกให้เคสนี้ไปแล้วแทนการออกใบที่สอง ส่วนการปิดซ้ำหลังปิดสำเร็จได้คำตอบเดิมจาก `Idempotency-Key` หรือ `423 record_locked` ดู [ใบแจ้งหนี้และการชำระเงิน](billing.md)
- **นัดติดตาม** ใช้บริการที่ส่งมา หรือบริการของนัดเดิม หมอที่ส่งมา หรือหมอเจ้าของเคส อาการที่แจ้งเป็น "นัดติดตามผล" ไม่ตรวจตารางเวรหรือ lead time เหมือนการจองของเคาน์เตอร์ แต่ exclusion constraint ของนัดยังทำงาน เวลาที่ชนกับนัดอื่นได้ `422 slot_unavailable` และทั้งการปิดเคสถูกยกเลิก เคสที่เปิดจากบัตร walk-in โดยไม่มีนัดต้องส่ง `service_id` เอง
- **แจ้งเตือน** ส่งหลัง commit ถ้าล้มก็แค่เขียน log ไม่ทำให้การปิดเคสล้ม และตอนนี้ช่องทางยังเป็น stub ดูหัวข้อข้อจำกัด
- คำตอบมี `consultation`, `record`, `follow_up`, `charges_satang` และ `invoice`

## เวชระเบียน

### เก็บอะไร

- `content_json` คือเคสทั้งก้อนตอนลงนาม เก็บเป็น `text` ไม่ใช่ `jsonb` เพราะ PostgreSQL จัดรูป `jsonb` ใหม่ตอนเขียน ไบต์ที่อ่านกลับจะไม่ใช่ไบต์ที่ถูก hash ไว้
- เป็นภาพถ่าย ไม่ใช่ตัวชี้ เปลี่ยนชื่อบริการหรือแก้ชื่อสัตว์ภายหลัง เอกสารที่ลงนามไม่เปลี่ยนตาม
- `content_hash` คือ SHA-256 ของเนื้อหา ผู้ลงนาม และเวลาลงนาม (ระดับมิลลิวินาที) คำตอบมี `integrity_verified` บอกว่ายังตรงกันไหม นี่ไม่ใช่ลายเซ็นดิจิทัล คนที่เข้าฐานข้อมูลได้คำนวณใหม่ได้ แต่การแก้แถวตรง ๆ จะเห็นเป็นความไม่ตรงกัน
- เวชระเบียนหนึ่งฉบับต่อหนึ่งเคส (unique index บน `consultation_id`) ผู้ลงนามมีคนเดียวตลอดไป

### อ่าน

`GET /staff/medical-records/{id}` ตอบเนื้อหาปัจจุบัน (`content`) เนื้อหาตอนลงนาม (`original_content`) รายการ amendment ผู้ลงนาม เวลา และ `integrity_verified`

ทุกการอ่านที่สำเร็จเขียน audit log ทันทีแยกจาก transaction อื่น การอ่านที่ถูกปฏิเสธไม่ลงในบันทึกการเข้าถึง ดู [audit log และความเป็นส่วนตัว](../security/audit-and-privacy.md)

### แก้ด้วย amendment

`POST /staff/medical-records/{id}/amendments` รับ `reason` และ `changes` (ชื่อช่องไปค่าใหม่)

- แก้ได้เฉพาะช่องข้อความ `chief_complaint`, `subjective`, `objective`, `assessment`, `plan` ช่องอื่นได้ `422`
- vitals คำวินิจฉัย หัตถการ และใบสั่งยา แก้ไม่ได้ เพราะส่วนอื่นของคลินิกทำตามไปแล้ว หัตถการถูกเก็บเงิน ยาอาจจ่ายไปแล้ว การแก้ทำทางใบแจ้งหนี้หรือเปิดเคสใหม่
- ต้องมีเหตุผล ไม่มีได้ `409 invalid_amendment` เพราะการแก้ที่ไม่มีเหตุผล อ่านย้อนหลังแล้วแยกไม่ออกจากการปลอมแปลง
- ค่าที่ไม่ต่างจากเดิมถูกข้าม ถ้าไม่มีอะไรต่างเลยได้ `422`
- amendment แต่ละครั้งเก็บเอกสารฉบับเต็มหลังแก้ และ diff รายช่อง (`{ "plan": { "from": ..., "to": ... } }`) เนื้อหาปัจจุบันคือ amendment ล่าสุด ฉบับเดิมไม่ถูกแตะ
- เขียน audit log พร้อม diff ชุดเดียวกัน `If-Match` ใช้ถ้าส่งมา ตอบ `201`

### ดูประวัติ

`GET /staff/medical-records/{id}/audit` ตอบสองส่วน amendment บอกว่าเอกสารตอนนี้เขียนว่าอะไรและมาถึงตรงนี้อย่างไร ส่วน audit log บอกว่าใครแตะ จากที่ไหน เมื่อไร รวมถึงการเปิดอ่านซึ่งไม่ทิ้งร่องรอยในเอกสาร ส่วน audit log แบ่งหน้าแบบ cursor `record:audit` แยกจาก `record:read` เพราะการอ่านเวชระเบียนกับการตรวจว่าใครอ่านเป็นอำนาจคนละแบบ

## สิ่งที่ไม่ใช่สถานะ

| เรื่อง | คืออะไร |
|---|---|
| หมอเจ้าของเคส | `vet_staff_id` คนที่เปิดเคส ไม่จำกัดว่าใครเขียนเคสได้ ใช้เมื่อคลินิกตั้งให้ผู้ลงนามต้องเป็นหมอเจ้าของเคส |
| คำวินิจฉัยหลัก | `is_primary` บนคำวินิจฉัย มีหนึ่งเดียวต่อเคส |
| ค่าใช้จ่ายของเคส | ผลรวมของหัตถการ คิดสดจากแถว ไม่ได้เก็บแยก |
| ยาจ่ายแล้วหรือยัง | `dispensed_at` บนใบสั่งยา ว่างจนกว่าจะจ่ายจริงผ่าน `POST /staff/prescriptions/{id}/dispense` พร้อม `stock_deducted` และ `charged_unit_price_satang` |
| แพ้ยาที่ยอมรับแล้ว | `allergy_acknowledgement` บนใบสั่งยา |
| ความถูกต้องของเวชระเบียน | `integrity_verified` คำนวณตอนอ่าน |

## หน้าตรวจบนเว็บ

`/consultations/<id>` หน้าทำงานของสัตวแพทย์ เปิดได้เมื่อถือ `consult:read`

**ทางเข้า** ปุ่ม **เริ่มตรวจ** บนบัตรคิว (คิวที่รอ เรียกแล้ว หรืออยู่ในห้อง) และในรายละเอียดนัดบนปฏิทิน (นัดที่เช็กอินแล้ว) ทั้งสองปุ่มขึ้นเมื่อถือ `consult:write` และเรียก `POST /staff/consultations` การเปิดเคสของการมาครั้งเดิมซ้ำได้เคสเดิมกลับมา ปุ่มจึงใช้กลับเข้าเคสที่ค้างไว้ได้ด้วย (ขึ้นว่า **เปิดเคสตรวจ** เมื่อสัตว์อยู่ในห้องแล้ว) หน้าโหลดเคสด้วย `GET /staff/consultations/{id}` ซึ่งเป็นส่วนขยาย ดู [contract-extensions.md § 14](../contract-extensions.md)

**หัวหน้า** ชื่อสัตว์ HN สถานะเคส ชนิด สายพันธุ์ น้ำหนักล่าสุด เจ้าของ (ลิงก์ไปหน้าลูกค้า) ลิงก์ไปหน้าสัตว์ และแถบแดงบอกชื่อยาที่แพ้ ซึ่งอ่านจากทะเบียนสัตว์ใหม่ทุกครั้งที่เปิดเคส

| ส่วน | ทำอะไร | ต้องมี scope |
|---|---|---|
| บันทึกการตรวจ | อาการที่มา และ S O A P บันทึกเองหลังหยุดพิมพ์ 3 วินาที ส่งเฉพาะช่องที่แก้ | `consult:write` |
| ตรวจร่างกายรายระบบ | ระบบละแถว เลือกปกติ ผิดปกติ (พร้อมสิ่งที่พบ) หรือไม่ได้ตรวจ บันทึกทั้งฟอร์มทับของเดิม | `consult:write` |
| การวินิจฉัย | เพิ่มทีละข้อ ข้อแรกเป็นวินิจฉัยหลักโดยค่าตั้งต้น | `consult:write` |
| สัญญาณชีพ | น้ำหนัก อุณหภูมิ ชีพจร การหายใจ กรอกค่าใดค่าหนึ่งก็บันทึกได้ | `vitals:write` |
| หัตถการและค่าบริการ | รายการ จำนวน ราคา พร้อมยอดรวมที่จะออกบิล แต่ละรายการมี `Idempotency-Key` ของตัวเอง | `consult:write` |
| ใบสั่งยา | ถ้าแพ้ยาที่สั่ง ฟอร์มหยุดและบอกชื่อยาที่แพ้ แพ้ระดับรุนแรงสั่งไม่ได้ ระดับอื่นใส่เหตุผลแล้วกดยืนยัน เหตุผลเก็บในใบสั่งยา | `rx:write` |
| ปิดเคสและลงนาม | ถามยืนยันอีกครั้ง จองนัดติดตามผลได้ในขั้นเดียวกัน | `record:sign` |

- **ส่วนที่บัญชีไม่มีสิทธิ์จะไม่มีฟอร์ม** ผู้ช่วยที่ถือแค่ `vitals:write` เห็นเคสและกรอกได้แค่สัญญาณชีพ บัญชีที่ไม่มี `record:sign` เห็นข้อความว่าให้สัตวแพทย์เป็นผู้ปิดเคส
- **ทุกส่วนบันทึกแยกกัน** แล้วหน้าโหลดเคสใหม่ สิ่งที่เห็นจึงเป็นสิ่งที่ backend เก็บไว้จริงเสมอ
- **ปิดเคสก่อนครบ** ได้ `incomplete_consultation` หน้าแสดงสิ่งที่ยังขาดพร้อมลิงก์ไปที่ส่วนนั้น
- **นัดติดตามผล** เคสที่มาจากนัดเลือกบริการ "เหมือนครั้งนี้" ได้ เคส walk-in ไม่มีบริการเดิมให้ใช้ จึงต้องเลือกบริการ (backend ตอบ 422 ถ้าไม่ส่ง) รายการบริการมาจาก `GET /services`
- **หลังปิดเคส** หน้าแสดงเลขบิลพร้อมลิงก์ไปหน้าบิล และเวลานัดติดตามผล ข้อมูลนี้อยู่ในที่อยู่ของหน้า เพราะเคสที่ปิดแล้วไม่ได้เก็บเลขบิลไว้ เคสที่ไม่มีหัตถการไม่ออกบิล ส่วนต่าง ๆ ของเคสที่ปิดแล้วเป็นแบบอ่านอย่างเดียว
- **แบบฟอร์มตรวจรายระบบ** ใช้รายชื่อระบบในไฟล์ content ของหน้า (`consultation-page.ts`) เพราะ contract มีแค่ `PATCH /clinic/settings/medical-record` ไม่มีทางอ่านแบบฟอร์มที่คลินิกตั้งไว้ ถ้าคลินิกตั้งแบบฟอร์มของตัวเองด้วยชื่ออื่น backend จะปฏิเสธระบบที่ไม่อยู่ในแบบฟอร์มและหน้าแสดงข้อความนั้น

## ข้อจำกัด

- **แจ้งสรุปการตรวจทาง LINE ยังเป็น stub** ข้อความผ่านประตูความยินยอมแล้วไปจบที่ `LoggingNotificationChannel` ซึ่งเขียน log ว่าไม่ได้ส่ง ดู [การแจ้งเตือน](notifications.md)
- **append-only บังคับในโค้ด ไม่ใช่ในฐานข้อมูล** ไม่มี trigger หรือสิทธิ์ระดับตารางกันการ `UPDATE` แถวของ `medical_records` สิ่งที่จับการแก้ตรง ๆ ได้คือ `content_hash`
- **role ตั้งต้น `veterinarian` ปิดเคสเองไม่ได้** ตารางใน contract ให้ `consult:*`, `record:write`, `rx:write` แต่ไม่ให้ `record:sign`, `record:read` และ `vitals:write` เจ้าของคลินิกต้องเพิ่มให้เอง ดู [จุดที่ contract กำกวม](../assumptions.md) และ [พนักงานและสิทธิ์](staff-and-roles.md)
- **ยังไม่ได้ทำ** หน้าเวชระเบียนที่ลงนามแล้วและการแก้ไขเวชระเบียนบนเว็บ และเทมเพลตการรักษาในหน้าตรวจ ส่วนรายการใบสั่งยา การจ่ายยาที่ตัดสต๊อกและคิดค่ายา ([สต๊อกยาและเวชภัณฑ์](inventory.md)) กราฟน้ำหนัก เทมเพลตแผนรักษา ผลแล็บ และเอกสาร PDF มี API แล้วใน [งานรักษาต่อเนื่อง](clinical-records.md) ส่วนที่เจ้าของเห็นอยู่ใน [portal ลูกค้า](portal.md#ประวัติการรักษา)

## โค้ด

| ส่วน | ไฟล์ |
|---|---|
| เคสและส่วนของเคส | `backend/src/HappyPaws.Domain/Consultations/` (`Consultation.cs`, `VitalsReading.cs`, `ExaminationFinding.cs`, `Diagnosis.cs`, `Treatment.cs`) |
| ช่องบังคับก่อนปิดเคส | `backend/src/HappyPaws.Domain/Consultations/ConsultationRequirements.cs`, `backend/src/HappyPaws.Domain/Clinics/MedicalRecordSettings.cs` |
| ใบสั่งยา | `backend/src/HappyPaws.Domain/Prescriptions/Prescription.cs` |
| เวชระเบียน | `backend/src/HappyPaws.Domain/MedicalRecords/` |
| use case ของการตรวจ | `backend/src/HappyPaws.Application/Consultations/` |
| ตรวจแพ้ยา | `backend/src/HappyPaws.Application/Consultations/AddPrescription/AddPrescriptionHandler.cs`, `AllergyConflictException.cs`, `backend/src/HappyPaws.Domain/Patients/PetAllergy.cs` |
| ปิดเคส | `backend/src/HappyPaws.Application/Consultations/CloseConsultation/CloseConsultationHandler.cs`, `backend/src/HappyPaws.Application/Billing/ConsultationBilling.cs` |
| เนื้อหาและ amendment | `backend/src/HappyPaws.Application/MedicalRecords/RecordContent.cs`, `MedicalRecords/AmendMedicalRecord/` |
| controller | `backend/src/HappyPaws.Api/Controllers/Staff/ConsultationsController.cs`, `Staff/ConsultationPartsController.cs`, `Staff/MedicalRecordsController.cs` |
| อ่านเคส (ส่วนขยาย) | `backend/src/HappyPaws.Application/Consultations/GetConsultation/` |
| หน้าตรวจ | `frontend/src/app/(staff)/consultations/`, `frontend/src/features/consultations/` |
| ปุ่มเริ่มตรวจ | `frontend/src/features/consultations/components/open-case-button.tsx` (ใช้ใน `features/queue/components/ticket-card.tsx` และ `features/appointments/components/detail/appointment-actions.tsx`) |
| 2FA ตาม scope | `backend/src/HappyPaws.Api/Authorization/TwoFactorRequirement.cs` |
| ตาราง | `backend/src/HappyPaws.Infrastructure/Persistence/Configurations/Consultations/`, `Configurations/MedicalRecords/` |
| เทสต์ | `backend/tests/HappyPaws.UnitTests/Domain/Consultations/`, `Domain/MedicalRecords/`, `Application/MedicalRecords/RecordContentTests.cs`, `backend/tests/HappyPaws.IntegrationTests/Api/Consultations/`, `Api/MedicalRecords/` |
