# การเชื่อมต่อระบบ

หมวด "Webhooks และ Integrations" ของ contract มีหกเส้น ให้ระบบอื่นรู้เมื่อเกิดเหตุการณ์ในคลินิก (webhook) ให้เครื่องเรียก API ได้เอง (API key) เก็บค่าตั้งของผู้ให้บริการชำระเงินและแล็บภายนอก และให้พนักงานดูนัดในแอปปฏิทินของตัวเอง

contract ให้แค่ method และ path ไม่มีรูปร่างคำขอ คำตอบ หรือ scope ทุกอย่างในหน้านี้จึงเป็นการตีความ การเลือก scope อยู่ใน [assumptions.md § 2](../assumptions.md)

## endpoint

| Method | Endpoint | สิทธิ์ | ทำอะไร |
|---|---|---|---|
| POST | `/staff/webhooks` | `integration:manage` | สร้าง webhook ขาออก ได้ secret สำหรับตรวจลายเซ็นครั้งเดียว |
| GET | `/staff/webhooks/{id}/deliveries` | `integration:manage` | ประวัติการส่งของ webhook หนึ่งตัว ใหม่สุดก่อน แบ่งหน้าด้วย cursor |
| POST | `/staff/api-keys` | `integration:manage` | ออก API key ให้ระบบอื่น ได้ key ครั้งเดียว |
| PUT | `/staff/integrations/payment` | `integration:manage` | ค่าตั้งผู้ให้บริการชำระเงิน (เก็บอย่างเดียว) |
| PUT | `/staff/integrations/lab` | `integration:manage` | ค่าตั้งแล็บภายนอก (เก็บอย่างเดียว) |
| GET | `/staff/integrations/calendar/ics` | พนักงานทุกคน หรือ token ในลิงก์ | ฟีดปฏิทินนัด หรือลิงก์สำหรับ subscribe |

## Webhook

### สร้าง

```json
POST /staff/webhooks
{ "url": "https://example.com/hooks/clinic", "events": ["appointment.created", "invoice.issued"], "description": "ระบบบัญชี" }
```

ตอบ `201`

```json
{
  "id": "whk_01K...",
  "url": "https://example.com/hooks/clinic",
  "events": ["appointment.created", "invoice.issued"],
  "description": "ระบบบัญชี",
  "active": true,
  "secret": "whsec_...",
  "signing": { "algorithm": "HMAC-SHA256", "header": "X-Signature", "timestamp_header": "X-Webhook-Timestamp", "signed_payload": "<timestamp>.<body>" },
  "created_at": "2026-10-09T03:00:00Z"
}
```

- `secret` แสดงในคำตอบนี้ครั้งเดียว ระบบเก็บแบบเข้ารหัส ไม่มี endpoint ไหนคืนค่านี้อีก คำตอบมี `Cache-Control: no-store`
- `events` ต้องเลือกจากรายการ 16 ตัวเท่านั้น ไม่มีค่า "ทุกเหตุการณ์" เพราะ event ที่เพิ่มในรุ่นหลังอาจพาข้อมูลที่ผู้รับไม่ควรได้ ผู้รับต้องเลือกเอง
- URL ต้องเป็น `https://` ไม่มีชื่อผู้ใช้หรือรหัสผ่าน ไม่มี `#` และไม่ใช่ที่อยู่ภายใน เช่น `localhost`, `10.x.x.x`, `169.254.169.254` หรือชื่อที่ลงท้าย `.local` และ `.internal` ผิดได้ `422` ใน Development ใช้ `http://localhost` ได้
- audit `integration.webhook_created` เก็บปลายทาง (ไม่รวม query string เพราะอาจมี token ของผู้รับ) และรายการ event ไม่เก็บ secret

### ส่งอย่างไร

```text
ธุรกรรมทางธุรกิจ (เช่น ออกบิล)
  -> เขียน webhook_events (outbox) ใน transaction เดียวกัน
  -> commit
งานเบื้องหลัง webhooks (ทุก 10 วินาที)
  -> แตก event เป็น webhook_deliveries หนึ่งแถวต่อ webhook ที่รับ event ชนิดนั้น
  -> จองแถวที่ถึงเวลา ส่ง แล้วบันทึกผล
```

