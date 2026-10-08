# งานรักษาต่อเนื่อง

สิ่งที่อยู่รอบการตรวจแต่ละครั้ง คือวัคซีน กราฟน้ำหนัก แล็บ ประวัติยาและการจ่ายยา เทมเพลตแผนรักษา ไฟล์แนบ และเอกสาร PDF ที่พิมพ์จากแฟ้มของสัตว์

ส่วนที่ contract ไม่ได้เขียนไว้และเราตีความเอง อยู่ใน [ข้อสมมติ § 36 และ § 38](../assumptions.md) การเปิดเคสและเวชระเบียนอยู่ใน [การตรวจและเวชระเบียน](consultations-and-records.md) แผนการรักษาที่จองนัดเป็นคอร์สอยู่ใน [นัดขั้นสูง](advanced-booking.md)

## Endpoint

| Method | Endpoint | สิทธิ์ | ทำอะไร |
|---|---|---|---|
| GET | `/staff/pets/{id}/vaccinations` | `patient:read` | วัคซีนที่ฉีดแล้ว และรอบถัดไปที่ครบกำหนด |
| POST | `/staff/pets/{id}/vaccinations` | `record:write` | บันทึกการฉีดหนึ่งเข็ม |
| GET | `/staff/pets/{id}/weights` | `patient:read` | จุดของกราฟน้ำหนัก |
| POST | `/staff/lab-requests` | `lab:request` | ส่งตรวจแล็บ |
| POST | `/staff/lab-requests/{id}/results` | `lab:write` | ลงผลแล็บ |
| GET | `/staff/lab-requests` | `lab:read` | รายการงานแล็บ |
| POST | `/staff/lab-requests/{id}/review` | `lab:read` | กดว่าอ่านผลแล้ว (ส่วนขยาย) |
| GET | `/staff/prescriptions` | `rx:read` | ประวัติยา พร้อมตรวจยาซ้ำซ้อน |
| POST | `/staff/prescriptions/{id}/dispense` | `rx:dispense` | จ่ายยา ตัดสต๊อก คิดค่ายา และได้ข้อมูลฉลาก |
| GET | `/staff/prescriptions/drug-options` | `rx:write` | ยาในคลังให้เลือกตอนสั่งยา พร้อมราคาและสถานะสต๊อกของสาขาเดียว (ส่วนขยาย) |
| GET | `/staff/templates/treatments` | `consult:read` | เทมเพลตแผนรักษาของคลินิก |
| POST | `/staff/pets/{id}/documents` | `record:write` | ขอ URL อัปโหลดไฟล์เข้าแฟ้มของสัตว์ |
| POST | `/staff/documents/generate` | `record:read` | พิมพ์ใบรับรองสุขภาพ ใบส่งตัว หรือสมุดวัคซีนเป็น PDF |

## วัคซีน

```http
POST /v1/staff/pets/pet_.../vaccinations
{
  "vaccine_name": "Rabies",
  "lot_number": "LOT-2026-114",
  "vial_expires_on": "2027-06-30",
  "valid_for_days": 365
}
```

| ช่อง | กติกา |
|---|---|
| `vaccine_name` | บังคับ ชื่อตามที่คลินิกใช้ |
| `lot_number`, `vial_expires_on` | **บังคับทั้งคู่** ใช้ตามหาสัตว์เมื่อผู้ผลิตเรียกคืนวัคซีนล็อตใด วัคซีนที่หมดอายุแล้วในวันฉีดถูกปฏิเสธ (`409 invalid_vaccination`) |
| `administered_at` | ไม่ส่งคือตอนนี้ บันทึกย้อนหลังได้ แต่ล่วงหน้าไม่ได้ |
| `next_due_on` หรือ `valid_for_days` | ส่งได้อย่างใดอย่างหนึ่ง ไม่ส่งเลยคือครบกำหนดอีก 1 ปี `valid_for_days: 0` คือไม่ต้องฉีดอีก |
| `consultation_id` | การตรวจที่ฉีดในครั้งนั้น ต้องเป็นของสัตว์ตัวเดียวกัน |

