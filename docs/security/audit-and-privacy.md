# Audit และความเป็นส่วนตัว

ส่วนหนึ่งของ [ความปลอดภัย](README.md)

คลินิกถือข้อมูลติดต่อของคนและข้อมูลสุขภาพของสัตว์ ในประเทศไทยจึงอยู่ภายใต้ PDPA เอกสารนี้บอกว่าระบบเก็บหลักฐานอะไรไว้ให้คลินิกพิสูจน์ได้ว่าใครทำอะไร ลูกค้ายินยอมอะไร และคำขอเรื่องข้อมูลถูกตอบเมื่อไร มุมของผู้ใช้อยู่ใน [ความยินยอมและข้อมูลส่วนบุคคล](../features/privacy-and-consent.md) เหตุผลที่ต้องเพิ่ม endpoint นอก contract อยู่ใน [Endpoint ส่วนขยาย](../contract-extensions.md)

## Audit log

### แถวหนึ่งเก็บอะไร

ตาราง `audit_entries` (`AuditEntry`) แถวหนึ่งตอบคำถาม "ใคร ทำอะไร กับข้อมูลไหน เมื่อไร จาก IP ใด"

| ช่อง | ที่มา |
|---|---|
| `action`, `resource_type`, `resource_id` | use case บอก ต้องเป็นค่าใน `AuditActions` และ `AuditResources` |
| `actor_user_id`, `actor_staff_id`, `actor_customer_id` | `AuditTrail` อ่านจาก token ของผู้เรียกเอง |
| `branch_id`, `pet_id` | use case บอก `pet_id` แยกเก็บเพราะ access log อ่านรายสัตว์ |
| `ip_address`, `user_agent` (ตัดที่ 400), `request_id` | อ่านจากคำขอ |
| `details_json` | รายละเอียดเฉพาะเรื่อง เช่น ค่าก่อนและหลัง เหตุผล |
| `occurred_at` | นาฬิกาของระบบ |

**use case ส่งผู้กระทำเองไม่ได้** `IAuditTrail` มีเพียง `Record(AuditEvent)` และ `RecordImmediatelyAsync(AuditEvent)` ผู้กระทำ IP และ request id ถูกเติมโดย `AuditTrail` จากผู้เรียกปัจจุบัน handler จึงบันทึกผู้กระทำผิดคนไม่ได้

**IP ที่บันทึกตอนนี้เป็นของเซิร์ฟเวอร์ Next.js** เพราะผู้ใช้ทุกคนเรียก API ผ่าน BFF และ API ไม่อ่าน `X-Forwarded-For` คำถาม "จาก IP ใด" จึงยังตอบไม่ได้จริง

### กติกาของตาราง

- **เขียนพร้อมการเปลี่ยนแปลง** `Record` เข้าร่วม transaction ที่เปิดอยู่ ถ้าการเปลี่ยนแปลง rollback แถว audit ก็หายไปด้วย เพราะไม่มีอะไรเกิดขึ้นจริง
- **การอ่านเขียนทันที** `RecordImmediatelyAsync` ใช้กับการเปิดเวชระเบียนและประวัติสัตว์ ซึ่งไม่มี transaction ให้เข้าร่วม ถ้าเขียนไม่สำเร็จ ระบบบันทึก error ระดับ `Error` พร้อมข้อมูลพอสร้างแถวที่หายขึ้นใหม่ แต่ไม่ทำให้การอ่านล้มเป็น 500
- **ชื่อ action ต้องอยู่ในรายการ** `AuditEntry.Write` โยน `unknown_audit_action` ถ้าชื่อไม่อยู่ใน `AuditActions.All` พิมพ์ผิดจึงไม่กลายเป็นหมวดใหม่ที่ไม่มีใครค้น
- **ไม่มี foreign key** ไปยังแถวที่อ้างถึง แถว audit ต้องอยู่รอดแม้ข้อมูลต้นทางถูกลบ cascade จะลบหลักฐาน restrict จะขวางการลบที่ชอบด้วยกฎหมาย
- **เพิ่มได้อย่างเดียวในระดับโค้ด** `AuditEntry` ไม่มีเมธอดแก้ไขเลย แต่ฐานข้อมูลไม่มี trigger หรือสิทธิ์แยกที่กัน `UPDATE` และ `DELETE` บัญชีฐานข้อมูลของแอปแก้แถวได้
- **ไม่ลบ** ไม่มีระยะเวลาเก็บ และไม่มีงานส่งออก