**outbox** use case เรียก `IWebhookOutbox.Enqueue` ภายใน transaction ของตัวเอง ซึ่งแค่เพิ่มแถวใน `webhook_events` ไม่ส่งอะไร ถ้า transaction ย้อนกลับ event ก็หายไปด้วย ถ้า commit แล้ว event อยู่บนดิสก์ก่อนคำตอบออกไป process ตายหลังจากนั้นก็ไม่หาย การส่งจริงไม่เกิดใน transaction เลย ผู้รับที่ช้าจึงไม่ถือ row lock ของบิล

**แตกเป็น delivery** งานเบื้องหลังล็อกแถวที่ยังไม่แตกด้วย `FOR UPDATE SKIP LOCKED` สร้าง delivery ให้ webhook ที่เปิดอยู่ รับ event ชนิดนั้น และสร้างก่อนเหตุการณ์เกิด แล้วติดป้ายว่าแตกแล้ว ทั้งหมดใน transaction เดียว unique `(event_id, subscription_id)` กันซ้ำอีกชั้น

**ส่ง** จอง delivery ที่ถึงเวลาทีละแถวด้วย `FOR UPDATE SKIP LOCKED` เลื่อนเวลาครบกำหนดออกไปเป็น lease (timeout บวกหนึ่งนาที) แล้ว commit จากนั้นส่งโดยไม่ถือ lock แล้วบันทึกผลใน transaction สั้นอีกอัน หลาย instance จึงไม่ส่งแถวเดียวกันซ้ำ instance ที่ตายระหว่างส่งทิ้งแถวที่กลับมาถึงเวลาเมื่อ lease หมด การส่งจึงเป็นแบบอย่างน้อยหนึ่งครั้ง ผู้รับควรกันซ้ำด้วย `X-Webhook-Id`

**ลองใหม่** contract บอก "retry 5 ครั้ง แบบ exponential ภายใน 24 ชม." อ่านว่าส่งครั้งแรกหนึ่งครั้ง แล้วลองใหม่อีกห้าครั้ง แต่ละช่วงยาวกว่าช่วงก่อนห้าเท่า

| ครั้งที่ | รอหลังครั้งก่อน | นับจากเหตุการณ์โดยประมาณ |
|---|---|---|
| 1 | ทันที | 0 |
| 2 | 1 นาที | 1 นาที |
| 3 | 5 นาที | 6 นาที |
| 4 | 25 นาที | 31 นาที |
| 5 | 125 นาที | 2 ชั่วโมง 36 นาที |
| 6 | 625 นาที | 13 ชั่วโมง |

ไม่สำเร็จครบหกครั้ง delivery เป็น `failed` และไม่ส่งอีก สำเร็จคือได้ 2xx อย่างอื่นนับเป็นไม่สำเร็จทั้งหมด รวม 3xx และ 4xx ไม่มี jitter เพื่อให้ผู้รับคาดเวลาได้

### สิ่งที่ผู้รับได้

```http
POST /hooks/clinic
Content-Type: application/json; charset=utf-8
User-Agent: HappyPaws-Webhooks/1.0
X-Webhook-Id: evt_01K...
X-Webhook-Event: invoice.issued
X-Webhook-Timestamp: 1791514800
X-Signature: sha256=5f2c...

{"id":"evt_01K...","type":"invoice.issued","occurred_at":"2026-10-09T03:00:00Z","data":{...}}
```

**ตรวจลายเซ็น** คำนวณ HMAC-SHA256 ด้วย secret ของ webhook บนข้อความ `<X-Webhook-Timestamp>.<body ทั้งก้อนตามที่ได้รับ>` เขียนเป็นเลขฐานสิบหกตัวเล็ก เติม `sha256=` ข้างหน้า แล้วเทียบกับ `X-Signature` แบบเวลาคงที่ ควรปฏิเสธ timestamp ที่เก่ากว่าห้านาทีด้วย เพื่อกันการนำคำขอที่ดักได้มาส่งซ้ำ body ของ event หนึ่งตัวเหมือนกันทุกครั้งที่ลองใหม่ เพราะเก็บเป็นข้อความตั้งแต่ตอนเขียน outbox

### event แต่ละตัวพาอะไรไป

