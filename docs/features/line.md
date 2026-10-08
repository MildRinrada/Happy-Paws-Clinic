# LINE

คลินิกส่งการแจ้งเตือนผ่าน LINE Official Account ของคลินิกเอง (ช่อง Messaging API) **LINE เป็นช่องทางส่งข้อความออกเท่านั้น** ไม่ใช้เข้าสู่ระบบ ไม่ผูกบัญชี และไม่ใช้ยืนยันตัวตน ที่อยู่ LINE ของลูกค้าเป็นข้อมูลสำหรับส่ง เหมือนอีเมล ดู [assumptions.md](../assumptions.md) หัวข้อ A และ B

endpoint ของ LINE ที่ทำในรอบนี้มีสามเส้น ส่วนสี่เส้นที่ใช้ LINE เป็นตัวตน (`POST /auth/line/callback`, `POST /portal/line/link`, `DELETE /portal/line/link`, `GET /line/liff/booking-context`) ตั้งใจไม่ทำ

## ตั้งค่า: `PUT /staff/integrations/line`

ต้องมี `integration:manage` ทุกฟิลด์ไม่บังคับ ฟิลด์ที่ไม่ส่งคงค่าเดิม สัญญาไม่มี `GET` จึงอ่านค่าปัจจุบันด้วยการส่ง body ว่าง `{}` ซึ่งไม่เขียนอะไร (ตีความแบบเดียวกับการตั้งค่าคิว)

```json
PUT /v1/staff/integrations/line
{
  "channel_id": "1650000000",
  "channel_secret": "...",
  "channel_access_token": "...",
  "enabled": true,
  "notification_types": { "receipt.issued": false },
  "broadcast_monthly_limit": 300
}
```

| ฟิลด์ | ความหมาย |
|---|---|
| `channel_id` | ตัวเลขจาก LINE Developers แสดงกลับได้ ไม่ใช่ความลับ |
| `channel_secret` | ใช้ตรวจลายเซ็น webhook **ไม่เคยถูกส่งกลับ** สตริงว่างคือลบค่าที่เก็บไว้ |
| `channel_access_token` | token แบบ long-lived ที่ใช้ push **ไม่เคยถูกส่งกลับ** สตริงว่างคือลบค่าที่เก็บไว้ |
| `enabled` | สวิตช์หลัก ปิดแล้วทุกข้อความไปทาง SMS หรืออีเมลแทน |
| `notification_types` | เปิดหรือปิดข้อความแต่ละชนิดทาง LINE เปลี่ยนเฉพาะชนิดที่ระบุ ชนิดที่ไม่รู้จักตอบ `422` |
| `broadcast_monthly_limit` | โควตาประกาศต่อเดือน 0 ถึง 1,000,000 ค่าเริ่มต้น 300 |

คำตอบมี `has_channel_secret`, `has_channel_access_token` (เก็บไว้ในฐานข้อมูลไหม), `secret_source` และ `token_source` (`database`, `configuration` หรือ `none` คือค่าที่ใช้จริงมาจากไหน), `can_push`, `can_verify`, สวิตช์ของทุกชนิดข้อความ, `webhook_path` (`/webhooks/line`) และ `version` ส่ง `If-Match` ได้ ถ้า version ไม่ตรงตอบ `409 version_conflict`

**ความลับมาจากสองที่** ค่าที่ตั้งผ่าน endpoint นี้ชนะค่าจาก configuration (`Messaging:Line:*`) ลบค่าในฐานข้อมูลแล้วจะกลับไปใช้ค่าจาก configuration

**ข้อควรระวัง** secret และ token ที่ตั้งผ่านหน้านี้เก็บในตาราง `line_channel_settings` **แบบยังไม่เข้ารหัส** เหมือนความลับ TOTP ถ้าทำได้ ให้ตั้งผ่าน environment แทน และควรเข้ารหัสคอลัมน์นี้ก่อนใช้งานจริง ทุกการแก้ถูกบันทึกใน audit log (`integration.line_configured`) โดยบอกแค่ว่าเปลี่ยนอะไร ไม่มีค่า

