# ฐานข้อมูล

ระบบใช้ PostgreSQL 17 ฐานเดียวต่อคลินิก ผ่าน EF Core 10 กับ Npgsql เอกสารนี้บอกว่ามีตารางอะไร เชื่อมกันอย่างไร กฎไหนอยู่ในฐานข้อมูลเอง transaction ทำงานอย่างไร และวิธีเพิ่ม migration

## ทำไมเป็น PostgreSQL

สัญญา API ไม่ได้ระบุฐานข้อมูล จึงใช้ค่าตั้งต้นของโปรเจกต์ และ PostgreSQL ก็มีสิ่งที่สัญญาต้องการพอดี

- `timestamptz` เก็บเวลาที่มี offset แบบ ISO 8601 ได้ถูกต้อง
- transaction จริง การปิดเคสเขียนเวชระเบียน คิว นัด และใบแจ้งหนี้พร้อมกัน
- exclusion constraint กับ advisory lock สำหรับกฎที่ต้องกันการเขียนพร้อมกัน
- `xmin` ใช้เป็น version ของ `If-Match` ได้โดยไม่ต้องเขียน trigger
- `pg_trgm` ทำให้ค้นชื่อแบบ `ILIKE '%...%'` ใช้ index ได้

โค้ดที่รู้จัก EF Core อยู่ใน `HappyPaws.Infrastructure` เท่านั้น Application และ Domain ไม่อ้าง EF Core

## schema อยู่ที่ไหน

```text
backend/src/HappyPaws.Infrastructure/Persistence/
  HappyPawsDbContext.cs        เปิด pg_trgm และสแกน configuration ทั้ง assembly
  Configurations/<โมดูล>/      mapping ต่อ aggregate (สืบจาก EntityConfiguration)
  Repositories/<โมดูล>/        ที่เดียวที่ query
  Migrations/                  18 migration และ model snapshot
  Converters/                  UtcDateTimeOffsetConverter, JsonListColumn
  Interceptors/                TimestampInterceptor ใส่ created_at และ updated_at
  Idempotency/                 ตาราง idempotency_records
```

ไม่มีไฟล์ SQL ที่เขียนมือแยกไว้ ถ้ามีสองที่จะกลายเป็นความจริงสองชุดที่ขัดกันได้

## ข้อตกลง

| เรื่อง | กติกา |
|---|---|
| id | ข้อความมี prefix ตามสัญญา เช่น `usr_`, `stf_`, `cus_`, `pet_`, `apt_`, `tkt_`, `con_`, `inv_`, `rcp_` ส่วนท้ายเป็น Crockford base32 ที่ 48 บิตแรกเป็นเวลา จึงเรียงตามเวลาที่สร้าง (`Domain/Common/EntityId.cs`) ยาวไม่เกิน 40 ตัวอักษร |
| เงิน | `bigint` หน่วยสตางค์ ไม่มี `float` หรือ `money` ในตารางเงิน |
| เวลา | `timestamptz` แปลงเป็น UTC ก่อนเขียนด้วย `UtcDateTimeOffsetConverter` เพราะ Npgsql ไม่รับ offset อื่น วันที่ตามปฏิทินคลินิก เช่นวันของคิวและวันหยุด เป็น `date` |
| enum | เก็บเป็น text ด้วยชื่อสมาชิกของ enum เช่น `Booked`, `CheckedIn` ไม่มี check constraint และไม่ใช้ enum type ของ PostgreSQL |
| version | คอลัมน์ระบบ `xmin` map เป็น `Entity.Version` และเป็น concurrency token ทุก aggregate |
| ชื่อ | ตารางและคอลัมน์เป็น snake_case (`UseSnakeCaseNamingConvention`) ประวัติ migration อยู่ในตาราง `__migrations` |
| รายการเล็ก | รายการ id เป็น `text[]` เช่น `roles.scopes`, `staff_memberships.role_ids` และ `branch_ids` รายการที่เป็น object เป็น `jsonb` เช่นเวลาทำการทั้งสัปดาห์และตารางราคาของบริการ |
| การลบ | ไม่ลบจริงในข้อมูลที่มีประวัติ ใช้คอลัมน์สถานะแทน เช่น `revoked_at`, `withdrawn_at`, `merged_into_customer_id`, `voided_at` |

## ตาราง

มี 62 ตาราง แบ่งตามโมดูลได้ดังนี้