ทุก event เป็นแบบบาง คือรหัสอ้างอิง เวลา สถานะ และยอดเงิน พอให้ผู้รับรู้ว่าเกิดอะไร แล้วดึงรายละเอียดผ่าน API ด้วย key ที่มี scope เหมาะสม ซึ่งที่นั่นมีการตรวจสิทธิ์ สาขา และ audit **ไม่มีข้อมูลสุขภาพ** (ผลวินิจฉัย อาการ ผลตรวจ ชื่อยา ขนาดยา เหตุผลของนัด บันทึก) และ **ไม่มีข้อมูลติดต่อ** (ชื่อ เบอร์ อีเมล ที่อยู่ของลูกค้า) ทั้งหมดอยู่ในไฟล์เดียว `Application/Integrations/Webhooks/WebhookPayloads.cs` เพื่อให้ทบทวนได้ในครั้งเดียว

| Event | `data` | ตั้งใจไม่ใส่ |
|---|---|---|
| `appointment.created`, `.rescheduled`, `.cancelled`, `.no_show`, `.checked_in` | `appointment_id`, `status`, `branch_id`, `customer_id`, `pet_id`, `service_id`, `staff_id`, `starts_at`, `ends_at` และ `queue_ticket_id` (เช็กอิน) `previous_starts_at` (เลื่อนนัด) | อาการที่แจ้ง เหตุผลที่ยกเลิก |
| `queue.ticket.called` | `ticket_id`, `number`, `branch_id`, `appointment_id`, `pet_id`, `customer_id`, `staff_id`, `room_id`, `called_at` | |
| `consultation.closed` | `consultation_id`, `branch_id`, `pet_id`, `customer_id`, `appointment_id`, `vet_staff_id`, `medical_record_id`, `closed_at` | อาการ ผลตรวจ วินิจฉัย การรักษา |
| `record.signed` | `record_id`, `consultation_id`, `branch_id`, `pet_id`, `customer_id`, `signed_by_staff_id`, `signed_at` | เนื้อหาเวชระเบียน hash ของเนื้อหา |
| `lab.result.ready` | `lab_request_id`, `branch_id`, `pet_id`, `consultation_id`, `resulted_at` | ชื่อการตรวจ ค่าผล ความผิดปกติ |
| `prescription.dispensed` | `prescription_id`, `consultation_id`, `pet_id`, `branch_id`, `dispensed_at` | ชื่อยา ขนาด จำนวน วิธีใช้ |
| `invoice.issued` | `invoice_id`, `number`, `branch_id`, `customer_id`, `pet_id`, `consultation_id`, `status`, `total_satang`, `balance_satang`, `issued_at` | รายการในบิล (มีชื่อยาและหัตถการ) |
| `payment.succeeded` | `payment_id`, `invoice_id`, `invoice_number`, `branch_id`, `customer_id`, `method`, `amount_satang`, `invoice_status`, `balance_satang`, `collected_at` | เลขสลิป |
| `payment.refunded` | `refund_id`, `invoice_id`, `invoice_number`, `branch_id`, `customer_id`, `payment_id`, `method`, `amount_satang`, `invoice_status`, `refunded_at` | เหตุผลที่คืนเงิน |
| `inventory.low_stock` | `item_id`, `branch_id`, `available`, `reorder_level` | |
| `customer.created` | `customer_id`, `created_at` | ชื่อ เบอร์ อีเมล |
| `pet.created` | `pet_id`, `customer_id`, `species`, `created_at` | ชื่อสัตว์ สายพันธุ์ ไมโครชิป |

**จุดที่ยิง event**

- นัด: จองจาก portal จองที่เคาน์เตอร์ นัดเป็นคอร์ส (หนึ่ง event ต่อนัด) นัดติดตามผลตอนปิดเคส เลื่อน ยกเลิก (รวมนัดที่ถูกยกเลิกเพราะสัตว์เสียชีวิต) ไม่มาตามนัด เช็กอิน
- `invoice.issued` ทุกทางที่ออกบิล คือออกเอง ปิดเคส ค่ายาที่จ่ายหลังบิลปิดยอด และขายแพ็กเกจ
- `inventory.low_stock` ยิงครั้งเดียวเมื่อยอดใช้ได้ของรายการในสาขาลดจากเหนือจุดสั่งซื้อลงไปถึงหรือต่ำกว่า จากการจ่ายยา จ่ายออก โอนออก หรือปรับยอดลง ระหว่างที่ยังต่ำอยู่ไม่ยิงซ้ำ รับเข้าจนพ้นจุดสั่งซื้อแล้วจึงยิงได้อีก
- `customer.created` ทั้งลงทะเบียนที่เคาน์เตอร์และลูกค้าสมัครเอง

