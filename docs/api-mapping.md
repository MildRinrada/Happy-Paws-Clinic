# แผนผัง API

ทุก endpoint ใน [`API Reference.dc.html`](../API%20Reference.dc.html) มีที่อยู่ในโค้ดที่หาเจอได้ทันที เอกสารนี้คือดัชนีนั้น

```text
API endpoint -> Controller -> Application use case -> Domain -> Data access
```

method, path และ scope คัดลอกจาก contract ตรงตัว เอกสารนี้ไม่ได้นิยาม API ใหม่ ช่องที่ contract ไม่ได้ระบุ scope เขียนว่า *(ไม่ระบุ)* และการตีความอยู่ใน [assumptions.md](./assumptions.md)

อ่านตารางอย่างไร

- ทุก path จริงอยู่ใต้ path base `/v1` เช่น `/clinic` เรียกจริงที่ `/v1/clinic` (ดู [assumptions.md § 31](./assumptions.md))
- path ของ controller นับจาก `backend/src/HappyPaws.Api/Controllers/`
- path ของ use case นับจาก `backend/src/HappyPaws.Application/`
- ✅ คือทำแล้วและมี integration test ครอบ แถวที่ไม่มี ✅ ชื่อ controller และ use case เป็นชื่อที่วางไว้ ยังไม่มีไฟล์จริง
- data access ทุกโมดูลเป็นแบบเดียวกัน คือ `HappyPaws.Infrastructure/Persistence/Repositories/<Module>` บน `HappyPawsDbContext` จึงไม่ใส่เป็นคอลัมน์ ช่อง Domain บอกชื่อ aggregate ส่วน endpoint ที่ต้องคุยกับระบบภายนอกจะระบุระบบนั้นไว้

## รูปแบบข้อมูลที่ส่ง

response เป็น snake_case และ **สมาชิกที่เป็น null จะไม่ถูกส่งมาเลย** ไม่ได้ส่งเป็น `null` ตัวอย่างเช่น `/me` ของ token พนักงานล้วนจะไม่มีคีย์ `customer_id` ฝั่ง client จึงต้องตรวจว่ามีคีย์หรือไม่ ไม่ใช่ตรวจว่าค่าเป็น null กติกานี้ทำให้ error envelope สะอาดด้วย เพราะ `message_en`, `field` และ `details` เป็นค่าเลือกได้ทั้งหมด

## ดัชนีโดเมนไปโฟลเดอร์

ช่อง use case ที่เขียนเป็น `<Module>/<โฟลเดอร์>/<ชื่อ>Handler` คือ use case ที่รวมอยู่ในโฟลเดอร์ร่วม เช่น ทุกเรื่องของ portal ฝั่งสัตว์เลี้ยงอยู่ใน `Patients/Portal/`

คอลัมน์ frontend เป็นโครงที่วางไว้ หน้าที่มีจริงตอนนี้มีแค่ `(public)/`, `(auth)/login`, `(staff)/queue` และ `(staff)/billing` ส่วน feature `home` ของหน้าแรกสาธารณะไม่ผูกกับ API โดเมนใด จึงไม่อยู่ในตารางนี้

| API domain | Backend module | Frontend feature | Frontend routes |
|---|---|---|---|
| Authentication | `Identity` | `authentication` | `(auth)/` |
| Account & Access | `Identity` | `account` | `(staff)/settings/account`, `(portal)/account` |
| Clinic setup | `Clinics` | `clinic` | `(public)/`, `(staff)/settings/clinic` |
| Staff & Permissions | `Staff`, `Access` | `staff`, `access-control` | `(staff)/settings/staff`, `(staff)/settings/roles` |
| Customer | `Customers` | `customers` | `(staff)/customers` |
| Pet | `Patients` | `pets` | `(staff)/pets`, `(portal)/pets` |
| Services | `ServiceCatalog` | `service-catalog` | `(public)/services`, `(staff)/settings/services` |
| Booking & Appointment | `Appointments` | `booking`, `appointments` | `(public)/booking`, `(portal)/appointments`, `(staff)/appointments` |
| Queue | `Queueing` | `queue` | `(staff)/queue`, `(portal)/queue` |
| Consultation | `Consultations` | `consultations` | `(staff)/consultations` |
| Medical record | `MedicalRecords` | `medical-records` | `(staff)/records`, `(portal)/pets/[id]/records` |
| Customer portal | *(กลุ่มผู้ใช้ ข้ามหลายโมดูล)* | route ใน `(portal)` | `(portal)/` |
| Schedules & Shifts | `Scheduling` | `scheduling` | `(staff)/schedules` |
| Lab | `Laboratory` | `laboratory` | `(staff)/lab` |
| Prescription | `Prescriptions` | `prescriptions` | `(staff)/prescriptions` |
| Notifications & Messaging | `Notifications`, `Messaging` | `notifications`, `messaging` | `(staff)/messages`, `(portal)/notifications` |
| LINE integration *(ส่งออกอย่างเดียว)* | `Integrations` | `line` | `(staff)/settings/line` |
| Operational stats | `Analytics` | `analytics` | `(staff)/dashboard` |
| Business dashboard | `Analytics` | `analytics` | `(staff)/analytics` |
| Billing & Payments | `Billing` | `billing` | `(staff)/billing`, `(portal)/invoices` |
| Inventory | `Inventory` | `inventory` | `(staff)/inventory` |
| Loyalty & Promotions | `Crm` | `crm` | `(staff)/crm`, `(portal)/packages` |
| Reminders & Automation | `Automation` | `automation` | `(staff)/automations` |
| Reports & Audit | `Reporting`, `Auditing` | `reporting`, `audit` | `(staff)/reports`, `(staff)/audit` |
| Webhooks & Integrations | `Integrations` | `integrations` | `(staff)/settings/integrations` |

## Authentication

| Method | Endpoint | ผู้เรียก | Controller | Use case | Domain | |
|---|---|---|---|---|---|---|
| POST | `/auth/register` | ลูกค้า | `Auth/AuthController` | `Identity/Register/` | `Identity`, `Customers` | ✅ |
| POST | `/auth/otp/request` | ลูกค้า | `Auth/OtpController` | `Identity/RequestOtp/` | `Identity` | ✅ |
| POST | `/auth/login` | ทั้งสองฝั่ง | `Auth/AuthController` | `Identity/Login/` | `Identity` | ✅ |
| POST | `/auth/staff/login` | พนักงาน | `Auth/StaffAuthController` | `Identity/StaffLogin/` | `Identity` | ✅ |
| POST | `/auth/2fa/verify` | พนักงาน | `Auth/StaffAuthController` | `Identity/VerifyTwoFactor/` | `Identity` | ✅ |
| POST | `/auth/refresh` | ทั้งสองฝั่ง | `Auth/SessionController` | `Identity/RefreshSession/` | `Identity` | ✅ |
| POST | `/auth/logout` | ทั้งสองฝั่ง | `Auth/SessionController` | `Identity/Logout/` | `Identity` | ✅ |
| POST | `/auth/password/reset` | ทั้งสองฝั่ง | `Auth/PasswordController` | `Identity/ResetPassword/` | `Identity` | ✅ |

ทางเข้าของลูกค้าทำครบแล้ว แต่ **ยังไม่มีตัวส่ง SMS และอีเมลจริง** รหัส OTP และลิงก์รีเซ็ตรหัสผ่านถูกเขียนลง log ของ API แทน (เฉพาะเมื่อเปิด `Auth:SignIn:LogUndeliveredSecrets` ซึ่งเปิดไว้แค่ในเครื่อง dev) รายละเอียดอยู่ใน [การเข้าสู่ระบบของลูกค้า](features/customer-sign-in.md)

## Account and Access

| Method | Endpoint | Scope | Controller | Use case | Domain | |
|---|---|---|---|---|---|---|
| GET | `/me` | เข้าสู่ระบบแล้ว | `Account/MeController` | `Identity/GetMe/` | `Identity`, `Staff` | ✅ |
| PATCH | `/me` | เข้าสู่ระบบแล้ว | `Account/MeController` | `Identity/UpdateProfile/` | `Identity` | ✅ |
| GET | `/me/capabilities` | เข้าสู่ระบบแล้ว | `Account/MeController` | อ่านจาก `ICurrentUser` ตรง (scope คำนวณตอนออก token โดย `Identity/Capabilities/EffectiveScopeResolver`) | `Access` | ✅ |
| GET | `/me/sessions` | เข้าสู่ระบบแล้ว | `Account/SessionsController` | `Identity/ManageSessions/` | `Identity` | ✅ |
| DELETE | `/me/sessions/{id}` | เข้าสู่ระบบแล้ว | `Account/SessionsController` | `Identity/ManageSessions/` | `Identity` | ✅ |
| POST | `/me/password` | เข้าสู่ระบบแล้ว | `Account/MeController` | `Identity/ChangePassword/` | `Identity` | ✅ |
| POST | `/me/2fa/enroll` | เข้าสู่ระบบแล้ว | `Account/MeController` | `Identity/EnrolTwoFactor/` | `Identity` | ✅ |
| PATCH | `/staff/users/{id}/status` | `user:manage` | `Staff/UserStatusController` | `Identity/SetUserStatus/` | `Identity` | ✅ |