| โมดูล | ตาราง |
|---|---|
| Identity และสิทธิ์ | `user_accounts`, `user_sessions`, `staff_memberships`, `roles`, `staff_invitations`, `phone_verifications`, `password_reset_requests` |
| คลินิก | `clinic_profile`, `branches`, `opening_hours`, `clinic_holidays`, `booking_settings`, `queue_settings`, `medical_record_settings`, `clinic_contacts` |
| บริการและตารางงาน | `clinic_services`, `staff_shifts`, `staff_time_off`, `assignment_rules` |
| ลูกค้าและสัตว์ | `customers`, `customer_invitations`, `pets`, `pet_ownerships`, `pet_allergies`, `pet_vaccinations`, `pet_documents`, `treatment_plans` |
| ความยินยอมและข้อความ | `customer_consents`, `data_subject_requests`, `inbox_notifications`, `notification_deliveries`, `line_channel_settings` |
| นัดและคิว | `appointments`, `slot_holds`, `queue_tickets`, `appointment_types`, `cancellation_policy`, `waiting_list` |
| การตรวจ | `consultations`, `consultation_vitals`, `consultation_findings`, `consultation_diagnoses`, `consultation_treatments`, `prescriptions`, `treatment_templates` |
| แล็บ | `lab_requests`, `lab_results` |
| เวชระเบียน | `medical_records`, `medical_record_amendments` |
| การเงินและแพ็กเกจ | `invoices`, `invoice_lines`, `invoice_discounts`, `invoice_payments`, `invoice_refunds`, `receipts`, `customer_packages` |
| เนื้อหาเว็บ | `articles` |
| เตือนอัตโนมัติและรายงาน | `automation_rules`, `automation_dispatches`, `report_exports` |
| ระบบ | `audit_entries`, `idempotency_records` |

ยังไม่มีตารางของคลังยา ข้อความสองทาง และ webhook ไฟล์ที่อัปโหลดไม่อยู่ในฐานข้อมูล `pet_documents` เก็บแค่ข้อมูลของไฟล์และ key ในที่เก็บไฟล์

### ตารางที่มีแถวเดียว

การตั้งค่าที่สัญญาเขียนทั้งก้อน เก็บเป็นหนึ่งแถวที่มี id คงที่ และมี check constraint กันแถวที่สอง

| ตาราง | id | check constraint |
|---|---|---|
| `clinic_profile` | `set_clinic` | `ck_clinic_profile_singleton` |
| `opening_hours` | `set_hours` | `ck_opening_hours_singleton` |
| `booking_settings` | `set_booking` | `ck_booking_settings_singleton` |
| `queue_settings` | `set_queue` | `ck_queue_settings_singleton` |
| `medical_record_settings` | `set_medical_record` | `ck_medical_record_settings_singleton` |
| `assignment_rules` | `set_assignment` | `ck_assignment_rules_singleton` |
| `appointment_types` | `set_appointment_types` | `ck_appointment_types_singleton` |
| `cancellation_policy` | `set_cancellation_policy` | `ck_cancellation_policy_singleton` |

แถวเดียวหมายถึง `xmin` เดียว จึงใช้ `If-Match` บอกได้ว่า "ทั้งสัปดาห์ถูกแก้ระหว่างที่คุณกำลังแก้" ถ้าแยกเจ็ดแถวจะบอกแบบนี้ไม่ได้ migration `BookingAndAppointments` ใส่แถว `set_booking` ให้ตั้งแต่แรก (ช่องนัด 30 นาที จองล่วงหน้าอย่างน้อย 120 นาที ยกเลิกได้ก่อน 12 ชั่วโมง จองได้ไกลสุด 60 วัน)

## ความสัมพันธ์

```text
user_accounts ─┬─ user_sessions            (ลบบัญชีแล้วเซสชันหายตาม)
               ├─ staff_memberships        (ไม่เกินหนึ่งต่อบัญชี) role_ids[], branch_ids[]
               └─ customers.user_id        (ไม่เกินหนึ่งต่อบัญชี)

customers ─┬─ pet_ownerships ─── pets ─┬─ pet_allergies
           │                           ├─ appointments ── queue_tickets (เมื่อเช็กอิน)
           │                           ├─ consultations ─┬─ vitals, findings, diagnoses, treatments, prescriptions
           │                           │                 ├─ medical_records ── medical_record_amendments
           │                           │                 └─ invoices ─┬─ lines, discounts, payments, refunds
           │                           │                               └─ receipts (หนึ่งต่อบิล)
           └─ customer_consents, data_subject_requests
```