### ป้องกัน SSRF

URL มาจากคนพิมพ์ แต่เซิร์ฟเวอร์เป็นผู้เรียก จากในเครือข่ายของคลินิก ถ้าไม่กัน ใครที่สร้าง webhook ได้ก็สั่งให้เซิร์ฟเวอร์ยิงเข้าเครื่องภายในหรือบริการ metadata ของ cloud ได้ ระบบกันไว้หลายชั้น

1. **ตอนสร้าง** ตรวจรูปแบบ URL ตามที่บอกข้างบน
2. **ตอนส่ง ก่อนสร้างคำขอ** resolve ชื่อโฮสต์ ถ้าที่อยู่ **ใดก็ได้** ที่ได้มาไม่ใช่ที่อยู่สาธารณะ ปฏิเสธทั้งหมด (`address_blocked`) ช่วงที่ปฏิเสธ คือ loopback, `10/8`, `172.16/12`, `192.168/16`, `100.64/10`, `169.254/16` (รวม metadata), `0/8`, `192.0.0/24`, `198.18/15`, multicast และช่วงสงวน ฝั่ง IPv6 คือ `::1`, `::`, `fc00::/7` (รวม metadata ของ AWS), `fe80::/10`, site-local, multicast และ IPv4 ที่ซ่อนใน IPv6 (`::ffff:a.b.c.d`, NAT64 `64:ff9b::/96`) ซึ่งตรวจตามกติกา IPv4
3. **ตอนต่อ socket** handler ของ HTTP client resolve และตรวจซ้ำใน connect callback แล้วต่อไปยังที่อยู่ที่เพิ่งตรวจ ชื่อที่ตอบที่อยู่สาธารณะในครั้งแรกแล้วเปลี่ยนเป็นภายในในครั้งที่สอง (DNS rebinding) จึงไม่ผ่าน
4. **ไม่ตาม redirect** คำตอบ 3xx ถูกบันทึกเป็นความล้มเหลว `redirect_not_followed` ไม่ตาม `Location`
5. **ไม่ใช้ proxy** proxy จะต่อแทนเราไปยังที่อยู่ที่เราไม่ได้ตรวจ
6. **timeout** ทั้งครั้งไม่เกิน `Webhooks:TimeoutSeconds` (ค่าเริ่มต้น 10 วินาที สูงสุด 30) ทั้งต่อ socket และรอคำตอบ
7. **อ่านคำตอบจำกัด** อ่านไม่เกิน 4 KB เก็บแค่ 1,000 ตัวอักษรแรกไว้ในประวัติ หน้าเว็บแสดงเป็นข้อความธรรมดา ไม่ตีความเป็น HTML

ใน Development `http://localhost` และ loopback ใช้ได้ (ปิดได้ด้วย `Webhooks:AllowLocalhostInDevelopment=false`) ช่วงส่วนตัวและ metadata ปฏิเสธทุก environment

### ประวัติการส่ง

`GET /staff/webhooks/{id}/deliveries?limit=25&cursor=` หนึ่งแถวต่อ event

```json
{ "data": [ {
  "id": "whd_...", "event_id": "evt_...", "event_type": "invoice.issued",
  "status": "pending", "attempts": 2, "max_attempts": 6,
  "next_attempt_at": "...", "last_attempt_at": "...", "last_status_code": 503,
  "last_error": "http_503", "last_response_body": "down for maintenance", "last_duration_ms": 120,
  "created_at": "..."
} ], "page": { "next_cursor": "..." } }
```

`last_error` เป็นรหัสสั้น คือ `http_<code>`, `timeout`, `redirect_not_followed`, `address_blocked`, `connection_failed` และ `secret_unavailable` (กุญแจ Data Protection ที่เข้ารหัส secret หายไป ดู [development.md](../development.md)) แต่ละแถวเก็บผลของครั้งล่าสุด ไม่ได้เก็บทุกครั้ง event และประวัติการส่งเก็บ 30 วัน แล้ว housekeeping ลบทิ้ง

## API key

### ออก key

```json
POST /staff/api-keys
{ "name": "ระบบบัญชี", "scopes": ["appointment:read", "customer:read"], "branch_ids": ["brn_..."], "expires_at": "2027-01-01T00:00:00+07:00" }
```