## Clinic setup

ทำครบทั้ง 11 เส้น controller ฝั่ง `Staff/` แยกตามสิ่งที่เปลี่ยน ไม่ได้แยกหนึ่งตัวต่อหนึ่ง path คือ `ClinicSetupController` ถือการเขียนข้อมูลที่สาธารณะเห็น (โปรไฟล์ สาขา เวลาทำการ วันหยุด) ส่วน `ClinicSettingsController` ถือเอกสารกติกาการทำงานสามชุด

| Method | Endpoint | Scope | Controller | Use case | Domain | |
|---|---|---|---|---|---|---|
| GET | `/clinic` | public | `Public/ClinicController` | `Clinics/GetClinicProfile/` | `Clinics` | ✅ |
| PATCH | `/clinic` | `settings:write` | `Staff/ClinicSetupController` | `Clinics/UpdateClinicProfile/` | `Clinics` | ✅ |
| GET | `/clinic/branches` | public | `Public/ClinicController` | `Clinics/ListBranches/` | `Clinics` | ✅ |
| POST | `/clinic/branches` | `settings:write` | `Staff/ClinicSetupController` | `Clinics/CreateBranch/` | `Clinics` | ✅ |
| GET | `/clinic/hours` | public | `Public/ClinicController` | `Clinics/GetOpeningHours/` | `Clinics` | ✅ |
| PUT | `/clinic/hours` | `settings:write` | `Staff/ClinicSetupController` | `Clinics/ReplaceOpeningHours/` | `Clinics` | ✅ |
| POST | `/clinic/holidays` | `settings:write` | `Staff/ClinicSetupController` | `Clinics/AddHoliday/` | `Clinics` | ✅ |
| GET | `/clinic/settings/booking` | `settings:read` | `Staff/ClinicSettingsController` | `Clinics/ManageBookingSettings/` | `Clinics` | ✅ |
| PATCH | `/clinic/settings/booking` | `settings:write` | `Staff/ClinicSettingsController` | `Clinics/ManageBookingSettings/` | `Clinics` | ✅ |
| PATCH | `/clinic/settings/queue` | `settings:write` | `Staff/ClinicSettingsController` | `Clinics/UpdateQueueSettings/` | `Clinics` | ✅ |
| PATCH | `/clinic/settings/medical-record` | `settings:write` | `Staff/ClinicSettingsController` | `Clinics/UpdateMedicalRecordSettings/` | `Clinics` | ✅ |

ค่าตั้งเหล่านี้ไม่ได้เก็บไว้เฉย ๆ โมดูลที่เกี่ยวข้องอ่านไปใช้จริง วันหยุดปิดช่วงจอง (`Appointments/Availability/BookingSlotResolver`) ค่าตั้งคิวกำหนดรูปแบบเลขคิวและการเริ่มเลขใหม่ทุกวัน (`Appointments/CheckInAppointment`, `Queueing/IssueWalkInTicket`) ค่าตั้งเวชระเบียนกำหนดว่าการตรวจต้องมีอะไรครบก่อนปิดเคส (`Consultations/CloseConsultation`) และแบบฟอร์มตรวจรายระบบ (`Consultations/RecordExamination`) การอ่านค่าที่ contract ไม่มีให้ ดู [assumptions.md § 27](./assumptions.md)

เจ้าของคนแรก สาขาแรก และชุด role แรก **สร้างผ่าน API นี้ไม่ได้** เพราะการเขียนทุกเส้นต้องมี scope และฐานข้อมูลว่างไม่มีใครถือ scope เลย ทั้งหมดมาจากคำสั่ง operator `bootstrap` ([development.md](./development.md)) ซึ่งตั้งใจทำเป็นคำสั่ง ไม่ใช่ endpoint

## Staff and Permissions

ทำครบทั้ง 10 เส้น

| Method | Endpoint | Scope | Controller | Use case | Domain | |
|---|---|---|---|---|---|---|
| GET | `/staff/members` | `staff:read` | `Staff/StaffMembersController` | `Staff/ListStaffMembers/` | `Staff` | ✅ |
| POST | `/staff/members/invitations` | `staff:manage` | `Staff/StaffInvitationsController` | `Staff/InviteStaffMember/` | `Staff` | ✅ |
| POST | `/staff/members/invitations/{id}/accept` | token ในคำเชิญ | `Staff/StaffInvitationsController` | `Staff/AcceptInvitation/` | `Staff`, `Identity` | ✅ |
| PATCH | `/staff/members/{id}` | `staff:manage` | `Staff/StaffMembersController` | `Staff/UpdateStaffMember/` | `Staff` | ✅ |
| POST | `/staff/members/{id}/deactivate` | `staff:manage` | `Staff/StaffMembersController` | `Staff/DeactivateStaffMember/` | `Staff` | ✅ |
| GET | `/staff/staff-types` | `staff:read` | `Staff/StaffMembersController` | `Staff/ListStaffTypes/` | `Staff` | ✅ |
| GET | `/staff/roles` | `role:read` | `Staff/RolesController` | `Access/ListRoles/` | `Access` | ✅ |
| POST | `/staff/roles` | `role:manage` | `Staff/RolesController` | `Access/CreateRole/` | `Access` | ✅ |
| PUT | `/staff/roles/{id}/permissions` | `role:manage` | `Staff/RolesController` | `Access/ReplaceRolePermissions/` | `Access` | ✅ |
| GET | `/staff/permissions` | `role:read` | `Staff/RolesController` | `Access/ListRoles/` (`ListPermissionCatalogueHandler` อ่าน `ScopeCatalogue`) | `Access` | ✅ |

การเขียนทุกเส้นในตารางนี้มีกฎสองข้อคุมอยู่ (รายละเอียดใน [authorization.md](./authorization.md)) คือ ให้ scope ที่ตัวเองไม่ได้ถือไม่ได้ (`Access/ScopeGrantPolicy`) และห้ามเปลี่ยนจนคลินิกไม่เหลือเจ้าของที่ใช้งานอยู่ (`Access/OwnerLockoutGuard`) การรับคำเชิญคือการเรียก endpoint เดียวกันสองครั้ง ดู [authentication.md](./authentication.md) และ [assumptions.md § 30](./assumptions.md)

## Customer and Pet

ทำครบทั้ง 12 เส้น

| Method | Endpoint | Scope | Controller | Use case | Domain | |
|---|---|---|---|---|---|---|
| GET | `/staff/customers?q=` | `customer:read` | `Staff/CustomersController` | `Customers/SearchCustomers/` | `Customers` | ✅ |
| POST | `/staff/customers` | `customer:write` | `Staff/CustomersController` | `Customers/CreateCustomer/` | `Customers` | ✅ |
| GET | `/staff/customers/{id}` | `customer:read` | `Staff/CustomersController` | `Customers/GetCustomer/` | `Customers` | ✅ |
| POST | `/staff/customers/{id}/invite` | `customer:write` | `Staff/CustomersController` | `Customers/InviteToPortal/` | `Customers`, `Identity` | ✅ |
| POST | `/staff/customers/{id}/merge` | `customer:merge` | `Staff/CustomersController` | `Customers/MergeCustomers/` | `Customers`, `Patients` | ✅ |
| GET | `/staff/pets` | `patient:read` | `Staff/PetsController` | `Patients/ListPets/` | `Patients` | ✅ |
| POST | `/staff/pets` | `patient:write` | `Staff/PetsController` | `Patients/CreatePet/` | `Patients` | ✅ |
| GET | `/staff/pets/{id}` | `patient:read` | `Staff/PetsController` | `Patients/GetPet/` | `Patients` | ✅ |
| GET | `/staff/pets/{id}/history` | `record:read` | `Staff/PetHistoryController` | `Patients/GetPetHistory/` | `Patients`, `MedicalRecords` | ✅ |
| POST | `/staff/pets/{id}/owners` | `patient:write` | `Staff/PetCarersController` | `Patients/ManageCarers/` | `Patients` | ✅ |
| DELETE | `/staff/pets/{id}/owners/{cid}` | `patient:write` | `Staff/PetCarersController` | `Patients/ManageCarers/` | `Patients` | ✅ |
| POST | `/staff/pets/{id}/deceased` | `patient:write` | `Staff/PetsController` | `Patients/MarkPetDeceased/` | `Patients`, `Appointments` | ✅ |