- **ลูกค้ากับสัตว์เป็นแบบหลายต่อหลาย** ผ่าน `pet_ownerships` และ `pets.primary_customer_id` บอกเจ้าของหลัก
- **พนักงานกับสาขา และพนักงานกับ role เป็น array** ใน `staff_memberships` ไม่ใช่ตารางเชื่อม จึงไม่มี foreign key ไปที่ `branches` หรือ `roles` การตรวจว่า id ถูกต้องอยู่ใน use case
- **บัตรคิวสร้างตอนเช็กอิน** บัตร walk-in ไม่มี `appointment_id`
- **เวชระเบียนเกิดตอนปิดเคส** หนึ่งเคสได้หนึ่งฉบับ แก้ไขด้วยการเพิ่มแถวใน `medical_record_amendments`
- **น้ำหนักอ่านจาก `consultation_vitals`** ไม่มีตารางน้ำหนักแยก
- **foreign key ส่วนใหญ่เป็น `Restrict`** ที่เป็น `Cascade` มีเฉพาะส่วนย่อยของ aggregate เช่นรายการในบิล ส่วนย่อยของเคส และเซสชันของบัญชี `medical_records` ไม่มี cascade จากที่ไหนเลย

## constraint ที่ถือกฎธุรกิจ

กฎเหล่านี้อยู่ในฐานข้อมูลเพราะต้องกันการเขียนพร้อมกันสองคำขอ ซึ่งการตรวจใน transaction เดียวมองไม่เห็นข้อมูลที่อีก transaction ยังไม่ commit

| กฎ | กลไก | ไฟล์ |
|---|---|---|
| สัตวแพทย์หนึ่งคนไม่ถูกจองเวลาซ้อนในสาขาเดียวกัน | exclusion constraint `ex_appointments_staff_slot` บน `(branch_id, staff_id, tstzrange(starts_at, ends_at))` เฉพาะนัดสถานะ `Booked`, `Confirmed`, `CheckedIn`, `InConsultation` ใช้ extension `btree_gist` ช่วงเป็นแบบ `[)` นัดต่อกันพอดีจึงไม่ชน | migration `AppointmentSlotExclusion` |
| แปลงการชนเป็น error ของสัญญา | `PostgresErrorTranslator` จับ `23P01` ของ constraint นี้แล้วตอบ `slot_unavailable` | `PostgresErrorTranslator.cs` |
| เลขคิวไม่ซ้ำ | `pg_advisory_xact_lock` ต่อสาขา (ต่อวันถ้าตั้งให้เริ่มใหม่ทุกวัน) และ unique index `(branch_id, service_date, number)` เป็นด่านสุดท้าย | `Repositories/Queueing/QueueRepositories.cs` |
| เลขใบแจ้งหนี้ ใบเสร็จ และ HN ไม่ซ้ำ | advisory lock ต่อชุดเลข เช่น `INV-202610-` แล้วแปลงส่วนท้ายเป็นตัวเลขก่อนหาค่ามากสุด เพราะการเทียบข้อความทำให้ `'10000'` มาก่อน `'9999'` เดือนนับตามเวลาคลินิก | `DocumentSequence.cs`, `DocumentNumberGenerator.cs` |
| หนึ่งเคสมีการวินิจฉัยหลักเดียว | unique index `ux_consultation_primary_diagnosis` บน `consultation_id` เฉพาะ `is_primary` | `ConsultationPartsConfiguration.cs` |
| หนึ่งเคสมีบิลเดียว | unique index บน `invoices.consultation_id` เฉพาะแถวที่ไม่ว่าง | `InvoiceConfiguration.cs` |
| หนึ่งบิลมีใบเสร็จเดียว | unique index บน `receipts.invoice_id` | `InvoicePartsConfiguration.cs` |
| หนึ่งเคสมีเวชระเบียนเดียว | unique index บน `medical_records.consultation_id` | `MedicalRecordConfiguration.cs` |
| วันหยุดไม่ซ้ำ | unique `(date, branch_id)` แบบ nulls not distinct วันหยุดทั้งคลินิกสองแถวในวันเดียวจึงชนกัน | `ClinicSetupConfiguration.cs` |
| ผู้ดูแลร่วมและแพ้ยาไม่ซ้ำ | unique `(pet_id, customer_id)` เฉพาะ `revoked_at IS NULL` และ `(pet_id, substance)` เฉพาะ `withdrawn_at IS NULL` | `PetConfiguration.cs`, `PetAllergyConfiguration.cs` |
| พิกัดสาขาครบคู่ | `ck_branches_coordinates_paired` มีทั้ง latitude และ longitude หรือไม่มีเลย | `BranchConfiguration.cs` |

เบอร์โทรลูกค้ามี index แต่ **ไม่ unique** เพราะคนในบ้านเดียวกันใช้เบอร์เดียวกันได้ และมี endpoint รวมรายชื่อซ้ำอยู่แล้ว