## ส่งข้อความ (push)

ข้อความถึงลูกค้าไปทาง LINE เมื่อครบทุกข้อ ไม่อย่างนั้นระบบลอง SMS แล้วอีเมล ดู [การแจ้งเตือน](notifications.md)

1. มี access token (จากหน้านี้หรือ configuration) และ `enabled` เปิดอยู่
2. ข้อความชนิดนั้นไม่ได้ถูกปิดทาง LINE
3. ลูกค้ามี `line_user_id` และไม่ได้บล็อกบัญชี
4. ลูกค้าไม่ได้ปิด LINE เองในบัญชี portal

ระบบเรียก `POST https://api.line.me/v2/bot/message/push` พร้อม bearer token และ `X-Line-Retry-Key` ข้อความเป็นข้อความธรรมดา ถ้าข้อมูลมีรหัสนัด รหัสบัตรคิว หรือรหัสบิล จะมีปุ่ม quick reply ใต้ข้อความ ตอนนี้ข้อความเรียกคิวมีปุ่ม "ดูคิวของฉัน" และใบเสร็จมีปุ่ม "ดูบิล" ปุ่มของนัด (ยืนยัน เลื่อน ยกเลิก) จะขึ้นเมื่อการเตือนนัดอัตโนมัติส่ง `appointment_id` มาด้วย

LINE นับ push ทุกข้อความ (รวมการเรียกคิวและใบเสร็จ) เข้าโควตาของแพ็กเกจ LINE เอง โควตาประกาศของระบบนับเฉพาะการประกาศ

## ประกาศ: `POST /staff/integrations/line/broadcast`

ต้องมี `notify:broadcast`

```json
{ "customer_ids": ["cus_1", "cus_2"], "message": "คลินิกหยุดวันจันทร์ที่ 13 ต.ค." }
```

- **กลุ่มลูกค้าคือรายชื่อในคำขอ** สัญญาบอกว่า "ประกาศถึงกลุ่มลูกค้าที่คัดไว้" แต่กลุ่มที่บันทึกไว้ (`POST /staff/segments`) เป็นของโมดูล CRM ซึ่งยังไม่มี จึงยังไม่รับ `segment_id` เมื่อมีโมดูล CRM กลุ่มจะถูกแปลงเป็นรายชื่อแบบเดียวกันนี้
- หนึ่งถึง 200 คนต่อครั้ง ข้อความไม่เกิน 1,000 ตัวอักษร ระบบ push ทีละคนในคำขอเดียว
- **เป็นข้อความการตลาด** ใช้เทมเพลต `clinic.announcement` ลูกค้าที่ไม่ได้ยินยอมรับการตลาดถูกข้าม (`no_consent`) ค่าเริ่มต้นของการตลาดคือไม่ยินยอม
- **LINE เท่านั้น ไม่ส่ง SMS แทน** ลูกค้าที่ไม่มี LINE ถูกข้าม (`no_address`)
- **โควตา** นับจากประวัติการส่ง เฉพาะประกาศที่ส่งสำเร็จในเดือนปฏิทินนี้ (เวลาไทย) ถ้าจำนวนที่ใช้ไปบวกจำนวนคนในคำขอเกินโควตา ตอบ `409 quota_exceeded` พร้อม `details.limit`, `used`, `requested` และไม่ส่งใครเลย ประกาศสองครั้งที่เริ่มพร้อมกันอาจผ่านการตรวจทั้งคู่ โควตาจึงเป็นงบ ไม่ใช่ล็อก
- LINE ไม่มี token หรือปิดอยู่ ตอบ `409 line_not_configured`
- คำตอบรูปเดียวกับ `POST /staff/notifications/send` บวก `quota` (`limit`, `used`, `remaining`) และถูกบันทึกใน audit log (`notification.broadcast`) พร้อมจำนวนผู้รับ ส่งแล้ว ล้มเหลว และข้าม

## webhook: `POST /webhooks/line`

