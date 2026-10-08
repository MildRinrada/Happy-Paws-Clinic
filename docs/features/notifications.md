# การแจ้งเตือน

ระบบส่งข้อความถึงลูกค้าเมื่อมีเหตุการณ์เรื่องสัตว์ของเขา เช่น ถูกเรียกคิว หรือได้ใบเสร็จ และพนักงานส่งเองได้จากหน้า "แจ้งเตือนลูกค้า" ข้อความออกจริงทาง **LINE, SMS และอีเมล** ตามที่ตั้งค่าไว้ ทุกครั้งที่พยายามส่งถูกบันทึกลง **ประวัติการส่ง** (delivery log) และทุกข้อความที่ผ่านด่านความยินยอมยังเข้า **กล่องแจ้งเตือนในเว็บ** (`GET /portal/notifications`) เหมือนเดิม ดู [portal ลูกค้า](portal.md#กล่องแจ้งเตือน)

เรื่อง LINE (การตั้งค่า webhook ปุ่มในแชต และการประกาศ) อยู่ใน [LINE](line.md)

## endpoint

| Method | Path | Scope | หน้าที่ |
|---|---|---|---|
| POST | `/staff/notifications/send` | `notify:send` | ส่งเทมเพลตถึงลูกค้าหนึ่งคนหรือหลายคน เลือกช่องทางได้ |
| GET | `/staff/notifications/templates` | `notify:manage` | รายการเทมเพลต ค่าที่ใช้ ความยินยอมที่ต้องมี และถ้อยคำแต่ละช่องทาง |
| GET | `/staff/notifications/log` | `notify:read` | ประวัติการส่ง หนึ่งแถวต่อหนึ่งครั้งที่ลองส่ง |
| PUT | `/staff/integrations/line` | `integration:manage` | ตั้งค่า LINE ดู [LINE](line.md) |
| POST | `/staff/integrations/line/broadcast` | `notify:broadcast` | ประกาศทาง LINE ดู [LINE](line.md) |
| POST | `/webhooks/line` | ลายเซ็น LINE | รับ event จาก LINE ดู [LINE](line.md) |

### ส่งข้อความ

```json
POST /v1/staff/notifications/send
{
  "template": "appointment.reminder",
  "customer_ids": ["cus_01J..."],
  "channel": "auto",
  "variables": { "pet_name": "ลูน่า", "date": "12 ต.ค.", "time": "10:30", "vet_name": "หมอเอ" },
  "branch_id": "brn_..."
}
```

- `template` ต้องเป็นเทมเพลตที่พนักงานส่งเองได้ มีสามตัว คือ `appointment.reminder`, `vaccine.due` และ `clinic.message` เทมเพลตที่ชี้เรกคอร์ดจริง เช่น ใบเสร็จหรือการเรียกคิว ส่งเองไม่ได้ เพราะจะบอกลูกค้าว่าจ่ายเงินหรือถูกเรียกแล้วทั้งที่ไม่จริง
- `customer_ids` หนึ่งถึง 100 คน id ซ้ำถูกตัดเหลือหนึ่ง id ที่ไม่มีหรือถูกรวมไปแล้วตอบ `404` ก่อนส่งใคร
- `channel` คือ `auto` (ค่าเริ่มต้น) `line` `sms` หรือ `email` ถ้าเลือกช่องทางเดียว ลูกค้าที่ไม่มีช่องทางนั้นจะไม่ได้รับ ไม่ส่งทางอื่นแทน
- `variables` ต้องมีค่าที่เทมเพลตบังคับ ค่าที่เทมเพลตไม่รู้จักถูกปฏิเสธ แต่ละค่ามีความยาวสูงสุด (ข้อความอิสระ 500 ตัวอักษร)
- `branch_id` ไม่บังคับ ใช้กรองประวัติการส่งตามสาขา ถ้าไม่ส่งและผู้ส่งสังกัดสาขาเดียว จะใช้สาขานั้น

คำตอบเป็น `200` เสมอเมื่อคำขอถูกต้อง แม้บางคนไม่ได้รับ

```json
{
  "batch_id": "nbt_01J...",
  "sent": 1, "failed": 0, "skipped": 1,
  "results": [
    { "customer_id": "cus_1", "status": "sent", "channel": "sms", "fallback_from": "line" },
    { "customer_id": "cus_2", "status": "skipped", "error_code": "no_consent" }
  ]
}
```

ผลอ่านกลับจากประวัติการส่งด้วย `batch_id` จึงตรงกับที่หน้าประวัติแสดง

### ประวัติการส่ง

`GET /staff/notifications/log` กรองได้ด้วย `status` (`sent`, `failed`, `skipped`), `channel` (`line`, `sms`, `email`), `source` (`system`, `manual`, `broadcast`, `automation`), `customer_id`, `template` และ `batch_id` เรียงใหม่ไปเก่า แบ่งหน้าแบบ cursor เหมือนรายการอื่น

| ฟิลด์ | ความหมาย |
|---|---|
| `channel` | ช่องทางที่ลอง ว่างเมื่อไม่ได้ถึงช่องทางใดเลย เช่น ไม่ได้รับความยินยอม |
| `recipient` | ที่อยู่ที่ใช้แบบปิดบางส่วน เช่น `***5678`, `s***@mail.com`, `U***a1b2` |
| `status` | `sent` ผู้ให้บริการรับข้อความแล้ว (ไม่ได้แปลว่าลูกค้าอ่านแล้ว) `failed` ผู้ให้บริการปฏิเสธหรือติดต่อไม่ได้ `skipped` ไม่ได้ลองส่ง |
| `error_code` | เหตุผล ดูตารางด้านล่าง |
| `fallback_from` | ช่องทางที่ล้มเหลวก่อนหน้า เมื่อแถวนี้คือการส่งแทน คือคำตอบของ "fallback ไป SMS เมื่อไหร่" |
| `source`, `batch_id`, `sent_by_staff_id`, `branch_id` | ใครหรืออะไรเป็นคนส่ง และส่งชุดไหน |

| `error_code` | ความหมาย |
|---|---|
| `no_consent` | ลูกค้าไม่ได้ยินยอมรับข้อความชนิดนี้ |
| `no_address` | ลูกค้าไม่มีที่อยู่ของช่องทางนี้ |
| `channel_not_configured` | ยังไม่ได้ตั้งค่าผู้ให้บริการของช่องทางนี้ หรือไม่ได้ตั้งเลยสักช่องทาง |
| `channel_disabled`, `template_disabled` | ปิด LINE ทั้งหมด หรือปิดข้อความชนิดนี้ทาง LINE |
| `opted_out` | ลูกค้าปิดช่องทางนี้เองในบัญชี (`PATCH /me` ช่อง `notifications`) |
| `unfollowed` | ลูกค้าบล็อกบัญชี LINE ของคลินิก |
| `no_channel`, `no_recipient` | ไม่มีช่องทางที่ส่งได้ หรือหาเรกคอร์ดลูกค้าไม่เจอ |
| `provider_rejected`, `provider_unauthorized`, `provider_quota`, `provider_error`, `provider_unreachable` | ผู้ให้บริการปฏิเสธข้อความ ปฏิเสธรหัสเข้าใช้ โควตาหมด ขัดข้อง หรือติดต่อไม่ได้ |

**สาขา** ผู้อ่านที่สังกัดสาขาเห็นแถวของสาขาตัวเอง และแถวที่ไม่ผูกสาขา เช่น คำเชิญเปิดบัญชี ประกาศ หรือการส่งของผู้ที่ไม่สังกัดสาขา เพราะแถวพวกนั้นเป็นของทั้งคลินิก ไม่ได้ audit การอ่าน เพราะแถวมีแค่ชื่อลูกค้าและชนิดข้อความ ไม่มีเนื้อความ และที่อยู่ถูกปิดบางส่วนแล้ว

## เทมเพลต

ถ้อยคำทุกชนิดอยู่ที่เดียวใน `Application/Notifications/Templates/NotificationTemplateCatalogue.cs` เป็นโค้ด ไม่ใช่ตาราง เพราะสัญญามีแค่การอ่านเทมเพลต ถ้ามีตารางก็จะเป็นสำเนาที่ไม่มีใครแก้ได้ เมื่อมี endpoint แก้ถ้อยคำ รายการนี้คือข้อมูลตั้งต้น

| template | ชื่อ | พนักงานส่งได้ | ความยินยอม | ค่าที่ใช้ |
|---|---|---|---|---|
| `appointment.reminder` | เตือนนัดหมาย | ได้ | ข้อความเรื่องการดูแล | `pet_name`, `date`, `time`, `vet_name` (ไม่บังคับ) |
| `vaccine.due` | ครบกำหนดวัคซีน | ได้ | ข้อความเรื่องการดูแล | `pet_name`, `date`, `vaccine_name` (ไม่บังคับ) |
| `clinic.message` | ข้อความถึงเจ้าของ | ได้ | ข้อความเรื่องการดูแล | `message` |
| `queue.called` | เรียกคิว | ไม่ได้ | ข้อความเรื่องการดูแล | `number`, `room` |
| `receipt.issued` | ใบเสร็จ | ไม่ได้ | ข้อความเรื่องการดูแล | `receipt_number`, `amount`, `invoice_id` |
| `consultation.summary` | สรุปการตรวจ | ไม่ได้ | ข้อความเรื่องการดูแล | `record_id` |
| `waiting_list.slot_available` | มีคิวว่าง | ไม่ได้ | ข้อความเรื่องการดูแล | `pet_name`, `service_name`, `date`, `time` |
| `pet.deceased_appointments_cancelled` | ยกเลิกนัดของสัตว์ที่เสียชีวิต | ไม่ได้ | ข้อความเรื่องการดูแล | ไม่มี |
| `customer.portal_invitation` | ชวนเปิดบัญชีออนไลน์ | ไม่ได้ | ข้อความเรื่องการดูแล | `display_name` |
| `clinic.announcement` | ประกาศ (LINE) | ไม่ได้ (ใช้กับการประกาศ) | การตลาด | `message` |
| `treatment_plan.session` | เตือนรอบแผนการรักษา | ไม่ได้ (ส่งโดย [เตือนอัตโนมัติ](automation.md)) | ข้อความเรื่องการดูแล | `pet_name`, `plan_title`, `session_number`, `date`, `time` (ไม่บังคับ) |

- แต่ละเทมเพลตมีหัวเรื่อง (กล่องแจ้งเตือนและหัวอีเมล) ข้อความ LINE (ใช้เป็นเนื้ออีเมลด้วย) และข้อความ SMS
- `{pet_name}` คือค่าที่เติม ส่วนใน `[ ... ]` แสดงเมื่อค่าข้างในมีครบ เช่น `เชิญที่ห้องตรวจ[ {room}]` ค่า `{clinic}` (ชื่อคลินิกจากโปรไฟล์) และ `{portal_url}` ระบบเติมให้เอง
- **SMS ของข้อความสั้นอยู่ในหนึ่งส่วนของ SMS ภาษาไทย (70 ตัวอักษร)** เพราะสองส่วนเสียเครดิตสองเท่า มี unit test ตรวจด้วยชื่อคลินิกยาว 20 ตัวอักษร
- **ไม่ส่งการวินิจฉัยออกนอกระบบของคลินิก** สรุปการตรวจทาง LINE, SMS และอีเมลบอกแค่ว่าบันทึกแล้ว ให้ดูใน portal เพราะทั้งสามผ่านเซิร์ฟเวอร์ของผู้ให้บริการ กล่องแจ้งเตือนในเว็บยังแสดงการวินิจฉัยเหมือนเดิม
- `clinic.message` ถูกจัดเป็นข้อความเรื่องการดูแล เพราะใช้แจ้งเรื่องของสัตว์ เช่น "ผลแล็บออกแล้ว" ข้อความโปรโมชันต้องใช้การประกาศ ซึ่งเป็นการตลาด คลินิกควรยืนยันการตัดสินนี้

## ข้อความเดินทางอย่างไร

```text
use case หรือพนักงาน
  │ หลัง transaction commit แล้วเท่านั้น
  ▼
INotificationDispatcher             สิ่งเดียวที่ use case รู้จัก
  │
  ▼
ConsentAwareNotificationDispatcher   ยินยอมไหม ไม่ยินยอม: บันทึก skipped no_consent แล้วจบ
  │
  ▼
inbox_notifications                  เก็บลงกล่องแจ้งเตือนในเว็บ
  │
  ▼
DeliveringNotificationChannel        เลือกช่องทาง ส่ง ส่งแทนเมื่อล้มเหลว บันทึกทุกครั้ง
  │
  ├─ LineDeliveryRoute    LINE Messaging API push
  ├─ SmsDeliveryRoute     ThaiBulkSMS (ตัวส่งเดียวกับรหัส OTP)
  └─ EmailDeliveryRoute   SMTP ผ่าน MailKit (ตัวส่งเดียวกับคำเชิญพนักงาน)
```

**เลือกช่องทาง** เมื่อไม่ระบุช่องทาง ระบบลอง LINE ก่อน แล้ว SMS แล้วอีเมล ข้ามช่องทางที่ส่งไม่ได้ เช่น ไม่ได้ตั้งค่า ลูกค้าไม่มีที่อยู่ ปิดไว้ หรือลูกค้าปิดเอง ถ้าลองแล้วล้มเหลว จะลองช่องทางถัดไป แถวของช่องทางถัดไปบอก `fallback_from` ไว้

**ไม่ได้ตั้งค่าเลย** (เครื่องนักพัฒนา) ข้อความไปที่ `LoggingNotificationChannel` ซึ่งเขียน log ระดับ warning เหมือนเดิม และประวัติการส่งบันทึก `skipped` `channel_not_configured` งานในเครื่อง dev จึงเดินต่อได้ และไม่มีใครเข้าใจผิดว่าส่งแล้ว

กล่องแจ้งเตือนและประวัติการส่งเขียนด้วย DbContext แยกของตัวเอง เพราะข้อความถูกส่งหลัง use case commit แล้วในคำขอเดียวกัน ถ้าใช้ context ของคำขอ การบันทึกจะพาสิ่งอื่นที่ context ยังถืออยู่ลงฐานข้อมูลไปด้วย

- **ส่งหลัง commit เสมอ ห้ามส่งใน transaction** gateway ที่ช้าจะถือ row lock ไว้ตลอดเวลา timeout และการส่งไม่สำเร็จห้ามทำให้งานทางคลินิก rollback
- **ส่งไม่สำเร็จไม่ทำให้คำขอล้ม** ช่องทางไม่ throw เขียน log ระดับ error พร้อมรหัสลูกค้า ชนิดข้อความ และรหัสผลจากผู้ให้บริการ ไม่เขียนเนื้อความหรือความลับลง log
- **แยก "ส่งได้ไหม" กับ "ส่งทางไหน"** dispatcher ตัดสินความยินยอม ช่องทางตัดสินวิธีส่ง ประตูความยินยอมจึงอยู่หน้าทุกช่องทางพร้อมกัน
- **ไม่มี outbox** ข้อความส่งครั้งเดียวในคำขอที่ทำให้เกิด ถ้า process ตายระหว่าง commit กับการส่ง ข้อความหายเหมือนเดิม สิ่งที่ดีขึ้นคือความล้มเหลวมีบันทึกในประวัติการส่ง ไม่ได้อยู่แค่ใน log ของแอป การส่งซ้ำอัตโนมัติยังไม่มี LINE push ใส่ `X-Line-Retry-Key` ไว้แล้ว ถ้ามีงานส่งซ้ำในอนาคต LINE จะไม่ส่งซ้ำให้ลูกค้า

## ที่อยู่ของลูกค้า

| ช่องทาง | มาจาก |
|---|---|
| LINE | `customers.line_user_id` ใหม่ในรอบนี้ ดู [LINE](line.md#ที่อยู่-line-ของลูกค้า) |
| SMS | `customers.phone` ส่งเป็นตัวเลขล้วน |
| อีเมล | `customers.email` |

ถ้าลูกค้ามีบัญชี portal และปิดช่องทางไว้ใน `PATCH /me` (`notifications.email`, `sms`, `line`) ช่องทางนั้นจะถูกข้ามด้วย `opted_out` ค่านี้เพิ่งมีผลจริงในรอบนี้

## คำเชิญพนักงาน รหัส OTP และลิงก์รีเซ็ตรหัสผ่าน

สามอย่างนี้ไม่ผ่าน `INotificationDispatcher` แต่ใช้ `IStaffInvitationSender`, `IOtpSender`, `IPasswordResetSender` เพราะผู้รับไม่ใช่ลูกค้าหรือข้อความมีความลับที่ห้ามลง log และห้ามเข้ากล่องแจ้งเตือน ตัวส่งใช้ transport SMS และอีเมลชุดเดียวกับข้างบน รายละเอียดอยู่ใน [พนักงานและสิทธิ์](staff-and-roles.md) และ [การเข้าสู่ระบบของลูกค้า](customer-sign-in.md)

- เมื่อตั้ง `Messaging:Email` คำเชิญส่งเป็นอีเมลจริง ถ้าไม่ได้ตั้ง ตัวส่งคือ `LoggingStaffInvitationSender` ซึ่งเขียน log ระดับ warning โดยไม่มีลิงก์ เว้นแต่เปิด `Staff:Invitations:LogUndeliveredLinks`
- รหัส OTP ส่งทาง SMS เมื่อตั้ง `Messaging:Sms` ลิงก์รีเซ็ตส่งทางอีเมลถ้าบัญชีมีอีเมล ไม่อย่างนั้นทาง SMS

## หน้าจอ

เมนู "แจ้งเตือนลูกค้า" (`/notifications`) มีแท็บตามสิทธิ์ รายการแท็บอยู่ใน `frontend/src/features/notifications/content/notification-tabs.ts` เพิ่มแท็บใหม่ได้ด้วยการเพิ่มบรรทัดเดียว `/notifications` พาไปแท็บแรกที่ใช้ได้

| แท็บ | path | scope |
|---|---|---|
| ประวัติการส่ง | `/notifications/log` | `notify:read` |
| ส่งข้อความ | `/notifications/send` | `notify:send` (ค้นหาลูกค้าต้องมี `customer:read` ด้วย) |
| ประกาศทาง LINE | `/notifications/broadcast` | `notify:broadcast` |
| เทมเพลต | `/notifications/templates` | `notify:manage` |
| เตือนอัตโนมัติ | `/notifications/automations` | `automation:read` ดู [เตือนอัตโนมัติ](automation.md) |
| ข้อความที่จะส่ง | `/notifications/reminders` | `automation:read` |

หน้าส่งข้อความเก็บรายการเทมเพลตที่ส่งได้และค่าของแต่ละตัวไว้ในฝั่งหน้าเว็บด้วย (`content/send-page.ts`) เพราะการอ่านเทมเพลตต้องมี `notify:manage` แต่ผู้ส่งอาจมีแค่ `notify:send` backend ตรวจซ้ำทุกครั้ง

## ยังไม่ได้ทำ

| เรื่อง | สถานะ |
|---|---|
| เตือนนัดล่วงหน้าอัตโนมัติ | ทำแล้ว ดู [เตือนอัตโนมัติ](automation.md) ข้อความจากกติกามี `source: automation` และ `batch_id` เป็น id ของบันทึกส่งแล้ว (`ard_...`) |
| ส่งซ้ำเมื่อล้มเหลว หรือ outbox | ไม่มี ดูหัวข้อข้างบน |
| แก้ถ้อยคำเทมเพลต | สัญญามีแค่การอ่าน |
| `/staff/threads` (กล่องข้อความรวมทุกช่องทาง) | นอกขอบเขตรอบนี้ |

## เพิ่มชนิดข้อความใหม่

1. เพิ่ม key ใน `NotificationTemplates` (`Application/Notifications/INotificationDispatcher.cs`)
2. เพิ่มถ้อยคำใน `NotificationTemplateCatalogue` unit test จะล้มถ้ามี key ที่ไม่มีถ้อยคำ หรือถ้อยคำใช้ค่าที่ไม่ได้ประกาศ
3. ผูกวัตถุประสงค์ใน `ConsentGate` ถ้าไม่ผูก จะถูกถือเป็นการตลาด และ unit test จะล้ม
4. ส่งผ่าน `INotificationDispatcher` หลัง commit เท่านั้น ใส่ `BranchId` ถ้าข้อความเป็นของสาขาใดสาขาหนึ่ง
5. ถ้าข้อมูลมี `appointment_id`, `ticket_id` หรือ `invoice_id` ข้อความ LINE จะมีปุ่มในแชตให้เอง

## โค้ด

| ส่วน | ไฟล์ |
|---|---|
| port ที่ use case ใช้ และรายชื่อ template | `backend/src/HappyPaws.Application/Notifications/INotificationDispatcher.cs` |
| ถ้อยคำ | `backend/src/HappyPaws.Application/Notifications/Templates/` |
| use case | `backend/src/HappyPaws.Application/Notifications/SendNotification/`, `ListTemplates/`, `GetDeliveryLog/`, `BroadcastToSegment/` |
| ประวัติการส่ง | `backend/src/HappyPaws.Domain/Notifications/NotificationDelivery.cs`, `backend/src/HappyPaws.Infrastructure/Persistence/Repositories/Notifications/DeliveryLogRepository.cs` |
| ประตูความยินยอม | `backend/src/HappyPaws.Application/Customers/ConsentGate.cs`, `backend/src/HappyPaws.Infrastructure/Notifications/ConsentAwareNotificationDispatcher.cs` |
| เลือกช่องทางและส่ง | `backend/src/HappyPaws.Infrastructure/Notifications/DeliveringNotificationChannel.cs`, `Routing/`, `Line/`, `Sms/`, `Email/` |
| การลงทะเบียน | `backend/src/HappyPaws.Infrastructure/Notifications/NotificationDeliveryRegistration.cs` |
| controller | `backend/src/HappyPaws.Api/Controllers/Staff/NotificationsController.cs`, `NotificationTemplatesController.cs`, `NotificationLogController.cs` |
| หน้าจอ | `frontend/src/features/notifications/`, `frontend/src/app/(staff)/notifications/` |
| เทสต์ | `backend/tests/HappyPaws.IntegrationTests/Api/Notifications/`, `backend/tests/HappyPaws.UnitTests/Application/Notifications/`, `backend/tests/HappyPaws.IntegrationTests/Api/Customers/ConsentEnforcementTests.cs` |