### อะไรต้องถูกบันทึก

contract มี endpoint อ่าน audit แต่ไม่ได้บอกว่าต้องเขียนอะไร รายการจึงอยู่ใน `Domain/Auditing/AuditActions.cs` ไฟล์เดียว เกณฑ์มีสองข้อ คือ **แตะข้อมูลส่วนบุคคลหรือข้อมูลสุขภาพ** หรือ **เปลี่ยนว่าใครทำอะไรได้** บวกงานคลินิกที่มีผลเรื่องเงินหรือหน้าที่ดูแลผู้ป่วย

| หมวด | action |
|---|---|
| การเปิดดูข้อมูลสุขภาพ (access log) | `record.viewed`, `pet.history_viewed`, `consultation.viewed`, `pet.prescriptions_viewed` (ประวัติยาของสัตว์ตัวเดียว), `pet.lab_results_viewed` (ผลแล็บของสัตว์ตัวเดียว), `pet.document_generated` (พิมพ์ใบรับรอง ใบส่งตัว หรือสมุดวัคซีน), `ai.pet_summary` (ผู้ช่วย AI สรุปประวัติสัตว์ให้หมอ ส่วนขยาย § 34) |
| การเปลี่ยนข้อมูลสุขภาพ | `consultation.opened`, `consultation.closed`, `consultation.vitals_recorded`, `consultation.examination_recorded`, `consultation.diagnosis_added`, `consultation.treatment_added`, `consultation.prescription_added`, `consultation.allergy_overridden`, `record.signed`, `record.amended`, `pet.allergy_recorded`, `pet.marked_deceased`, `pet.deceased_undone`, `pet.created`, `pet.updated`, `pet.vaccination_recorded`, `pet.treatment_plan_created`, `pet.document_added`, `lab.requested`, `lab.results_recorded`, `lab.results_reviewed`, `prescription.dispensed` |
| ข้อมูลส่วนบุคคลและความยินยอม | `customer.created`, `customer.merged`, `consent.recorded`, `consent.withdrawn`, `data_request.raised`, `data_request.closed` |
| เปลี่ยนว่าใครทำอะไรได้ | `auth.signed_in`, `auth.sign_in_failed`, `auth.password_changed`, `auth.session_revoked`, `auth.registered` (ลูกค้าเปิดบัญชีเอง), `auth.password_reset` (ตั้งรหัสใหม่จากลิงก์), `auth.credentials_reset`, `pet.access_granted`, `pet.access_revoked`, `customer.invited`, `clinic.bootstrapped`, `staff.invited`, `staff.invitation_accepted`, `staff.updated`, `staff.deactivated`, `staff.public_profile_updated`, `staff.seeded`, `user.status_changed`, `account.profile_updated`, `auth.two_factor_enrolled`, `auth.recovery_code_used`, `schedule.shifts_created`, `schedule.time_off_recorded`, `schedule.assignment_rules_replaced`, `role.created`, `role.permissions_replaced`, `settings.medical_record_changed`, `queue.display_created`, `queue.display_claimed`, `queue.display_revoked` (token ของจอห้องรอ ส่วนขยาย § 30) |
| งานที่มีผลเรื่องเงินหรือการดูแล | `appointment.cancelled`, `appointment.no_show`, `settings.cancellation_policy_changed`, `waiting_list.removed`, `queue.priority_changed`, `invoice.issued`, `invoice.voided`, `invoice.discounted`, `payment.collected`, `payment.refunded`, `receipt.issued`, `service.pricing_changed` |
| ผู้ช่วย AI สำหรับพนักงาน (ส่วนขยาย § 34) | `ai.soap_draft`, `ai.owner_message_draft`, `ai.ask` บันทึกแค่ id ของเคส บทสนทนา หรือชื่อเครื่องมือที่ทำงาน ไม่มีคำสั่งหรือคำตอบของโมเดล |