เปิดโดยไม่ต้องมี bearer token (`[AllowAnonymous]`) และยืนยันด้วยลายเซ็นแทน

1. อ่าน body ดิบทั้งก้อน (ไม่เกิน 1 MB)
2. ตรวจ `X-Line-Signature` ว่าเท่ากับ base64 ของ HMAC-SHA256 ของ body ด้วย channel secret เปรียบเทียบแบบเวลาคงที่ ไม่ตรง ไม่มี header หรือไม่ได้ตั้ง secret เลย ตอบ `401` และไม่อ่าน body
3. body ที่ลายเซ็นถูกแต่ไม่ใช่รูปแบบของ LINE ตอบ `400`
4. ทำแต่ละ event แยกกัน event ที่ล้มไม่ทำให้ event อื่นล้ม แล้วตอบ `200` เสมอ เพื่อไม่ให้ LINE ส่งซ้ำทั้งชุด

| event | ระบบทำอะไร |
|---|---|
| `follow` | ตอบข้อความต้อนรับ ถ้าที่อยู่นี้เป็นของลูกค้าที่เคยบล็อก จะเริ่มส่งถึงเขาอีกครั้ง ไม่สร้างหรือผูกอะไร |
| `unfollow` | ถ้าที่อยู่นี้เป็นของลูกค้า หยุด push ถึงเขาทันที ประวัติการส่งจะบอก `unfollowed` และข้อความไปทาง SMS แทน |
| `message` | ตอบอัตโนมัติว่าบัญชีนี้ใช้ส่งการแจ้งเตือนเท่านั้น พร้อมลิงก์ portal ไม่เก็บข้อความ |
| `postback` | ทำตามปุ่ม ดูข้างล่าง |

event จากกลุ่มหรือห้องแชต และ event โหมด `standby` ถูกข้าม คำตอบใช้ reply API (`/v2/bot/message/reply`) ซึ่งไม่นับโควตา

### ปุ่มในแชต (postback)

ข้อมูลของปุ่มเป็น query string เช่น `action=confirm_appointment&appointment_id=apt_...` action ที่รับคือห้าตัวใน `Domain/Integrations/LinePostbackActions.cs` ตัวอื่นตอบว่าไม่พบรายการ

ระบบหาลูกค้าจาก LINE user id ที่ส่งมา (การค้นจากที่อยู่ส่ง ไม่ใช่การยืนยันตัวตน) ถ้าไม่มีลูกค้าที่บันทึกที่อยู่นี้ไว้ จะตอบว่ายังทำรายการจากแชตไม่ได้ พร้อมลิงก์ portal แล้ว **ตรวจสิทธิ์ซ้ำทุกครั้ง** นัดต้องเป็นของสัตว์ที่ลูกค้าคนนั้นดูแล (กฎเดียวกับ portal) บิลต้องเป็นของลูกค้าคนนั้น ไม่ผ่านตอบเหมือนไม่มีรายการ

| action | ทำอะไร |
|---|---|
| `confirm_appointment` | ยืนยันนัด กดซ้ำได้ไม่ error |
| `cancel_appointment` | กดครั้งแรกถามก่อนพร้อมปุ่ม "ยืนยันยกเลิกนัด" กดครั้งที่สองจึงยกเลิก ใช้นโยบายยกเลิกเหมือนลูกค้ายกเลิกเอง (นับว่ายกเลิกกระชั้นได้) แล้วแจ้งคิวรอ เหตุผลบันทึกว่า "ยกเลิกผ่านปุ่มใน LINE" ผู้ยกเลิกคือบัญชี portal ของลูกค้า หรือรหัสลูกค้าถ้าไม่มีบัญชี |
| `reschedule_appointment` | ตอบลิงก์ไปเลือกเวลาใหม่ใน portal เพราะปุ่มเลือกเวลาไม่ได้ |
| `view_queue` | ตอบเลขคิววันนี้ สถานะ และจำนวนคิวที่รออยู่ข้างหน้า |
| `open_invoice` | ตอบเลขบิล ยอดรวม ยอดค้าง และลิงก์ portal |

