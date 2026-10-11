# ดัชนี API

ดัชนีว่าแต่ละ endpoint อยู่ที่ controller ไหน และ use case ไหน ทั้งเส้นของ [`API Reference.dc.html`](../API%20Reference.dc.html) และเส้นที่เพิ่มทีหลัง ใช้หาโค้ดของ endpoint ที่สนใจได้ทันที

```text
API endpoint -> Controller -> Application use case -> Domain -> Data access
```

method, path และ scope คัดลอกจาก contract ตรงตัว เอกสารนี้ไม่ได้นิยาม API ใหม่ ช่องที่ contract ไม่ได้ระบุ scope เขียนว่า *(ไม่ระบุ)* และการตีความอยู่ใน [assumptions.md](./assumptions.md)

อ่านตารางอย่างไร

- ทุก path จริงอยู่ใต้ path base `/v1` เช่น `/clinic` เรียกจริงที่ `/v1/clinic` (ดู [assumptions.md § 31](./assumptions.md))
- กติกาของ response เช่นช่องที่เป็น null จะไม่ถูกส่งมาเลย อยู่ที่ [รูปแบบข้อมูลที่ส่ง](./development.md#รูปแบบข้อมูลที่ส่ง)
- path ของ controller นับจาก `backend/src/HappyPaws.Api/Controllers/`
- path ของ use case นับจาก `backend/src/HappyPaws.Application/`
- data access ทุกโมดูลเป็นแบบเดียวกัน คือ `HappyPaws.Infrastructure/Persistence/Repositories/<Module>` บน `HappyPawsDbContext` จึงไม่ใส่เป็นคอลัมน์ ช่อง Domain บอกชื่อ aggregate ส่วน endpoint ที่ต้องคุยกับระบบภายนอกจะระบุระบบนั้นไว้

## ดัชนีโดเมนไปโฟลเดอร์

ช่อง use case ที่เขียนเป็น `<Module>/<โฟลเดอร์>/<ชื่อ>Handler` คือ use case ที่รวมอยู่ในโฟลเดอร์ร่วม เช่น ทุกเรื่องของ portal ฝั่งสัตว์เลี้ยงอยู่ใน `Patients/Portal/`

| API domain | Backend module | Frontend feature | Frontend routes |
|---|---|---|---|
| Authentication | `Identity` | `authentication` | `(auth)/` |
| Account & Access | `Identity` | `account` | `(staff)/settings/account`, `(portal)/account` |
| Clinic setup | `Clinics` | `clinic` | `(public)/`, `(staff)/settings/clinic` |
| Staff & Permissions | `Staff`, `Access` | `staff`, `access-control` | `(staff)/settings/staff`, `(staff)/settings/roles` |
| Customer | `Customers` | `customers` | `(staff)/customers` (ค้นหา ลงทะเบียน หน้าลูกค้า มีแล้ว) |
| Pet | `Patients` | `pets` | `(staff)/pets`, `(portal)/pets` |
| Services | `ServiceCatalog` | `service-catalog` | `(public)/services`, `(staff)/settings/services` |
| Booking & Appointment | `Appointments` | `booking`, `appointments` | `(public)/book`, `(portal)/appointments`, `(staff)/appointments` |
| Queue | `Queueing` | `queue` | `(staff)/queue`, `(portal)/queue` |
| Consultation | `Consultations` | `consultations` | `(staff)/consultations` |
| Medical record | `MedicalRecords` | `medical-records` | `(staff)/records`, `(portal)/pets/[id]/records` |
| Customer portal | *(กลุ่มผู้ใช้ ข้ามหลายโมดูล)* | route ใน `(portal)` | `(portal)/` |
| Schedules & Shifts | `Scheduling` | `scheduling` | `(staff)/schedules` |
| Lab | `Laboratory` | `laboratory` | `(staff)/lab` |
| Prescription | `Prescriptions` | `prescriptions` | `(staff)/prescriptions` |
| Notifications & Messaging | `Notifications`, `Messaging` | `notifications`, `messaging` | `(staff)/notifications` (ประวัติ ส่ง ประกาศ เทมเพลต), `(staff)/messages`, `(portal)/notifications` |
| LINE integration *(ส่งออกอย่างเดียว)* | `Integrations` | `integrations` | `(staff)/settings/line`, `(staff)/notifications/broadcast` |
| Operational stats | `Analytics` | `analytics` | `(staff)/dashboard` (การ์ดวันนี้), `(staff)/reports` |
| Business dashboard | `Analytics` | `analytics` | `(staff)/reports` (แท็บธุรกิจ) |
| Billing & Payments | `Billing` | `billing` | `(staff)/billing`, `(portal)/invoices` |
| Inventory | `Inventory` | `inventory` | `(staff)/inventory` (แท็บรายการ แจ้งเตือน รับเข้า จ่ายออก ปรับยอด สั่งซื้อ) |
| Loyalty & Promotions | `Crm` | `crm` | `(staff)/crm` (ขายแพ็กเกจ สร้างคูปอง กลุ่มลูกค้า แต้มและสมาชิก), `(portal)/packages` |
| Reminders & Automation | `Automation` | `automation` | `(staff)/notifications/automations`, `(staff)/notifications/reminders` |
| Reports & Audit | `Reporting`, `Auditing` | `reporting`, `audit` | `(staff)/reports` (ส่งออกรายงาน), `(staff)/audit` |
| Webhooks & Integrations | `Integrations` | `integrations` | `(staff)/settings/integrations` |

## Authentication

| Method | Endpoint | ผู้เรียก | Controller | Use case | Domain |
|---|---|---|---|---|---|
| POST | `/auth/register` | ลูกค้า | `Auth/AuthController` | `Identity/Register/` | `Identity`, `Customers` |
| POST | `/auth/otp/request` | ลูกค้า | `Auth/OtpController` | `Identity/RequestOtp/` | `Identity` |
| POST | `/auth/login` | ทั้งสองฝั่ง | `Auth/AuthController` | `Identity/Login/` | `Identity` |
| POST | `/auth/staff/login` | พนักงาน | `Auth/StaffAuthController` | `Identity/StaffLogin/` | `Identity` |
| POST | `/auth/2fa/verify` | พนักงาน | `Auth/StaffAuthController` | `Identity/VerifyTwoFactor/` | `Identity` |
| POST | `/auth/refresh` | ทั้งสองฝั่ง | `Auth/SessionController` | `Identity/RefreshSession/` | `Identity` |
| POST | `/auth/logout` | ทั้งสองฝั่ง | `Auth/SessionController` | `Identity/Logout/` | `Identity` |
| POST | `/auth/password/reset` | ทั้งสองฝั่ง | `Auth/PasswordController` | `Identity/ResetPassword/` | `Identity` |

ทางเข้าของลูกค้าทำครบแล้ว แต่ **ยังไม่มีตัวส่ง SMS และอีเมลจริง** รหัส OTP และลิงก์รีเซ็ตรหัสผ่านถูกเขียนลง log ของ API แทน (เฉพาะเมื่อเปิด `Auth:SignIn:LogUndeliveredSecrets` ซึ่งเปิดไว้แค่ในเครื่อง dev) รายละเอียดอยู่ใน [การเข้าสู่ระบบของลูกค้า](features/customer-sign-in.md)

## Account and Access

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| GET | `/me` | เข้าสู่ระบบแล้ว | `Account/MeController` | `Identity/GetMe/` | `Identity`, `Staff` |
| PATCH | `/me` | เข้าสู่ระบบแล้ว | `Account/MeController` | `Identity/UpdateProfile/` | `Identity` |
| GET | `/me/capabilities` | เข้าสู่ระบบแล้ว | `Account/MeController` | อ่านจาก `ICurrentUser` ตรง (scope คำนวณตอนออก token โดย `Identity/Capabilities/EffectiveScopeResolver`) | `Access` |
| GET | `/me/sessions` | เข้าสู่ระบบแล้ว | `Account/SessionsController` | `Identity/ManageSessions/` | `Identity` |
| DELETE | `/me/sessions/{id}` | เข้าสู่ระบบแล้ว | `Account/SessionsController` | `Identity/ManageSessions/` | `Identity` |
| POST | `/me/password` | เข้าสู่ระบบแล้ว | `Account/MeController` | `Identity/ChangePassword/` | `Identity` |
| POST | `/me/2fa/enroll` | เข้าสู่ระบบแล้ว | `Account/MeController` | `Identity/EnrolTwoFactor/` | `Identity` |
| PATCH | `/staff/users/{id}/status` | `user:manage` | `Staff/UserStatusController` | `Identity/SetUserStatus/` | `Identity` |

## Clinic setup

ทำครบทั้ง 11 เส้น controller ฝั่ง `Staff/` แยกตามสิ่งที่เปลี่ยน ไม่ได้แยกหนึ่งตัวต่อหนึ่ง path คือ `ClinicSetupController` ถือการเขียนข้อมูลที่สาธารณะเห็น (โปรไฟล์ สาขา เวลาทำการ วันหยุด) ส่วน `ClinicSettingsController` ถือเอกสารกติกาการทำงานสามชุด

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| GET | `/clinic` | public | `Public/ClinicController` | `Clinics/GetClinicProfile/` | `Clinics` |
| PATCH | `/clinic` | `settings:write` | `Staff/ClinicSetupController` | `Clinics/UpdateClinicProfile/` | `Clinics` |
| GET | `/clinic/branches` | public | `Public/ClinicController` | `Clinics/ListBranches/` | `Clinics` |
| POST | `/clinic/branches` | `settings:write` | `Staff/ClinicSetupController` | `Clinics/CreateBranch/` | `Clinics` |
| GET | `/clinic/hours` | public | `Public/ClinicController` | `Clinics/GetOpeningHours/` | `Clinics` |
| PUT | `/clinic/hours` | `settings:write` | `Staff/ClinicSetupController` | `Clinics/ReplaceOpeningHours/` | `Clinics` |
| POST | `/clinic/holidays` | `settings:write` | `Staff/ClinicSetupController` | `Clinics/AddHoliday/` | `Clinics` |
| GET | `/clinic/settings/booking` | `settings:read` | `Staff/ClinicSettingsController` | `Clinics/ManageBookingSettings/` | `Clinics` |
| PATCH | `/clinic/settings/booking` | `settings:write` | `Staff/ClinicSettingsController` | `Clinics/ManageBookingSettings/` | `Clinics` |
| PATCH | `/clinic/settings/queue` | `settings:write` | `Staff/ClinicSettingsController` | `Clinics/UpdateQueueSettings/` | `Clinics` |
| PATCH | `/clinic/settings/medical-record` | `settings:write` | `Staff/ClinicSettingsController` | `Clinics/UpdateMedicalRecordSettings/` | `Clinics` |

ค่าตั้งเหล่านี้ไม่ได้เก็บไว้เฉย ๆ โมดูลที่เกี่ยวข้องอ่านไปใช้จริง วันหยุดปิดช่วงจอง (`Appointments/Availability/BookingSlotResolver`) ค่าตั้งคิวกำหนดรูปแบบเลขคิวและการเริ่มเลขใหม่ทุกวัน (`Appointments/CheckInAppointment`, `Queueing/IssueWalkInTicket`) ค่าตั้งเวชระเบียนกำหนดว่าการตรวจต้องมีอะไรครบก่อนปิดเคส (`Consultations/CloseConsultation`) และแบบฟอร์มตรวจรายระบบ (`Consultations/RecordExamination`) การอ่านค่าที่ contract ไม่มีให้ ดู [assumptions.md § 27](./assumptions.md)

หน้าจอของการเขียนทั้งหมดในหมวดนี้อยู่ใต้ `/settings` ฝั่งพนักงาน ดู [ตั้งค่าคลินิก](features/clinic-setup.md)

เจ้าของคนแรก สาขาแรก และชุด role แรก **สร้างผ่าน API นี้ไม่ได้** เพราะการเขียนทุกเส้นต้องมี scope และฐานข้อมูลว่างไม่มีใครถือ scope เลย ทั้งหมดมาจากคำสั่ง operator `bootstrap` ([development.md](./development.md)) ซึ่งตั้งใจทำเป็นคำสั่ง ไม่ใช่ endpoint

## Staff and Permissions

ทำครบทั้ง 10 เส้น

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| GET | `/staff/members` | `staff:read` | `Staff/StaffMembersController` | `Staff/ListStaffMembers/` | `Staff` |
| POST | `/staff/members/invitations` | `staff:manage` | `Staff/StaffInvitationsController` | `Staff/InviteStaffMember/` | `Staff` |
| POST | `/staff/members/invitations/{id}/accept` | token ในคำเชิญ | `Staff/StaffInvitationsController` | `Staff/AcceptInvitation/` | `Staff`, `Identity` |
| PATCH | `/staff/members/{id}` | `staff:manage` | `Staff/StaffMembersController` | `Staff/UpdateStaffMember/` | `Staff` |
| POST | `/staff/members/{id}/deactivate` | `staff:manage` | `Staff/StaffMembersController` | `Staff/DeactivateStaffMember/` | `Staff` |
| GET | `/staff/staff-types` | `staff:read` | `Staff/StaffMembersController` | `Staff/ListStaffTypes/` | `Staff` |
| GET | `/staff/roles` | `role:read` | `Staff/RolesController` | `Access/ListRoles/` | `Access` |
| POST | `/staff/roles` | `role:manage` | `Staff/RolesController` | `Access/CreateRole/` | `Access` |
| PUT | `/staff/roles/{id}/permissions` | `role:manage` | `Staff/RolesController` | `Access/ReplaceRolePermissions/` | `Access` |
| GET | `/staff/permissions` | `role:read` | `Staff/RolesController` | `Access/ListRoles/` (`ListPermissionCatalogueHandler` อ่าน `ScopeCatalogue`) | `Access` |

การเขียนทุกเส้นในตารางนี้มีกฎสองข้อคุมอยู่ (รายละเอียดใน [authorization.md](./authorization.md)) คือ ให้ scope ที่ตัวเองไม่ได้ถือไม่ได้ (`Access/ScopeGrantPolicy`) และห้ามเปลี่ยนจนคลินิกไม่เหลือเจ้าของที่ใช้งานอยู่ (`Access/OwnerLockoutGuard`) การรับคำเชิญคือการเรียก endpoint เดียวกันสองครั้ง ดู [authentication.md](./authentication.md) และ [assumptions.md § 30](./assumptions.md)

## Customer and Pet

ทำครบทั้ง 12 เส้น

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| GET | `/staff/customers?q=` | `customer:read` | `Staff/CustomersController` | `Customers/SearchCustomers/` | `Customers` |
| POST | `/staff/customers` | `customer:write` | `Staff/CustomersController` | `Customers/CreateCustomer/` | `Customers` |
| GET | `/staff/customers/{id}` | `customer:read` | `Staff/CustomersController` | `Customers/GetCustomer/` | `Customers` |
| POST | `/staff/customers/{id}/invite` | `customer:write` | `Staff/CustomersController` | `Customers/InviteToPortal/` | `Customers`, `Identity` |
| POST | `/staff/customers/{id}/merge` | `customer:merge` | `Staff/CustomersController` | `Customers/MergeCustomers/` | `Customers`, `Patients` |
| GET | `/staff/pets` | `patient:read` | `Staff/PetsController` | `Patients/ListPets/` | `Patients` |
| POST | `/staff/pets` | `patient:write` | `Staff/PetsController` | `Patients/CreatePet/` | `Patients` |
| GET | `/staff/pets/{id}` | `patient:read` | `Staff/PetsController` | `Patients/GetPet/` | `Patients` |
| GET | `/staff/pets/{id}/history` | `record:read` | `Staff/PetHistoryController` | `Patients/GetPetHistory/` | `Patients`, `MedicalRecords` |
| POST | `/staff/pets/{id}/owners` | `patient:write` | `Staff/PetCarersController` | `Patients/ManageCarers/` | `Patients` |
| DELETE | `/staff/pets/{id}/owners/{cid}` | `patient:write` | `Staff/PetCarersController` | `Patients/ManageCarers/` | `Patients` |
| POST | `/staff/pets/{id}/deceased` | `patient:write` | `Staff/PetsController` | `Patients/MarkPetDeceased/` | `Patients`, `Appointments` |

การปิดเรกคอร์ดสัตว์ที่เสียชีวิต **ยกเลิกนัดในอนาคตทุกนัด** และแจ้งเจ้าของครั้งเดียว ใน transaction เดียว contract ไม่ได้บอกทั้งสองอย่าง และถ้าปล่อยตามที่ contract เงียบ ผลคือค่า default ที่แย่ที่สุด คือมีข้อความเตือนนัดหรือโทรศัพท์จากเคาน์เตอร์ถึงสัตว์ที่ตายไปแล้ว กฎและทางเลือกที่ไม่เลือกอยู่ใน [contract-extensions.md § 6](./contract-extensions.md)

## Services

ทำครบทั้ง 5 เส้น

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| GET | `/services` | public | `Public/ServicesController` | `ServiceCatalog/ListBookableServices/` | `ServiceCatalog` |
| GET | `/services/categories` | public | `Public/ServicesController` | `ServiceCatalog/ListBookableServices/` (`ListServiceCategoriesHandler` อ่าน `ServiceCategories`) | `ServiceCatalog` |
| POST | `/staff/services` | `service:manage` | `Staff/ServicesController` | `ServiceCatalog/CreateService/` | `ServiceCatalog` |
| PATCH | `/staff/services/{id}/pricing` | `service:manage` | `Staff/ServicesController` | `ServiceCatalog/UpdatePricing/` | `ServiceCatalog` |
| PATCH | `/staff/services/{id}/availability` | `service:manage` | `Staff/ServicesController` | `ServiceCatalog/UpdateAvailability/` | `ServiceCatalog` |

การเปลี่ยนราคา **เป็นการตั้งล่วงหน้า ไม่ใช่เขียนทับ** `PATCH .../pricing` เพิ่มเวอร์ชันราคาที่มีวันที่เข้าไปในตารางราคาของบริการ การอ่านทุกครั้งแสดงเวอร์ชันที่มีผลในวันที่ถาม (`ClinicService.PricingOn`) จึงใส่ราคาใหม่ไว้ก่อนวันที่มีผลได้ และราคาเก่ายังอยู่ในระบบ ราคาในแคตตาล็อกเป็น *ราคาตั้ง* บรรทัดในใบแจ้งหนี้ยังมีราคาต่อหน่วยของตัวเอง และยังไม่มีอะไรคัดลอกราคาตั้งไปใส่บิล ส่วน availability จำกัดว่าบริการจองออนไลน์ได้ที่สาขาไหนและช่วงเวลาไหน การหาช่วงว่างอ่านค่านี้ตอนคำนวณ slot

หน้าจัดการบริการใช้ส่วนขยายอีกสองเส้น คือ `GET /staff/services` และ `PATCH /staff/services/{id}` ([ส่วนขยาย § 17](#ส่วนขยาย-contract)) เพราะเมนูสาธารณะไม่มีบริการที่จองได้ที่เคาน์เตอร์อย่างเดียว และไม่มีทางแก้ชื่อหรือระยะเวลา หน้าจออยู่ใน [บริการ](features/services.md)

## Booking and Appointment

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| GET | `/booking/availability` | public | `Public/BookingAvailabilityController` | `Appointments/GetAvailability/` | `Appointments`, `Scheduling` |
| POST | `/booking/holds` | portal | `Portal/BookingHoldsController` | `Appointments/HoldSlot/` | `Appointments` |
| POST | `/portal/appointments` | portal | `Portal/PortalAppointmentsController` | `Appointments/BookAppointment/` | `Appointments` |
| POST | `/staff/appointments` | `appointment:write` | `Staff/AppointmentsController` | `Appointments/CreateStaffAppointment/` | `Appointments` |
| GET | `/staff/appointments?date=&status=` | `appointment:read` | `Staff/AppointmentsController` | `Appointments/ListAppointments/` | `Appointments` |
| GET | `/appointments/{id}` | ทั้งสองฝั่ง | `Account/AppointmentsController` | `Appointments/GetAppointment/` | `Appointments` |
| POST | `/appointments/{id}/reschedule` | ทั้งสองฝั่ง | `Account/AppointmentActionsController` | `Appointments/RescheduleAppointment/` | `Appointments` |
| POST | `/appointments/{id}/cancel` | ทั้งสองฝั่ง | `Account/AppointmentActionsController` | `Appointments/CancelAppointment/` | `Appointments` |
| POST | `/appointments/{id}/confirm` | ทั้งสองฝั่ง | `Account/AppointmentActionsController` | `Appointments/ConfirmAppointment/` | `Appointments` |
| POST | `/appointments/{id}/check-in` | `appointment:checkin` | `Account/AppointmentActionsController` | `Appointments/CheckInAppointment/` | `Appointments`, `Queueing` |
| PATCH | `/staff/appointments/{id}` | `appointment:write` | `Staff/AppointmentsController` | `Appointments/UpdateAppointment/` | `Appointments` |

`/appointments/{id}/*` ใช้ร่วมกันทั้งพนักงานและลูกค้า controller ตัวเดียวรับทั้งสองกลุ่ม ส่วน use case ใช้กฎความเป็นเจ้าของและเวลาตัดรอบที่ต่างกันระหว่างสองกลุ่ม ถ้าแยกเป็น controller `/staff/...` กับ `/portal/...` จะขัดกับ path ใน contract

route ที่ใช้ร่วมเหล่านี้ **ไม่มี attribute `[RequireScope]`** เพราะลูกค้าถือแค่ `portal` ไม่มี scope `appointment:*` เลย ถ้าใส่ attribute จะล็อกผู้ใช้ครึ่งหนึ่งที่ contract ระบุไว้ การตัดสินจึงอยู่ใน `Application/Appointments/AppointmentAccess.cs` แทน ซึ่งรับพนักงานตาม scope และสาขา รับลูกค้าตามการเป็นผู้ดูแลสัตว์ และตอบ `404` กับคนอื่นทั้งหมด ไม่ตอบ `403` เพราะ `403` เท่ากับยืนยันว่า id นัดนั้นมีอยู่จริง

### สองการเขียนที่ต้องให้ฐานข้อมูลช่วยกัน

การจองมี invariant สองข้อที่ชั้น application บังคับเองไม่ได้ ควรรู้ก่อนแก้โมดูลนี้

- **สัตวแพทย์ต้องไม่ถูกจองซ้อน** use case ตรวจ slot ซ้ำใน transaction ซึ่งจำเป็นแต่ไม่พอ ภายใต้ `READ COMMITTED` การจองสองรายการพร้อมกันต่างอ่านเห็นปฏิทินว่าง และไม่เห็นแถวที่อีกฝั่งยังไม่ commit exclusion constraint `ex_appointments_staff_slot` (migration `AppointmentSlotExclusion`) ปฏิเสธการเขียนรายการที่สอง แล้ว `PostgresErrorTranslator` แปลงกลับเป็น `slot_unavailable` ตาม contract
- **คนสองคนต้องไม่ได้เลขคิวเดียวกัน** `pg_advisory_xact_lock` ที่ใช้คีย์ต่อสาขาต่อวัน เรียงการออกเลขใน transaction ของการเช็กอิน และ unique index บน `(branch_id, service_date, number)` เป็นด่านสุดท้าย

## Queue

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| GET | `/staff/queue?branch=&room=&date=&include_finished=&status=` | `queue:read` | `Staff/QueueController` | `Queueing/GetQueueBoard/` | `Queueing` |
| POST | `/staff/queue/tickets` | `queue:write` | `Staff/QueueController` | `Queueing/IssueWalkInTicket/` | `Queueing` |
| POST | `/staff/queue/{id}/assign` | `queue:write` | `Staff/QueueTicketActionsController` | `Queueing/AssignTicket/` | `Queueing` |
| POST | `/staff/queue/{id}/call` | `queue:call` | `Staff/QueueTicketActionsController` | `Queueing/CallTicket/` | `Queueing`, `Notifications` |
| POST | `/staff/queue/{id}/skip` | `queue:call` | `Staff/QueueTicketActionsController` | `Queueing/SkipTicket/` | `Queueing` |
| GET | `/portal/queue/{id}` | portal | `Portal/PortalQueueController` | `Queueing/GetMyTicket/` | `Queueing` |
| SSE | `/queue/stream?branch=` | ทั้งสองฝั่ง และ token ของจอ (ส่วนขยาย § 30) | `Realtime/QueueStreamController` | ส่งจาก `IQueueEventStream` | `Queueing` |

SSE เป็นเส้นเดียวใน contract ที่ไม่ใช่ REST ตัวส่งอยู่ที่ `Infrastructure/Realtime/QueueEventBroker.cs` เป็นการกระจายภายใน process เดียว เพราะหนึ่งคลินิกคือหนึ่ง deployment ไม่มีอะไรต้องประสานข้ามเครื่อง controller เปิด stream แล้วส่ง domain event ต่อ (`ticket.called`, `ticket.updated`, `board.refresh`) ไม่มี `[RequireScope]` ด้วยเหตุผลเดียวกับ route นัดที่ใช้ร่วม และกรองให้ลูกค้าเห็นเฉพาะคิวของตัวเอง

สังเกตการแบ่ง scope ที่ contract วางไว้และโค้ดทำตาม คือ การจัดกระดาน (`assign`) ใช้ `queue:write` ส่วนการเรียกและการข้ามใช้ `queue:call` พนักงานต้อนรับจัดคิวได้ แต่การเรียกคนไข้เข้าห้องเป็นสิทธิ์แยก

เรื่องที่ควรรู้ก่อนต่อยอดคิว

- **การออกเลขทำภายใต้ advisory lock** ต่อสาขาต่อวัน ใน transaction ของการเช็กอินหรือ walk-in และมี unique index บน `(branch_id, service_date, number)` เป็นด่านสุดท้าย ทั้งสองทางเข้าคิวใช้ตัวออกเลขเดียวกัน walk-in กับนัดที่เช็กอินจึงไม่มีทางได้เลขซ้ำกัน
- **`skipped` เป็นสถานะรอ ไม่ใช่ทางออก** contract ขอให้ "เลื่อนไปท้ายแถว" คิวที่ถูกข้ามจึงยังอยู่ และเรียงหลังทุกคนที่ยังไม่ถูกข้าม ดู [assumptions.md § 17](./assumptions.md)
- **จอห้องรอมี token ของตัวเอง** ห้าเส้นของ § 30 ให้ทีวีเปิดกระดานที่ย่อแล้วและ stream ของสาขาได้โดยไม่มีเซสชันพนักงาน token ใช้ได้สองเส้นนั้นเท่านั้น ดู [ส่วนขยาย contract](#ส่วนขยาย-contract)
- **บัตรคิวฝั่งพนักงานมีเหตุผลและเวลา** และ `GET /staff/queue` กรองสถานะได้ (§ 31) `assign` ตรวจว่าเป็นหมอของสาขา
- **มีอีกสอง route ที่ contract ไม่ได้นิยาม** คือ `priority` และ `leave` ลำดับการเรียกคือ `priority` ก่อน แล้วคิวที่ยังไม่ถูกข้าม แล้วเวลาที่ถูกข้าม แล้ว `number` เคสฉุกเฉินจึงแซงทุกคนรวมถึงคนที่ถูกข้ามไปแล้ว ทั้งสองเส้นอยู่ใน [ส่วนขยาย contract](#ส่วนขยาย-contract)

## Consultation and Medical record

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| POST | `/staff/consultations` | `consult:write` | `Staff/ConsultationsController` | `Consultations/OpenConsultation/` | `Consultations`, `Queueing`, `Appointments` |
| PATCH | `/staff/consultations/{id}` | `consult:write` | `Staff/ConsultationsController` | `Consultations/UpdateConsultation/` | `Consultations` |
| POST | `/staff/consultations/{id}/vitals` | `vitals:write` | `Staff/ConsultationPartsController` | `Consultations/RecordVitals/` | `Consultations` |
| POST | `/staff/consultations/{id}/examination` | `consult:write` | `Staff/ConsultationPartsController` | `Consultations/RecordExamination/` | `Consultations` |
| POST | `/staff/consultations/{id}/diagnoses` | `consult:write` | `Staff/ConsultationPartsController` | `Consultations/AddDiagnosis/` | `Consultations` |
| POST | `/staff/consultations/{id}/treatments` | `consult:write` | `Staff/ConsultationPartsController` | `Consultations/AddTreatment/` | `Consultations` |
| POST | `/staff/consultations/{id}/prescriptions` | `rx:write` | `Staff/ConsultationPartsController` | `Consultations/AddPrescription/` | `Prescriptions`, `Patients` |
| POST | `/staff/consultations/{id}/close` | `record:sign` | `Staff/ConsultationPartsController` | `Consultations/CloseConsultation/` | `Consultations`, `MedicalRecords`, `Queueing`, `Appointments` |
| GET | `/staff/medical-records/{id}` | `record:read` | `Staff/MedicalRecordsController` | `MedicalRecords/GetMedicalRecord/` | `MedicalRecords`, `Auditing` |
| POST | `/staff/medical-records/{id}/amendments` | `record:amend` | `Staff/MedicalRecordsController` | `MedicalRecords/AmendMedicalRecord/` | `MedicalRecords` |

route เขียนแปดเส้นแบ่งไว้สอง controller `ConsultationsController` เปิดเคสและบันทึกร่าง `ConsultationPartsController` ถือส่วนย่อยของเคสและการปิดเคส การแบ่งนี้ตาม scope ซึ่งเป็นจุดที่น่าสนใจที่สุดของส่วนนี้ อ่าน scope ไล่ลงมาจะเห็นโมเดล role ของ contract คือ `vitals:write` ให้ผู้ช่วยชั่งน้ำหนักได้อย่างเดียว `consult:write` คือพื้นที่ทำงานของสัตวแพทย์ `rx:write` คือการสั่งยา และ `record:sign` ซึ่งผู้ช่วยและพนักงานต้อนรับไม่มี คือการลงนามในเอกสารทางการแพทย์ถาวร

**กฎสามข้อที่ทำให้ส่วนนี้ทำงานตามที่ contract บอก**

- **เคสที่ปิดแล้วตอบ `423 record_locked` ไม่ใช่ `409`** การแก้ทุกอย่างบน aggregate ผ่านตัวกันจุดเดียว จึงไม่มีทางไหนลืมตรวจ ความต่างนี้สำคัญกับผู้เรียก `409` ชวนให้ลองใหม่ ส่วน `423` บอกว่าให้ส่ง amendment
- **เวชระเบียนที่ลงนามแล้วเก็บ snapshot ไม่ใช่ตัวชี้** `MedicalRecord.ContentJson` คือเคสทั้งก้อนที่ serialize ณ เวลาลงนาม เก็บเป็น `text` ไม่ใช่ `jsonb` เพราะ PostgreSQL จัดรูป `jsonb` ใหม่ตอนเขียน ไบต์ที่อ่านกลับมาจะไม่ตรงกับไบต์ที่ทำ hash `ContentHash` ครอบเนื้อหา ผู้ลงนาม และเวลาลงนาม และส่งกลับให้ผู้เรียกเป็น `integrity_verified`
- **amendment ต่อท้ายเสมอ** ต้นฉบับไม่ถูกแตะ แต่ละ amendment เก็บฉบับแก้ทั้งหมดพร้อม diff ระดับฟิลด์ และแก้ได้เฉพาะฟิลด์บรรยาย เพราะการรักษาถูกคิดเงินไปแล้ว และยาอาจจ่ายไปแล้ว ดู [assumptions.md § 20](./assumptions.md)

`CloseConsultation` เป็น transaction ที่กว้างที่สุดในระบบ คือลงนามเวชระเบียน ตั้งคิวเป็น served ปิดนัด และจองนัดติดตามผล ได้ทั้งหมดหรือไม่ได้เลย การแจ้งเตือนส่ง **หลัง** commit เพราะ LINE push ที่ล้มเหลวต้องไม่ทำให้เวชระเบียนที่ลงนามแล้ว rollback

**"ออกบิล" เกิดใน transaction เดียวกันนี้** การปิดเคสออกใบแจ้งหนี้ผ่าน `Billing/ConsultationBilling` ตัวอย่างใน contract เอง (`record.signed` แล้ว `invoice.issued ฿1,460`) จึงเป็นสิ่งที่ endpoint ทำจริง เคสที่ไม่มีการรักษาไม่ออกบิล เพราะบิล ฿0 คือเอกสารที่เคาน์เตอร์ต้องมาดู และเป็นแถวในรายงานรายได้ทุกฉบับ เพียงเพื่อบอกว่าไม่ได้คิดเงินอะไร

**คิดเงินการรักษา และยาที่จ่ายแล้ว** "เข้าบิลอัตโนมัติ" เขียนไว้ที่ endpoint treatments ดังนั้นทุกหัตถการขึ้นบิล `Prescription` ตอนเขียนไม่มีราคา ยาคิดเงินตอนจ่ายจริง ("ตัดสต๊อกเมื่อจ่ายจริง") ด้วยราคาขายของรายการในคลังที่ชื่อตรงกัน ราคาถูกเก็บไว้ที่ใบสั่งยาตอนจ่าย ยาที่จ่ายก่อนปิดเคสขึ้นบิลตอนปิดเคสเป็นรายการชนิด `medicine` ข้างหัตถการ ยาที่จ่ายหลังปิดเคสถูกเพิ่มลงบิลของเคสถ้ายัง `open` ถ้าบิลรับเงินแล้วได้บิลใบใหม่ ยาที่ยังไม่จ่ายหรือไม่พบในคลังไม่ถูกคิดเงิน ดู [สต๊อกยาและเวชภัณฑ์](features/inventory.md#ค่ายาขึ้นบิลเดียวกับการรักษา)

## Customer portal

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| GET | `/portal/home` | portal | `Portal/PortalHomeController` | `Patients/Portal/GetPortalHomeHandler` | ข้ามหลายโมดูล |
| GET | `/portal/pets` | portal | `Portal/PortalPetsController` | `Patients/Portal/ListMyPetsHandler` | `Patients` |
| PATCH | `/portal/pets/{id}` | portal | `Portal/PortalPetsController` | `Patients/Portal/UpdatePetByOwnerHandler` | `Patients` |
| GET | `/portal/pets/{id}/records` | portal | `Portal/PortalPetRecordsController` | `MedicalRecords/GetOwnerVisibleRecords/` | `MedicalRecords`, `Laboratory`, `Patients` |
| GET | `/portal/appointments` | portal | `Portal/PortalAppointmentsController` | `Appointments/ListMyAppointments/` | `Appointments` |
| GET | `/portal/notifications` | portal | `Portal/PortalNotificationsController` | `Notifications/ListMyNotifications/` | `Notifications` |
| POST | `/portal/documents` | portal | `Portal/PortalDocumentsController` | `Patients/Documents/UploadOwnerDocumentHandler` | `Patients` และที่เก็บไฟล์ |
| GET | `/portal/invoices` | portal | `Portal/PortalInvoicesController` | `Billing/ListMyInvoices/` | `Billing` |
| GET | `/portal/packages` | portal | `Portal/PortalPackagesController` | `Crm/GetMyPackageBalance/` | `Crm` |

`PATCH /portal/pets/{id}` รับเฉพาะรูป ชื่อเล่น อาหาร และพฤติกรรม ฟิลด์ทางคลินิกถูกปฏิเสธที่ขอบ DTO คือ DTO ฝั่งเจ้าของไม่มีฟิลด์เหล่านั้นอยู่เลย คำขอที่ปลอมขึ้นจึงเข้าไม่ถึง

ข้อความทุกฉบับที่ผ่านด่านความยินยอมถูกเก็บลงกล่องแจ้งเตือนในเว็บด้วย (`ConsentAwareNotificationDispatcher`) การกดว่าอ่านแล้วเป็นส่วนขยาย `POST /portal/notifications/read` ส่วน `/portal/packages` แสดงแพ็กเกจที่ขายผ่าน `POST /staff/packages` ในรูปเดียวกับที่พนักงานเห็น รายละเอียดอยู่ใน [portal ลูกค้า](features/portal.md)

## Schedules and Shifts

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| GET | `/staff/schedules?from=&to=` | `schedule:read` | `Staff/SchedulesController` | `Scheduling/GetScheduleCalendar/` | `Scheduling` |
| POST | `/staff/schedules/shifts` | `schedule:write` | `Staff/SchedulesController` | `Scheduling/CreateShifts/` | `Scheduling` |
| POST | `/staff/schedules/time-off` | `schedule:write` | `Staff/SchedulesController` | `Scheduling/RequestTimeOff/` | `Scheduling`, `Appointments` |
| GET | `/staff/schedules/availability` | `schedule:read` | `Staff/SchedulesController` | `Scheduling/GetStaffAvailability/` | `Scheduling` |
| PUT | `/staff/schedules/assignment-rules` | `schedule:write` | `Staff/SchedulesController` | `Scheduling/ReplaceAssignmentRules/` | `Scheduling` |

การแก้และลบเวรทีละเวร การอ่านกติกาจ่ายเคส และการคัดลอกสัปดาห์ เป็นส่วนขยาย § 47 ถึง 49 อยู่ใน [ตารางส่วนขยาย](#ส่วนขยาย-contract)

กะทำงานคือ input ของ `/booking/availability` ช่วงว่างมีได้เฉพาะที่มีกะ หักนัดที่จองแล้ว และวันหยุด เวลาพักและวันลาไม่ต้องหักแยก เพราะทั้งสองอย่างเป็นช่องว่างในกะอยู่แล้ว (เวลาพักแบ่งกะเป็นสองกะ วันลาตัดเวลาออกจากกะ) รายละเอียดอยู่ใน [ตารางเวร](features/schedules.md)

## Advanced appointment

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| POST | `/staff/appointments/recurring` | `appointment:write` | `Staff/RecurringAppointmentsController` | `Appointments/CreateRecurringSeries/` | `Appointments`, `Scheduling` |
| GET | `/staff/appointments/conflicts` | `appointment:read` | `Staff/AppointmentConflictsController` | `Appointments/DetectConflicts/` | `Appointments`, `Scheduling` |
| POST | `/staff/waiting-list` | `appointment:write` | `Staff/WaitingListController` | `Appointments/AddToWaitingList/` | `Appointments`, `Notifications` |
| POST | `/staff/appointments/{id}/no-show` | `appointment:write` | `Staff/AppointmentsController` | `Appointments/MarkNoShow/` | `Appointments` |
| PUT | `/staff/appointment-types` | `settings:write` | `Staff/AppointmentTypesController` | `Appointments/ReplaceAppointmentTypes/` | `Appointments` |
| PUT | `/staff/policies/cancellation` | `settings:write` | `Staff/CancellationPolicyController` | `Appointments/ReplaceCancellationPolicy/` | `Appointments` |

ประเภทนัดและนโยบายยกเลิกไม่ได้เก็บไว้เฉย ๆ การจองของลูกค้าอ่านว่าประเภทนั้นจองออนไลน์ได้ไหมและต้องจองล่วงหน้ากี่นาที ทุกทางจองอ่านระยะเวลา การเช็กอินตั้งความเร่งด่วนของคิวตามประเภท และการยกเลิกของลูกค้าที่กระชั้นกว่า cutoff ถูกนับเป็นครั้งที่ผิดนัด รายละเอียดอยู่ใน [นัดขั้นสูง](features/advanced-booking.md)

## Advanced patient management

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| GET | `/staff/pets/{id}/vaccinations` | `patient:read` | `Staff/PetVaccinationsController` | `Patients/ListVaccinations/` | `Patients` |
| POST | `/staff/pets/{id}/vaccinations` | `record:write` | `Staff/PetVaccinationsController` | `Patients/RecordVaccination/` | `Patients` |
| POST | `/staff/pets/{id}/allergies` | `record:write` | `Staff/PetsController` | `Patients/RecordAllergy/` | `Patients` |
| GET | `/staff/pets/{id}/weights` | `patient:read` | `Staff/PetWeightsController` | `Patients/GetWeightHistory/` (อ่านจาก `consultation_vitals`) | `Consultations` |
| POST | `/staff/pets/{id}/treatment-plans` | `consult:write` | `Staff/TreatmentPlansController` | `Patients/CreateTreatmentPlan/` | `Patients`, `Appointments` |
| POST | `/staff/pets/{id}/documents` | `record:write` | `Staff/PetDocumentsController` | `Patients/Documents/CreateDocumentUploadUrlHandler` | `Patients` และที่เก็บไฟล์ |

ทำแพ้ยาก่อนส่วนอื่นของหมวดนี้ เพราะ contract ให้ endpoint สั่งยารับผิดชอบ "ตรวจแพ้ยา" และการตรวจที่ไม่มีข้อมูลให้ตรวจเทียบเป็นแค่ของประดับ

วันครบกำหนดวัคซีนเก็บไว้ที่ตัววัคซีนแต่ละเข็ม (`next_due_on`) เพื่อให้กติกาเตือนของโมดูล Automation อ่านได้เมื่อสร้าง แผนการรักษาจองนัดทุกครั้งผ่านตัวจองชุดนัดตัวเดียวกับ `POST /staff/appointments/recurring` ไฟล์ใช้ pre-signed URL ที่ API ลงชื่อเอง ดูส่วนขยาย § 12

`GET /staff/pets/{id}/weights` ไม่มีตารางของตัวเอง ชุดน้ำหนักอ่านจาก `consultation_vitals` ตามสัตว์ ถ้ามีตารางน้ำหนักแยก จะเป็นสำเนาที่สองของค่าวัดเดียวกัน และขัดกับเวชระเบียนต้นทางได้

## Lab, Prescription and medical documents

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| POST | `/staff/lab-requests` | `lab:request` | `Staff/LabRequestsController` | `Laboratory/CreateLabRequest/` | `Laboratory` |
| POST | `/staff/lab-requests/{id}/results` | `lab:write` | `Staff/LabResultsController` | `Laboratory/RecordLabResults/` | `Laboratory` |
| GET | `/staff/lab-requests?status=pending` | `lab:read` | `Staff/LabRequestsController` | `Laboratory/ListLabRequests/` | `Laboratory` |
| GET | `/staff/prescriptions?pet_id=` | `rx:read` | `Staff/PrescriptionsController` | `Prescriptions/ListPrescriptions/` | `Prescriptions` |
| POST | `/staff/prescriptions/{id}/dispense` | `rx:dispense` | `Staff/PrescriptionDispenseController` | `Prescriptions/DispensePrescription/` | `Prescriptions`, `Inventory`, `Billing` |
| GET | `/staff/templates/treatments` | `consult:read` | `Staff/TreatmentTemplatesController` | `Consultations/ListTreatmentTemplates/` | `Consultations` |
| POST | `/staff/documents/generate` | `record:read` | `Staff/DocumentGenerationController` | `MedicalRecords/GenerateDocument/` | `MedicalRecords` และตัวสร้าง PDF (QuestPDF) |
| GET | `/staff/medical-records/{id}/audit` | `record:audit` | `Staff/MedicalRecordsController` | `MedicalRecords/GetRecordAudit/` | `MedicalRecords`, `Auditing` |

มุมมอง audit ของเวชระเบียนตอบจากสองแหล่ง เพราะคำถามมีสองครึ่ง amendment บอกว่า *ตอนนี้เอกสารเขียนว่าอะไร และมาถึงตรงนี้อย่างไร* ส่วน audit entry บอกว่า *ใครแตะ จากที่ไหน เมื่อไร* รวมถึงการเปิดดู ซึ่งไม่ทิ้งร่องรอยในตัวเอกสาร

ใบสั่งยา **ถูกเขียน** โดยโมดูลการตรวจ และ **ถูกจ่าย** โดยห้องยา contract แยกสองอย่างนี้ ("ตัดสต๊อกเมื่อจ่ายจริง") entity ก็แยกเช่นกัน `DispensedAt` เป็น null จนกว่าจะมี `rx:dispense` และคลังยาไม่ถูกแตะก่อนหน้านั้น

การจ่ายยาบันทึกว่าจ่ายแล้ว ตัดสต๊อกของสาขาที่เปิดเคส (`Inventory/Dispensing/StockDispenser`) คิดค่ายา (`Billing/MedicineBilling`) และคืนข้อมูลฉลาก ใน transaction เดียว ยาที่ชื่อไม่ตรงกับรายการในคลังหรือของไม่พอไม่ถูกปฏิเสธ คำตอบบอกด้วย `stock_deducted` และ `stock.status` ดู [assumptions.md § 42](./assumptions.md) ใบสั่งยาที่หมอเลือกยาจากคลังผ่าน `GET /staff/prescriptions/drug-options` มี `inventory_item_id` การจ่ายใช้รหัสนั้นก่อน และเทียบชื่อเฉพาะใบที่ไม่มีรหัส ([ส่วนขยาย § 20](#ส่วนขยาย-contract)) แล็บมีส่วนขยายหนึ่งเส้นคือ `POST /staff/lab-requests/{id}/review` สำหรับกดว่าอ่านผลแล้ว ดู [งานรักษาต่อเนื่อง](features/clinical-records.md)

## Notifications and Messaging

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| POST | `/staff/notifications/send` | `notify:send` | `Staff/NotificationsController` | `Notifications/SendNotification/` | `Notifications` |
| GET | `/staff/notifications/templates` | `notify:manage` | `Staff/NotificationTemplatesController` | `Notifications/ListTemplates/` | `Notifications` |
| GET | `/staff/notifications/log` | `notify:read` | `Staff/NotificationLogController` | `Notifications/GetDeliveryLog/` | `Notifications` |
| GET | `/staff/threads` | `message:read` | `Staff/ThreadsController` | `Messaging/ListThreads/` | `Messaging` |
| POST | `/staff/threads/{id}/messages` | `message:write` | `Staff/ThreadsController` | `Messaging/ReplyToThread/` | `Messaging` |

ข้อความออกจริงทาง LINE, SMS และอีเมล ทุกครั้งที่ลองส่งมีแถวในตาราง `notification_deliveries` ซึ่ง `GET /staff/notifications/log` อ่าน เทมเพลตเป็นโค้ด (`Notifications/Templates/`) เพราะสัญญามีแค่การอ่าน รายละเอียดอยู่ใน [การแจ้งเตือน](features/notifications.md)

กล่องข้อความลูกค้ามีหนึ่งกล่องต่อลูกค้าหนึ่งคน (`message_threads`, `thread_messages`) กล่องเริ่มเมื่อลูกค้าที่ผูก LINE แล้วพิมพ์ถึงบัญชี LINE ของคลินิก (event `message` ของ `POST /webhooks/line` เรียก `Messaging/ReceiveMessage/`) คำตอบของพนักงานส่งผ่าน `INotificationDispatcher` ด้วยเทมเพลต `thread.reply` ลองช่องทางที่ลูกค้าใช้ล่าสุดก่อน ทั้งสองเส้นอยู่ใน controller เดียว รายละเอียดอยู่ใน [ข้อความลูกค้า](features/messaging.md)

## LINE integration

**LINE เป็นช่องทางส่งการแจ้งเตือนออก ไม่ใช่ผู้ยืนยันตัวตน** ไม่มีการเข้าสู่ระบบด้วย LINE ไม่มีการผูกบัญชี ไม่มีการจับคู่เบอร์โทรกับ LINE ที่อยู่ LINE ของลูกค้าเป็นข้อมูลสำหรับส่งข้อความในเรกคอร์ดลูกค้า เหมือนอีเมล ไม่ใช่ credential ไม่ใช่ primary key และไม่ใช่เซสชัน ดู [assumptions.md § A](./assumptions.md)

```text
Application -> Notification port -> Infrastructure/Notifications/Line/ -> LINE Messaging API
```

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| PUT | `/staff/integrations/line` | `integration:manage` | `Staff/LineIntegrationController` | `Integrations/ConfigureLineChannel/` | `Integrations` |
| POST | `/staff/integrations/line/broadcast` | `notify:broadcast` | `Staff/LineBroadcastController` | `Notifications/BroadcastToSegment/` | `Notifications` (กลุ่มลูกค้าคือรายชื่อในคำขอ หรือ `segment_id` ของ `Crm`) |
| POST | `/webhooks/line` | ลายเซ็น LINE | `Webhooks/LineWebhookController` | `Integrations/HandleLineWebhook/` | `Integrations` |

`POST /webhooks/line` เป็น `[AllowAnonymous]` และยืนยันด้วยลายเซ็นของ LINE channel แทน bearer token เป็น callback ของ integration รับ event `follow`, `message` และ postback แล้วส่งต่อให้ handler ไม่ออก token และไม่สร้างบัญชี `follow` และ `message` ยังจดผู้ติดตามล่าสุดไว้ให้เคาน์เตอร์เลือกเป็นที่อยู่ LINE ของลูกค้า ([contract-extensions.md § 18](./contract-extensions.md)) `message` จากที่อยู่ที่ผูกกับลูกค้าแล้วเข้ากล่องข้อความของลูกค้าคนนั้น ([ข้อความลูกค้า](features/messaging.md))

postback action ใน `Domain/Integrations/LinePostbackActions.cs` ทำงานกับเรกคอร์ดจริง handler จึงต้องหาที่อยู่ LINE ที่ส่งมากลับไปเป็นลูกค้าที่ลงทะเบียนที่อยู่นั้นไว้ แล้วใช้กฎสิทธิ์ของ portal ตามปกติ นี่คือการค้นจากที่อยู่ส่ง ไม่ใช่การยืนยันตัวตน ทำตามแบบนี้แล้ว แต่ยังไม่มีทางบันทึกที่อยู่ LINE ลงเรกคอร์ดลูกค้าผ่าน API จึงยังไม่มีลูกค้าคนใดถูกพบจริง ดู [LINE](features/line.md) และ [assumptions.md](./assumptions.md) (§ B, § 41)

**อยู่นอกขอบเขต มีใน reference แต่ไม่สร้าง** `POST /auth/line/callback`, `POST /portal/line/link`, `DELETE /portal/line/link`, `GET /line/liff/booking-context` ทั้งสี่เส้นมีไว้เพื่อใช้ LINE เป็นตัวตนเท่านั้น ดู [assumptions.md § A](./assumptions.md)

## Operational statistics

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| GET | `/staff/dashboard/today` | `dashboard:read` | `Staff/DashboardController` | `Analytics/GetTodayDashboard/` | `Analytics` |
| GET | `/staff/stats/appointments` | `stats:read` | `Staff/StatsController` | `Analytics/GetAppointmentStats/` | `Analytics` |
| GET | `/staff/stats/queue` | `stats:read` | `Staff/StatsController` | `Analytics/GetQueueStats/` | `Analytics` |
| GET | `/staff/stats/workload` | `stats:read` | `Staff/StatsController` | `Analytics/GetWorkloadStats/` | `Analytics` |

## Business dashboard

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| GET | `/staff/analytics/revenue` | `analytics:read` | `Staff/AnalyticsController` | `Analytics/GetRevenueAnalytics/` | `Analytics` |
| GET | `/staff/analytics/services` | `analytics:read` | `Staff/AnalyticsController` | `Analytics/GetServiceAnalytics/` | `Analytics` |
| GET | `/staff/analytics/retention` | `analytics:read` | `Staff/AnalyticsController` | `Analytics/GetRetentionAnalytics/` | `Analytics` |
| GET | `/staff/analytics/staff-performance` | `analytics:hr` | `Staff/AnalyticsController` | `Analytics/GetStaffPerformance/` | `Analytics` |

analytics อ่านผ่าน SQL projection ที่เขียนมือใน `Infrastructure/Persistence/Repositories/Analytics/` ไม่โหลด aggregate เป็นที่เดียวที่มี SQL เขียนมือใน repository ทุกค่าเป็น parameter ตัวเลขแต่ละตัวนับอย่างไร โดยเฉพาะรายรับ ดู [ภาพรวมและรายงาน](features/dashboard.md)

## Billing and Payments

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| POST | `/staff/invoices` | `invoice:write` | `Staff/InvoicesController` | `Billing/CreateInvoice/` | `Billing` |
| POST | `/staff/invoices/{id}/discounts` | `invoice:discount` | `Staff/InvoiceTransactionsController` | `Billing/ApplyDiscount/` | `Billing` |
| POST | `/staff/invoices/{id}/payments` | `payment:collect` | `Staff/InvoiceTransactionsController` | `Billing/CollectPayment/` | `Billing` |
| POST | `/staff/invoices/{id}/refunds` | `payment:refund` | `Staff/InvoiceTransactionsController` | `Billing/RefundPayment/` | `Billing` |
| POST | `/staff/invoices/{id}/receipt` | `receipt:issue` | `Staff/InvoiceTransactionsController` | `Billing/IssueReceipt/` | `Billing` |
| GET | `/portal/invoices` | portal | `Portal/PortalInvoicesController` | `Billing/ListMyInvoices/` | `Billing` |

ร่างแรกของตารางนี้มีสี่ controller แยกหนึ่งตัวต่อหนึ่ง action ตอนสร้างจริงรวมเหลือสองตัว ด้วยเหตุผลเดียวกับส่วนย่อยของการตรวจ `InvoicesController` ถือตัวเอกสาร (สร้าง อ่าน รายการ void) และ `InvoiceTransactionsController` ถือเงินที่เคลื่อนไหวกับเอกสารนั้น controller action เดียวสี่ตัวคือสี่ไฟล์ที่มี route ไฟล์ละเส้น

**สถานะคำนวณเอา ไม่เคยถูกตั้ง** `open`, `partially_paid`, `paid`, `refunded` คำนวณใหม่จากรายการ ส่วนลด การชำระ และการคืนเงินของใบนั้นหลังทุกการเคลื่อนไหว คอลัมน์สถานะที่ผู้เรียกตั้งได้ วันหนึ่งจะเขียนว่า `paid` ทั้งที่ยังค้างยอด และความระวังที่จุดเรียกกันเรื่องนี้ไม่ได้

**การชำระเงินไม่มี gateway** `PUT /staff/integrations/payment` ตั้งค่าผู้ให้บริการที่ระบบนี้ไม่มี การบันทึกการจ่ายด้วยบัตรจึงหมายถึง "เครื่องรูดบัตรอนุมัติแล้ว และเลขสลิปอยู่ใน `reference`" ไม่ใช่ "ระบบตัดบัตรแล้ว" เมื่อมี gateway ให้วางไว้ *หน้า* handler ไม่ใช่ข้างใน เพราะการอนุมัติบัตรเป็นการเรียกผ่านเครือข่าย และต้องไม่เกิดขณะ transaction ถือ row lock ของใบแจ้งหนี้

อีกสาม route ในส่วนนี้ **ไม่อยู่ใน contract** ดูใน [ส่วนขยาย contract](#ส่วนขยาย-contract) ด้านล่าง

## Inventory

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| GET | `/staff/inventory/items` | `inventory:read` | `Staff/InventoryController` | `Inventory/ListItems/` | `Inventory` |
| POST | `/staff/inventory/movements` | `inventory:write` | `Staff/InventoryStockController` | `Inventory/RecordMovement/` | `Inventory` |
| POST | `/staff/inventory/adjustments` | `inventory:adjust` | `Staff/InventoryStockController` | `Inventory/AdjustStock/` | `Inventory`, `Auditing` |
| GET | `/staff/inventory/alerts` | `inventory:read` | `Staff/InventoryController` | `Inventory/ListAlerts/` | `Inventory` |
| POST | `/staff/inventory/purchase-orders` | `inventory:purchase` | `Staff/InventoryStockController` | `Inventory/CreatePurchaseOrder/` | `Inventory`, `Auditing` |

ร่างแรกมีห้า controller ตอนสร้างจริงรวมเหลือสองตัว `InventoryController` ถือการอ่าน `InventoryStockController` ถือการเขียนสามเส้น ซึ่งแต่ละเส้นใช้ scope ของตัวเอง

**สต๊อกเป็นของสาขา สมุดสต๊อกแก้ไม่ได้** รายการในคลังใช้ทั้งคลินิก จำนวนเก็บเป็นล็อตต่อสาขา และทุกการเปลี่ยนยอดเขียนแถวในสมุด (`stock_movements`) ที่มีจำนวนเท่ากัน ภายใต้ advisory lock ต่อรายการต่อสาขา contract ไม่มี endpoint สร้างรายการ รายการจึงเกิดจากการรับเข้าหรือใบสั่งซื้อที่ส่ง `new_item` และใบสั่งซื้อรับของบางส่วนได้ด้วยการรับเข้าที่อ้าง `purchase_order_id` รายละเอียดอยู่ใน [สต๊อกยาและเวชภัณฑ์](features/inventory.md) และการตีความใน [assumptions.md § 42](./assumptions.md)

## Loyalty, Packages and Promotions

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| POST | `/staff/segments` | `crm:manage` | `Staff/SegmentsController` | `Crm/CreateSegment/` | `Crm` (`Segment`) |
| POST | `/staff/packages` | `crm:manage` | `Staff/PackagesController` | `Crm/CreatePackage/` | `Crm` (`CustomerPackage`), `Billing` |
| GET | `/portal/packages` | portal | `Portal/PortalPackagesController` | `Crm/GetMyPackageBalance/` | `Crm` (อยู่ในตาราง Customer portal ด้วย) |
| POST | `/staff/coupons` | `crm:manage` | `Staff/CouponsController` | `Crm/CreateCoupon/` | `Crm` (`Coupon`) |
| GET | `/staff/loyalty/{customer_id}` | `crm:read` | `Staff/LoyaltyController` | `Crm/GetLoyaltyAccount/` | `Crm` (`LoyaltyEntry`) |

ขายแพ็กเกจคือออกบิลหนึ่งใบในจังหวะเดียวกัน (ต้องส่ง `Idempotency-Key`) แพ็กเกจใช้ได้เมื่อบิลนั้นชำระครบ แล้วถูกตัดสิทธิ์เองเมื่อบิลมีบริการที่แพ็กเกจครอบ ส่วนนั้นลงบิลเป็นรายการราคา 0 คูปองและแต้มใช้ผ่าน `POST /staff/invoices/{id}/discounts` ด้วยช่อง `coupon_code` หรือ `points` แต้มได้ 1 แต้มต่อ ฿100 ที่จ่ายจริงเมื่อบิลชำระครบ และถูกหักคืนตามเงินที่คืน กลุ่มลูกค้าเก็บเป็นเงื่อนไข และใช้ส่งประกาศ LINE ด้วย `segment_id` การอ่านและปิดคูปอง การอ่านกลุ่มลูกค้าและแพ็กเกจ การปรับแต้มด้วยมือ และกติกาแต้มที่ตั้งได้ เป็นส่วนขยาย [contract-extensions.md § 25](./contract-extensions.md) รายละเอียดอยู่ใน [แพ็กเกจ คูปอง และแต้มสะสม](features/crm.md) และการตีความใน [assumptions.md § 43](./assumptions.md)

## Reminders and Automation

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| GET | `/staff/automations` | `automation:read` | `Staff/AutomationsController` | `Automation/ListAutomations/` | `Automation` |
| POST | `/staff/automations` | `automation:manage` | `Staff/AutomationsController` | `Automation/CreateAutomation/` | `Automation` |
| POST | `/staff/automations/{id}/simulate` | `automation:manage` | `Staff/AutomationSimulationController` | `Automation/SimulateAutomation/` | `Automation` |
| GET | `/staff/reminders/upcoming` | `automation:read` | `Staff/RemindersController` | `Automation/ListUpcomingReminders/` | `Automation` |

กฎ automation คือ `trigger + condition + action` ประเมินที่ฝั่ง server โดย hosted service ใน `Infrastructure/Jobs/` (`RecurringJobHost`, งาน `AutomationJob`) `simulate` และ `upcoming` ใช้ `AutomationEvaluator` ตัวเดียวกับการรันจริงในโหมด dry-run เป็นเส้นทางโค้ดเดียว ผลพรีวิวจึงไม่มีทางต่างจากการรันจริง ข้อความแต่ละฉบับถูกจองด้วย unique key ใน `automation_dispatches` ก่อนส่ง จึงไม่ส่งซ้ำแม้มี API หลาย instance การแก้ เปิดปิด และลบกติกา (`PATCH` และ `DELETE /staff/automations/{id}`) ไม่อยู่ใน contract ดู [ส่วนขยาย contract](#ส่วนขยาย-contract) § 19 รายละเอียดอยู่ใน [เตือนอัตโนมัติ](features/automation.md)

การส่งออกรายงานเป็นงานเบื้องหลังของตัวตั้งเวลาเดียวกัน (`ReportExportJob`) worker จองงานด้วย `FOR UPDATE SKIP LOCKED` ไฟล์เป็น CSV, Excel หรือ PDF ตามช่อง `format` (สองแบบหลังเป็นส่วนขยาย § 21) เก็บผ่าน `LocalFileStorage` และดาวน์โหลดด้วย URL ที่ลงชื่อ อายุ 1 ชั่วโมง นอกจาก `report:export` ยังต้องมี scope ของรายงานนั้นด้วย ดู [ภาพรวมและรายงาน](features/dashboard.md#ส่งออกรายงาน)

## Reports and Audit

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| POST | `/staff/reports/exports` | `report:export` | `Staff/ReportExportsController` | `Reporting/StartExport/` | `Reporting` |
| GET | `/staff/reports/exports/{id}` | `report:export` | `Staff/ReportExportsController` | `Reporting/GetExportStatus/` | `Reporting` |
| GET | `/staff/audit-logs` | `audit:read` | `Staff/AuditLogsController` | `Auditing/ListAuditLogs/` | `Auditing` |
| GET | `/staff/audit-logs/access` | `audit:read` | `Staff/AuditLogsController` | `Auditing/ListAuditLogs/` (ตัวกรอง access) | `Auditing` |

handler ตัวเดียวรับทั้งสอง route แยกด้วยตัวกรอง สองเส้นต่างกันที่ action ที่เลือก ไม่ได้ต่างที่พฤติกรรม ถ้าแยก handler จะต้องดูแลการจำกัดสาขาสองที่ ซึ่งเป็นการซ้ำแบบที่ทำ audit log รั่วในการแก้ครั้งที่สอง

**สิ่งที่ต้องเขียนลง audit เป็นรายชื่อที่ตั้งชื่อไว้** ไม่ใช่ธรรมเนียม คือ `Domain/Auditing/AuditActions.cs` `AuditEntry.Write` ปฏิเสธ action ที่ไม่อยู่ในรายชื่อ การพิมพ์ผิดจึงสร้างหมวดที่ไม่มีใคร query ขึ้นมาเงียบ ๆ ไม่ได้ contract มี endpoint อ่าน audit log แต่ไม่เคยบอกว่าต้องเขียนอะไร ดู [contract-extensions.md § 5](./contract-extensions.md)

## Webhooks and Integrations

| Method | Endpoint | Scope | Controller | Use case | Domain |
|---|---|---|---|---|---|
| POST | `/staff/webhooks` | *(ไม่ระบุ)* ใช้ `integration:manage` | `Staff/WebhooksController` | `Integrations/CreateWebhookSubscription/` | `Integrations` |
| GET | `/staff/webhooks/{id}/deliveries` | *(ไม่ระบุ)* ใช้ `integration:manage` | `Staff/WebhookDeliveriesController` | `Integrations/ListWebhookDeliveries/` | `Integrations` |
| POST | `/staff/api-keys` | *(ไม่ระบุ)* ใช้ `integration:manage` | `Staff/ApiKeysController` | `Integrations/CreateApiKey/` | `Integrations` |
| PUT | `/staff/integrations/payment` | *(ไม่ระบุ)* ใช้ `integration:manage` | `Staff/PaymentIntegrationController` | `Integrations/ConfigurePaymentProvider/` | `Integrations` (ไม่มี gateway ภายนอก) |
| PUT | `/staff/integrations/lab` | *(ไม่ระบุ)* ใช้ `integration:manage` | `Staff/LabIntegrationController` | `Integrations/ConfigureLabProvider/` | `Integrations` (ไม่มีตัวเชื่อมแล็บ) |
| GET | `/staff/integrations/calendar/ics` | *(ไม่ระบุ)* พนักงานทุกคน หรือ token ในลิงก์ | `Staff/CalendarFeedController` | `Integrations/GetCalendarFeed/` | `Integrations` |

scope ที่เลือกและเหตุผลอยู่ใน [assumptions.md § 2](./assumptions.md) รายละเอียดทั้งหมด (รูปร่างคำขอและคำตอบ และสิ่งที่ event แต่ละตัวพาไป) อยู่ใน [การเชื่อมต่อระบบ](features/integrations.md) รายการ แก้ ปิด ลบ หมุน secret ส่งซ้ำ ยกเลิก key และสร้างลิงก์ปฏิทินใหม่ เป็นส่วนขยาย อยู่ในตารางด้านล่างและ [contract-extensions.md § 22](./contract-extensions.md)

**event ของ webhook ขาออก** 16 ตัวใน `Domain/Integrations/WebhookEventTypes.cs` ถูกเขียนลงตาราง outbox `webhook_events` ใน transaction เดียวกับการเปลี่ยนแปลงทางธุรกิจ (`IWebhookOutbox`) งานเบื้องหลัง `webhooks` แตกเป็น `webhook_deliveries` หนึ่งแถวต่อ webhook แล้วส่งจาก `Infrastructure/Webhooks/` พร้อม HMAC-SHA256 ใน `X-Signature` ส่งไม่สำเร็จลองใหม่อีก 5 ครั้งห่างกัน 1, 5, 25, 125 และ 625 นาที (ครั้งสุดท้ายราว 13 ชั่วโมงหลังเหตุการณ์ ภายใน 24 ชั่วโมงตาม contract)

**API key** ส่งเป็น `Authorization: Bearer hp_...` header เดียวกับ access token ของคน เพราะ contract ไม่ได้กำหนด header อื่น ระบบแยกจากคำนำหน้า `hp_` key ใช้ได้เฉพาะ endpoint ที่ประกาศ `RequireScope` เท่านั้น

## ส่วนขยาย contract

endpoint ที่ **ไม่อยู่** ใน `API Reference.dc.html` บางเส้นปิดช่องโหว่ที่เจอตอนสร้างตาม contract บางเส้นเป็นฟีเจอร์ที่เจ้าของงานขอเพิ่ม ช่องสุดท้ายบอกเลขข้อ เหตุผล ทางเลือกที่ไม่เลือก และสิ่งที่ตั้งใจไม่ทำ อยู่ใน [contract-extensions.md](./contract-extensions.md) และแต่ละเส้นมีหมายเหตุว่าเป็นส่วนขยายใน controller ของตัวเอง

| Method | Endpoint | Scope | Controller | Use case | ช่องโหว่ |
|---|---|---|---|---|---|
| POST | `/staff/queue/{id}/priority` | `queue:write` | `Staff/QueueTicketActionsController` | `Queueing/SetTicketPriority/` | § 1 |
| POST | `/staff/queue/{id}/leave` | `queue:write` | `Staff/QueueTicketActionsController` | `Queueing/LeaveQueue/` | § 2 |
| GET | `/portal/consents` | portal | `Portal/PortalPrivacyController` | `Customers/GetConsents/` | § 4 |
| PUT | `/portal/consents` | portal | `Portal/PortalPrivacyController` | `Customers/RecordConsent/` | § 4 |
| GET | `/staff/customers/{id}/consents` | `customer:read` | `Staff/PrivacyController` | `Customers/GetConsents/` | § 4 |
| POST | `/staff/customers/{id}/consents` | `customer:write` | `Staff/PrivacyController` | `Customers/RecordConsent/` | § 4 |
| POST | `/portal/data-requests` | portal | `Portal/PortalPrivacyController` | `Customers/DataRequests/` | § 4 |
| GET | `/staff/data-requests` | `customer:read` | `Staff/PrivacyController` | `Customers/DataRequests/` | § 4 |
| POST | `/staff/data-requests/{id}/close` | `customer:write` | `Staff/PrivacyController` | `Customers/DataRequests/` | § 4 |
| POST | `/staff/invoices/{id}/void` | `invoice:write` | `Staff/InvoicesController` | `Billing/VoidInvoice/` | § 3 |
| GET | `/staff/invoices/{id}` | `invoice:write` | `Staff/InvoicesController` | `Billing/GetInvoice/` | § 7 |
| GET | `/staff/invoices` | `invoice:write` | `Staff/InvoicesController` | `Billing/ListInvoices/` | § 7 |
| GET | `/articles` | สาธารณะ | `Public/ArticlesController` | `Articles/ListPublishedArticles/` | § 9 |
| GET | `/articles/{slug}` | สาธารณะ | `Public/ArticlesController` | `Articles/GetArticle/` | § 9 |
| GET | `/team` | สาธารณะ | `Public/TeamController` | `Staff/ListTeam/` | § 10 |
| GET | `/staff/departments` | `staff:read` | `Staff/StaffMembersController` | `Staff/ListDepartments/` | § 10 |
| PUT | `/staff/members/{id}/public-profile` | `staff:manage` | `Staff/StaffMembersController` | `Staff/ReplacePublicProfile/` | § 10 |
| POST | `/staff/lab-requests/{id}/review` | `lab:read` | `Staff/LabRequestsController` | `Laboratory/ReviewLabResults/` | § 11 |
| PUT | `/storage/uploads/{token}` | token ที่ลงชื่อ | `Storage/StorageController` | `Patients/Documents/StoredFilesHandler` | § 12 |
| GET | `/storage/files/{token}` | token ที่ลงชื่อ | `Storage/StorageController` | `Patients/Documents/StoredFilesHandler` | § 12 |
| POST | `/portal/notifications/read` | portal | `Portal/PortalNotificationsController` | `Notifications/ListMyNotifications/` (`MarkNotificationsReadHandler`) | § 13 |
| GET | `/staff/consultations/{id}` | `consult:read` | `Staff/ConsultationsController` | `Consultations/GetConsultation/` | § 14 |
| GET | `/clinic/contacts` | สาธารณะ | `Public/ClinicController` | `Clinics/ManageContacts/` (`ListContactsHandler`) | § 15 |
| POST | `/clinic/contacts` | `settings:write` | `Staff/ClinicContactsController` | `Clinics/ManageContacts/` (`AddContactHandler`) | § 15 |
| PATCH | `/clinic/contacts/{id}` | `settings:write` | `Staff/ClinicContactsController` | `Clinics/ManageContacts/` (`UpdateContactHandler`) | § 15 |
| DELETE | `/clinic/contacts/{id}` | `settings:write` | `Staff/ClinicContactsController` | `Clinics/ManageContacts/` (`RemoveContactHandler`) | § 15 |
| GET | `/assistant` | สาธารณะ | `Public/AssistantController` | `Assistant/Chat/` (`GetAssistantStatusHandler`) | § 16 |
| POST | `/assistant/chat` | สาธารณะ | `Public/AssistantController` | `Assistant/Chat/` (`ChatHandler`) | § 16 |
| GET | `/staff/services` | `service:manage` | `Staff/ServicesController` | `ServiceCatalog/ListServices/` | § 17 |
| PATCH | `/staff/services/{id}` | `service:manage` | `Staff/ServicesController` | `ServiceCatalog/UpdateService/` | § 17 |
| GET | `/staff/integrations/line/followers` | `customer:write` | `Staff/LineFollowersController` | `Integrations/ListLineFollowers/` | § 18 |
| PUT | `/staff/customers/{id}/line` | `customer:write` | `Staff/CustomerLineController` | `Customers/LineAddress/` (`LinkLineAddressHandler`) | § 18 |
| DELETE | `/staff/customers/{id}/line` | `customer:write` | `Staff/CustomerLineController` | `Customers/LineAddress/` (`UnlinkLineAddressHandler`) | § 18 |
| PATCH | `/staff/automations/{id}` | `automation:manage` | `Staff/AutomationsController` | `Automation/UpdateAutomation/` | § 19 |
| DELETE | `/staff/automations/{id}` | `automation:manage` | `Staff/AutomationsController` | `Automation/DeleteAutomation/` | § 19 |
| GET | `/staff/prescriptions/drug-options` | `rx:write` | `Staff/PrescriptionsController` | `Prescriptions/ListDrugOptions/` | § 20 |
| GET | `/staff/webhooks` | `integration:manage` | `Staff/WebhooksController` | `Integrations/ListWebhooks/` | § 22 |
| PATCH | `/staff/webhooks/{id}` | `integration:manage` | `Staff/WebhooksController` | `Integrations/UpdateWebhookSubscription/` | § 22 |
| DELETE | `/staff/webhooks/{id}` | `integration:manage` | `Staff/WebhooksController` | `Integrations/DeleteWebhookSubscription/` | § 22 |
| POST | `/staff/webhooks/{id}/rotate-secret` | `integration:manage` | `Staff/WebhooksController` | `Integrations/RotateWebhookSecret/` | § 22 |
| POST | `/staff/webhooks/{id}/deliveries/{deliveryId}/redeliver` | `integration:manage` | `Staff/WebhookDeliveriesController` | `Integrations/RedeliverWebhookDelivery/` | § 22 |
| GET | `/staff/api-keys` | `integration:manage` | `Staff/ApiKeysController` | `Integrations/ListApiKeys/` | § 22 |
| DELETE | `/staff/api-keys/{id}` | `integration:manage` | `Staff/ApiKeysController` | `Integrations/RevokeApiKey/` | § 22 |
| POST | `/staff/integrations/calendar/token` | พนักงานทุกคน ลิงก์ของตัวเองเท่านั้น | `Staff/CalendarTokenController` | `Integrations/RotateCalendarFeed/` | § 22 |
| GET | `/staff/policies/cancellation` | `settings:read` | `Staff/CancellationPolicyController` | `Appointments/GetAppointmentRules/` | § 23 |
| GET | `/staff/appointment-types` | `appointment:read` | `Staff/AppointmentTypesController` | `Appointments/GetAppointmentRules/` | § 23 |
| GET | `/portal/appointment-types` | `portal` | `Portal/PortalAppointmentTypesController` | `Appointments/GetAppointmentRules/` | § 23 |
| GET | `/staff/waiting-list` | `appointment:write` | `Staff/WaitingListController` | `Appointments/ListWaitingList/` | § 23 |
| DELETE | `/staff/waiting-list/{id}` | `appointment:write` | `Staff/WaitingListController` | `Appointments/RemoveFromWaitingList/` | § 23 |
| GET | `/portal/thread` | `portal` | `Portal/PortalThreadController` | `Messaging/Portal/` (`GetMyThreadHandler`) | § 24 |
| POST | `/portal/thread/messages` | `portal` | `Portal/PortalThreadController` | `Messaging/Portal/` (`WriteToClinicHandler`) | § 24 |
| GET | `/staff/coupons` | `crm:read` | `Staff/CouponsController` | `Crm/ListCoupons/` | § 25 |
| POST | `/staff/coupons/{id}/deactivate` | `crm:manage` | `Staff/CouponsController` | `Crm/DeactivateCoupon/` | § 25 |
| GET | `/staff/segments` | `crm:read` | `Staff/SegmentsController` | `Crm/ListSegments/` | § 25 |
| GET | `/staff/segments/{id}` | `crm:read` | `Staff/SegmentsController` | `Crm/GetSegment/` | § 25 |
| GET | `/staff/packages` | `crm:read` | `Staff/PackagesController` | `Crm/ListPackages/` | § 25 |
| POST | `/staff/loyalty/{customer_id}/adjustments` | `crm:manage` | `Staff/LoyaltyController` | `Crm/AdjustPoints/` | § 25 |
| GET | `/clinic/settings/loyalty` | `settings:read` | `Staff/LoyaltySettingsController` | `Crm/ManageLoyaltySettings/` | § 25 |
| PUT | `/clinic/settings/loyalty` | `settings:write` | `Staff/LoyaltySettingsController` | `Crm/ManageLoyaltySettings/` | § 25 |
| PATCH | `/staff/inventory/items/{id}` | `inventory:write` | `Staff/InventoryManagementController` | `Inventory/UpdateItem/` | § 25 |
| GET | `/staff/inventory/purchase-orders` | `inventory:purchase` | `Staff/InventoryManagementController` | `Inventory/ListPurchaseOrders/` | § 25 |
| POST | `/staff/inventory/purchase-orders/{id}/cancel` | `inventory:purchase` | `Staff/InventoryManagementController` | `Inventory/CancelPurchaseOrder/` | § 25 |
| GET | `/staff/articles` | `content:manage` | `Staff/StaffArticlesController` | `Articles/Manage/ListStaffArticlesHandler` | § 26 |
| GET | `/staff/articles/{id}` | `content:manage` | `Staff/StaffArticlesController` | `Articles/Manage/GetStaffArticleHandler` | § 26 |
| POST | `/staff/articles` | `content:manage` | `Staff/StaffArticlesController` | `Articles/Manage/CreateArticleHandler` | § 26 |
| PATCH | `/staff/articles/{id}` | `content:manage` | `Staff/StaffArticlesController` | `Articles/Manage/UpdateArticleHandler` | § 26 |
| POST | `/staff/articles/{id}/publish` | `content:manage` | `Staff/StaffArticlesController` | `Articles/Manage/PublishArticleHandler` | § 26 |
| POST | `/staff/articles/{id}/unpublish` | `content:manage` | `Staff/StaffArticlesController` | `Articles/Manage/UnpublishArticleHandler` | § 26 |
| DELETE | `/staff/articles/{id}` | `content:manage` | `Staff/StaffArticlesController` | `Articles/Manage/DeleteArticleHandler` | § 26 |
| POST | `/staff/articles/{id}/cover` | `content:manage` | `Staff/StaffArticlesController` | `Articles/Manage/RequestArticleCoverHandler` | § 26 |
| POST | `/booking/guest/start` | สาธารณะ | `Public/GuestBookingController` | `Appointments/GuestBooking/` (`StartGuestBookingHandler`) | § 27 |
| POST | `/booking/guest/verify` | สาธารณะ | `Public/GuestBookingController` | `Appointments/GuestBooking/` (`VerifyGuestBookingHandler`) | § 27 |
| PATCH | `/staff/pets/{id}` | `patient:write` | `Staff/PetsController` | `Patients/UpdatePet/` | § 28 |
| POST | `/staff/pets/{id}/deceased/undo` | `patient:write` | `Staff/PetsController` | `Patients/UndoPetDeceased/` | § 29 |
| POST | `/staff/queue/displays` | `queue:write` | `Staff/QueueDisplaysController` | `Queueing/Displays/CreateQueueDisplayHandler` | § 30 |
| GET | `/staff/queue/displays?branch=` | `queue:write` | `Staff/QueueDisplaysController` | `Queueing/Displays/ListQueueDisplaysHandler` | § 30 |
| DELETE | `/staff/queue/displays/{id}` | `queue:write` | `Staff/QueueDisplaysController` | `Queueing/Displays/RevokeQueueDisplayHandler` | § 30 |
| POST | `/queue/displays/claim` | สาธารณะ จำกัดต่อ IP | `Display/DisplayClaimController` | `Queueing/Displays/ClaimQueueDisplayHandler` | § 30 |
| GET | `/queue/display-board?branch=` | token ของจอเท่านั้น | `Display/DisplayBoardController` | `Queueing/Displays/GetDisplayBoardHandler` | § 30 |
| POST | `/staff/members/{id}/public-profile/photo` | `staff:manage` | `Staff/StaffMembersController` | `Staff/Photos/RequestStaffPhotoHandler` | § 32 |
| GET | `/staff/inventory/forecast?branch=&cover_days=` | `inventory:read` | `Staff/ForecastsController` | `Forecasting/Inventory/GetInventoryForecastHandler` | § 33 |
| GET | `/staff/stats/demand-forecast?branch=&from=&to=` | `stats:read` | `Staff/ForecastsController` | `Forecasting/Demand/GetDemandForecastHandler` | § 33 |
| GET | `/staff/appointments/attendance-risk?branch=&date=` | `appointment:read` | `Staff/ForecastsController` | `Forecasting/Attendance/GetAttendanceRiskHandler` | § 33 |
| GET | `/staff/customers/due-back?reason=&branch=&cursor=` | `crm:read` | `Staff/ForecastsController` | `Forecasting/DueBack/ListDueBackHandler` | § 33 |
| POST | `/staff/ai/pets/{id}/summary` | `record:read` + `ai:use` | `Staff/StaffAiController` | `StaffAi/PetSummary/SummarizePetHandler` | § 34 |
| POST | `/staff/ai/consultations/{id}/soap-draft` | `consult:write` + `ai:use` | `Staff/StaffAiController` | `StaffAi/SoapDraft/DraftSoapHandler` | § 34 |
| POST | `/staff/ai/drafts/owner-message` | `message:write` + `ai:use` | `Staff/StaffAiController` | `StaffAi/OwnerMessage/DraftOwnerMessageHandler` | § 34 |
| POST | `/staff/ai/ask` | `ai:use` | `Staff/StaffAiController` | `StaffAi/Ask/AskHandler` | § 34 |
| GET | `/staff/ai/daily-brief?branch_id=` | `dashboard:read` | `Staff/StaffAiController` | `StaffAi/DailyBrief/GetDailyBriefHandler` | § 34 |
| GET | `/staff/ai/usage` | `ai:use` | `Staff/StaffAiController` | `StaffAi/Usage/GetStaffAiUsageHandler` | § 34 |
| GET | `/privacy-policy` | สาธารณะ | `Public/PrivacyPolicyController` | `PrivacyPolicies/GetPublicPrivacyPolicyHandler` | § 35 |
| GET | `/privacy-policy/versions/{version}` | สาธารณะ | `Public/PrivacyPolicyController` | `PrivacyPolicies/GetPublicPrivacyPolicyHandler` | § 35 |
| GET | `/staff/privacy-policy/versions` | `settings:read` | `Staff/StaffPrivacyPolicyController` | `PrivacyPolicies/ListPrivacyPolicyVersionsHandler` | § 35 |
| POST | `/staff/privacy-policy/versions` | `settings:write` | `Staff/StaffPrivacyPolicyController` | `PrivacyPolicies/CreatePrivacyPolicyDraftHandler` | § 35 |
| PATCH | `/staff/privacy-policy/versions/{id}` | `settings:write` | `Staff/StaffPrivacyPolicyController` | `PrivacyPolicies/UpdatePrivacyPolicyDraftHandler` | § 35 |
| POST | `/staff/privacy-policy/versions/{id}/publish` | `settings:danger` | `Staff/StaffPrivacyPolicyController` | `PrivacyPolicies/PublishPrivacyPolicyHandler` | § 35 |
| GET | `/portal/reviews/pending` | `portal` | `Portal/PortalReviewsController` | `Reviews/Portal/ListPendingReviewsHandler` | § 36 |
| POST | `/portal/reviews` | `portal` | `Portal/PortalReviewsController` | `Reviews/Portal/SubmitPortalReviewHandler` | § 36 |
| GET | `/reviews/invitations/{token}` | สาธารณะ token ใช้ครั้งเดียว | `Public/ReviewInvitationsController` | `Reviews/Invitations/GetReviewInvitationHandler` | § 36 |
| POST | `/reviews/invitations/{token}` | สาธารณะ token ใช้ครั้งเดียว จำกัดต่อ IP | `Public/ReviewInvitationsController` | `Reviews/Invitations/SubmitInvitationReviewHandler` | § 36 |
| GET | `/staff/reviews?status=&cursor=` | `content:manage` | `Staff/StaffReviewsController` | `Reviews/Staff/ListStaffReviewsHandler` | § 36 |
| POST | `/staff/reviews/{id}/publish` | `content:manage` | `Staff/StaffReviewsController` | `Reviews/Staff/PublishReviewHandler` | § 36 |
| POST | `/staff/reviews/{id}/reject` | `content:manage` | `Staff/StaffReviewsController` | `Reviews/Staff/RejectReviewHandler` | § 36 |
| POST | `/staff/reviews/{id}/reply` | `content:manage` | `Staff/StaffReviewsController` | `Reviews/Staff/ReplyToReviewHandler` | § 36 |
| GET | `/reviews?cursor=&limit=` | สาธารณะ | `Public/ReviewsController` | `Reviews/Public/ListPublicReviewsHandler` | § 36 |
| GET | `/queue/wait-estimate?branch=` | สาธารณะ จำกัดต่อ IP | `Public/WaitEstimateController` | `Queueing/WaitEstimate/GetWaitEstimateHandler` | § 37 |
| GET | `/portal/referral` | `portal` | `Portal/PortalReferralController` | `Referrals/Portal/GetMyReferralHandler` | § 39 |
| GET | `/referrals/{code}` | สาธารณะ จำกัดต่อ IP | `Public/ReferralsController` | `Referrals/Public/CheckReferralCodeHandler` | § 39 |
| GET | `/staff/referrals?customer=&cursor=` | `crm:read` | `Staff/StaffReferralsController` | `Referrals/Staff/ListReferralsHandler` | § 39 |
| PATCH | `/staff/schedules/shifts/{id}` | `schedule:write` | `Staff/SchedulesController` | `Scheduling/ChangeShift/` | § 47 |
| DELETE | `/staff/schedules/shifts/{id}` | `schedule:write` | `Staff/SchedulesController` | `Scheduling/RemoveShift/` | § 47 |
| GET | `/staff/schedules/assignment-rules` | `schedule:read` | `Staff/SchedulesController` | `Scheduling/GetAssignmentRules/` | § 48 |
| POST | `/staff/schedules/shifts/copy-week` | `schedule:write` | `Staff/SchedulesController` | `Scheduling/CopyWeek/` | § 49 |

§ 18 เพิ่มช่อง `line` ในคำตอบของ `GET /staff/customers/{id}` และ § 19 เพิ่มช่อง `version` ในกติกาที่ `GET` และ `POST /staff/automations` ตอบ ทั้งสองเป็นการเพิ่มช่อง ไม่ได้เปลี่ยนหรือเอาช่องเดิมออก

§ 20 เพิ่ม `inventory_item_id` ที่ไม่บังคับในคำขอ `POST /staff/consultations/{id}/prescriptions` และช่องเดียวกันในใบสั่งยาของคำตอบเคส คำขอที่มีแค่ชื่อยาทำงานเหมือนเดิมทุกอย่าง

§ 21 เพิ่มค่า `xlsx` และ `pdf` ให้ช่อง `format` ที่ไม่บังคับของ `POST /staff/reports/exports` ค่าเริ่มต้นยังเป็น `csv` ไม่มี endpoint ใหม่

§ 22 เพิ่มรหัส `webhook_disabled` และ `webhook_deleted` ในช่อง `last_error` ของประวัติการส่ง (`GET /staff/webhooks/{id}/deliveries`) สำหรับรายการที่ระบบหยุดเพราะ webhook ถูกปิดหรือถูกลบ เป็นการเพิ่มค่า ไม่ได้เปลี่ยนรูปของคำตอบ

§ 23 ไม่เปลี่ยนเส้นเดิมของ contract แถวของ `GET /staff/waiting-list` มี `status`, `pet_name` และ `service_name` เพิ่มจากรูปของคำตอบ `POST /staff/waiting-list` และการถอนคิวรอเขียน audit `waiting_list.removed`

§ 24 ไม่เปลี่ยนรูปของ `/staff/threads` แต่เพิ่มค่า `portal` ให้ช่อง `channel` ของข้อความขาเข้าและช่อง `reply_channel` คำตอบถึงกล่องที่ลูกค้าเขียนล่าสุดจาก portal ไม่มีช่องทางให้ลองก่อน จึงเข้ากล่องแจ้งเตือนและส่งทาง LINE, SMS หรืออีเมลตามลำดับปกติ

§ 25 เพิ่มช่องในเส้นเดิมโดยไม่เปลี่ยนของเดิม คือ คูปองในคำตอบของ `POST /staff/coupons` มี `status`, `remaining` และช่องการปิด กลุ่มลูกค้ามี `counted_at` บัญชีแต้ม (`GET /staff/loyalty/{customer_id}`) มี `expired_points`, `next_expiry`, `rules.points_expire_after_months` และแถวประวัติมี `expires_at` กับชนิด `adjust` รายการในคลัง (`GET /staff/inventory/items`) รับตัวกรอง `active` และมี `active` กับ `retired_at` ใบสั่งซื้อมีสถานะ `cancelled` และ `received_cost_satang` การใช้คูปองที่ปิดแล้วตอบ `409 coupon_deactivated` และการรับเข้า สั่งซื้อ หรือสั่งยาด้วยรายการที่เลิกใช้ตอบ `409 inventory_item_retired`

§ 26 เพิ่มช่อง `cover_image_url` (ลิงก์รูปปกที่ลงชื่อแล้ว อายุหนึ่งชั่วโมง มีเฉพาะบทความที่เผยแพร่อยู่) ในคำตอบของ `GET /articles` และ `GET /articles/{slug}` ซึ่งเป็นส่วนขยาย § 9 อยู่แล้ว และเพิ่ม scope `content:manage` เป็น scope แรกที่ไม่อยู่ในรายการของ contract ตอบ `409 article_slug_taken` เมื่อ slug ซ้ำกับบทความอื่น รวมบทความที่ลบไปแล้ว

§ 27 เพิ่มช่อง `new_pet` (`name`, `species`) ที่ไม่บังคับใน `POST /portal/appointments` ใช้แทน `pet_id` เพื่อลงทะเบียนสัตว์ใหม่ใน transaction เดียวกับนัด คำขอที่มี `pet_id` ทำงานเหมือนเดิม ส่งทั้งสองช่องหรือไม่ส่งเลยได้ `422` และเพิ่มจุดประสงค์ `booking` ของรหัส OTP ซึ่ง `POST /auth/otp/request` ไม่รับ สองเส้นใหม่ไม่ต้องเข้าสู่ระบบ มี rate limit ต่อ IP ของตัวเอง (`guest-hold` และ `guest-verify`) และ `verify` เขียนคุกกี้ refresh แบบเดียวกับ `POST /auth/login`


§ 28 เพิ่มช่องที่ไม่บังคับ `sex`, `birth_date`, `birth_date_estimated` และ `neutered` ในคำขอ `POST /staff/pets` และสี่ช่องเดียวกันในคำตอบของสัตว์ทุกเส้น (`GET /staff/pets`, `GET /staff/pets/{id}`, สัตว์ใน `GET /staff/customers/{id}` และ `GET /portal/pets`) ช่องที่ไม่ทราบไม่อยู่ในคำตอบ ยกเว้น `sex` ที่เป็น `unknown` และ `birth_date_estimated` ที่เป็น `false` วันเกิดในอนาคตตอบ `422` ที่ช่อง `birth_date` audit ใหม่ `pet.updated` ไม่มี webhook event ใหม่ migration `PetIdentityAndUndoDeath`

§ 29 ไม่เปลี่ยนเส้นเดิมของ contract `POST /staff/pets/{id}/deceased` ทำงานเหมือนเดิม เส้นใหม่เขียน audit `pet.deceased_undone` และคอลัมน์ภายใน `pets.deceased_undone_at` (migration เดียวกับ § 28) ทำให้การแจ้งเตือนวัคซีนและนัดในแผนการรักษาที่ถึงเวลาส่งก่อนการยกเลิกไม่ถูกส่งย้อนหลัง

§ 30 ให้เส้นเดิมของ contract `GET /queue/stream?branch=` รับ token ของจอ (`Bearer hpdsp_...`) เป็นผู้ฟังแบบที่สาม เฉพาะสาขาของจอ ข้อมูลของ event ที่ส่งให้จอมีแค่ `branch_id` ไม่มี `ticket_id` และ stream ของจอถูกตรวจซ้ำทุก heartbeat จึงปิดเองภายใน 20 วินาทีหลังจอถูกยกเลิก พนักงานและลูกค้ายังได้ข้อมูลเหมือนเดิม token ของจอใช้กับเส้นอื่นไม่ได้เลย (`401`) audit ใหม่ `queue.display_created`, `queue.display_claimed`, `queue.display_revoked` ไม่มี scope ใหม่ migration `QueueDisplays`

§ 31 ไม่มี endpoint ใหม่ บัตรคิวในคำตอบฝั่งพนักงาน (`GET /staff/queue`, `POST /staff/queue/tickets`, การกระทำบนบัตรทุกเส้น และ `POST /appointments/{id}/check-in` เมื่อพนักงานเรียก) มีช่องที่ไม่บังคับเพิ่มห้าช่อง คือ `priority_reason`, `entered_room_at`, `served_at`, `left_at` และ `left_reason` เจ้าของสัตว์ที่เช็กอินเองไม่ได้ห้าช่องนี้ `GET /staff/queue` รับ `status` ที่ไม่บังคับ (สถานะคั่นด้วยจุลภาค ค่าที่ไม่รู้จักได้ `422`) และ `assign` กับ `staff_id` ของการออกบัตร walk-in ตอบ `422` ที่ช่อง `staff_id` เมื่อไม่ใช่สัตวแพทย์ที่ยังทำงานอยู่ในสาขาของบัตร เป็นการแก้บั๊ก รูปของคำขอและคำตอบไม่เปลี่ยน

§ 32 เพิ่มช่อง `photo_url` (ลิงก์รูปที่อัปโหลด ลงชื่อแล้ว อายุหนึ่งชั่วโมง มีเฉพาะคนที่แสดงบนเว็บ) ในคำตอบของ `GET /team` และ `photo_url` อายุ 30 นาทีใน `public_profile` ฝั่งพนักงาน ซึ่งเป็นส่วนขยาย § 10 อยู่แล้ว `photo_path` เดิมรับค่าแบบใหม่ที่ขึ้นต้นด้วย `storage:` สำหรับรูปที่อัปโหลด ส่วนค่าที่ขึ้นต้นด้วย `/` ทำงานเหมือนเดิม และ `GET /team` ไม่ส่งค่า `storage:` ออกไป ไม่มี migration

§ 33 เพิ่มค่า `customer_lapsed` ให้ `trigger.type` ของ `POST` และ `PATCH /staff/automations` (`offset_days` -1 ถึง -90 เท่านั้น) และค่า `customer` ให้ `subject.type` ของ `POST /staff/automations/{id}/simulate` และ `GET /staff/reminders/upcoming` ข้อความใช้เทมเพลตใหม่ `customer.lapsed` ซึ่งผ่านความยินยอมด้านการตลาด ไม่มี endpoint ใหม่และไม่มี migration สำหรับส่วนนี้ ทั้งสี่เส้นใหม่ใช้พารามิเตอร์ `branch` เหมือนเส้นอ่านอื่น รายละเอียดอยู่ใน [คาดการณ์จากข้อมูลของคลินิก](features/forecasts.md)

§ 34 เพิ่ม scope `ai:use` เป็น scope ที่สองที่ไม่อยู่ในรายการของ contract เส้นที่เรียกโมเดลทุกเส้นต้องมี `ai:use` คู่กับ scope ของข้อมูลที่อ่าน ร่างคำแนะนำหลังรักษาต้องมี `consult:read` และร่างคำตอบต้องมี `message:read` เพิ่ม (ตรวจใน use case) ไม่มีเส้นใดบันทึกหรือส่งอะไรเอง รหัส error ใหม่สองตัว คือ `503 ai_unavailable` (ระบบไม่ได้ตั้งโมเดล หรือโมเดลตอบไม่ได้) และ `429 ai_quota_exceeded` (โควตาของเดือนหมด `details` มี `month`, `limit`, `used`) ร่างบันทึกของเคสที่ปิดแล้วได้ `409 state_conflict` `GET /staff/ai/daily-brief` ตอบ `200` เสมอ พร้อม `status` เป็น `ready`, `failed`, `pending` หรือ `disabled` ไม่มีเส้นเดิมของ contract ถูกแก้ API key ถือ `ai:use` ไม่ได้ audit ใหม่ `ai.pet_summary`, `ai.soap_draft`, `ai.owner_message_draft`, `ai.ask` migration `StaffAiAssistant` รายละเอียดอยู่ใน [ผู้ช่วย AI สำหรับพนักงาน](features/staff-ai.md)

§ 35 ไม่เพิ่ม scope ใช้ `settings:read`, `settings:write` และ `settings:danger` ของ contract การเผยแพร่เป็นของเจ้าของคลินิกคนเดียว เพราะ `clinic_admin` ไม่มี `settings:danger` และต้องผ่าน 2FA เส้นเดิมของ contract ไม่ถูกแก้ แต่พฤติกรรมของเส้นความยินยอมเปลี่ยน คือ `PUT /portal/consents`, `POST /staff/customers/{id}/consents` และ `POST /booking/guest/verify` ยังรับช่อง `policy_version` แต่ไม่ใช้ค่านั้น ระบบใส่เลขฉบับของประกาศที่เผยแพร่อยู่ตอนบันทึกเอง และใส่ว่างเมื่อยังไม่มีฉบับที่เผยแพร่ `GET /privacy-policy` ตอบ `200` เสมอ ถ้ายังไม่มีฉบับที่เผยแพร่ได้ `published: false` สองเส้นสาธารณะให้แคชได้ 60 วินาที รหัส error ใหม่หนึ่งตัวคือ `409 privacy_policy_label_taken` audit ใหม่ `privacy_policy.draft_created`, `privacy_policy.draft_updated`, `privacy_policy.published` migration `ClinicPrivacyPolicy` รายละเอียดอยู่ใน [ประกาศความเป็นส่วนตัวของคลินิก](features/privacy-policy.md)

§ 36 ไม่เพิ่ม scope ฝั่งพนักงานใช้ `content:manage` ของบทความ เพิ่มค่า `visit_completed` ให้ `trigger.type` ของ `POST` และ `PATCH /staff/automations` (`offset_days` -1 ถึง -30 เท่านั้น นับเป็นชั่วโมงหรือใส่เงื่อนไขบริการไม่ได้) และค่า `visit` ให้ `subject.type` ของ `POST /staff/automations/{id}/simulate` และ `GET /staff/reminders/upcoming` ข้อความใช้เทมเพลตใหม่ `review.invitation` ซึ่งผ่านความยินยอมด้านการตลาด และมีลิงก์รีวิวในช่อง `review_url` ที่สร้างตอนส่งจริงเท่านั้น บันทึกส่งแล้วที่ไม่ได้ส่งเพราะการมานั้นได้คำเชิญแล้วมีรหัส `already_invited` รายการกติกามีกติกาเริ่มต้นเพิ่มหนึ่งข้อ (`atm_starter_review_2d`) แบบปิด รหัส error ใหม่สองตัวคือ `409 review_already_submitted` และ `409 review_invitation_expired` การไม่เผยแพร่ที่ไม่มีเหตุผลจากรายการได้ `422` ที่ช่อง `reason` และสถานะที่เปลี่ยนไม่ได้ได้ `409 state_conflict` `POST /reviews/invitations/{token}` มี rate limit ต่อ IP ของตัวเอง (`review-submit`) `GET /reviews` ให้แคชได้ 60 วินาที audit ใหม่ `review.published`, `review.rejected`, `review.replied` migration `CustomerReviews` รายละเอียดอยู่ใน [รีวิวจากลูกค้า](features/reviews.md)

§ 39 ไม่เพิ่ม scope เพิ่มช่องที่ไม่บังคับ `referral_code` ในคำขอ `POST /booking/guest/verify` และ `POST /auth/register` ซึ่งเป็นเส้นของ contract รหัสที่ใช้ไม่ได้ถูกข้ามโดยไม่มี error และคำตอบของทั้งสองเส้นไม่เปลี่ยน `GET` และ `PUT /clinic/settings/loyalty` มีสี่ช่องใหม่ `referral_enabled`, `referral_referrer_points`, `referral_friend_points`, `referral_monthly_cap` ใน `PUT` ไม่บังคับ ช่องที่ไม่ส่งคงค่าเดิม แถวประวัติของ `GET /staff/loyalty/{customer_id}` มีชนิดใหม่ `referral` `GET /referrals/{code}` ตอบ `200` พร้อม `valid` เท่านั้นเสมอ มี rate limit ต่อ IP ของตัวเอง (`referral-check`) audit ใหม่ `referral.rewarded` migration `ReferAFriend` รายละเอียดอยู่ใน [ชวนเพื่อน](features/referrals.md)

§ 47 ถึง 49 ไม่เพิ่ม scope ใช้ `schedule:read` และ `schedule:write` เดิม เวรในคำตอบของ `GET /staff/schedules`, `POST /staff/schedules/shifts` และ `shifts_remaining` ของ `POST /staff/schedules/time-off` มีช่อง `version` เพิ่ม และรายการที่ชนใน `details` ของ `shift_overlap` และ `staff_on_time_off` มีช่อง `staff_id` เพิ่ม ทั้งสองเป็นช่องใหม่ ไม่ได้เปลี่ยนช่องเดิม รหัส error ใหม่สองตัวคือ `409 shift_ended` และ `409 shift_started` audit ใหม่ `schedule.shift_changed`, `schedule.shift_removed`, `schedule.week_copied` ไม่มี migration รายละเอียดอยู่ใน [ตารางเวร](features/schedules.md)

§ 10 ยังเพิ่มของบน endpoint เดิมของ contract โดยไม่เปลี่ยนของเดิม คือ `GET /staff/members` รับ `?department=` และ `?role=` และทุกแถวมี `department` กับ `public_profile` ส่วน `PATCH /staff/members/{id}` รับ `department` contract ระบุแค่ scope และคำอธิบายบรรทัดเดียวของสองเส้นนี้ ไม่ได้กำหนดรูปของ request หรือ response จึงไม่มีอะไรที่เอกสารกำหนดถูกแก้

ส่วนขยายด้านบิลทั้งสามใช้ `invoice:write` เพราะรายการ scope ของ contract ปิดตาย `PUT /staff/roles/{id}/permissions` ถูกกำหนดให้ปฏิเสธ scope ที่ไม่รู้จัก ถ้าคิด `invoice:read` หรือ `invoice:void` ขึ้นมาเอง endpoint นั้นจะไม่มี role ใดใน reference เรียกได้เลย นี่คือทางที่ประดิษฐ์น้อยที่สุด ไม่ใช่ทางที่ดีที่สุด

ส่วนขยายอีกสองข้อ **ไม่ได้เพิ่ม endpoint** ซึ่งควรสังเกต เพราะสิ่งที่ contract ขาดส่วนใหญ่ไม่ใช่ endpoint

| ช่องโหว่ | คำตอบ | อยู่ที่ |
|---|---|---|
| § 5 ไม่มีที่ไหนบอกว่าต้อง audit อะไร | รายชื่อที่ตั้งชื่อและตรวจได้ | `Domain/Auditing/AuditActions.cs` |
| § 6 นัดในอนาคตของสัตว์ที่เสียชีวิตไม่มีนิยาม | กฎที่เขียนชัดบน endpoint ของ contract เอง | `Patients/MarkPetDeceased/` |

ความยินยอมบังคับที่ notification port ไม่ใช่ในแต่ละ use case `ConsentAwareNotificationDispatcher` ครอบหน้าทุกช่องทาง และ `INotificationDispatcher` ซึ่งเป็นสิ่งเดียวที่ use case resolve ได้ *ก็คือ* decorator ตัวนั้น จึงไม่มีทางส่งข้อความโดยไม่ผ่านด่านนี้

## ขอบเขต

`API Reference.dc.html` มี 164 endpoint ตารางด้านบนมีครบทุกเส้น ยกเว้นสี่เส้นที่ตั้งใจตัดออก เพราะใช้ LINE เป็นตัวตนของผู้ใช้ คือ `POST /auth/line/callback`, `POST /portal/line/link`, `DELETE /portal/line/link` และ `GET /line/liff/booking-context` ดู [assumptions.md § A](./assumptions.md) `GET /portal/invoices` และ `GET /portal/packages` อยู่ในตารางสองที่ คือใต้ Customer portal และใต้โมดูลเจ้าของ

จำนวนเส้นที่เพิ่มนอก contract นับไว้ที่ [contract-extensions.md](./contract-extensions.md) ที่เดียว

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