ตอบ `201`

```json
{ "id": "key_01K...", "name": "ระบบบัญชี", "key": "hp_k7m2qx9ab4_...", "prefix": "hp_k7m2qx9ab4",
  "scopes": ["appointment:read", "customer:read"], "branch_ids": ["brn_..."],
  "expires_at": "2027-01-01T00:00:00+07:00", "created_at": "..." }
```

- **แสดง key ครั้งเดียว** รูปแบบ `hp_<prefix 10 ตัว>_<ความลับ 256 bit>` ฐานข้อมูลเก็บแค่ prefix (ไว้ระบุตัว) และ SHA-256 ของ key ทั้งก้อน ไม่มีคอลัมน์ใดเก็บ key ได้ SHA-256 ไม่มี work factor เพียงพอเพราะ key สุ่ม 256 bit ไม่มีอะไรให้เดา เหมือน refresh token
- **scope** ต้องเป็น scope ในแค็ตตาล็อกที่ key ถือได้ และผู้สร้างถืออยู่ key ถือไม่ได้คือ scope ระดับ 2FA ทั้งหมด (`record:*`, `consult:write`, `rx:write`, `rx:dispense`, `payment:refund`, `billing:payout`, `settings:danger`, `user:manage`, `role:manage`) `integration:manage` (key ที่ออก key ได้หรือเปลี่ยน webhook ได้จะอยู่เหนือการปิดกั้นทุกอย่าง) `staff:manage` (key ที่เชิญพนักงานได้สร้างคนขึ้นมาเองได้) และ wildcard ทุกแบบ ผิดข้อแรกได้ `422` ผิดข้อหลังได้ `403 insufficient_scope` แบบเดียวกับการให้ role
- **สาขา** ผู้สร้างที่ผูกสาขาเลือกได้เฉพาะสาขาของตัวเอง ไม่เลือกหมายถึงทุกสาขาของตัวเอง ผู้สร้างที่ไม่ผูกสาขาเลือกสาขาใดก็ได้ หรือไม่เลือกเลยหมายถึงทุกสาขา
- **อายุ** บังคับทุก key ไม่ระบุได้ 90 วัน นานสุด 365 วัน เพราะ contract ไม่มี endpoint ยกเลิก key การหมดอายุจึงเป็นทางเดียวที่รับประกันว่า key ที่ถูกลืมจะหยุดทำงาน
- audit `integration.api_key_created` เก็บชื่อ prefix scope สาขา และวันหมดอายุ ไม่เก็บ key

### ใช้ key

```http
Authorization: Bearer hp_k7m2qx9ab4_...
```

header เดียวกับ access token ของคน ระบบเห็นคำนำหน้า `hp_` แล้วส่งให้ตัวตรวจ API key (`ApiKeyAuthenticationHandler`) แทน JWT ทุกคำขอ

1. หา key จาก prefix เทียบ hash แบบเวลาคงที่ ตรวจว่ายังไม่หมดอายุและไม่ถูกยกเลิก
2. ตรวจผู้สร้างใหม่ทุกครั้ง บัญชีต้องยังเข้าสู่ระบบได้ และสมาชิกพนักงานต้องยังใช้งานอยู่ ไม่อย่างนั้น `401`
3. scope ที่ใช้ได้จริงคือ scope ของ key ที่ผู้สร้างยังถืออยู่ **ตอนนี้** เอาสิทธิ์ออกจากคน สิทธิ์นั้นก็หายจาก key ของเขาด้วย
4. สาขาตัดด้วยสาขาปัจจุบันของผู้สร้างเช่นกัน ถ้าตัดแล้วไม่เหลือสาขาเลย key ถูกปฏิเสธ ไม่ปล่อยให้กลายเป็น "ทุกสาขา"
5. เขียน `last_used_at` ไม่เกินหนึ่งครั้งต่อนาที

key ทำหน้าที่แทนผู้สร้าง แถว audit จึงมีผู้สร้างเป็นผู้กระทำ และมี `via_api_key` ใน `details` บอกว่าเป็น key ไหน key **ใช้ได้เฉพาะ endpoint ที่ประกาศ `RequireScope`** เส้นที่ไม่มี scope เป็นของคน เช่น `/me`, `/me/sessions`, `/auth/logout` และเส้นที่ลูกค้ากับพนักงานใช้ร่วมกัน key ได้ `403` (`InteractiveCallerRequirement`) ไม่อย่างนั้น key ที่มีแค่ `appointment:read` ก็แก้โปรไฟล์หรือเพิกถอนเซสชันของผู้สร้างได้ ฟีดปฏิทินก็ปฏิเสธ key