`GET` ตอบสองรายการ คือ `doses` ทุกเข็มเรียงใหม่ไปเก่า และ `due` หนึ่งรายการต่อชนิดวัคซีน คิดจากเข็มล่าสุดของชนิดนั้น สถานะเป็น `overdue` (เลยกำหนด) `due_soon` (ภายใน 30 วัน) หรือ `upcoming`

- สุนัขที่ฉีดพิษสุนัขบ้าทุกปีมาห้าปี มีห้าเข็มแต่มีวันครบกำหนดเดียว เข็มเก่าไม่ถูกนับว่าเลยกำหนด เพราะเข็มใหม่ฉีดแทนแล้ว
- สัตว์ที่เสียชีวิตไม่มีรายการ `due` และบันทึกเข็มใหม่ไม่ได้
- การเตือนตามวันครบกำหนดเป็นกติกาของ [เตือนอัตโนมัติ](automation.md) (`vaccine_due`) ใช้วันครบกำหนดของเข็มล่าสุดของวัคซีนแต่ละชนิด
- เข็มที่บันทึกขึ้นในไทม์ไลน์ของสัตว์ (`GET /staff/pets/{id}/history`) เป็น `kind: "vaccination"`

## กราฟน้ำหนัก

```json
{ "pet_id": "pet_...", "unit": "kg", "points": [ { "recorded_at": "...", "weight_kg": 4.2, "consultation_id": "con_..." } ] }
```

อ่านจากน้ำหนักที่บันทึกใน vitals ของแต่ละการตรวจ (`POST /staff/consultations/{id}/vitals`) ไม่มีตารางน้ำหนักแยก จุดสุดท้ายของกราฟจึงตรงกับ "น้ำหนักล่าสุด" บนหน้าสัตว์เสมอ เรียงเก่าไปใหม่ ไม่เกิน 400 จุด กรองช่วงด้วย `?from=&to=` ได้

## แล็บ

**ส่งตรวจ**

```http
POST /v1/staff/lab-requests
{
  "pet_id": "pet_...",
  "consultation_id": "con_...",
  "destination": "in_house",
  "priority": "urgent",
  "tests": [ { "name": "CBC" }, { "name": "Chemistry", "code": "CHEM10" } ]
}
```

- `destination` เป็น `in_house` หรือ `external` แล็บภายนอกต้องใส่ `external_lab_name`
- สาขาของคำขอมาจากการตรวจที่อ้างถึง ถ้าไม่มีการตรวจ ใช้ `branch_id` หรือสาขาเดียวของผู้เรียก
- คำขอแล็บเป็นเรกคอร์ดของตัวเอง ไม่ใช่ส่วนของเคส เพราะผลมักมาหลังปิดเคสและลงนามเวชระเบียนไปแล้ว

**ลงผล**

```http
POST /v1/staff/lab-requests/lab_.../results
{
  "results": [
    { "analyte": "ALT", "value": 180, "unit": "U/L", "reference_low": 10, "reference_high": 100 },
    { "analyte": "Heartworm Ag", "value_text": "positive", "abnormal": true }
  ]
}
```

- ค่าตัวเลขที่มีช่วงอ้างอิง ระบบคิดธงเอง ต่ำกว่าช่วงเป็น `low` สูงกว่าเป็น `high` อยู่ในช่วงเป็น `normal`
- ค่าที่เป็นข้อความใช้ `abnormal` ที่ผู้ส่งบอก ถ้าไม่บอกจะไม่มีธง
- ลงผลซ้ำได้ เช่น CBC วันนี้ เคมีพรุ่งนี้ ผลใหม่ต่อท้ายผลเดิม

**รายการงาน** มีสามสถานะ

| สถานะ | แปลว่า |
|---|---|
| `pending` | รอผลจากแล็บ |
| `resulted` | ผลมาแล้ว ยังไม่มีหมออ่าน |
| `reviewed` | หมออ่านแล้ว |

