# การแจ้งเตือน

ระบบส่งข้อความถึงลูกค้าเมื่อมีเหตุการณ์เรื่องสัตว์ของเขา เช่น ถูกเรียกคิว หรือได้ใบเสร็จ ทางเดินของข้อความสร้างเสร็จแล้วตั้งแต่ use case จนถึงประตูกั้นความยินยอม **แต่ยังไม่มีช่องทางส่งจริง** LINE, SMS และอีเมลยังเป็น stub ที่เขียน log อย่างเดียว สิ่งที่ถึงลูกค้าจริงตอนนี้มีทางเดียว คือ **กล่องแจ้งเตือนในเว็บ** (`GET /portal/notifications`) ซึ่งเก็บทุกข้อความที่ผ่านด่านความยินยอม ดู [portal ลูกค้า](portal.md#กล่องแจ้งเตือน)

## ข้อความที่ระบบสร้างตอนนี้

| template | เกิดเมื่อ | ข้อมูลที่แนบ | use case |
|---|---|---|---|
| `queue.called` | เรียกคิว `POST /staff/queue/{id}/call` | เลขคิว ห้อง | `Queueing/CallTicket/` |
| `consultation.summary` | ปิดเคสและลงนามเวชระเบียน | รหัสเวชระเบียน การวินิจฉัยหลัก ค่าหัตถการ | `Consultations/CloseConsultation/` |
| `receipt.issued` | ออกใบเสร็จครั้งแรก (กดซ้ำไม่ส่งซ้ำ) | เลขบิล เลขใบเสร็จ ยอดเงิน | `Billing/IssueReceipt/` |
| `pet.deceased_appointments_cancelled` | บันทึกสัตว์เสียชีวิตแล้วมีนัดในอนาคตถูกยกเลิก ส่งครั้งเดียวไม่ว่ายกเลิกกี่นัด | รหัสสัตว์ จำนวนนัดที่ยกเลิก | `Patients/MarkPetDeceased/` |
| `customer.portal_invitation` | เคาน์เตอร์เชิญลูกค้าเปิดบัญชี portal | ชื่อลูกค้า | `Customers/InviteToPortal/` |
| `waiting_list.slot_available` | มีคนยกเลิกนัดที่ตรงกับคิวรอ แจ้งไม่เกินสามคนแรก | ชื่อสัตว์ ชื่อบริการ วันและเวลาที่ว่าง | `Appointments/AddToWaitingList/WaitingListNotifier` |

ถ้อยคำภาษาไทยของแต่ละ template สำหรับกล่องแจ้งเตือนอยู่ใน `Application/Notifications/NotificationText.cs` ตัวส่งของแต่ละช่องทางเขียนถ้อยคำของตัวเองได้

ทุก template ผูกกับวัตถุประสงค์ `service_messages` template อื่นที่เพิ่มในอนาคตโดยไม่ได้ผูก จะถูกถือเป็นการตลาด ดู [ความยินยอม](privacy-and-consent.md)

## ข้อความเดินทางอย่างไร

```text
use case
  │ หลัง transaction commit แล้วเท่านั้น
  ▼
INotificationDispatcher            สิ่งเดียวที่ use case รู้จัก
  │
  ▼
ConsentAwareNotificationDispatcher  ลูกค้ายินยอมให้ส่งข้อความชนิดนี้ไหม
  │ ไม่ยินยอม: เขียน log ระดับ information แล้วจบ
  ▼
inbox_notifications                 เก็บลงกล่องแจ้งเตือนในเว็บ (เก็บไม่ได้ก็ส่งต่อ)
  │
  ▼
INotificationChannel                ส่งทางไหน (ตอนนี้คือ LoggingNotificationChannel)
```

กล่องแจ้งเตือนเขียนด้วย DbContext แยกของตัวเอง เพราะข้อความถูกส่งหลัง use case commit แล้วในคำขอเดียวกัน ถ้าใช้ context ของคำขอ การบันทึกข้อความจะพาสิ่งอื่นที่ context ยังถืออยู่ลงฐานข้อมูลไปด้วย

- **ส่งหลัง commit เสมอ ห้ามส่งใน transaction** gateway ที่ช้าจะถือ row lock ไว้ตลอดเวลา timeout และการส่งไม่สำเร็จห้ามทำให้งานทางคลินิก rollback แลกกับการที่ข้อความอาจหายถ้า process ตายระหว่าง commit กับการส่ง ซึ่งดีกว่าการเรียกคิวล้มเพราะ LINE ล่ม
- **ส่งไม่สำเร็จไม่ทำให้คำขอล้ม** ทุกจุดที่ส่งครอบด้วย try/catch แล้วเขียน log ระดับ error พร้อมรหัสลูกค้า สัตว์ที่ถูกเรียกคิวแล้วก็ถูกเรียกแล้ว ไม่ว่าข้อความจะถึงหรือไม่
- **ช่องทางห้าม throw** สำหรับข้อความที่ส่งไม่ได้ (กติกาของ `INotificationChannel`)
- **use case บอกแค่ "ส่งอะไร ถึงใคร"** คำขอมีแค่ `CustomerId`, `TemplateKey` และ `Data` ไม่มีข้อความจริง ไม่รู้ว่าลูกค้าใช้ช่องทางไหน ถ้อยคำจะอยู่กับตัวส่งของแต่ละช่องทาง แปลหรือปรับได้โดยไม่แตะ use case
- **แยก "ส่งได้ไหม" กับ "ส่งทางไหน"** dispatcher ตัดสินว่าส่งได้ไหม ช่องทางตัดสินว่าส่งอย่างไร การแยกทำให้ประตูความยินยอมอยู่หน้าทุกช่องทางพร้อมกัน แทนที่จะเขียนซ้ำในแต่ละ adapter แล้วสักวันมีตัวที่ลืม

## สิ่งที่เป็น stub

| ส่วน | ตอนนี้ทำอะไร |
|---|---|
| `LoggingNotificationChannel` | เขียน log ระดับ warning ว่าไม่ได้ส่ง พร้อม template รหัสลูกค้า และข้อมูลที่แนบทั้งหมด แล้วคืนค่าสำเร็จ |
| `Infrastructure/Notifications/Line/`, `Sms/`, `Email/` | โฟลเดอร์ว่าง (มีแค่ `.gitkeep`) |
| `LoggingStaffInvitationSender` | ดูหัวข้อคำเชิญพนักงานด้านล่าง |

stub ตั้งใจให้ดัง เพราะ stub อันตรายตรงที่ไม่มีใครรู้ว่ามันยังติดตั้งอยู่ ข้อความที่ไม่ถึงใครจึงต้องเห็นใน log ไม่ใช่สำเร็จเงียบ ๆ

**ข้อควรระวัง** stub เขียนข้อมูลที่แนบทั้งหมดลง log รวมถึงการวินิจฉัยใน `consultation.summary` ซึ่งเป็นข้อมูลสุขภาพ ถ้า log ถูกส่งออกไปที่อื่น ต้องเปลี่ยนตัวส่งก่อน หรือปิด log ระดับ warning ของคลาสนี้

## คำเชิญพนักงานใช้ทางแยก

คำเชิญพนักงานไม่ผ่าน `INotificationDispatcher` แต่ใช้ `IStaffInvitationSender` เพราะสองเหตุผล dispatcher ส่งถึงลูกค้าและอยู่หลังความยินยอมของลูกค้า ส่วนคนที่ถูกเชิญไม่ใช่ลูกค้า และข้อความเชิญมีความลับ (token) ที่ห้ามลง log แบบข้อความเรียกคิว

- ตอนนี้ตัวส่งคือ `LoggingStaffInvitationSender` เขียน log ระดับ warning ว่าไม่ได้ส่ง พร้อมรหัสคำเชิญ อีเมล และวันหมดอายุ (7 วัน) ไม่เขียนลิงก์
- ตั้ง `Staff:Invitations:LogUndeliveredLinks` เป็น `true` เพื่อให้เขียนลิงก์รับคำเชิญลง log ได้ ค่าเริ่มต้นปิด `appsettings.Development.json` เปิดไว้เพื่อให้นักพัฒนาทำ flow ให้จบได้ ห้ามเปิดในที่ที่ log ออกจากเครื่อง เพราะลิงก์คือ credential
- รูปแบบลิงก์ตั้งที่ `Staff:Invitations:AcceptUrlTemplate` token อยู่หลัง `#` ซึ่งเบราว์เซอร์ไม่ส่งไปที่เซิร์ฟเวอร์ จึงไม่ติดใน access log และ `Referer`

รายละเอียดคำเชิญดู [พนักงานและสิทธิ์](staff-and-roles.md)

รหัส OTP และลิงก์รีเซ็ตรหัสผ่านก็ใช้ทางแยกด้วยเหตุผลเดียวกัน คือ `IOtpSender` และ `IPasswordResetSender` ตัวส่งตอนนี้คือ `LoggingCustomerSignInSenders` เขียนรหัสหรือลิงก์ลง log เฉพาะเมื่อเปิด `Auth:SignIn:LogUndeliveredSecrets` ดู [การเข้าสู่ระบบของลูกค้า](customer-sign-in.md)

## ยังไม่ได้ทำ

| เรื่อง | สถานะ |
|---|---|
| ตัวส่ง LINE, SMS, อีเมล | ยังไม่มี |
| ที่เก็บปลายทาง LINE ของลูกค้า | ลูกค้ามีแค่ `Phone` และ `Email` ยังไม่มีช่องเก็บ LINE user id ที่ตัวส่ง LINE ต้องใช้ |
| เตือนนัดล่วงหน้า | ยังไม่มีตัวตั้งเวลา และยังไม่มี template `appointment.reminder` |
| บันทึกการส่ง (delivery log) | ยังไม่มีตาราง ตอนนี้มีแค่ log ของแอป |
| ส่งซ้ำเมื่อล้มเหลว หรือ outbox | ไม่มี ข้อความที่ส่งไม่ได้หายไป |
| endpoint ใน contract | `POST /staff/notifications/send`, `GET /staff/notifications/templates`, `GET /staff/notifications/log`, `PUT /staff/integrations/line`, `POST /staff/integrations/line/broadcast`, `POST /webhooks/line` ยังไม่ได้ทำ (`GET /portal/notifications` ทำแล้ว) |

LINE เป็นช่องทางส่งข้อความออกเท่านั้น ไม่ใช่การเข้าสู่ระบบหรือการยืนยันตัวตน endpoint ใน contract ที่มีไว้ผูกบัญชี LINE กับลูกค้าถูกตัดออกจากขอบเขต ([assumptions.md](../assumptions.md) หัวข้อ A และ B)

หน้าแรกสาธารณะสัญญาเรื่องแจ้งเตือนผ่าน LINE หลายอย่างที่ยังไม่มีจริง ดู [หน้าแรก](home-page.md)

## เพิ่มช่องทางส่งจริง

1. เขียนคลาสที่ implement `INotificationChannel` ในโฟลเดอร์ของช่องทาง เช่น `Infrastructure/Notifications/Line/` ตัวส่งเลือกช่องทางจากข้อมูลลูกค้า ลูกค้าที่ไม่มีปลายทางของช่องทางนั้นใช้ช่องทางอื่น หรือไม่ส่งเลย
2. เขียนถ้อยคำของแต่ละ template ในตัวส่ง ไม่ใช่ใน use case
3. ห้าม throw สำหรับข้อความที่ส่งไม่ได้ เขียน log แทน และห้ามเขียนข้อมูลสุขภาพหรือความลับลง log
4. เปลี่ยนการลงทะเบียน `INotificationChannel` ใน `backend/src/HappyPaws.Infrastructure/DependencyInjection.cs` อย่าลงทะเบียน dispatcher ตัวอื่นแทน `ConsentAwareNotificationDispatcher` เพราะจะเท่ากับถอดประตูความยินยอมออก
5. template ใหม่เพิ่มใน `NotificationTemplates` และผูกวัตถุประสงค์ใน `ConsentGate` ถ้าไม่ผูก จะถูกถือเป็นการตลาด

## โค้ด

| ส่วน | ไฟล์ |
|---|---|
| port ที่ use case ใช้ และรายชื่อ template | `backend/src/HappyPaws.Application/Notifications/INotificationDispatcher.cs` |
| port ของช่องทาง | `backend/src/HappyPaws.Application/Notifications/INotificationChannel.cs` |
| ประตูความยินยอม | `backend/src/HappyPaws.Application/Customers/ConsentGate.cs`, `backend/src/HappyPaws.Infrastructure/Notifications/ConsentAwareNotificationDispatcher.cs` |
| ช่องทาง stub | `backend/src/HappyPaws.Infrastructure/Notifications/LoggingNotificationChannel.cs` |
| คำเชิญพนักงาน | `backend/src/HappyPaws.Application/Staff/IStaffInvitationSender.cs`, `backend/src/HappyPaws.Infrastructure/Staff/LoggingStaffInvitationSender.cs` |
| กล่องแจ้งเตือน | `backend/src/HappyPaws.Domain/Notifications/InboxNotification.cs`, `backend/src/HappyPaws.Application/Notifications/NotificationText.cs`, `backend/src/HappyPaws.Infrastructure/Persistence/Repositories/Notifications/InboxRepository.cs` |
| การลงทะเบียน | `backend/src/HappyPaws.Infrastructure/DependencyInjection.cs` (หัวข้อ Notifications) |
| เทสต์ | `backend/tests/HappyPaws.IntegrationTests/Api/Customers/ConsentEnforcementTests.cs` |