## ข้อมูลที่ไม่ถูกแก้

ไม่มี constraint บังคับ แต่ entity ไม่มี method แก้ และ repository ไม่มีคำสั่ง update

| ตาราง | เหตุผล |
|---|---|
| `audit_entries` | ประวัติที่แก้ได้ไม่ใช่หลักฐาน |
| `medical_records`, `medical_record_amendments` | ฉบับเดิมไม่ถูกแตะ การแก้คือแถวใหม่ที่มีเนื้อหาฉบับแก้และ diff |
| `customer_consents` | หนึ่งแถวต่อการตัดสินใจหนึ่งครั้ง ค่าปัจจุบันคือแถวล่าสุดของแต่ละวัตถุประสงค์ |
| `invoice_payments`, `invoice_refunds`, `invoice_discounts` | เป็นบัญชีแยกประเภท เงินที่เคลื่อนแล้วแก้ด้วยแถวใหม่ |

สองเรื่องที่ต้องระวังเป็นพิเศษ

- **`invoices.status` เก็บไว้แต่ไม่มีใครตั้งค่าเอง** มีไว้ให้ `?status=open` ใช้ index ค่าคำนวณใหม่ใน `Invoice.Recompute()` ทุกครั้งที่มีการเคลื่อนไหว ส่วนยอดรวม ยอดชำระ และยอดค้างไม่มีคอลัมน์เลย คำนวณจากแถวลูกทุกครั้ง
- **`medical_records.content_json` เป็น `text` ไม่ใช่ `jsonb`** เพราะ `jsonb` เรียงคีย์และตัดช่องว่างใหม่ ไบต์ที่อ่านกลับจะไม่ตรงกับที่ hash ไว้ `content_hash` ครอบเนื้อหา ผู้ลงนาม และเวลาลงนามที่ตัดเหลือระดับมิลลิวินาที

## transaction

`IUnitOfWork.ExecuteInTransactionAsync` (`Persistence/UnitOfWork.cs`) ใช้ execution strategy ของ Npgsql ที่เปิด `EnableRetryOnFailure()` ไว้

- เมื่อเกิดข้อผิดพลาดชั่วคราว ระบบรัน **callback ทั้งก้อนใหม่** และล้าง change tracker ก่อนทุกรอบ
- ผลคือ **ต้องโหลดข้อมูลที่จะแก้ข้างใน callback** ข้อมูลที่โหลดไว้ก่อนเรียกจะหลุดจาก tracker และการแก้หายไปโดยไม่มี error
- ถ้ามี transaction เปิดอยู่แล้ว การเรียกซ้อนจะเข้าร่วม transaction เดิม เช่น `ConsultationBilling` ที่ถูกเรียกจากการปิดเคส
- `IAuditTrail.Record` เข้าร่วม transaction เดียวกัน ถ้า rollback แถว audit ก็หายไปด้วย ส่วนการอ่านที่ต้องบันทึกใช้ `RecordImmediatelyAsync`
- ห้ามเรียกบริการภายนอกใน callback ทำหลัง commit เสมอ

## idempotency

`idempotency_records` มี primary key `(key, endpoint, subject)` การจอง key คือการ INSERT สองคำขอที่มาพร้อมกันจะมีตัวเดียวที่ INSERT สำเร็จ อีกตัวอ่านแถวที่มีอยู่แล้วตอบซ้ำหรือตอบว่ากำลังทำอยู่ แถวมี `expires_at` 24 ชั่วโมงหลังสร้าง และมี index รอไว้ แต่ **ยังไม่มีงานที่ลบแถวหมดอายุ** และการค้นหาก็ไม่ได้กรองเวลา รายละเอียดฝั่ง HTTP อยู่ใน [การป้องกันคำขอ](security/request-protection.md)

## migration