การปิดเรกคอร์ดสัตว์ที่เสียชีวิต **ยกเลิกนัดในอนาคตทุกนัด** และแจ้งเจ้าของครั้งเดียว ใน transaction เดียว contract ไม่ได้บอกทั้งสองอย่าง และถ้าปล่อยตามที่ contract เงียบ ผลคือค่า default ที่แย่ที่สุด คือมีข้อความเตือนนัดหรือโทรศัพท์จากเคาน์เตอร์ถึงสัตว์ที่ตายไปแล้ว กฎและทางเลือกที่ไม่เลือกอยู่ใน [contract-extensions.md § 6](./contract-extensions.md)

## Services

ทำครบทั้ง 5 เส้น

| Method | Endpoint | Scope | Controller | Use case | Domain | |
|---|---|---|---|---|---|---|
| GET | `/services` | public | `Public/ServicesController` | `ServiceCatalog/ListBookableServices/` | `ServiceCatalog` | ✅ |
| GET | `/services/categories` | public | `Public/ServicesController` | `ServiceCatalog/ListBookableServices/` (`ListServiceCategoriesHandler` อ่าน `ServiceCategories`) | `ServiceCatalog` | ✅ |
| POST | `/staff/services` | `service:manage` | `Staff/ServicesController` | `ServiceCatalog/CreateService/` | `ServiceCatalog` | ✅ |
| PATCH | `/staff/services/{id}/pricing` | `service:manage` | `Staff/ServicesController` | `ServiceCatalog/UpdatePricing/` | `ServiceCatalog` | ✅ |
| PATCH | `/staff/services/{id}/availability` | `service:manage` | `Staff/ServicesController` | `ServiceCatalog/UpdateAvailability/` | `ServiceCatalog` | ✅ |

การเปลี่ยนราคา **เป็นการตั้งล่วงหน้า ไม่ใช่เขียนทับ** `PATCH .../pricing` เพิ่มเวอร์ชันราคาที่มีวันที่เข้าไปในตารางราคาของบริการ การอ่านทุกครั้งแสดงเวอร์ชันที่มีผลในวันที่ถาม (`ClinicService.PricingOn`) จึงใส่ราคาใหม่ไว้ก่อนวันที่มีผลได้ และราคาเก่ายังอยู่ในระบบ ราคาในแคตตาล็อกเป็น *ราคาตั้ง* บรรทัดในใบแจ้งหนี้ยังมีราคาต่อหน่วยของตัวเอง และยังไม่มีอะไรคัดลอกราคาตั้งไปใส่บิล ส่วน availability จำกัดว่าบริการจองออนไลน์ได้ที่สาขาไหนและช่วงเวลาไหน การหาช่วงว่างอ่านค่านี้ตอนคำนวณ slot

## Booking and Appointment

| Method | Endpoint | Scope | Controller | Use case | Domain | |
|---|---|---|---|---|---|---|
| GET | `/booking/availability` | public | `Public/BookingAvailabilityController` | `Appointments/GetAvailability/` | `Appointments`, `Scheduling` | ✅ |
| POST | `/booking/holds` | portal | `Portal/BookingHoldsController` | `Appointments/HoldSlot/` | `Appointments` | ✅ |
| POST | `/portal/appointments` | portal | `Portal/PortalAppointmentsController` | `Appointments/BookAppointment/` | `Appointments` | ✅ |
| POST | `/staff/appointments` | `appointment:write` | `Staff/AppointmentsController` | `Appointments/CreateStaffAppointment/` | `Appointments` | ✅ |
| GET | `/staff/appointments?date=&status=` | `appointment:read` | `Staff/AppointmentsController` | `Appointments/ListAppointments/` | `Appointments` | ✅ |
| GET | `/appointments/{id}` | ทั้งสองฝั่ง | `Account/AppointmentsController` | `Appointments/GetAppointment/` | `Appointments` | ✅ |
| POST | `/appointments/{id}/reschedule` | ทั้งสองฝั่ง | `Account/AppointmentActionsController` | `Appointments/RescheduleAppointment/` | `Appointments` | ✅ |
| POST | `/appointments/{id}/cancel` | ทั้งสองฝั่ง | `Account/AppointmentActionsController` | `Appointments/CancelAppointment/` | `Appointments` | ✅ |
| POST | `/appointments/{id}/confirm` | ทั้งสองฝั่ง | `Account/AppointmentActionsController` | `Appointments/ConfirmAppointment/` | `Appointments` | ✅ |
| POST | `/appointments/{id}/check-in` | `appointment:checkin` | `Account/AppointmentActionsController` | `Appointments/CheckInAppointment/` | `Appointments`, `Queueing` | ✅ |
| PATCH | `/staff/appointments/{id}` | `appointment:write` | `Staff/AppointmentsController` | `Appointments/UpdateAppointment/` | `Appointments` | ✅ |

`/appointments/{id}/*` ใช้ร่วมกันทั้งพนักงานและลูกค้า controller ตัวเดียวรับทั้งสองกลุ่ม ส่วน use case ใช้กฎความเป็นเจ้าของและเวลาตัดรอบที่ต่างกันระหว่างสองกลุ่ม ถ้าแยกเป็น controller `/staff/...` กับ `/portal/...` จะขัดกับ path ใน contract

route ที่ใช้ร่วมเหล่านี้ **ไม่มี attribute `[RequireScope]`** เพราะลูกค้าถือแค่ `portal` ไม่มี scope `appointment:*` เลย ถ้าใส่ attribute จะล็อกผู้ใช้ครึ่งหนึ่งที่ contract ระบุไว้ การตัดสินจึงอยู่ใน `Application/Appointments/AppointmentAccess.cs` แทน ซึ่งรับพนักงานตาม scope และสาขา รับลูกค้าตามการเป็นผู้ดูแลสัตว์ และตอบ `404` กับคนอื่นทั้งหมด ไม่ตอบ `403` เพราะ `403` เท่ากับยืนยันว่า id นัดนั้นมีอยู่จริง

### สองการเขียนที่ต้องให้ฐานข้อมูลช่วยกัน

การจองมี invariant สองข้อที่ชั้น application บังคับเองไม่ได้ ควรรู้ก่อนแก้โมดูลนี้

- **สัตวแพทย์ต้องไม่ถูกจองซ้อน** use case ตรวจ slot ซ้ำใน transaction ซึ่งจำเป็นแต่ไม่พอ ภายใต้ `READ COMMITTED` การจองสองรายการพร้อมกันต่างอ่านเห็นปฏิทินว่าง และไม่เห็นแถวที่อีกฝั่งยังไม่ commit exclusion constraint `ex_appointments_staff_slot` (migration `AppointmentSlotExclusion`) ปฏิเสธการเขียนรายการที่สอง แล้ว `PostgresErrorTranslator` แปลงกลับเป็น `slot_unavailable` ตาม contract
- **คนสองคนต้องไม่ได้เลขคิวเดียวกัน** `pg_advisory_xact_lock` ที่ใช้คีย์ต่อสาขาต่อวัน เรียงการออกเลขใน transaction ของการเช็กอิน และ unique index บน `(branch_id, service_date, number)` เป็นด่านสุดท้าย

## Queue

| Method | Endpoint | Scope | Controller | Use case | Domain | |
|---|---|---|---|---|---|---|
| GET | `/staff/queue?branch=&room=` | `queue:read` | `Staff/QueueController` | `Queueing/GetQueueBoard/` | `Queueing` | ✅ |
| POST | `/staff/queue/tickets` | `queue:write` | `Staff/QueueController` | `Queueing/IssueWalkInTicket/` | `Queueing` | ✅ |
| POST | `/staff/queue/{id}/assign` | `queue:write` | `Staff/QueueTicketActionsController` | `Queueing/AssignTicket/` | `Queueing` | ✅ |
| POST | `/staff/queue/{id}/call` | `queue:call` | `Staff/QueueTicketActionsController` | `Queueing/CallTicket/` | `Queueing`, `Notifications` | ✅ |
| POST | `/staff/queue/{id}/skip` | `queue:call` | `Staff/QueueTicketActionsController` | `Queueing/SkipTicket/` | `Queueing` | ✅ |
| GET | `/portal/queue/{id}` | portal | `Portal/PortalQueueController` | `Queueing/GetMyTicket/` | `Queueing` | ✅ |
| SSE | `/queue/stream?branch=` | ทั้งสองฝั่ง | `Realtime/QueueStreamController` | ส่งจาก `IQueueEventStream` | `Queueing` | ✅ |

SSE เป็นเส้นเดียวใน contract ที่ไม่ใช่ REST ตัวส่งอยู่ที่ `Infrastructure/Realtime/QueueEventBroker.cs` เป็นการกระจายภายใน process เดียว เพราะหนึ่งคลินิกคือหนึ่ง deployment ไม่มีอะไรต้องประสานข้ามเครื่อง controller เปิด stream แล้วส่ง domain event ต่อ (`ticket.called`, `ticket.updated`, `board.refresh`) ไม่มี `[RequireScope]` ด้วยเหตุผลเดียวกับ route นัดที่ใช้ร่วม และกรองให้ลูกค้าเห็นเฉพาะคิวของตัวเอง