**rate limit** key มีหน้าต่างของตัวเอง แยกจากผู้สร้าง `RateLimits:PerApiKeyPerMinute` ค่าเริ่มต้นเท่ากับ `RateLimits:PerTokenPerMinute` (600 ครั้งต่อนาทีตาม contract) งานที่วิ่งถี่ของ key จึงไม่ทำให้ผู้สร้างใช้หน้าเคาน์เตอร์ไม่ได้ และกลับกัน

## ผู้ให้บริการชำระเงินและแล็บภายนอก

**ไม่มีตัวเชื่อมต่อจริง** ระบบนี้ยังไม่มี payment gateway และไม่มีตัวเชื่อมแล็บ สองเส้นนี้เก็บค่าตั้งและตรวจรูปแบบเท่านั้น การเปิด `enabled` ยังไม่เปลี่ยนการทำงานใด ๆ การจ่ายด้วยบัตรยังหมายถึง "เครื่องรูดบัตรอนุมัติแล้ว" และผลแล็บยังบันทึกด้วยมือ คำตอบมี `connected: false` เสมอเพื่อบอกเรื่องนี้

```json
PUT /staff/integrations/payment
{ "provider": "opn", "mode": "test", "enabled": false, "public_key": "pkey_test_...", "secret_key": "skey_test_...", "webhook_secret": "..." }

PUT /staff/integrations/lab
{ "provider": "idexx", "enabled": false, "account_id": "TH-12345", "base_url": "https://api.lab.example", "api_key": "..." }
```

- ทุกช่องเลือกได้ ช่องที่ไม่ส่งคงค่าเดิม สตริงว่างคือลบค่า body ว่างไม่เขียนอะไรและคืนค่าปัจจุบัน เพราะ contract ไม่มี `GET` (แบบเดียวกับ LINE)
- **ความลับเขียนได้อย่างเดียว** `secret_key`, `webhook_secret`, `api_key` เข้ารหัสด้วย Data Protection ก่อนเก็บ และไม่เคยคืน คำตอบบอกแค่ `has_secret_key`, `has_webhook_secret`, `has_api_key`
- `provider` เป็นชื่อสั้นตัวพิมพ์เล็ก ตัวเลข หรือ `_` 2 ถึง 30 ตัว ไม่ใช่รายการตายตัว เพราะรายการจะอ้างว่ารองรับผู้ให้บริการที่ยังไม่มีตัวเชื่อม
- `mode` คือ `test` หรือ `live` เปิดการชำระเงินได้เมื่อมีผู้ให้บริการและ secret key แล้ว เปิดแล็บได้เมื่อมีชื่อแล็บแล้ว ไม่ครบได้ `409`
- `base_url` ของแล็บใช้กติกาเดียวกับ URL ของ webhook เพราะวันหนึ่งตัวเชื่อมจะเรียกจากในเครือข่ายของคลินิก
- รองรับ `If-Match` คืน `ETag` และ audit `integration.payment_configured`, `integration.lab_configured` บอกว่าช่องไหนเปลี่ยน ไม่เก็บค่าความลับ

## ฟีดปฏิทิน

โปรแกรมปฏิทิน (Google Calendar, Apple Calendar, Outlook) ดึงฟีดจาก URL ตามรอบของมันเอง และส่ง bearer token ไม่ได้ จึงยืนยันตัวด้วย token ในลิงก์

| เรียกแบบ | ได้อะไร |
|---|---|
| `GET ...?token=hpcal_...` ไม่ต้องเข้าสู่ระบบ | ไฟล์ iCalendar ของเจ้าของลิงก์ (สิ่งที่แอปปฏิทิน subscribe) |
| เข้าสู่ระบบแล้ว `Accept: application/json` | ลิงก์ของตัวเอง `{ "path": "/staff/integrations/calendar/ics?token=...", "token": "...", "can_view_branch": true, "created_at": "...", "last_used_at": "..." }` สร้างครั้งแรกที่ขอ และได้ลิงก์เดิมทุกครั้ง |
| เข้าสู่ระบบแล้ว แบบอื่น | ไฟล์ iCalendar สำหรับดาวน์โหลดครั้งเดียว |