`GET /staff/lab-requests?status=pending&requested_by=me` คืองานค้างของหมอคนที่เรียก รายการจำกัดตามสาขาของผู้เรียกเสมอ ถ้าส่ง `pet_id` การเปิดดูถูกเขียนลง access log (`pet.lab_results_viewed`)

**กดว่าอ่านแล้ว** `POST /staff/lab-requests/{id}/review` เป็นส่วนขยาย ([contract-extensions.md § 11](../contract-extensions.md)) บันทึกว่าใครอ่านเมื่อไร ถ้ามีผลเข้ามาเพิ่มหลังอ่าน คำขอกลับเป็น `resulted` อีกครั้ง เพราะค่าที่เพิ่งเข้ามายังไม่มีใครเห็น คำขอที่ยังไม่มีผลกดอ่านไม่ได้ (`409`)

## ประวัติยาและการจ่ายยา

`GET /staff/prescriptions?pet_id=` คืนยาทุกตัวที่สัตว์เคยได้ ใหม่ไปเก่า แต่ละรายการมี

- `active` ยังอยู่ในช่วงกินยาหรือไม่
- `course_ends_at` วันที่ยาหมดตามจำนวนวัน
- `duplicate_of` ใบสั่งยาอื่นของยาตัวเดียวกันที่ช่วงกินยาทับกัน คือ "ตรวจซ้ำซ้อน"

ยาตัวเดียวกันดูจากคำแรกของชื่อ "Amoxicillin 250mg" กับ "amoxicillin 500 mg" จึงเป็นยาเดียวกัน แต่ชื่อการค้าสองชื่อของยาตัวเดียวกันระบบไม่รู้ เหมือนการตรวจแพ้ยา ถ้าไม่ส่ง `pet_id` จะได้รายการของทั้งคลินิกในสาขาของผู้เรียก ใช้ `?dispensed=false` เป็นงานค้างของห้องยา

**จ่ายยา** `POST /staff/prescriptions/{id}/dispense` ไม่มี body

```json
{
  "prescription": { "id": "rx_...", "dispensed_at": "...", "...": "..." },
  "label": {
    "clinic_name": "Happy Paws Clinic", "pet_name": "ลูน่า", "hospital_number": "HN-2026-0042",
    "owner_name": "คุณสมใจ", "drug_name": "Meloxicam", "dose": "0.5 ml", "frequency": "วันละครั้ง",
    "duration_days": 3, "quantity": 3, "prescribed_by": "สพ.ญ. มินท์", "prescriber_license_no": "ส.พ. 06120",
    "dispensed_on": "2026-10-02"
  },
  "stock_deducted": true,
  "stock": { "status": "deducted", "item_id": "itm_...", "item_name": "Meloxicam", "unit": "ml", "available": 40,
             "lots": [ { "lot_id": "lot_...", "lot_number": "M12", "expires_on": "2027-03-31", "quantity": 3 } ] },
  "charge": { "placement": "on_close", "unit_price_satang": 2000, "amount_satang": 6000 }
}
```

- `label` เป็นข้อมูล ไม่ใช่รูป เครื่องพิมพ์ฉลาก PDF และหน้าจอวางข้อความต่างกัน API จึงไม่เลือกให้
- **ตัดสต๊อกในจังหวะเดียวกัน** ใบสั่งยาที่หมอเลือกยาจากคลังมีรหัสรายการ ระบบใช้รายการนั้นเลยแม้ชื่อในใบจะต่าง ใบที่พิมพ์ชื่อเองไม่มีรหัส ระบบจึงหารายการในคลังที่ชื่อตรงกับชื่อยาทั้งชื่อ (ไม่สนตัวพิมพ์และช่องว่างซ้ำ) แล้วตัดจากล็อตของสาขาที่เปิดเคส ล็อตที่หมดอายุก่อนออกก่อน การบันทึกจ่าย การตัดสต๊อก และค่ายา อยู่ใน transaction เดียว
- **ไม่ปฏิเสธการจ่ายเพราะสต๊อก** ไม่พบยาในคลัง คือ `stock.status: not_matched` ไม่ตัดและไม่คิดเงิน ของในระบบไม่พอ คือ `insufficient` ไม่ตัด (ไม่ตัดบางส่วน) แต่คิดเงิน เพราะยาถูกจ่ายไปจริง ตัดได้คือ `deducted` และ `stock_deducted: true`
- **ค่ายาขึ้นบิลของเคส** ราคาขายของรายการ ณ ตอนจ่าย คูณจำนวน `charge.placement` บอกว่าไปไหน `on_close` (เคสยังเปิด ขึ้นบิลตอนปิดเคส) `invoiced` (ลงบิลของเคสที่ยังไม่รับเงิน) หรือ `separate_invoice` (บิลของเคสรับเงินแล้ว จึงออกบิลใหม่) ดู [สต๊อกยาและเวชภัณฑ์](inventory.md)
- ประวัติยาบอกผลการจ่ายด้วย `inventory_item_id`, `stock_deducted` และ `charged_unit_price_satang`
- จ่ายได้ครั้งเดียว ครั้งที่สองตอบ `409 state_conflict`
- การเขียนใบสั่งยา (`rx:write`) กับการจ่าย (`rx:dispense`) เป็นคนละสิทธิ์