handler ของ portal ไม่ถูกเรียกตรง เพราะรับผู้เรียกจาก access token ซึ่ง webhook ไม่มี การยืนยันและยกเลิกเรียก method ของ entity ตัวเดียวกับที่ handler เหล่านั้นใช้ กฎจึงยังอยู่ที่ entity

## ที่อยู่ LINE ของลูกค้า

รอบนี้เพิ่ม `customers.line_user_id` (ไม่ซ้ำกันเมื่อมีค่า) และ `customers.line_unfollowed_at`

**ยังไม่มีทางใส่ค่านี้ผ่าน API** สัญญามีแค่ endpoint ผูกบัญชีที่ถูกตัดออก และ LINE user id เป็นรหัสเฉพาะของแต่ละช่อง พนักงานพิมพ์เองไม่ได้ ตอนนี้ระบบส่ง LINE และรับปุ่มได้ครบ แต่จะถึงลูกค้าได้เมื่อมีวิธีบันทึกที่อยู่ ทางที่เสนอ (ต้องขออนุมัติเป็นส่วนขยาย) คือให้เคาน์เตอร์เลือกจากผู้ติดตามที่เพิ่งเพิ่มเพื่อน หรือให้ลูกค้าส่งรหัสสั้นจาก portal เข้าแชต ซึ่งเป็นการยืนยันที่อยู่ส่ง แบบเดียวกับยืนยันอีเมล ทั้งสองทางต้องตัดสินก่อนว่าไม่ขัดกับหัวข้อ A ระหว่างนี้ข้อความไปทาง SMS หรืออีเมล

## หน้าจอ

| หน้า | path | scope |
|---|---|---|
| ตั้งค่า LINE (สถานะ ข้อมูลช่องทาง สวิตช์ โควตา) | `/settings/line` | `integration:manage` |
| ประกาศทาง LINE | `/notifications/broadcast` | `notify:broadcast` |

หน้าตั้งค่าไม่แสดง secret และ token ที่เก็บไว้ ช่องที่เว้นว่างคือใช้ค่าเดิม มีช่องติ๊กสำหรับลบค่าที่เก็บไว้ หน้า webhook แสดง path ที่ต้องนำไปต่อท้าย URL สาธารณะของ API แล้วใส่ใน LINE Developers

## ตั้งค่าบนเซิร์ฟเวอร์

ดู [การพัฒนา](../development.md#ตั้งค่า-line) สรุปคือ `Messaging__Line__ChannelSecret`, `Messaging__Line__ChannelAccessToken`, `Messaging__Line__ChannelId` และ `Messaging__Line__PortalUrl`

## โค้ด

| ส่วน | ไฟล์ |
|---|---|
| การตั้งค่า | `backend/src/HappyPaws.Domain/Integrations/LineChannelSettings.cs`, `backend/src/HappyPaws.Application/Integrations/ConfigureLineChannel/` |
| webhook | `backend/src/HappyPaws.Api/Controllers/Webhooks/LineWebhookController.cs`, `backend/src/HappyPaws.Application/Integrations/HandleLineWebhook/` |
| ข้อมูลปุ่ม | `backend/src/HappyPaws.Application/Integrations/LinePostbackData.cs`, `backend/src/HappyPaws.Domain/Integrations/LinePostbackActions.cs` |
| ประกาศ | `backend/src/HappyPaws.Application/Notifications/BroadcastToSegment/`, `backend/src/HappyPaws.Api/Controllers/Staff/LineBroadcastController.cs` |
| client ของ LINE และลายเซ็น | `backend/src/HappyPaws.Infrastructure/Notifications/Line/` |
| หน้าจอ | `frontend/src/features/integrations/`, `frontend/src/app/(staff)/settings/line/`, `frontend/src/app/(staff)/notifications/broadcast/` |
| เทสต์ | `backend/tests/HappyPaws.IntegrationTests/Api/Integrations/`, `Api/Notifications/BroadcastTests.cs`, `Messaging/LineClientTests.cs` |