- `?view=mine` (ค่าเริ่มต้น) นัดที่ตัวเองเป็นหมอผู้รับผิดชอบ พนักงานทุกคนใช้ได้ `?view=branch` นัดทุกนัดในสาขาที่สังกัด ต้องมี `appointment:read`
- ช่วงเวลาย้อนหลัง 30 วันถึงล่วงหน้า 180 วัน ไม่เกิน 2,000 นัด นัดที่ยกเลิกหรือไม่มาแสดงเป็น `STATUS:CANCELLED` เพื่อให้แอปลบออก
- **ความเป็นส่วนตัว** แต่ละนัดมีแค่เวลา ชื่อบริการ ชื่อสัตว์ สาขา และรหัสนัด ไม่มีอาการที่แจ้ง เหตุผล บันทึก ผลวินิจฉัย หรือข้อมูลติดต่อของเจ้าของ เพราะลิงก์ฟีดไปอยู่ในแอปปฏิทินที่ซิงก์ข้ามอุปกรณ์และแชร์ได้ คำตอบมี `Cache-Control: private, no-store` และ `X-Robots-Tag: noindex`
- **token** ฐานข้อมูลเก็บ SHA-256 ไว้ค้นหา และเก็บ token แบบเข้ารหัสไว้ให้หน้าเว็บแสดงลิงก์เดิมได้ ไม่ต้องสร้างใหม่จนปฏิทินที่ subscribe แล้วเสีย token ไม่ได้ให้สิทธิ์อื่นนอกจากฟีด และถูกตรวจกับเจ้าของทุกครั้ง พนักงานที่ถูกปิดบัญชีได้ `401` ทันที token ผิดได้ `401`
- audit `integration.calendar_feed_created` ตอนสร้างลิงก์ การดึงฟีดไม่เขียน audit (แอปดึงทุกครึ่งชั่วโมง) แต่บันทึก `last_used_at` ไม่เกินหนึ่งครั้งต่อห้านาที

## หน้าจอ

แท็บ "เชื่อมต่อระบบ" ใน `/settings` (เห็นเมื่อมี `integration:manage`) มีหน้าย่อย

| หน้า | ทำอะไร |
|---|---|
| `/settings/integrations/webhooks` | สร้าง webhook เลือก event แสดง secret ครั้งเดียวพร้อมปุ่มคัดลอกและคำเตือน และช่องใส่รหัส webhook เพื่อเปิดประวัติการส่ง |
| `/settings/integrations/webhooks/[id]` | ประวัติการส่ง สถานะ ครั้งที่ลอง เวลาลองครั้งถัดไป และคำตอบจากปลายทางแบบย่อ |
| `/settings/integrations/api-keys` | สร้าง key เลือกได้เฉพาะ scope ที่ตัวเองมีและ key ถือได้ เลือกสาขาและอายุ แสดง key ครั้งเดียวพร้อมปุ่มคัดลอกและคำเตือน |
| `/settings/integrations/payment` | ค่าตั้งผู้ให้บริการชำระเงิน ช่องความลับแบบเขียนอย่างเดียว และคำบอกว่ายังไม่มีตัวเชื่อม |
| `/settings/integrations/lab` | ค่าตั้งแล็บภายนอก แบบเดียวกัน |
| `/settings/integrations/calendar` | ลิงก์ปฏิทินของตัวเอง (และทั้งสาขาถ้ามีสิทธิ์) พร้อมปุ่มคัดลอกและคำเตือนเรื่องความเป็นส่วนตัว พนักงานทุกคนเปิดได้ |

โค้ดหน้าจออยู่ที่ `frontend/src/features/integrations/` (หน้า LINE ยังอยู่ที่เดิม) และ `frontend/src/app/(staff)/settings/integrations/`

## ข้อจำกัดที่รู้อยู่