**เลือกยาจากคลังตอนสั่งยา** เป็นส่วนขยาย ([contract-extensions.md § 20](../contract-extensions.md)) `POST /staff/consultations/{id}/prescriptions` รับ `inventory_item_id` ไม่บังคับ ได้มาจาก `GET /staff/prescriptions/drug-options?q=&branch=` (`rx:write`) ซึ่งคืนแค่รหัส ชื่อ หน่วย ราคาขาย จำนวนที่ใช้ได้ และสถานะ `ok` `low` `out` ของสาขาเดียว ไม่มีต้นทุนหรือล็อต สัตวแพทย์จึงไม่ต้องถือ `inventory:read`

- ไม่ส่ง `drug_name` ระบบใช้ชื่อของรายการ ส่งมาด้วยระบบเก็บชื่อที่พิมพ์ไว้บนฉลาก
- รหัสที่ไม่มีตอบ `404` เวชภัณฑ์ตอบ `422`
- การตรวจแพ้ยาเทียบทั้งชื่อที่พิมพ์และชื่อของรายการ ขั้นตอน `409 allergy_conflict` และการยืนยันเหตุผลเหมือนเดิม
- ใบสั่งยาในคำตอบของเคสมี `inventory_item_id` เมื่อผูกกับรายการ คำขอที่ไม่มีรหัสทำงานเหมือนเดิมทุกอย่าง

## เทมเพลตแผนรักษา

`GET /staff/templates/treatments?species=&q=` คืนเทมเพลตที่ใช้อยู่ เรียงตามชื่อ แต่ละอันมีหัตถการ ยา ข้อความแผนการรักษา และถ้าเป็นคอร์ส จะบอกจำนวนครั้งกับระยะห่าง

- `species` คืนเทมเพลตของชนิดนั้นรวมกับเทมเพลตที่ใช้ได้ทุกชนิด
- เทมเพลตไม่เก็บราคา หัตถการที่ชี้บริการใช้ราคาของบริการในวันที่ใช้
- contract มีแค่ `GET` ตอนนี้ใส่เทมเพลตด้วยคำสั่ง `seed-treatment-templates` ซึ่งใส่ตัวอย่างหกแบบ เช่น ทำหมันแมวเพศเมีย วัคซีนลูกสุนัข 3 เข็ม คอร์สเคมีบำบัดทุก 3 สัปดาห์ ขนาดยาในตัวอย่างเขียนว่า "ตามน้ำหนักตัว" เพราะเป็นข้อมูลตัวอย่าง ไม่ใช่คำแนะนำทางการรักษา

```sh
dotnet run --project backend/src/HappyPaws.Api -- seed-treatment-templates
```

## ไฟล์แนบในแฟ้มสัตว์

ทำสองขั้นแบบ pre-signed URL

**ขั้นที่ 1** บอกว่าจะส่งไฟล์อะไร

```http
POST /v1/staff/pets/pet_.../documents
{ "file_name": "xray.png", "content_type": "image/png", "size_bytes": 482133, "kind": "imaging", "visible_to_owner": true }
```