นอกเกณฑ์สองข้อมีหมวดเดียวที่ตั้งใจบันทึก คือ **บทความบนหน้าเว็บ** `article.created`, `article.updated`, `article.published`, `article.unpublished`, `article.deleted` บทความไม่ใช่ข้อมูลส่วนบุคคล แต่เป็นคลินิกพูดต่อสาธารณะในนามสัตวแพทย์ เมื่อหน้าเว็บพูดสิ่งที่ไม่ควรพูดต้องตอบได้ว่าใครเอาขึ้นและเมื่อไร เนื้อหาบทความไม่ถูกคัดลอกลง log มีแค่ความยาว ดู [contract-extensions.md § 26](../contract-extensions.md)

รายการนี้เป็น **ข้อกำหนด** ไม่ใช่สิ่งที่มีอยู่ตอนนี้ สิ่งที่ยังไม่ตรง

| action | สถานะ |
|---|---|
| `auth.signed_in`, `auth.sign_in_failed`, `auth.password_changed`, `auth.session_revoked` | ยังไม่มีโค้ดเขียน การเข้าสู่ระบบ การเปลี่ยนรหัสผ่าน และการสั่งออกเครื่อง จึงไม่อยู่ใน audit log |
| `consultation.viewed` | ยังไม่มี endpoint อ่านการตรวจที่เปิดอยู่ |
| `appointment.cancelled` | เขียนเฉพาะตอนยกเลิกนัดอัตโนมัติเมื่อสัตว์เสียชีวิต `POST /appointments/{id}/cancel` ไม่เขียน มีเพียง log ระดับ Information |

แถวที่ไม่มีผู้กระทำโดยตั้งใจ คือ `clinic.bootstrapped` และ `auth.credentials_reset` (มาจากคำสั่งบนเซิร์ฟเวอร์) กับ `staff.invitation_accepted` (ผู้รับคำเชิญยังไม่มี token) แถวเหล่านี้โยงกลับด้วย `resource_id` และ `details_json` แทน เช่น `staff.invitation_accepted` เก็บรหัสคำเชิญและรหัสพนักงานผู้เชิญ `clinic.bootstrapped` เก็บรหัสบัญชีและอีเมลของเจ้าของ

สิ่งที่ตั้งใจไม่บันทึก และเขียนเหตุผลไว้ข้างรายการ

- การบันทึกร่างการตรวจอัตโนมัติ เพราะเกิดทุกไม่กี่วินาที จะกลบแถวที่สำคัญ เวชระเบียนที่ลงนามและ amendment บอกผลสุดท้ายอยู่แล้ว
- การค้นหาหรือเปิดข้อมูลลูกค้า พนักงานเคาน์เตอร์เปิดหลายสิบคนต่อชั่วโมง เส้นแบ่งคือข้อมูลสุขภาพ เปิดแฟ้มการรักษาถูกบันทึก เปิดเบอร์โทรไม่ถูกบันทึก

### อ่าน audit log

| Endpoint | scope | ตัวกรอง |
|---|---|---|
| `GET /staff/audit-logs` | `audit:read` | `action`, `resource_type`, `resource_id`, `pet_id`, `actor_user_id`, `from`, `to`, `limit`, `cursor` |
| `GET /staff/audit-logs/access` | `audit:read` | เฉพาะ action การเปิดดู กรองด้วย `pet_id`, `actor_user_id`, `from`, `to` |
| `GET /staff/medical-records/{id}/audit` | `record:audit` | ประวัติการแก้ของเวชระเบียนฉบับนั้น |