สังเกตการแบ่ง scope ที่ contract วางไว้และโค้ดทำตาม คือ การจัดกระดาน (`assign`) ใช้ `queue:write` ส่วนการเรียกและการข้ามใช้ `queue:call` พนักงานต้อนรับจัดคิวได้ แต่การเรียกคนไข้เข้าห้องเป็นสิทธิ์แยก

เรื่องที่ควรรู้ก่อนต่อยอดคิว

- **การออกเลขทำภายใต้ advisory lock** ต่อสาขาต่อวัน ใน transaction ของการเช็กอินหรือ walk-in และมี unique index บน `(branch_id, service_date, number)` เป็นด่านสุดท้าย ทั้งสองทางเข้าคิวใช้ตัวออกเลขเดียวกัน walk-in กับนัดที่เช็กอินจึงไม่มีทางได้เลขซ้ำกัน
- **`skipped` เป็นสถานะรอ ไม่ใช่ทางออก** contract ขอให้ "เลื่อนไปท้ายแถว" คิวที่ถูกข้ามจึงยังอยู่ และเรียงหลังทุกคนที่ยังไม่ถูกข้าม ดู [assumptions.md § 17](./assumptions.md)
- **มีอีกสอง route ที่ contract ไม่ได้นิยาม** คือ `priority` และ `leave` ลำดับการเรียกคือ `priority` ก่อน แล้วคิวที่ยังไม่ถูกข้าม แล้วเวลาที่ถูกข้าม แล้ว `number` เคสฉุกเฉินจึงแซงทุกคนรวมถึงคนที่ถูกข้ามไปแล้ว ทั้งสองเส้นอยู่ใน [ส่วนขยาย contract](#ส่วนขยาย-contract)

## Consultation and Medical record

| Method | Endpoint | Scope | Controller | Use case | Domain | |
|---|---|---|---|---|---|---|
| POST | `/staff/consultations` | `consult:write` | `Staff/ConsultationsController` | `Consultations/OpenConsultation/` | `Consultations`, `Queueing`, `Appointments` | ✅ |
| PATCH | `/staff/consultations/{id}` | `consult:write` | `Staff/ConsultationsController` | `Consultations/UpdateConsultation/` | `Consultations` | ✅ |
| POST | `/staff/consultations/{id}/vitals` | `vitals:write` | `Staff/ConsultationPartsController` | `Consultations/RecordVitals/` | `Consultations` | ✅ |
| POST | `/staff/consultations/{id}/examination` | `consult:write` | `Staff/ConsultationPartsController` | `Consultations/RecordExamination/` | `Consultations` | ✅ |
| POST | `/staff/consultations/{id}/diagnoses` | `consult:write` | `Staff/ConsultationPartsController` | `Consultations/AddDiagnosis/` | `Consultations` | ✅ |
| POST | `/staff/consultations/{id}/treatments` | `consult:write` | `Staff/ConsultationPartsController` | `Consultations/AddTreatment/` | `Consultations` | ✅ |
| POST | `/staff/consultations/{id}/prescriptions` | `rx:write` | `Staff/ConsultationPartsController` | `Consultations/AddPrescription/` | `Prescriptions`, `Patients` | ✅ |
| POST | `/staff/consultations/{id}/close` | `record:sign` | `Staff/ConsultationPartsController` | `Consultations/CloseConsultation/` | `Consultations`, `MedicalRecords`, `Queueing`, `Appointments` | ✅ |
| GET | `/staff/medical-records/{id}` | `record:read` | `Staff/MedicalRecordsController` | `MedicalRecords/GetMedicalRecord/` | `MedicalRecords`, `Auditing` | ✅ |
| POST | `/staff/medical-records/{id}/amendments` | `record:amend` | `Staff/MedicalRecordsController` | `MedicalRecords/AmendMedicalRecord/` | `MedicalRecords` | ✅ |

route เขียนแปดเส้นแบ่งไว้สอง controller `ConsultationsController` เปิดเคสและบันทึกร่าง `ConsultationPartsController` ถือส่วนย่อยของเคสและการปิดเคส การแบ่งนี้ตาม scope ซึ่งเป็นจุดที่น่าสนใจที่สุดของส่วนนี้ อ่าน scope ไล่ลงมาจะเห็นโมเดล role ของ contract คือ `vitals:write` ให้ผู้ช่วยชั่งน้ำหนักได้อย่างเดียว `consult:write` คือพื้นที่ทำงานของสัตวแพทย์ `rx:write` คือการสั่งยา และ `record:sign` ซึ่งผู้ช่วยและพนักงานต้อนรับไม่มี คือการลงนามในเอกสารทางการแพทย์ถาวร

**กฎสามข้อที่ทำให้ส่วนนี้ทำงานตามที่ contract บอก**

- **เคสที่ปิดแล้วตอบ `423 record_locked` ไม่ใช่ `409`** การแก้ทุกอย่างบน aggregate ผ่านตัวกันจุดเดียว จึงไม่มีทางไหนลืมตรวจ ความต่างนี้สำคัญกับผู้เรียก `409` ชวนให้ลองใหม่ ส่วน `423` บอกว่าให้ส่ง amendment
- **เวชระเบียนที่ลงนามแล้วเก็บ snapshot ไม่ใช่ตัวชี้** `MedicalRecord.ContentJson` คือเคสทั้งก้อนที่ serialize ณ เวลาลงนาม เก็บเป็น `text` ไม่ใช่ `jsonb` เพราะ PostgreSQL จัดรูป `jsonb` ใหม่ตอนเขียน ไบต์ที่อ่านกลับมาจะไม่ตรงกับไบต์ที่ทำ hash `ContentHash` ครอบเนื้อหา ผู้ลงนาม และเวลาลงนาม และส่งกลับให้ผู้เรียกเป็น `integrity_verified`
- **amendment ต่อท้ายเสมอ** ต้นฉบับไม่ถูกแตะ แต่ละ amendment เก็บฉบับแก้ทั้งหมดพร้อม diff ระดับฟิลด์ และแก้ได้เฉพาะฟิลด์บรรยาย เพราะการรักษาถูกคิดเงินไปแล้ว และยาอาจจ่ายไปแล้ว ดู [assumptions.md § 20](./assumptions.md)

`CloseConsultation` เป็น transaction ที่กว้างที่สุดในระบบ คือลงนามเวชระเบียน ตั้งคิวเป็น served ปิดนัด และจองนัดติดตามผล ได้ทั้งหมดหรือไม่ได้เลย การแจ้งเตือนส่ง **หลัง** commit เพราะ LINE push ที่ล้มเหลวต้องไม่ทำให้เวชระเบียนที่ลงนามแล้ว rollback

**"ออกบิล" เกิดใน transaction เดียวกันนี้** การปิดเคสออกใบแจ้งหนี้ผ่าน `Billing/ConsultationBilling` ตัวอย่างใน contract เอง (`record.signed` แล้ว `invoice.issued ฿1,460`) จึงเป็นสิ่งที่ endpoint ทำจริง เคสที่ไม่มีการรักษาไม่ออกบิล เพราะบิล ฿0 คือเอกสารที่เคาน์เตอร์ต้องมาดู และเป็นแถวในรายงานรายได้ทุกฉบับ เพียงเพื่อบอกว่าไม่ได้คิดเงินอะไร

**คิดเงินเฉพาะการรักษา** "เข้าบิลอัตโนมัติ" เขียนไว้ที่ endpoint treatments ที่เดียว และการรักษาเป็นส่วนเดียวของเคสที่มีราคา `Prescription` บันทึกยา ขนาด และจำนวน ไม่มีเงินอยู่ตรงไหนเลย ยาคิดเงินตอนจ่ายจริง ("ตัดสต๊อกเมื่อจ่ายจริง") ซึ่งต้องใช้โมดูล Inventory เพื่อรู้ราคา ถ้าใส่ตัวเลขตอนนี้ จะเป็นค่าใช้จ่ายบนบิลลูกค้าที่ไม่มีอะไรในระบบรองรับ

## Customer portal

| Method | Endpoint | Scope | Controller | Use case | Domain | |
|---|---|---|---|---|---|---|
| GET | `/portal/home` | portal | `Portal/PortalHomeController` | `Patients/Portal/GetPortalHomeHandler` | ข้ามหลายโมดูล | ✅ |
| GET | `/portal/pets` | portal | `Portal/PortalPetsController` | `Patients/Portal/ListMyPetsHandler` | `Patients` | ✅ |
| PATCH | `/portal/pets/{id}` | portal | `Portal/PortalPetsController` | `Patients/Portal/UpdatePetByOwnerHandler` | `Patients` | ✅ |
| GET | `/portal/pets/{id}/records` | portal | `Portal/PortalPetRecordsController` | `MedicalRecords/GetOwnerVisibleRecords/` | `MedicalRecords`, `Laboratory`, `Patients` | ✅ |
| GET | `/portal/appointments` | portal | `Portal/PortalAppointmentsController` | `Appointments/ListMyAppointments/` | `Appointments` | ✅ |
| GET | `/portal/notifications` | portal | `Portal/PortalNotificationsController` | `Notifications/ListMyNotifications/` | `Notifications` | ✅ |
| POST | `/portal/documents` | portal | `Portal/PortalDocumentsController` | `Patients/Documents/UploadOwnerDocumentHandler` | `Patients` และที่เก็บไฟล์ | ✅ |
| GET | `/portal/invoices` | portal | `Portal/PortalInvoicesController` | `Billing/ListMyInvoices/` | `Billing` | ✅ |
| GET | `/portal/packages` | portal | `Portal/PortalPackagesController` | `Crm/GetMyPackageBalance/` | `Crm` | ✅ |

`PATCH /portal/pets/{id}` รับเฉพาะรูป ชื่อเล่น อาหาร และพฤติกรรม ฟิลด์ทางคลินิกถูกปฏิเสธที่ขอบ DTO คือ DTO ฝั่งเจ้าของไม่มีฟิลด์เหล่านั้นอยู่เลย คำขอที่ปลอมขึ้นจึงเข้าไม่ถึง

ข้อความทุกฉบับที่ผ่านด่านความยินยอมถูกเก็บลงกล่องแจ้งเตือนในเว็บด้วย (`ConsentAwareNotificationDispatcher`) การกดว่าอ่านแล้วเป็นส่วนขยาย `POST /portal/notifications/read` ส่วน `/portal/packages` อ่านได้แล้ว แต่จะว่างจนกว่าจะมี `POST /staff/packages` ของ Tier 3 รายละเอียดอยู่ใน [portal ลูกค้า](features/portal.md)

## Schedules and Shifts

| Method | Endpoint | Scope | Controller | Use case | Domain | |
|---|---|---|---|---|---|---|
| GET | `/staff/schedules?from=&to=` | `schedule:read` | `Staff/SchedulesController` | `Scheduling/GetScheduleCalendar/` | `Scheduling` | ✅ |
| POST | `/staff/schedules/shifts` | `schedule:write` | `Staff/SchedulesController` | `Scheduling/CreateShifts/` | `Scheduling` | ✅ |
| POST | `/staff/schedules/time-off` | `schedule:write` | `Staff/SchedulesController` | `Scheduling/RequestTimeOff/` | `Scheduling`, `Appointments` | ✅ |
| GET | `/staff/schedules/availability` | `schedule:read` | `Staff/SchedulesController` | `Scheduling/GetStaffAvailability/` | `Scheduling` | ✅ |
| PUT | `/staff/schedules/assignment-rules` | `schedule:write` | `Staff/SchedulesController` | `Scheduling/ReplaceAssignmentRules/` | `Scheduling` | ✅ |

กะทำงานคือ input ของ `/booking/availability` ช่วงว่างมีได้เฉพาะที่มีกะ หักนัดที่จองแล้ว และวันหยุด เวลาพักและวันลาไม่ต้องหักแยก เพราะทั้งสองอย่างเป็นช่องว่างในกะอยู่แล้ว (เวลาพักแบ่งกะเป็นสองกะ วันลาตัดเวลาออกจากกะ) รายละเอียดอยู่ใน [ตารางเวร](features/schedules.md)

## Advanced appointment

| Method | Endpoint | Scope | Controller | Use case | Domain | |
|---|---|---|---|---|---|---|
| POST | `/staff/appointments/recurring` | `appointment:write` | `Staff/RecurringAppointmentsController` | `Appointments/CreateRecurringSeries/` | `Appointments`, `Scheduling` | ✅ |
| GET | `/staff/appointments/conflicts` | `appointment:read` | `Staff/AppointmentConflictsController` | `Appointments/DetectConflicts/` | `Appointments`, `Scheduling` | ✅ |
| POST | `/staff/waiting-list` | `appointment:write` | `Staff/WaitingListController` | `Appointments/AddToWaitingList/` | `Appointments`, `Notifications` | ✅ |
| POST | `/staff/appointments/{id}/no-show` | `appointment:write` | `Staff/AppointmentsController` | `Appointments/MarkNoShow/` | `Appointments` | ✅ |
| PUT | `/staff/appointment-types` | `settings:write` | `Staff/AppointmentTypesController` | `Appointments/ReplaceAppointmentTypes/` | `Appointments` | ✅ |
| PUT | `/staff/policies/cancellation` | `settings:write` | `Staff/CancellationPolicyController` | `Appointments/ReplaceCancellationPolicy/` | `Appointments` | ✅ |

ประเภทนัดและนโยบายยกเลิกไม่ได้เก็บไว้เฉย ๆ การจองของลูกค้าอ่านว่าประเภทนั้นจองออนไลน์ได้ไหมและต้องจองล่วงหน้ากี่นาที ทุกทางจองอ่านระยะเวลา การเช็กอินตั้งความเร่งด่วนของคิวตามประเภท และการยกเลิกของลูกค้าที่กระชั้นกว่า cutoff ถูกนับเป็นครั้งที่ผิดนัด รายละเอียดอยู่ใน [นัดขั้นสูง](features/advanced-booking.md)

## Advanced patient management

| Method | Endpoint | Scope | Controller | Use case | Domain | |
|---|---|---|---|---|---|---|
| GET | `/staff/pets/{id}/vaccinations` | `patient:read` | `Staff/PetVaccinationsController` | `Patients/ListVaccinations/` | `Patients` | ✅ |
| POST | `/staff/pets/{id}/vaccinations` | `record:write` | `Staff/PetVaccinationsController` | `Patients/RecordVaccination/` | `Patients` | ✅ |
| POST | `/staff/pets/{id}/allergies` | `record:write` | `Staff/PetsController` | `Patients/RecordAllergy/` | `Patients` | ✅ |
| GET | `/staff/pets/{id}/weights` | `patient:read` | `Staff/PetWeightsController` | `Patients/GetWeightHistory/` (อ่านจาก `consultation_vitals`) | `Consultations` | ✅ |
| POST | `/staff/pets/{id}/treatment-plans` | `consult:write` | `Staff/TreatmentPlansController` | `Patients/CreateTreatmentPlan/` | `Patients`, `Appointments` | ✅ |
| POST | `/staff/pets/{id}/documents` | `record:write` | `Staff/PetDocumentsController` | `Patients/Documents/CreateDocumentUploadUrlHandler` | `Patients` และที่เก็บไฟล์ | ✅ |

ทำแพ้ยาก่อนส่วนอื่นของหมวดนี้ เพราะ contract ให้ endpoint สั่งยารับผิดชอบ "ตรวจแพ้ยา" และการตรวจที่ไม่มีข้อมูลให้ตรวจเทียบเป็นแค่ของประดับ

วันครบกำหนดวัคซีนเก็บไว้ที่ตัววัคซีนแต่ละเข็ม (`next_due_on`) เพื่อให้กติกาเตือนของโมดูล Automation อ่านได้เมื่อสร้าง แผนการรักษาจองนัดทุกครั้งผ่านตัวจองชุดนัดตัวเดียวกับ `POST /staff/appointments/recurring` ไฟล์ใช้ pre-signed URL ที่ API ลงชื่อเอง ดูส่วนขยาย § 12

`GET /staff/pets/{id}/weights` ไม่มีตารางของตัวเอง ชุดน้ำหนักอ่านจาก `consultation_vitals` ตามสัตว์ ถ้ามีตารางน้ำหนักแยก จะเป็นสำเนาที่สองของค่าวัดเดียวกัน และขัดกับเวชระเบียนต้นทางได้

## Lab, Prescription and medical documents

| Method | Endpoint | Scope | Controller | Use case | Domain | |
|---|---|---|---|---|---|---|
| POST | `/staff/lab-requests` | `lab:request` | `Staff/LabRequestsController` | `Laboratory/CreateLabRequest/` | `Laboratory` | ✅ |
| POST | `/staff/lab-requests/{id}/results` | `lab:write` | `Staff/LabResultsController` | `Laboratory/RecordLabResults/` | `Laboratory` | ✅ |
| GET | `/staff/lab-requests?status=pending` | `lab:read` | `Staff/LabRequestsController` | `Laboratory/ListLabRequests/` | `Laboratory` | ✅ |
| GET | `/staff/prescriptions?pet_id=` | `rx:read` | `Staff/PrescriptionsController` | `Prescriptions/ListPrescriptions/` | `Prescriptions` | ✅ |
| POST | `/staff/prescriptions/{id}/dispense` | `rx:dispense` | `Staff/PrescriptionDispenseController` | `Prescriptions/DispensePrescription/` | `Prescriptions` (ยังไม่ตัดสต๊อก) | ✅ |
| GET | `/staff/templates/treatments` | `consult:read` | `Staff/TreatmentTemplatesController` | `Consultations/ListTreatmentTemplates/` | `Consultations` | ✅ |
| POST | `/staff/documents/generate` | `record:read` | `Staff/DocumentGenerationController` | `MedicalRecords/GenerateDocument/` | `MedicalRecords` และตัวสร้าง PDF (QuestPDF) | ✅ |
| GET | `/staff/medical-records/{id}/audit` | `record:audit` | `Staff/MedicalRecordsController` | `MedicalRecords/GetRecordAudit/` | `MedicalRecords`, `Auditing` | ✅ |

มุมมอง audit ของเวชระเบียนตอบจากสองแหล่ง เพราะคำถามมีสองครึ่ง amendment บอกว่า *ตอนนี้เอกสารเขียนว่าอะไร และมาถึงตรงนี้อย่างไร* ส่วน audit entry บอกว่า *ใครแตะ จากที่ไหน เมื่อไร* รวมถึงการเปิดดู ซึ่งไม่ทิ้งร่องรอยในตัวเอกสาร

ใบสั่งยา **ถูกเขียน** โดยโมดูลการตรวจ แต่ยัง **ไม่ถูกจ่าย** contract แยกสองอย่างนี้ ("ตัดสต๊อกเมื่อจ่ายจริง") entity ก็แยกเช่นกัน `DispensedAt` เป็น null จนกว่าจะมี `rx:dispense` และคลังยาไม่ถูกแตะก่อนหน้านั้น

การจ่ายยาตอนนี้บันทึกว่าจ่ายแล้วและคืนข้อมูลฉลาก แต่ **ยังไม่ตัดสต๊อก** เพราะยังไม่มีโมดูล Inventory คำตอบบอกตรง ๆ ว่า `stock_deducted: false` แล็บมีส่วนขยายหนึ่งเส้นคือ `POST /staff/lab-requests/{id}/review` สำหรับกดว่าอ่านผลแล้ว ดู [งานรักษาต่อเนื่อง](features/clinical-records.md)

## Notifications and Messaging

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| POST | `/staff/notifications/send` | `notify:send` | `Staff/NotificationsController` | `Notifications/SendNotification/` | `Notifications` |
| GET | `/staff/notifications/templates` | `notify:manage` | `Staff/NotificationTemplatesController` | `Notifications/ListTemplates/` | `Notifications` |
| GET | `/staff/notifications/log` | `notify:read` | `Staff/NotificationLogController` | `Notifications/GetDeliveryLog/` | `Notifications` |
| GET | `/staff/threads` | `message:read` | `Staff/ThreadsController` | `Messaging/ListThreads/` | `Messaging` |
| POST | `/staff/threads/{id}/messages` | `message:write` | `Staff/ThreadMessagesController` | `Messaging/ReplyToThread/` | `Messaging` |

## LINE integration

**LINE เป็นช่องทางส่งการแจ้งเตือนออก ไม่ใช่ผู้ยืนยันตัวตน** ไม่มีการเข้าสู่ระบบด้วย LINE ไม่มีการผูกบัญชี ไม่มีการจับคู่เบอร์โทรกับ LINE ที่อยู่ LINE ของลูกค้าเป็นข้อมูลสำหรับส่งข้อความในเรกคอร์ดลูกค้า เหมือนอีเมล ไม่ใช่ credential ไม่ใช่ primary key และไม่ใช่เซสชัน ดู [assumptions.md § A](./assumptions.md)

```text
Application -> Notification port -> Infrastructure/Notifications/Line/ -> LINE Messaging API
```

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| PUT | `/staff/integrations/line` | `integration:manage` | `Staff/LineIntegrationController` | `Integrations/ConfigureLineChannel/` | `Integrations` |
| POST | `/staff/integrations/line/broadcast` | `notify:broadcast` | `Staff/LineBroadcastController` | `Notifications/BroadcastToSegment/` | `Notifications`, `Crm` |
| POST | `/webhooks/line` | ลายเซ็น LINE | `Webhooks/LineWebhookController` | `Integrations/HandleLineWebhook/` | `Integrations` |

`POST /webhooks/line` เป็น `[AllowAnonymous]` และยืนยันด้วยลายเซ็นของ LINE channel แทน bearer token เป็น callback ของ integration รับ event `follow`, `message` และ postback แล้วส่งต่อให้ handler ไม่ออก token และไม่สร้างบัญชี

postback action ใน `Domain/Integrations/LinePostbackActions.cs` ทำงานกับเรกคอร์ดจริง handler จึงต้องหาที่อยู่ LINE ที่ส่งมากลับไปเป็นลูกค้าที่ลงทะเบียนที่อยู่นั้นไว้ แล้วใช้กฎสิทธิ์ของ portal ตามปกติ นี่คือการค้นจากที่อยู่ส่ง ไม่ใช่การยืนยันตัวตน เรื่องการระบุผู้ส่งยัง [เปิดอยู่](./assumptions.md) (§ B)

**อยู่นอกขอบเขต มีใน reference แต่ไม่สร้าง** `POST /auth/line/callback`, `POST /portal/line/link`, `DELETE /portal/line/link`, `GET /line/liff/booking-context` ทั้งสี่เส้นมีไว้เพื่อใช้ LINE เป็นตัวตนเท่านั้น ดู [assumptions.md § A](./assumptions.md)

## Operational statistics

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| GET | `/staff/dashboard/today` | `dashboard:read` | `Staff/DashboardController` | `Analytics/GetTodayDashboard/` | `Analytics` |
| GET | `/staff/stats/appointments` | `stats:read` | `Staff/AppointmentStatsController` | `Analytics/GetAppointmentStats/` | `Analytics` |
| GET | `/staff/stats/queue` | `stats:read` | `Staff/QueueStatsController` | `Analytics/GetQueueStats/` | `Analytics` |
| GET | `/staff/stats/workload` | `stats:read` | `Staff/WorkloadStatsController` | `Analytics/GetWorkloadStats/` | `Analytics` |

## Business dashboard

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| GET | `/staff/analytics/revenue` | `analytics:read` | `Staff/RevenueAnalyticsController` | `Analytics/GetRevenueAnalytics/` | `Analytics` |
| GET | `/staff/analytics/services` | `analytics:read` | `Staff/ServiceAnalyticsController` | `Analytics/GetServiceAnalytics/` | `Analytics` |
| GET | `/staff/analytics/retention` | `analytics:read` | `Staff/RetentionAnalyticsController` | `Analytics/GetRetentionAnalytics/` | `Analytics` |
| GET | `/staff/analytics/staff-performance` | `analytics:hr` | `Staff/StaffPerformanceController` | `Analytics/GetStaffPerformance/` | `Analytics` |

analytics อ่านผ่าน read model หรือ SQL projection เฉพาะ ไม่โหลด aggregate เป็นที่เดียวที่คาดว่าจะมี SQL เขียนมือใน repository และเหมาะสมที่จะมี

## Billing and Payments

| Method | Endpoint | Scope | Controller | Use case | Domain | |
|---|---|---|---|---|---|---|
| POST | `/staff/invoices` | `invoice:write` | `Staff/InvoicesController` | `Billing/CreateInvoice/` | `Billing` | ✅ |
| POST | `/staff/invoices/{id}/discounts` | `invoice:discount` | `Staff/InvoiceTransactionsController` | `Billing/ApplyDiscount/` | `Billing` | ✅ |
| POST | `/staff/invoices/{id}/payments` | `payment:collect` | `Staff/InvoiceTransactionsController` | `Billing/CollectPayment/` | `Billing` | ✅ |
| POST | `/staff/invoices/{id}/refunds` | `payment:refund` | `Staff/InvoiceTransactionsController` | `Billing/RefundPayment/` | `Billing` | ✅ |
| POST | `/staff/invoices/{id}/receipt` | `receipt:issue` | `Staff/InvoiceTransactionsController` | `Billing/IssueReceipt/` | `Billing` | ✅ |
| GET | `/portal/invoices` | portal | `Portal/PortalInvoicesController` | `Billing/ListMyInvoices/` | `Billing` | ✅ |

ร่างแรกของตารางนี้มีสี่ controller แยกหนึ่งตัวต่อหนึ่ง action ตอนสร้างจริงรวมเหลือสองตัว ด้วยเหตุผลเดียวกับส่วนย่อยของการตรวจ `InvoicesController` ถือตัวเอกสาร (สร้าง อ่าน รายการ void) และ `InvoiceTransactionsController` ถือเงินที่เคลื่อนไหวกับเอกสารนั้น controller action เดียวสี่ตัวคือสี่ไฟล์ที่มี route ไฟล์ละเส้น

**สถานะคำนวณเอา ไม่เคยถูกตั้ง** `open`, `partially_paid`, `paid`, `refunded` คำนวณใหม่จากรายการ ส่วนลด การชำระ และการคืนเงินของใบนั้นหลังทุกการเคลื่อนไหว คอลัมน์สถานะที่ผู้เรียกตั้งได้ วันหนึ่งจะเขียนว่า `paid` ทั้งที่ยังค้างยอด และความระวังที่จุดเรียกกันเรื่องนี้ไม่ได้

**การชำระเงินไม่มี gateway** `PUT /staff/integrations/payment` ตั้งค่าผู้ให้บริการที่ระบบนี้ไม่มี การบันทึกการจ่ายด้วยบัตรจึงหมายถึง "เครื่องรูดบัตรอนุมัติแล้ว และเลขสลิปอยู่ใน `reference`" ไม่ใช่ "ระบบตัดบัตรแล้ว" เมื่อมี gateway ให้วางไว้ *หน้า* handler ไม่ใช่ข้างใน เพราะการอนุมัติบัตรเป็นการเรียกผ่านเครือข่าย และต้องไม่เกิดขณะ transaction ถือ row lock ของใบแจ้งหนี้

อีกสาม route ในส่วนนี้ **ไม่อยู่ใน contract** ดูใน [ส่วนขยาย contract](#ส่วนขยาย-contract) ด้านล่าง

## Inventory

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| GET | `/staff/inventory/items` | `inventory:read` | `Staff/InventoryItemsController` | `Inventory/ListItems/` | `Inventory` |
| POST | `/staff/inventory/movements` | `inventory:write` | `Staff/InventoryMovementsController` | `Inventory/RecordMovement/` | `Inventory` |
| POST | `/staff/inventory/adjustments` | `inventory:adjust` | `Staff/InventoryAdjustmentsController` | `Inventory/AdjustStock/` | `Inventory` |
| GET | `/staff/inventory/alerts` | `inventory:read` | `Staff/InventoryAlertsController` | `Inventory/ListAlerts/` | `Inventory` |
| POST | `/staff/inventory/purchase-orders` | `inventory:purchase` | `Staff/PurchaseOrdersController` | `Inventory/CreatePurchaseOrder/` | `Inventory` |

## Loyalty, Packages and Promotions

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| POST | `/staff/segments` | `crm:manage` | `Staff/SegmentsController` | `Crm/CreateSegment/` | `Crm` |
| POST | `/staff/packages` | `crm:manage` | `Staff/PackagesController` | `Crm/CreatePackage/` | `Crm` |
| GET | `/portal/packages` | portal | `Portal/PortalPackagesController` | `Crm/GetMyPackageBalance/` | `Crm` (✅ อยู่ในตาราง Customer portal) |
| POST | `/staff/coupons` | `crm:manage` | `Staff/CouponsController` | `Crm/CreateCoupon/` | `Crm` |
| GET | `/staff/loyalty/{customer_id}` | `crm:read` | `Staff/LoyaltyController` | `Crm/GetLoyaltyAccount/` | `Crm` |

## Reminders and Automation

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| GET | `/staff/automations` | `automation:read` | `Staff/AutomationsController` | `Automation/ListAutomations/` | `Automation` |
| POST | `/staff/automations` | `automation:manage` | `Staff/AutomationsController` | `Automation/CreateAutomation/` | `Automation` |
| POST | `/staff/automations/{id}/simulate` | `automation:manage` | `Staff/AutomationSimulationController` | `Automation/SimulateAutomation/` | `Automation` |
| GET | `/staff/reminders/upcoming` | `automation:read` | `Staff/RemindersController` | `Automation/ListUpcomingReminders/` | `Automation` |

กฎ automation คือ `trigger + condition + action` ประเมินที่ฝั่ง server โดย hosted service ใน `Infrastructure/Jobs/` (โฟลเดอร์ยังว่าง) `simulate` ใช้ตัวประเมินเดียวกันในโหมด dry-run เป็นเส้นทางโค้ดเดียว ผลพรีวิวจึงไม่มีทางต่างจากการรันจริง

## Reports and Audit

| Method | Endpoint | Scope | Controller | Use case | Domain | |
|---|---|---|---|---|---|---|
| POST | `/staff/reports/exports` | `report:export` | `Staff/ReportExportsController` | `Reporting/StartExport/` | `Reporting` | |
| GET | `/staff/reports/exports/{id}` | `report:export` | `Staff/ReportExportsController` | `Reporting/GetExportStatus/` | `Reporting` | |
| GET | `/staff/audit-logs` | `audit:read` | `Staff/AuditLogsController` | `Auditing/ListAuditLogs/` | `Auditing` | ✅ |
| GET | `/staff/audit-logs/access` | `audit:read` | `Staff/AuditLogsController` | `Auditing/ListAuditLogs/` (ตัวกรอง access) | `Auditing` | ✅ |

handler ตัวเดียวรับทั้งสอง route แยกด้วยตัวกรอง สองเส้นต่างกันที่ action ที่เลือก ไม่ได้ต่างที่พฤติกรรม ถ้าแยก handler จะต้องดูแลการจำกัดสาขาสองที่ ซึ่งเป็นการซ้ำแบบที่ทำ audit log รั่วในการแก้ครั้งที่สอง

**สิ่งที่ต้องเขียนลง audit เป็นรายชื่อที่ตั้งชื่อไว้** ไม่ใช่ธรรมเนียม คือ `Domain/Auditing/AuditActions.cs` `AuditEntry.Write` ปฏิเสธ action ที่ไม่อยู่ในรายชื่อ การพิมพ์ผิดจึงสร้างหมวดที่ไม่มีใคร query ขึ้นมาเงียบ ๆ ไม่ได้ contract มี endpoint อ่าน audit log แต่ไม่เคยบอกว่าต้องเขียนอะไร ดู [contract-extensions.md § 5](./contract-extensions.md)

## Webhooks and Integrations

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| POST | `/staff/webhooks` | *(ไม่ระบุ)* | `Staff/WebhooksController` | `Integrations/CreateWebhookSubscription/` | `Integrations` |
| GET | `/staff/webhooks/{id}/deliveries` | *(ไม่ระบุ)* | `Staff/WebhookDeliveriesController` | `Integrations/ListWebhookDeliveries/` | `Integrations` |
| POST | `/staff/api-keys` | *(ไม่ระบุ)* | `Staff/ApiKeysController` | `Integrations/CreateApiKey/` | `Integrations` |
| PUT | `/staff/integrations/payment` | *(ไม่ระบุ)* | `Staff/PaymentIntegrationController` | `Integrations/ConfigurePaymentProvider/` | `Integrations` |
| PUT | `/staff/integrations/lab` | *(ไม่ระบุ)* | `Staff/LabIntegrationController` | `Integrations/ConfigureLabProvider/` | `Integrations` |
| GET | `/staff/integrations/calendar/ics` | *(ไม่ระบุ)* | `Staff/CalendarFeedController` | `Integrations/GetCalendarFeed/` | `Integrations` |

event ของ webhook ขาออก (`appointment.created` ถึง `pet.created`) ประกาศไว้ใน `Domain/Integrations/WebhookEventTypes.cs` แผนคือส่งจาก `Infrastructure/Webhooks/` พร้อม HMAC-SHA256 ใน `X-Signature` และลองใหม่แบบ exponential ห้าครั้งภายใน 24 ชั่วโมง ตอนนี้โฟลเดอร์ `Infrastructure/Webhooks/` ยังว่าง

## ส่วนขยาย contract

endpoint 21 เส้นที่ **ไม่อยู่** ใน `API Reference.dc.html` 12 เส้นแรกและ 4 เส้นสุดท้ายปิดช่องโหว่ที่เจอตอนสร้างตาม contract ส่วนสองเส้นของบทความและสามเส้นของทีมเป็นฟีเจอร์ที่ขอเพิ่ม แต่ละเส้นได้รับอนุมัติก่อนสร้าง ไม่ได้ทำเอาเอง และแต่ละเส้นมีหมายเหตุว่าเป็นส่วนขยายใน controller ของตัวเอง เหตุผล ทางเลือกที่ไม่เลือก และสิ่งที่ตั้งใจไม่ทำ อยู่ใน [contract-extensions.md](./contract-extensions.md)

| Method | Endpoint | Scope | Controller | Use case | ช่องโหว่ | |
|---|---|---|---|---|---|---|
| POST | `/staff/queue/{id}/priority` | `queue:write` | `Staff/QueueTicketActionsController` | `Queueing/SetTicketPriority/` | § 1 | ✅ |
| POST | `/staff/queue/{id}/leave` | `queue:write` | `Staff/QueueTicketActionsController` | `Queueing/LeaveQueue/` | § 2 | ✅ |
| GET | `/portal/consents` | portal | `Portal/PortalPrivacyController` | `Customers/GetConsents/` | § 4 | ✅ |
| PUT | `/portal/consents` | portal | `Portal/PortalPrivacyController` | `Customers/RecordConsent/` | § 4 | ✅ |
| GET | `/staff/customers/{id}/consents` | `customer:read` | `Staff/PrivacyController` | `Customers/GetConsents/` | § 4 | ✅ |
| POST | `/staff/customers/{id}/consents` | `customer:write` | `Staff/PrivacyController` | `Customers/RecordConsent/` | § 4 | ✅ |
| POST | `/portal/data-requests` | portal | `Portal/PortalPrivacyController` | `Customers/DataRequests/` | § 4 | ✅ |
| GET | `/staff/data-requests` | `customer:read` | `Staff/PrivacyController` | `Customers/DataRequests/` | § 4 | ✅ |
| POST | `/staff/data-requests/{id}/close` | `customer:write` | `Staff/PrivacyController` | `Customers/DataRequests/` | § 4 | ✅ |
| POST | `/staff/invoices/{id}/void` | `invoice:write` | `Staff/InvoicesController` | `Billing/VoidInvoice/` | § 3 | ✅ |
| GET | `/staff/invoices/{id}` | `invoice:write` | `Staff/InvoicesController` | `Billing/GetInvoice/` | § 7 | ✅ |
| GET | `/staff/invoices` | `invoice:write` | `Staff/InvoicesController` | `Billing/ListInvoices/` | § 7 | ✅ |
| GET | `/articles` | สาธารณะ | `Public/ArticlesController` | `Articles/ListPublishedArticles/` | § 9 | ✅ |
| GET | `/articles/{slug}` | สาธารณะ | `Public/ArticlesController` | `Articles/GetArticle/` | § 9 | ✅ |
| GET | `/team` | สาธารณะ | `Public/TeamController` | `Staff/ListTeam/` | § 10 | ✅ |
| GET | `/staff/departments` | `staff:read` | `Staff/StaffMembersController` | `Staff/ListDepartments/` | § 10 | ✅ |
| PUT | `/staff/members/{id}/public-profile` | `staff:manage` | `Staff/StaffMembersController` | `Staff/ReplacePublicProfile/` | § 10 | ✅ |
| POST | `/staff/lab-requests/{id}/review` | `lab:read` | `Staff/LabRequestsController` | `Laboratory/ReviewLabResults/` | § 11 | ✅ |
| PUT | `/storage/uploads/{token}` | token ที่ลงชื่อ | `Storage/StorageController` | `Patients/Documents/StoredFilesHandler` | § 12 | ✅ |
| GET | `/storage/files/{token}` | token ที่ลงชื่อ | `Storage/StorageController` | `Patients/Documents/StoredFilesHandler` | § 12 | ✅ |
| POST | `/portal/notifications/read` | portal | `Portal/PortalNotificationsController` | `Notifications/ListMyNotifications/` (`MarkNotificationsReadHandler`) | § 13 | ✅ |

§ 10 ยังเพิ่มของบน endpoint เดิมของ contract โดยไม่เปลี่ยนของเดิม คือ `GET /staff/members` รับ `?department=` และ `?role=` และทุกแถวมี `department` กับ `public_profile` ส่วน `PATCH /staff/members/{id}` รับ `department` contract ระบุแค่ scope และคำอธิบายบรรทัดเดียวของสองเส้นนี้ ไม่ได้กำหนดรูปของ request หรือ response จึงไม่มีอะไรที่เอกสารกำหนดถูกแก้

ส่วนขยายด้านบิลทั้งสามใช้ `invoice:write` เพราะรายการ scope ของ contract ปิดตาย `PUT /staff/roles/{id}/permissions` ถูกกำหนดให้ปฏิเสธ scope ที่ไม่รู้จัก ถ้าคิด `invoice:read` หรือ `invoice:void` ขึ้นมาเอง endpoint นั้นจะไม่มี role ใดใน reference เรียกได้เลย นี่คือทางที่ประดิษฐ์น้อยที่สุด ไม่ใช่ทางที่ดีที่สุด

ส่วนขยายอีกสองข้อ **ไม่ได้เพิ่ม endpoint** ซึ่งควรสังเกต เพราะสิ่งที่ contract ขาดส่วนใหญ่ไม่ใช่ endpoint

| ช่องโหว่ | คำตอบ | อยู่ที่ |
|---|---|---|
| § 5 ไม่มีที่ไหนบอกว่าต้อง audit อะไร | รายชื่อที่ตั้งชื่อและตรวจได้ | `Domain/Auditing/AuditActions.cs` |
| § 6 นัดในอนาคตของสัตว์ที่เสียชีวิตไม่มีนิยาม | กฎที่เขียนชัดบน endpoint ของ contract เอง | `Patients/MarkPetDeceased/` |

ความยินยอมบังคับที่ notification port ไม่ใช่ในแต่ละ use case `ConsentAwareNotificationDispatcher` ครอบหน้าทุกช่องทาง และ `INotificationDispatcher` ซึ่งเป็นสิ่งเดียวที่ use case resolve ได้ *ก็คือ* decorator ตัวนั้น จึงไม่มีทางส่งข้อความโดยไม่ผ่านด่านนี้

## ความครอบคลุม

API reference มี **164 endpoint** ยืนยันโดยนับป้าย method ใน HTML ต้นฉบับ คือ 164 แถวในตาราง ไม่มีแถวซ้ำ คู่ `method + path` ไม่ซ้ำ 164 คู่ และไม่มี endpoint ที่พูดถึงในเนื้อความแต่ไม่อยู่ในตาราง รายละเอียดการนับอยู่ใน [assumptions.md § 1](./assumptions.md)

| | |
|---|---|
| endpoint ใน reference | **164** |
| มีที่อยู่ในตารางข้างบน | **160** |
| ตัดออกโดยการตัดสินขอบเขต (LINE เป็นตัวตน) | **4** |
| ทำแล้วและมีเทสต์ (✅) | **123** |
| เพิ่มนอก contract | **21** ดู [ส่วนขยาย contract](#ส่วนขยาย-contract) |

ตรวจเทียบกับ contract ด้วยโปรแกรมแล้ว ได้ 160 เส้นที่มีที่อยู่ ไม่มีเส้นที่แต่งขึ้น และ 4 เส้นที่ตั้งใจตัดออก (`GET /portal/invoices` และ `GET /portal/packages` อยู่ในตารางสองที่ คือใต้ Customer portal และใต้โมดูลเจ้าของ ตารางจึงมี 162 แถวสำหรับ 160 endpoint)

สี่เส้นที่ตัดออกคือ `POST /auth/line/callback`, `POST /portal/line/link`, `DELETE /portal/line/link` และ `GET /line/liff/booking-context` ดู [assumptions.md § A](./assumptions.md)

**ไม่มีอะไรในนี้ที่อนุมานขึ้นเอง** หัวของ reference บอกว่ามี **214 endpoint** แต่เอกสารไม่มีเส้นเหล่านั้น และไม่มีการแต่ง endpoint ขึ้นมาให้ครบจำนวน ความต่างนี้แจ้งเจ้าของ contract แล้ว ระหว่างที่ยังไม่มีคำตอบ ขอบเขตงานคือ 164 เส้น ถ้า endpoint ใดไม่อยู่ในตารางข้างบน แปลว่ายังไม่มี และต้องมาจาก contract ฉบับปรับปรุง ไม่ใช่มาจากการเดา

21 เส้นใน [ส่วนขยาย contract](#ส่วนขยาย-contract) เป็นข้อยกเว้นเดียว และแยกไว้ให้เห็นชัดด้วยเหตุนั้น แต่ละเส้นรายงานเป็นช่องโหว่ก่อน ได้รับอนุมัติทีหลัง แล้วจึงสร้าง และมีหมายเหตุว่าเป็นส่วนขยายใน controller ของตัวเอง ไม่มีเส้นใดเปลี่ยนชื่อ path ที่มีในเอกสาร เปลี่ยน method หรือแก้ request หรือ response ที่เอกสารกำหนด

## โค้ด

| ส่วน | ไฟล์ |
|---|---|
| controller ทั้งหมด | `backend/src/HappyPaws.Api/Controllers/` (`Account/`, `Auth/`, `Portal/`, `Public/`, `Realtime/`, `Staff/`, `Storage/`) |
| use case | `backend/src/HappyPaws.Application/<Module>/<UseCase>/` |
| รายการ scope | `backend/src/HappyPaws.Domain/Access/Scopes.cs`, `ScopeCatalogue.cs` |
| repository | `backend/src/HappyPaws.Infrastructure/Persistence/Repositories/<Module>/` |
| รูปแบบหน้าของรายการ | `backend/src/HappyPaws.Api/Http/CursorPageJsonConverter.cs` |
| แปลง error ของ PostgreSQL | `backend/src/HappyPaws.Infrastructure/Persistence/PostgresErrorTranslator.cs` |
| stream ของคิว | `backend/src/HappyPaws.Infrastructure/Realtime/QueueEventBroker.cs` |
| ด่านความยินยอม | `backend/src/HappyPaws.Infrastructure/Notifications/ConsentAwareNotificationDispatcher.cs` |