| ลำดับ | ชื่อ | เพิ่มอะไร |
|---|---|---|
| 1 | `InitialFoundation` | `idempotency_records` |
| 2 | `IdentityModule` | บัญชี เซสชัน role สมาชิกพนักงาน |
| 3 | `RefreshTokenReuseDetection` | hash ของ refresh token รุ่นก่อน |
| 4 | `BookingAndAppointments` | สาขา บริการ ลูกค้า สัตว์ นัด hold กะงาน บัตรคิว |
| 5 | `AppointmentSlotExclusion` | `btree_gist` และ `ex_appointments_staff_slot` |
| 6 | `QueueModule` | คอลัมน์ลำดับและ index ของคิว |
| 7 | `ConsultationsAndAudit` | การตรวจ เวชระเบียน ใบสั่งยา แพ้ยา audit |
| 8 | `ConsentAndDataRights` | ความยินยอม คำขอเรื่องข้อมูล |
| 9 | `Billing` | ใบแจ้งหนี้และตารางลูก ใบเสร็จ |
| 10 | `CustomersAndPatients` | `pg_trgm`, คำเชิญลูกค้า, index ค้นชื่อ |
| 11 | `ClinicSetupStaffAndServices` | การตั้งค่าคลินิก วันหยุด คำเชิญพนักงาน |
| 12 | `Articles` | ตาราง `articles` |
| 13 | `StaffDepartmentsAndPublicProfiles` | `department` และโปรไฟล์สาธารณะบน `staff_memberships` ใส่แผนกให้พนักงานเดิมตาม `staff_type` |
| 14 | `SchedulingAndAccountSelfService` | ตาราง `staff_time_off` และ `assignment_rules` (แถวเดียว) บน `user_accounts` เพิ่มภาษา ช่องทางแจ้งเตือน ความลับ 2FA ที่รอยืนยัน และ hash ของ recovery code |
| 15 | `TierTwoClinicalAppointmentsAndPortal` | 13 ตารางของงานรักษาต่อเนื่อง นัดขั้นสูง การเข้าสู่ระบบของลูกค้า และ portal บน `appointments` เพิ่ม `type_code` (นัดเดิมเป็น `regular`) `series_id` และ `is_late_cancellation` บน `pets` เพิ่มชื่อเล่น รูป อาหาร และพฤติกรรม |
| 16 | `ClinicContacts` | ตาราง `clinic_contacts` (ช่องทางติดต่อของคลินิก) |
| 17 | `NotificationDeliveryAndLineChannel` | ตาราง `notification_deliveries` (ประวัติการส่ง หนึ่งแถวต่อหนึ่งครั้งที่ลองส่ง) และ `line_channel_settings` (แถวเดียว) บน `customers` เพิ่ม `line_user_id` (ไม่ซ้ำเมื่อมีค่า) และ `line_unfollowed_at` |
| 18 | `AutomationAndReportExports` | ตาราง `automation_rules` (กติกาเตือนอัตโนมัติ ใส่กติกาเริ่มต้นสี่ข้อแบบปิดไว้) `automation_dispatches` (บันทึกส่งแล้ว unique `(rule_id, subject_kind, subject_id, due_key)` คือสิ่งที่กันการส่งซ้ำ) และ `report_exports` (งานส่งออกรายงาน) |

**นำ migration ขึ้นฐานข้อมูล** ใช้คำสั่ง operator ไม่ใช่ `dotnet ef database update`

```sh
cd backend
dotnet run --project src/HappyPaws.Api -- migrate
```

API ไม่ migrate เองตอนเปิด เพราะ instance ที่ migrate ตอนเปิดอาจเปลี่ยนโครงสร้างกลางวันทำการ

**สร้าง migration ใหม่** ติดตั้ง local tool ครั้งแรกด้วย `dotnet tool restore` แล้ว

```sh
cd backend
dotnet dotnet-ef migrations add <ชื่อ> --project src/HappyPaws.Infrastructure --startup-project src/HappyPaws.Api --output-dir Persistence/Migrations
```

`DesignTimeDbContextFactory` สร้าง context ด้วย connection string หลอก จึงสร้าง migration ได้โดยไม่ต้องมีฐานข้อมูล ข้อเสียคือ `dotnet ef database update` จะต่อไปที่ฐานหลอกนั้น ถ้าจำเป็นต้องใช้จริง ให้ตั้ง `HAPPYPAWS_DESIGNTIME_CONNECTION` ก่อน อ่าน SQL ที่สร้างก่อน commit ทุกครั้ง และอย่าแก้ migration ที่เคยขึ้นฐานข้อมูลจริงแล้ว

## โค้ดที่เกี่ยวข้อง

| ไฟล์ | หน้าที่ |
|---|---|
| `Persistence/HappyPawsDbContext.cs` | context เดียว, `pg_trgm`, convention ของเวลาและทศนิยม |
| `Persistence/Configurations/EntityConfiguration.cs` | key, `xmin`, `created_at`, `updated_at` ของทุก aggregate |
| `Persistence/UnitOfWork.cs` | transaction และ retry |
| `Persistence/PostgresErrorTranslator.cs` | แปลง constraint ที่ตั้งใจสร้างเป็น error ของสัญญา |
| `Persistence/DocumentSequence.cs` | เลขเอกสารแบบมี lock |
| `Persistence/DatabaseMigrator.cs` | คำสั่ง `migrate` |
| `Persistence/DesignTimeDbContextFactory.cs` | context สำหรับ `dotnet-ef` |