- เรียงใหม่สุดก่อน แบ่งหน้าด้วย cursor
- ผู้อ่านที่ผูกสาขาเห็นเฉพาะแถวของสาขาตัวเอง แถวที่ไม่มีสาขา (เช่น ความยินยอม การเชิญพนักงาน) ผู้ถือ `audit:read` ทุกคนเห็น
- access log แยก endpoint เพราะตอบคำถามคนละแบบ "ใครแก้เวชระเบียนนี้" เป็นเรื่องการกำกับดูแลทางคลินิก ส่วน "ใครเปิดดูแฟ้มของสัตว์ตัวนี้" เป็นเรื่องความเป็นส่วนตัว และเป็นสิ่งที่เจ้าของข้อมูลมีสิทธิ์ถาม
- ตาม role ตั้งต้น มีเพียง `clinic_owner` และ `clinic_admin` ที่ถือ `audit:read`

## ความยินยอม

contract ไม่มีเรื่องความยินยอมเลย ระบบจึงเพิ่มเอง (`CustomerConsent`)

**เป็นสมุดบันทึก ไม่ใช่ธง** ทุกการให้และการถอนเป็นแถวใหม่ สถานะปัจจุบันคือแถวล่าสุดของแต่ละวัตถุประสงค์ ธงที่แก้ทับได้ตอบได้แค่ "ส่งข้อความวันนี้ได้ไหม" แต่คำร้องเรียนถามว่า "ยินยอมวันไหน ถอนวันไหน"

| วัตถุประสงค์ | ค่าเริ่มต้น | เหตุผล |
|---|---|---|
| `service_messages` | ยินยอม | เตือนนัด เรียกคิว สรุปการรักษา ลูกค้าให้เบอร์มาเพื่อให้ติดต่อเรื่องสัตว์ของตัวเอง |
| `marketing` | ไม่ยินยอม | โปรโมชันต้องขอก่อนเสมอ |
| `medical_record_sharing` | ไม่ยินยอม | ส่งเวชระเบียนให้คลินิกอื่นหรือบริษัทประกัน |
| `extended_data_processing` | ไม่ยินยอม | เก็บหรือใช้ข้อมูลเกินที่การรักษาต้องใช้ |

- ส่งสถานะเดิมซ้ำไม่เพิ่มแถว แต่การยืนยันค่าเริ่มต้นครั้งแรกเพิ่มแถว เพราะ "ถูกถามแล้วตอบว่าใช่" ต่างจาก "ไม่เคยมีใครถาม"
- บันทึกที่เคาน์เตอร์ (`source = counter`) ต้องมีผู้บันทึก ไม่อย่างนั้นโดเมนปฏิเสธ แถวเก็บ `recorded_by_user_id`, `policy_version` และ `note` ได้
- ทุกแถวเขียน audit แยก `consent.recorded` กับ `consent.withdrawn`

| Endpoint | สิทธิ์ |
|---|---|
| `GET /portal/consents`, `PUT /portal/consents` | ลูกค้าที่เข้าสู่ระบบ ใช้รหัสลูกค้าจาก token เท่านั้น |
| `GET /staff/customers/{id}/consents` | `customer:read` |
| `POST /staff/customers/{id}/consents` | `customer:write` |

### ด่านก่อนส่งข้อความ

ความยินยอมบังคับที่จุดส่งข้อความ ไม่ได้ตรวจในแต่ละ use case `INotificationDispatcher` ที่ use case ขอได้มีตัวเดียวคือ `ConsentAwareNotificationDispatcher` จึงไม่มีทางส่งข้อความโดยไม่ผ่านด่าน