ได้ `document` และ `upload` ที่มี `url`, `method` (`PUT`), `headers` และ `expires_at`

**ขั้นที่ 2** ส่งไฟล์ไปที่ `upload.url` ด้วย `PUT` และ `Content-Type` ตามที่ประกาศ ได้ `204` เมื่อรับไฟล์แล้ว

| กติกา | ค่า |
|---|---|
| ชนิดไฟล์ | JPEG PNG WEBP HEIC และ PDF |
| ขนาด | ไม่เกิน 10 MB และไม่เกินขนาดที่ประกาศในขั้นที่ 1 |
| อายุ URL อัปโหลด | 15 นาที |
| อายุลิงก์ดาวน์โหลด | 10 นาที |
| `kind` | `photo`, `lab_result`, `referral`, `imaging`, `other` |
| ให้เจ้าของเห็น | `visible_to_owner` ปิดไว้ก่อน |

URL ที่ผิด หมดอายุ ชนิดไม่ตรง หรือไฟล์ใหญ่เกิน ตอบ `403` เอกสารที่ไฟล์ยังไม่มาไม่มีลิงก์ดาวน์โหลด และไม่แสดงให้เจ้าของเห็น ที่เก็บไฟล์ตอนนี้เป็นดิสก์ของเครื่องที่รัน API ดู [contract-extensions.md § 12](../contract-extensions.md)

## เอกสาร PDF

```http
POST /v1/staff/documents/generate
{ "type": "referral_letter", "pet_id": "pet_...", "refer_to": "โรงพยาบาลสัตว์...", "reason": "ส่งตรวจ CT" }
```

ตอบเป็นไฟล์ PDF (`application/pdf`) ตรง ๆ ระบบไม่เก็บไฟล์ไว้

| `type` | เนื้อหา | ต้องมี |
|---|---|---|
| `health_certificate` | ข้อมูลสัตว์ น้ำหนักล่าสุด ข้อความรับรอง วัคซีนที่ยังคุ้มกัน การแพ้ยา | ผู้ออกมีเลขใบอนุญาต |
| `referral_letter` | ปลายทาง เหตุผล การตรวจครั้งล่าสุด วินิจฉัย การรักษา ยา การแพ้ยา แผนต่อ | ผู้ออกมีเลขใบอนุญาต และสัตว์มีเวชระเบียนที่ลงนามแล้ว |
| `vaccination_book` | ประวัติการฉีดทุกเข็มพร้อมล็อตและวันครบกำหนด | ไม่มีเงื่อนไขเพิ่ม |

- ไม่ส่ง `consultation_id` ระบบใช้เวชระเบียนล่าสุดของสัตว์ ใบส่งตัวที่สัตว์ยังไม่เคยมีเวชระเบียนตอบ `422`
- ชื่อผู้ออกถูกพิมพ์เป็นผู้ลงนาม ใบรับรองและใบส่งตัวจึงออกได้เฉพาะคนที่มีเลขใบอนุญาตในข้อมูลพนักงาน ไม่อย่างนั้นได้ `403`
- `remarks` แทนข้อความรับรองตั้งต้นของใบรับรองสุขภาพได้
- ทุกครั้งที่พิมพ์เขียน `pet.document_generated` ลง access log
- วันที่พิมพ์เป็นภาษาไทยแบบปีพุทธศักราช ฟอนต์ Sarabun (สัญญาอนุญาต SIL Open Font License) ฝังอยู่ในไฟล์ จึงอ่านได้เหมือนกันทุกเครื่อง
- ใช้ไลบรารี QuestPDF ด้วยสัญญาอนุญาต Community ซึ่งใช้ได้ฟรีกับองค์กรที่รายได้ต่ำกว่าเกณฑ์ของผู้พัฒนา ถ้าคลินิกเกินเกณฑ์ต้องซื้อสัญญาอนุญาต

## หน้าจอพนักงาน

ทุกปุ่มและฟอร์มขึ้นตาม scope ของบัญชี การซ่อนเป็นเรื่อง UX เท่านั้น backend ตรวจซ้ำทุกคำขอ