- contract ไม่มีรายการ webhook หรือ API key จึงต้องจดรหัส webhook ไว้เองเพื่อเปิดประวัติการส่ง และไม่มีทางยกเลิก key ก่อนหมดอายุผ่าน API
- ปิด webhook ไม่ได้ แก้ URL ไม่ได้ หมุน secret ไม่ได้ ต้องสร้างใหม่
- ประวัติเก็บแค่ผลครั้งล่าสุดของแต่ละ event ไม่ได้เก็บทุกครั้งที่ลอง
- หน้าลิงก์ปฏิทินอยู่ใต้แท็บที่ต้องมี `integration:manage` พนักงานคนอื่นต้องเปิด `/settings/integrations/calendar` ตรง ๆ ควรมีลิงก์จากหน้าบัญชีของฉัน
- ลิงก์ปฏิทินที่หลุดไปใช้ได้จนกว่าเจ้าของจะถูกปิดบัญชี เพราะยังไม่มีทางสร้างลิงก์ใหม่
- ถ้ากุญแจ Data Protection หาย secret ของ webhook และค่าตั้งที่เข้ารหัสจะอ่านไม่ได้ webhook จะส่งไม่สำเร็จด้วย `secret_unavailable` ต้องสร้างใหม่ ดู [development.md](../development.md)
- ความลับ TOTP และความลับของ LINE ยังเก็บแบบไม่เข้ารหัส เป็นช่องโหว่ที่รู้อยู่และแยกจากงานนี้ ([system-overview.md § 5 A](../system-overview.md))

## ข้อเสนอส่วนขยาย (ยังไม่ได้ขออนุมัติ ยังไม่ได้ทำ)

| ข้อเสนอ | เหตุผล |
|---|---|
| `GET /staff/webhooks` | รายการ webhook ทั้งหมด ไม่ต้องจดรหัสเอง |
| `PATCH /staff/webhooks/{id}` และ `DELETE /staff/webhooks/{id}` | ปิด เปิด แก้ URL หรือ event และลบ (คอลัมน์ `active` มีแล้ว) |
| `POST /staff/webhooks/{id}/rotate-secret` | หมุน secret โดยไม่ต้องสร้าง webhook ใหม่ |
| `POST /staff/webhooks/{id}/deliveries/{delivery_id}/redeliver` | ส่งซ้ำ delivery ที่ `failed` แล้ว |
| `GET /staff/api-keys` | รายการ key (prefix, scope, สาขา, วันหมดอายุ, `last_used_at`) ไม่มีตัว key |
| `DELETE /staff/api-keys/{id}` | ยกเลิก key ทันที คอลัมน์ `revoked_at` และการตรวจตอนยืนยันตัวมีแล้ว |
| `POST /staff/integrations/calendar/token` | สร้างลิงก์ปฏิทินใหม่และทำให้ลิงก์เดิมใช้ไม่ได้ |

## โค้ด

| ส่วน | ไฟล์ |
|---|---|
| domain | `backend/src/HappyPaws.Domain/Integrations/` (`WebhookSubscription`, `WebhookEvent`, `WebhookDelivery`, `WebhookAddressRules`, `WebhookUrl`, `ApiKey`, `PaymentProviderSettings`, `LabProviderSettings`, `CalendarFeed`) |
| scope ที่ key ถือได้ | `backend/src/HappyPaws.Domain/Access/SecondFactorScopes.cs` |
| use case | `backend/src/HappyPaws.Application/Integrations/` |
| outbox และ payload | `backend/src/HappyPaws.Application/Integrations/Webhooks/` |
| ตัวส่ง การตรวจที่อยู่ และการเข้ารหัส | `backend/src/HappyPaws.Infrastructure/Webhooks/` |
| งานเบื้องหลัง | `backend/src/HappyPaws.Infrastructure/Jobs/WebhookJob.cs` |
| การยืนยันตัวด้วย API key | `backend/src/HappyPaws.Api/Authorization/ApiKeyAuthenticationHandler.cs`, `InteractiveCallerRequirement.cs`, `backend/src/HappyPaws.Api/Extensions/AuthenticationExtensions.cs` |
| controller | `backend/src/HappyPaws.Api/Controllers/Staff/` (`WebhooksController`, `WebhookDeliveriesController`, `ApiKeysController`, `PaymentIntegrationController`, `LabIntegrationController`, `CalendarFeedController`) |
| เทสต์ | `backend/tests/HappyPaws.IntegrationTests/Api/Integrations/`, `backend/tests/HappyPaws.IntegrationTests/Webhooks/`, `backend/tests/HappyPaws.UnitTests/Domain/Integrations/`, `backend/tests/HappyPaws.UnitTests/Application/Integrations/` |