- `ConsentGate` จับคู่ template กับวัตถุประสงค์แบบระบุตรง ๆ ตอนนี้ `queue.called`, `consultation.summary`, `pet.deceased_appointments_cancelled`, `receipt.issued` และ `customer.portal_invitation` เป็น `service_messages`
- template ที่ไม่อยู่ในรายการถือเป็น `marketing` ซึ่งเป็นฝั่งที่เข้มกว่า
- ข้อความที่ถูกกันบันทึก log ระดับ Information ว่า template ไหน ลูกค้าคนไหน วัตถุประสงค์อะไร เพื่อให้ตอบได้ว่าทำไมลูกค้าไม่ได้ข้อความ
- ข้อความที่ถูกกันไม่ทำให้งานที่สั่งส่งล้ม (มีเทสต์)
- คำเชิญพนักงานใช้ port แยก (`IStaffInvitationSender`) ไม่ผ่านด่านนี้ เพราะผู้ถูกเชิญไม่ใช่ลูกค้า

ช่องทางส่งจริงยังเป็น stub ทั้งหมด ดู [การแจ้งเตือน](../features/notifications.md)

## คำขอเรื่องข้อมูลส่วนบุคคล

`DataSubjectRequest` เป็นงานที่มีคนรับผิดชอบ ไม่ใช่ปุ่มที่ทำงานเอง **ไม่มีอะไรถูกลบอัตโนมัติ** คำขอลบข้อมูลกับคลินิกสัตวแพทย์มักชนกับหน้าที่เก็บเวชระเบียน ซึ่งระบบนี้ไม่ลบเลย การชั่งสองเรื่องนี้ต้องมีคนตัดสินและรับผิดชอบ สิ่งที่ API รับประกันคือคำขอถูกรับ ถูกจับเวลา และถูกตอบ

| Endpoint | สิทธิ์ | ทำอะไร |
|---|---|---|
| `POST /portal/data-requests` | ลูกค้าที่เข้าสู่ระบบ | ยื่นคำขอ `access`, `correction`, `erasure` หรือ `portability` |
| `GET /staff/data-requests?status=` | `customer:read` | รายการคำขอ เรียง **เก่าสุดก่อน** พร้อม `open_days` |
| `POST /staff/data-requests/{id}/close` | `customer:write` | ปิดเป็น `completed` หรือ `refused` ต้องมี `resolution` รองรับ `If-Match` |

- รายการนี้เรียงต่างจากทุกรายการในระบบ เพราะคำขอบนสุดคือคำขอที่ใกล้กำหนดตามกฎหมายที่สุด
- `refused` เป็นผลปกติ ไม่ใช่ error ปฏิเสธการลบเพราะต้องเก็บเวชระเบียนตามกฎหมายทำได้ แต่ต้องตอบเป็นลายลักษณ์อักษรพร้อมเหตุผล
- ปิดซ้ำตอบ `409 state_conflict` ปิดโดยไม่บอกผลตอบ 409 `invalid_data_request`
- ยื่นและปิดเขียน audit ทั้งคู่ แถวปิดเก็บผล เหตุผล และจำนวนวันที่เปิดอยู่
- ลูกค้าปิดคำขอของตัวเองไม่ได้ (มีเทสต์)

ยังไม่มีเครื่องมือส่งออกข้อมูลของบุคคลหนึ่ง หรือลบหรือปิดบังข้อมูลตามคำขอ ผู้ปิดคำขอต้องทำงานนั้นนอกระบบ และยังไม่มีระยะเวลาเก็บข้อมูลแยกตามประเภท เพราะเป็นนโยบายที่ที่ปรึกษากฎหมายของคลินิกต้องกำหนด

## สิ่งที่อยู่ใน log

log ถูกส่งต่อ ทำสำเนา และเก็บด้วยกติกาที่ต่างจากฐานข้อมูล จึงไม่ควรมีข้อมูลสุขภาพ ตอนนี้เป็นแบบนี้