**หน้าสัตว์** `/pets/<id>`

- **บันทึกวัคซีน** ฟอร์มท้ายการ์ดวัคซีน ต้องถือ `record:write` และสัตว์ยังมีชีวิต
- **แผนการรักษาหลายครั้ง** การ์ดในคอลัมน์ซ้าย ต้องถือ `consult:write` เลือกบริการ หมอ และวันของแต่ละครั้ง บันทึกแล้วแสดงนัดที่จองให้ทันที บริการมาจาก `GET /services` จึงเลือกได้เฉพาะบริการที่เปิดจองออนไลน์
- **เอกสาร** การ์ดในคอลัมน์ขวา
  - อัปโหลดไฟล์ (ต้องถือ `record:write`) เบราว์เซอร์ส่งไฟล์ตรงไปที่ URL ที่เซ็นแล้ว แบบเดียวกับ portal
  - พิมพ์ PDF สมุดวัคซีน ใบรับรองสุขภาพ และใบส่งตัว (ต้องถือ `record:read`) ไฟล์ดาวน์โหลดในเบราว์เซอร์ทันที
- ปุ่ม **ประวัติยาและจ่ายยา** บนหัวหน้า (ต้องถือ `rx:read`) เปิดหน้าจ่ายยาของสัตว์ตัวนั้น

**แล็บ** `/lab` เมนู **แล็บ** เห็นเมื่อถือ `lab:read` หรือ `lab:write`

- แท็บสถานะ รอผล ผลยังไม่มีคนอ่าน อ่านแล้ว และทั้งหมด กับตัวเลือก "ที่ฉันส่ง" เฉพาะสาขาที่เลือกไว้
- บัญชีที่ลงผลได้ (`lab:write`) เปิดมาที่แท็บรอผล หมอเปิดมาที่แท็บผลยังไม่มีคนอ่าน
- **ลงผล** (`lab:write`) ใส่ได้หลายแถว ชื่อค่า ค่า หน่วย ช่วงปกติ และติ๊กผิดปกติ ค่าที่เป็นตัวเลขส่งเป็น `value` ที่เหลือส่งเป็น `value_text`
- **อ่านผลแล้ว** (`lab:read`) ขึ้นเมื่อมีผลที่ยังไม่มีคนอ่าน
- **หน้าตรวจ** มีส่วน **ส่งตรวจแล็บ** ใต้การวินิจฉัย สั่งตรวจได้ระหว่างเคสยังเปิดและถือ `lab:request` เลือกตรวจในคลินิกหรือส่งแล็บนอก (ต้องใส่ชื่อแล็บ) และติ๊กด่วนได้ รายการคำขอของเคสยังแสดงหลังปิดเคส เพราะผลมักมาทีหลัง

**จ่ายยา** `/prescriptions` เมนู **จ่ายยา** เห็นเมื่อถือ `rx:dispense`

- ยังไม่เลือกสัตว์ หน้าแสดงช่องค้นหาสัตว์ และรายการ **รอจ่ายยา** ของสาขาที่ใช้ได้ (`GET /staff/prescriptions?dispensed=false`)
- เลือกสัตว์แล้ว (`?pet_id=`) แสดงการแพ้ยา แล้วประวัติยา โดยใบที่ยังไม่จ่ายขึ้นก่อน แต่ละใบมีป้าย รอจ่าย จ่ายแล้ว กำลังใช้ยา และยาซ้ำซ้อน ใบที่ซ้ำมีกล่องแดงบอกว่าซ้ำกับใบไหน
- กดจ่ายสองครั้ง ครั้งที่สองเป็นการยืนยัน และเตือนอีกรอบถ้ายาซ้ำ จ่ายแล้วแสดงฉลากพร้อมปุ่ม **พิมพ์ฉลาก** ตอนพิมพ์มีแค่ฉลากบนกระดาษ
- ใบที่รอจ่ายมีป้าย **ผูกกับสต๊อก** หรือ **ชื่อพิมพ์เอง จับคู่ด้วยชื่อตอนจ่าย**
- หลังจ่าย หน้าบอกว่าตัดสต๊อกไปกี่หน่วย หรือทำไมไม่ได้ตัด และค่ายาไปอยู่บิลไหน ใบที่จ่ายแล้วแต่ไม่ได้ตัดสต๊อกมีป้าย **ไม่ได้ตัดสต๊อก** ในประวัติยา