| แหล่ง | เขียนอะไร |
|---|---|
| `RequestContextMiddleware` | scope ของทุกบรรทัด มี `RequestId`, `Method`, `Path` (ไม่มี query string) และช่อง `UserId`, `StaffId` ซึ่งตอนนี้ว่างเสมอ เพราะ middleware นี้ทำงานก่อน `UseAuthentication` |
| `StaffLoginHandler` | อีเมลที่พิมพ์ทุกครั้งที่เข้าสู่ระบบไม่สำเร็จ และรหัสบัญชีเมื่อบัญชีถูกระงับ |
| `VerifyTwoFactorHandler`, `RefreshSessionHandler` | รหัสบัญชีหรือรหัสเซสชันเมื่อยืนยันไม่ผ่านหรือพบ token ถูกใช้ซ้ำ |
| `ApiExceptionHandler` | ชนิด exception, method, path, request id และข้อความของ exception ส่วน 500 เขียน exception เต็ม |
| `LoggingStaffInvitationSender` | รหัสคำเชิญ อีเมล วันหมดอายุ และลิงก์เต็มเมื่อเปิด `LogUndeliveredLinks` |
| `LoggingNotificationChannel` (stub) | template รหัสลูกค้า และข้อมูลทุกช่องในข้อความ ซึ่งรวม **คำวินิจฉัย** ใน `consultation.summary` และชื่อลูกค้าใน `customer.portal_invitation` |
| `ConsentAwareNotificationDispatcher` | template รหัสลูกค้า และวัตถุประสงค์ เมื่อข้อความถูกกัน |
| EF Core ใน `Development` | คำสั่ง SQL ระดับ Information ไม่มีค่า parameter เพราะไม่ได้เปิด sensitive data logging |

ข้อที่ควรแก้ก่อนใช้งานจริง คือให้ stub แจ้งเตือนเขียนเฉพาะ template กับรหัส ย้ายการตั้ง log scope ไปหลัง authentication หรืออ่านผู้ใช้ตอนเขียน log และพิจารณาบันทึกอีเมลที่เข้าสู่ระบบไม่สำเร็จแบบปิดบังบางส่วน

## โค้ด

| ส่วน | ไฟล์ |
|---|---|
| รายการ action และแถว audit | `backend/src/HappyPaws.Domain/Auditing/AuditActions.cs`, `AuditEntry.cs` |
| การเขียน audit | `backend/src/HappyPaws.Application/Auditing/IAuditTrail.cs`, `backend/src/HappyPaws.Infrastructure/Auditing/AuditTrail.cs` |
| การอ่าน audit | `backend/src/HappyPaws.Api/Controllers/Staff/AuditLogsController.cs`, `backend/src/HappyPaws.Application/Auditing/ListAuditLogs/` |
| ตาราง audit | `backend/src/HappyPaws.Infrastructure/Persistence/Configurations/Auditing/` |
| ความยินยอม | `backend/src/HappyPaws.Domain/Customers/CustomerConsent.cs`, `backend/src/HappyPaws.Application/Customers/ConsentGate.cs`, `RecordConsent/`, `GetConsents/` |
| ด่านก่อนส่ง | `backend/src/HappyPaws.Infrastructure/Notifications/ConsentAwareNotificationDispatcher.cs`, `LoggingNotificationChannel.cs` |
| คำขอเรื่องข้อมูล | `backend/src/HappyPaws.Domain/Customers/DataSubjectRequest.cs`, `backend/src/HappyPaws.Application/Customers/DataRequests/` |
| endpoint | `backend/src/HappyPaws.Api/Controllers/Portal/PortalPrivacyController.cs`, `backend/src/HappyPaws.Api/Controllers/Staff/PrivacyController.cs` |
| เทสต์ | `backend/tests/HappyPaws.IntegrationTests/Api/Customers/`, `Api/MedicalRecords/MedicalRecordTests.cs`, `backend/tests/HappyPaws.UnitTests/Domain/Auditing/AuditActionsTests.cs`, `Application/Customers/ConsentGateTests.cs` |