**สั่งยาในหน้าตรวจ** ช่องชื่อยาค้นยาในคลังของสาขาของเคสขณะพิมพ์ แต่ละตัวมีป้าย มีของ ใกล้หมด หรือหมด พร้อมจำนวนที่เหลือและราคาต่อหน่วย เลือกด้วยเมาส์หรือลูกศรกับ Enter แล้วชื่อถูกเติมให้และช่องจำนวนบอกหน่วย พิมพ์ชื่อเองก็ได้ ใต้ช่องขึ้นว่า "ไม่ผูกกับสต๊อก ตอนจ่ายจะจับคู่ด้วยชื่อ ถ้าชื่อไม่ตรงกับในคลังทุกตัวอักษร จะไม่ตัดสต๊อกและไม่คิดเงิน" พิมพ์ทับชื่อที่เลือกไว้คือยกเลิกการผูก รายการยาของเคสมีป้าย **ผูกกับสต๊อก** หรือ **ไม่ผูกกับสต๊อก** ทุกแถว

**เทมเพลต** ฟอร์มการรักษาในหน้าตรวจมีปุ่ม **เลือกจากเทมเพลต** โหลดเทมเพลตครั้งแรกที่เปิด เลือกแล้วดูหัตถการ ยา และแผนได้ ปุ่ม **ใส่ในฟอร์ม** เติมชื่อ จำนวน และหมายเหตุ แล้วพาไปช่องราคา ไม่มีอะไรถูกบันทึกจนกว่าจะกด **เพิ่ม**

## ข้อจำกัดที่รู้อยู่

- ไม่มี endpoint อ่านแผนการรักษาและรายการเอกสารของสัตว์ฝั่งพนักงาน หน้าสัตว์จึงเห็นแผนเฉพาะตอนเพิ่งสร้าง นัดของแผนดูได้ในปฏิทินนัด
- อัปโหลดจากเบราว์เซอร์ต้องใส่ origin ของหน้าเว็บใน `Cors:AllowedOrigins` ของ API ตอนนี้ตั้งไว้เฉพาะ `http://localhost:3000` ถ้าเปิดหน้าเว็บที่ port อื่น อัปโหลดจะไม่สำเร็จ และเหลือแถวเอกสารที่ไม่มีไฟล์
- ผลแล็บรับแค่ค่า ไม่รับไฟล์แนบ ใบรายงานจากแล็บให้อัปโหลดเป็นเอกสารของสัตว์แบบ `lab_result` ยกเลิกคำขอหรือแก้ผลที่บันทึกแล้วไม่ได้ เพราะไม่มี endpoint
- คำขอแล็บและใบสั่งยามีแค่รหัสพนักงาน หน้าจอจึงไม่แสดงชื่อคนส่งตรวจหรือคนอ่านผล
- คำตอบของหน้าตรวจไม่มีคำขอแล็บ หน้าตรวจจึงอ่าน `GET /staff/lab-requests?pet_id=` แล้วเลือกเฉพาะของเคสนั้น ทุกครั้งที่เปิดหน้าตรวจด้วยบัญชีที่ถือ `lab:read` จึงมีบรรทัด `pet.lab_results_viewed` ใน access log
- เทมเพลตไม่มีราคา หมอพิมพ์ราคาเอง และยาในเทมเพลตยังเติมลงใบสั่งยาไม่ได้
- ยาที่หมอพิมพ์ชื่อเองยังผูกกับรายการในคลังด้วยชื่อเท่านั้น ชื่อต้องตรงกับชื่อรายการ ไม่อย่างนั้นยาจ่ายได้แต่ไม่ตัดสต๊อกและไม่คิดเงิน ยาที่เลือกจากคลังไม่มีปัญหานี้
- การเตือนวัคซีนและรอบแผนการรักษาทำแล้วใน [เตือนอัตโนมัติ](automation.md) แต่กติกาเริ่มต้นปิดไว้ คลินิกต้องเปิดใช้เอง
- การตรวจยาซ้ำซ้อนดูจากชื่อยา ไม่รู้จักกลุ่มยาหรือชื่อการค้า
- สร้างหรือแก้เทมเพลตผ่าน API ไม่ได้
- ไฟล์อยู่บนดิสก์ของ API ไม่ได้เข้ารหัส และยังไม่มีการลบไฟล์ของเอกสารที่ไม่เคยอัปโหลดเสร็จ

## โค้ด

| ส่วน | ไฟล์ |
|---|---|
| วัคซีน แผนการรักษา ไฟล์แนบ | `backend/src/HappyPaws.Domain/Patients/Vaccination.cs`, `TreatmentPlan.cs`, `PetDocument.cs` |
| แล็บ | `backend/src/HappyPaws.Domain/Laboratory/`, `backend/src/HappyPaws.Application/Laboratory/` |
| ยา | `backend/src/HappyPaws.Domain/Prescriptions/Prescription.cs`, `backend/src/HappyPaws.Application/Prescriptions/` |
| เทมเพลต | `backend/src/HappyPaws.Domain/Consultations/TreatmentTemplate.cs`, `backend/src/HappyPaws.Application/Consultations/ListTreatmentTemplates/`, `SeedTreatmentTemplates/` |
| use case ฝั่งสัตว์ | `backend/src/HappyPaws.Application/Patients/` (`ListVaccinations`, `RecordVaccination`, `GetWeightHistory`, `Documents`) |
| PDF | `backend/src/HappyPaws.Application/MedicalRecords/GenerateDocument/`, `backend/src/HappyPaws.Infrastructure/Documents/QuestPdfRenderer.cs` |
| ที่เก็บไฟล์ | `backend/src/HappyPaws.Infrastructure/Storage/LocalFileStorage.cs`, `backend/src/HappyPaws.Api/Controllers/Storage/StorageController.cs` |
| endpoint | `backend/src/HappyPaws.Api/Controllers/Staff/` (`PetVaccinationsController`, `PetWeightsController`, `LabRequestsController`, `LabResultsController`, `PrescriptionsController`, `PrescriptionDispenseController`, `TreatmentTemplatesController`, `PetDocumentsController`, `DocumentGenerationController`) |
| หน้าสัตว์ | `frontend/src/app/(staff)/pets/[id]/page.tsx`, `frontend/src/features/pets/` (`components/profile/`, `actions/`) |
| หน้าแล็บ | `frontend/src/app/(staff)/lab/page.tsx`, `frontend/src/features/laboratory/` |
| หน้าจ่ายยาและเทมเพลต | `frontend/src/app/(staff)/prescriptions/page.tsx`, `frontend/src/features/prescriptions/`, `frontend/src/features/consultations/components/treatments/template-picker.tsx` |
| เลือกยาจากคลังตอนสั่งยา (ส่วนขยาย § 20) | `backend/src/HappyPaws.Application/Prescriptions/ListDrugOptions/`, `backend/src/HappyPaws.Application/Consultations/AddPrescription/`, `frontend/src/features/consultations/components/prescriptions/` (`drug-search-field.tsx`, `drug-suggestions.tsx`, `stock-link-badge.tsx`), `frontend/src/features/prescriptions/api/listDrugOptions.ts` |
| เทสต์ | `backend/tests/HappyPaws.IntegrationTests/Api/Patients/VaccinationAndWeightTests.cs`, `DocumentUploadTests.cs`, `Api/Laboratory/LabRequestTests.cs`, `Api/Prescriptions/PharmacyTests.cs`, `DispensingStockTests.cs`, `DrugPickerTests.cs`, `Api/MedicalRecords/DocumentGenerationTests.cs`, `backend/tests/HappyPaws.UnitTests/Domain/Patients/ClinicalRecordsTests.cs` |
