# ข้อมูลและความลับ

ส่วนหนึ่งของ [ความปลอดภัย](README.md)

เอกสารนี้บอกว่าข้อมูลที่ใช้พิสูจน์ตัวตนเก็บอย่างไร ความลับของระบบอยู่ที่ไหน และค่าตั้งค่าตัวไหนมีผลจริง โครงสร้างตารางทั้งหมดอยู่ใน [ฐานข้อมูล](../database.md)

## รหัสผ่าน

ใช้ `PasswordHasher<T>` ของ ASP.NET Core Identity ผ่าน `IdentityPasswordHasher` ใช้เฉพาะตัว hash ไม่ได้ใช้ระบบคุกกี้หรือหน้าจอของ Identity เพราะ contract กำหนด endpoint token เซสชัน และ 2FA ของตัวเอง

- รูปแบบเป็น Identity รุ่น 3 คือ PBKDF2 HMAC-SHA512 100,000 รอบ salt สุ่ม 16 ไบต์ต่อบัญชี ค่าทั้งหมดเก็บในข้อความ hash เอง ค่า hash หลอกใน `StaffLoginHandler` เข้ารหัสค่าชุดนี้ไว้ด้วย
- ถ้าวันหนึ่งเพิ่มจำนวนรอบ hash เก่ายังตรวจผ่าน และถูก hash ใหม่ตอนเข้าสู่ระบบครั้งถัดไป (`SuccessRehashNeeded`)
- เก็บใน `user_accounts.password_hash` บัญชีที่ยังไม่มีรหัสผ่านมีค่าเป็น null
- ยาว 8 ถึง 256 ตัวอักษร ไม่บังคับตัวพิมพ์ใหญ่ ตัวเลข หรือสัญลักษณ์ เพราะกฎแบบนั้นผลักให้คนใช้รหัสที่เดาง่าย ด่านจริงคือ 2FA
- ยังไม่ตรวจกับรายการรหัสผ่านที่รั่วไหล
- รหัสผ่านที่คำสั่ง operator สร้างให้คือ 18 ไบต์สุ่ม (144 บิต) เข้ารหัส base64 แบบปลอดภัยต่อ URL

## Token ที่เก็บเป็น hash

ค่าที่สุ่มยาวพอไม่ต้องใช้ hash ที่ช้า SHA-256 ธรรมดาพอ เพราะไม่มีอะไรให้เดา จุดประสงค์คือสำเนาฐานข้อมูลที่หลุดออกไปต้องไม่ให้เซสชันหรือคำเชิญที่ใช้ได้

| ค่า | สุ่ม | เก็บ | เทียบ |
|---|---|---|---|
| refresh token | 32 ไบต์ | SHA-256 ใน `user_sessions` ทั้งรุ่นปัจจุบันและรุ่นก่อน | ค้นด้วย hash ใน query |
| token ในลิงก์คำเชิญ | 32 ไบต์ | SHA-256 ใน `staff_invitations.token_hash` | `CryptographicOperations.FixedTimeEquals` |
| challenge ของ 2FA | คือ refresh token ของเซสชันที่ยังไม่ผ่าน 2FA | เหมือน refresh token | ค้นด้วย hash |
| รหัส OTP ของลูกค้า | 6 หลัก | SHA-256 ของ "เบอร์:รหัส" ใน `phone_verifications.code_hash` | เทียบ hash เดาผิดได้ 5 ครั้งต่อรหัส อายุ 5 นาที |
| token ในลิงก์รีเซ็ตรหัสผ่าน | 32 ไบต์ | SHA-256 ใน `password_reset_requests.token_hash` | ค้นด้วย hash ใช้ได้ครั้งเดียว อายุ 30 นาที |
| access token | ไม่เก็บ | JWT ลงนาม ตรวจด้วยกุญแจ | ลายเซ็น |
| API key | 32 ไบต์ และ prefix 10 ตัว | SHA-256 ของ key ทั้งก้อนใน `api_keys.secret_hash` | หาด้วย prefix แล้ว `FixedTimeEquals` |
| token ของลิงก์ปฏิทิน | 32 ไบต์ | SHA-256 ใน `calendar_feeds.token_hash` และ ciphertext ไว้แสดงลิงก์เดิม | ค้นด้วย hash |
| token ของจอห้องรอ (`hpdsp_`) | 32 ไบต์ | SHA-256 ใน `queue_displays.token_hash` ไม่มี ciphertext ไม่มีที่ไหนแสดงซ้ำ | ค้นด้วย hash แล้ว `FixedTimeEquals` |
| รหัสตั้งจอห้องรอ | 8 ตัวจาก 30 ตัวอักษร | SHA-256 ใน `queue_displays.setup_code_hash` ล้างทิ้งตอนแลกเป็น token หรือตอนยกเลิก | ค้นด้วย hash ใช้ได้ครั้งเดียว อายุ 30 นาที |
| URL อัปโหลดและดาวน์โหลดไฟล์ | ไม่เก็บ | ลงชื่อด้วย HMAC-SHA256 บอกสิทธิ์ key ชนิด ขนาด และเวลาหมดอายุไว้ในตัว | `FixedTimeEquals` |

รหัส OTP มีแค่ล้านแบบ hash จึงกันการเดาไม่ได้ถ้าใครได้สำเนาฐานข้อมูลไป สิ่งที่กันจริงคืออายุ 5 นาที และการใช้ได้ครั้งเดียว ลิงก์รีเซ็ตวาง token ไว้หลัง `#` เหมือนลิงก์คำเชิญ

**กุญแจลงชื่อ URL ของไฟล์** อยู่ที่ `Storage:SigningKey` ถ้าไม่ตั้ง คำนวณจากกุญแจ JWT ด้วย HMAC จึงไม่ใช่กุญแจเดียวกัน ใครได้ URL ไปก็โหลดไฟล์ได้จนกว่าจะหมดอายุ 10 นาที เหมือน pre-signed URL ของ S3

**รูปปกบทความเป็นไฟล์เดียวที่คนไม่เข้าสู่ระบบเปิดได้** ([contract-extensions.md § 26](../contract-extensions.md)) ไม่มี URL ถาวรหรือ bucket สาธารณะ `GET /articles` และ `GET /articles/{slug}` สร้างลิงก์ที่ลงชื่อแล้วอายุหนึ่งชั่วโมงทุกครั้งที่อ่าน และสร้างเฉพาะตอนที่บทความเผยแพร่อยู่ รูปของร่างจึงไม่มีลิงก์ที่คนนอกเห็น และรูปของบทความที่นำลงแล้วเปิดได้ต่อไม่เกินหนึ่งชั่วโมงสำหรับคนที่ได้ลิงก์ไปก่อน หน้าแก้บทความได้ลิงก์อายุ 30 นาที รูปปกไม่ใช่ข้อมูลส่วนบุคคล

ลิงก์คำเชิญวาง token ไว้หลัง `#` (`AcceptUrlTemplate` เป็น `.../invitations/{id}#token={token}`) เบราว์เซอร์ไม่ส่งส่วนนี้ไปเซิร์ฟเวอร์ token จึงไม่อยู่ใน access log และ header `Referer`

## ความลับที่เข้ารหัสด้วย Data Protection

ค่าที่ระบบต้องอ่านกลับเพื่อใช้งานเก็บเป็น hash ไม่ได้ จึงเข้ารหัสด้วย ASP.NET Core Data Protection ผ่าน `ISecretProtector` (`Infrastructure/Webhooks/DataProtectionSecretProtector.cs`) แต่ละชนิดมี purpose ของตัวเอง ciphertext ที่คัดลอกข้ามคอลัมน์จึงถอดไม่ได้

| ค่า | คอลัมน์ |
|---|---|
| secret ลงนาม webhook | `webhook_subscriptions.secret_ciphertext` |
| secret key และ webhook secret ของผู้ให้บริการชำระเงิน | `payment_provider_settings.secret_key_ciphertext`, `webhook_secret_ciphertext` |
| API key ของแล็บ | `lab_provider_settings.api_key_ciphertext` |
| token ของลิงก์ปฏิทิน | `calendar_feeds.token_ciphertext` |

กุญแจอยู่ที่ `DataProtection:KeysDirectory` นอกฐานข้อมูล หมุนเองทุก 90 วันและเก็บกุญแจเก่าไว้ถอด ต้องเป็นโฟลเดอร์ถาวรตอนติดตั้งจริง ดู [development.md](../development.md#กุญแจเข้ารหัสความลับ) ใครได้ทั้งสำเนาฐานข้อมูลและโฟลเดอร์กุญแจไปจะถอดได้ทั้งหมด จึงควรสำรองสองอย่างนี้แยกกัน

## ความลับ TOTP

- สุ่ม 20 ไบต์ เข้ารหัส base32 เก็บใน `user_accounts.two_factor_secret`
- ระหว่างรับคำเชิญ ความลับที่ยังไม่ได้พิสูจน์เก็บใน `staff_invitations.pending_two_factor_secret` และถูกล้างเมื่อรับคำเชิญสำเร็จ
- API ไม่คืนความลับของบัญชีที่ลงทะเบียนแล้วอีกเลย มีเพียงการตอบรับคำเชิญขั้นแรก และคำสั่ง operator ที่พิมพ์บนหน้าจอครั้งเดียว

**ทั้งสองคอลัมน์เก็บแบบไม่เข้ารหัส** `UserAccountConfiguration` และ `StaffInvitationConfiguration` กำหนดเพียงความยาว 512 ไม่มีตัวแปลงค่า คำอธิบายใน `UserAccount.cs` ที่บอกว่า "Encrypted at rest by the infrastructure layer" ไม่ตรงกับโค้ด ใครได้สำเนาฐานข้อมูลไปจะสร้างรหัส 2FA ของทุกคนได้ ทางแก้ที่เหมาะกับโค้ดชุดนี้คือ ASP.NET Core Data Protection ใส่เป็นตัวแปลงค่าใน configuration ของ EF แล้วเขียน migration ที่เข้ารหัสค่าเดิม

**รหัสสำรอง** บัญชีที่ลงทะเบียน 2FA เองด้วย `POST /me/2fa/enroll` ได้ recovery code 8 รหัส เก็บเป็น SHA-256 ดู [บัญชีของตัวเอง](../features/account.md)

## กุญแจลงนาม JWT

| ค่า | กติกา |
|---|---|
| `Auth:Jwt:SigningKey` | บังคับ ยาวอย่างน้อย 32 ตัวอักษร ตรวจตอนเปิด API (`ValidateDataAnnotations` กับ `ValidateOnStart`) ไม่ผ่านแล้ว API ไม่เปิด |
| `Auth:Jwt:Issuer` | บังคับ ตรวจตอนเปิดเช่นกัน |
| `Auth:Jwt:Audience` | ค่าเริ่มต้น `happypaws-api` |
| อัลกอริทึม | HS256 ทั้งตอนออกและตอนตรวจ ฝั่งตรวจไม่รับอัลกอริทึมอื่น |

- กุญแจเดียวทั้งออกและตรวจ เพราะแต่ละคลินิกเป็นการติดตั้งของตัวเอง ไม่มีผู้ออก token กลาง
- ยังไม่รองรับกุญแจหลายดอก การเปลี่ยนกุญแจทำให้ access token ที่ออกไปแล้วใช้ไม่ได้ทันที แต่ refresh token ยังใช้ได้ เพราะอยู่ในฐานข้อมูล ผู้ใช้จึงได้ token ใหม่ตอน refresh โดยไม่ต้องเข้าสู่ระบบใหม่
- `appsettings.json` ตั้งกุญแจและ issuer เป็นค่าว่าง API ใน environment อื่นนอกจาก `Development` จึงไม่เปิดจนกว่าจะส่งค่าผ่าน environment เช่น `Auth__Jwt__SigningKey`
- `appsettings.Development.json` มีกุญแจ dev อยู่ใน repository พร้อมคำเตือนว่าห้ามใช้นอกเครื่องตัวเอง ค่านี้ถูก `dotnet publish` คัดลอกไปอยู่ใน image ด้วย และ `docker-compose.yml` ตั้ง `ASPNETCORE_ENVIRONMENT` เป็น `Development` เป็นค่าเริ่มต้น ถ้ารัน compose โดยไม่ตั้งค่า กุญแจนี้คือกุญแจที่ใช้จริง

## ความลับอื่นในค่าตั้งค่า

| ค่า | อยู่ที่ | หมายเหตุ |
|---|---|---|
| รหัสผ่าน PostgreSQL | `ConnectionStrings:Postgres` ใน `appsettings.Development.json` (`happypaws`) และ `POSTGRES_PASSWORD` ใน `.env` | ค่าเริ่มต้นของ compose คือ `happypaws` ส่วน `.env.example` ใช้ `change-me` |
| รหัสผ่านของคำสั่ง operator | `--password`, `--owner-password` หรือ `HAPPYPAWS_OPERATOR_PASSWORD` | ใช้ตัวแปร environment เพื่อไม่ให้อยู่ในประวัติคำสั่ง |
| LINE channel | `LINE_CHANNEL_ID`, `LINE_CHANNEL_SECRET`, `LINE_CHANNEL_ACCESS_TOKEN` ใน `.env.example` | ยังไม่มีโค้ดอ่าน ตัวส่ง LINE ยังไม่ได้ทำ |

- `.gitignore` กัน `.env`, `.env.local`, `.env.*.local`, `*.pfx`, `*.key`
- ความลับของการเชื่อมต่อภายนอก (LINE ช่องทางชำระเงิน แล็บ) ตั้งใจให้อยู่ในค่าตั้งค่า ไม่ใช่คอลัมน์ในฐานข้อมูล แต่ตอนนี้ยังไม่มีการเชื่อมต่อใดเลย
- การเข้ารหัสระดับแอปพลิเคชันมีเฉพาะคอลัมน์ในหัวข้อ [ความลับที่เข้ารหัสด้วย Data Protection](#ความลับที่เข้ารหัสด้วย-data-protection) คอลัมน์อื่น รวมถึงความลับ TOTP และของ LINE ยังไม่เข้ารหัส การเข้ารหัสดิสก์และสำเนาสำรองเป็นหน้าที่ของโครงสร้างพื้นฐานที่ติดตั้ง

## ค่าตั้งค่าที่มีผลจริง

`appsettings.json` มีค่าหลายตัวที่ดูเหมือนตั้งได้ แต่โค้ดไม่ได้อ่าน ค่าจริงเขียนตายตัวในโค้ด แก้ไฟล์ตั้งค่าแล้วจึงไม่มีอะไรเปลี่ยน

| คีย์ | มีโค้ดอ่าน | ค่าที่ใช้จริงอยู่ที่ |
|---|---|---|
| `Auth:Jwt:*` | อ่าน | `JwtOptions` |
| `Auth:RefreshTokenLifetimeDays` (30) | ไม่อ่าน | `UserSession.Lifetime` 30 วัน |
| `Auth:RefreshTokenRotation` (true) | ไม่อ่าน | หมุนทุกครั้งเสมอ |
| `Auth:PasswordResetLinkLifetimeMinutes` (30) | ไม่อ่าน | `PasswordResetRequest.Lifetime` 30 นาที |
| `Auth:SignIn:LogUndeliveredSecrets`, `PasswordResetUrlTemplate` | อ่าน | `SignInDeliveryOptions` |
| `Storage:LocalRoot`, `SigningKey`, `PublicBaseUrl` | อ่าน | `StorageOptions` |
| `Auth:StaffInvitationLifetimeDays` (7) | ไม่อ่าน | `StaffInvitation.Lifetime` 7 วัน |
| `Auth:BookingHoldLifetimeMinutes` (10) | ไม่อ่าน | `SlotHold.Lifetime` 10 นาที |
| `Auth:TwoFactorRequiredForMedicalScopes` (true) | ไม่อ่าน | กฎ 2FA เปิดตลอด |
| `RateLimits:PerTokenPerMinute`, `RateLimits:MessagingPerMinute` | อ่าน | `RateLimitingExtensions` |
| `RateLimits:OtpPerPhonePer10Minutes` (3) | อ่าน | `OtpSettings` ใช้ใน `RequestOtpHandler` |
| `RateLimits:GuestHoldsPerIpPerHour` (30), `RateLimits:GuestVerifiesPerIpPerHour` (60) | อ่าน | `RateLimitingExtensions` policy `guest-hold` และ `guest-verify` (ส่วนขยาย § 27) |
| `Cors:AllowedOrigins` | อ่าน | `Program.cs` |
| `Staff:Invitations:LogUndeliveredLinks`, `AcceptUrlTemplate` | อ่าน | `StaffInvitationOptions` |
| `Clinic:TimeZone`, `Clinic:DefaultCulture` | อ่าน | `ClinicOptions` |

ถ้าจะให้ค่าที่ไม่ได้อ่านมีผล ต้องผูกเข้า options class ก่อน หรือลบออกจากไฟล์เพื่อไม่ให้คนเข้าใจผิด

## ข้อมูลที่ระบบเก็บซึ่งควรระวัง

| ที่เก็บ | มีอะไร | อยู่นานแค่ไหน |
|---|---|---|
| `user_sessions` | IP (ตอนนี้เป็นของเซิร์ฟเวอร์ Next.js) user agent เวลาใช้ล่าสุด | ไม่ถูกลบ แม้เพิกถอนหรือหมดอายุแล้ว |
| `idempotency_records` | SHA-256 ของ body และ **response เต็ม** แบบ `jsonb` ของคำขอสร้างนัด คิว การตรวจ ใบสั่งยา ใบแจ้งหนี้ การรับเงิน และคืนเงิน | `expires_at` 24 ชั่วโมง แต่ไม่มีงานลบ จึงอยู่ตลอดไป |
| `audit_entries` | ใครทำอะไร IP user agent และรายละเอียดเป็น JSON | ไม่ลบ ดู [Audit และความเป็นส่วนตัว](audit-and-privacy.md) |
| `phone_verifications` | เบอร์โทรที่ขอรหัส และ hash ของรหัส | ไม่ลบ แถวที่หมดอายุควรถูกลบด้วยงานเบื้องหลังที่ยังไม่มี |
| `inbox_notifications` | ข้อความทุกฉบับที่คลินิกส่งถึงลูกค้า | ไม่ลบ |
| ไฟล์ใน `Storage:LocalRoot` | รูปและเอกสารทางการแพทย์ที่อัปโหลด | ไม่ลบ ไม่ได้เข้ารหัสบนดิสก์ |
| log ของ API | ดูหัวข้อ log ใน [Audit และความเป็นส่วนตัว](audit-and-privacy.md#สิ่งที่อยู่ใน-log) | ขึ้นกับที่ติดตั้ง |

`idempotency_records` คือสำเนาข้อมูลการรักษาและการเงินอีกชุดที่ไม่มีใครดูแล ควรมีงานลบแถวที่เกิน `expires_at` ก่อนใช้งานจริง

## โค้ด

| ส่วน | ไฟล์ |
|---|---|
| hash รหัสผ่าน | `backend/src/HappyPaws.Infrastructure/Identity/IdentityPasswordHasher.cs` |
| TOTP | `backend/src/HappyPaws.Infrastructure/Identity/TotpService.cs` |
| กุญแจและการออก token | `backend/src/HappyPaws.Infrastructure/Identity/JwtOptions.cs`, `JwtAccessTokenIssuer.cs`, `backend/src/HappyPaws.Infrastructure/DependencyInjection.cs` |
| hash ของ token | `backend/src/HappyPaws.Application/Identity/Sessions/RefreshTokens.cs`, `backend/src/HappyPaws.Application/Staff/IStaffInvitationSender.cs` (`InvitationTokens`) |
| คอลัมน์ของบัญชีและคำเชิญ | `backend/src/HappyPaws.Infrastructure/Persistence/Configurations/Identity/UserAccountConfiguration.cs`, `Configurations/Staff/StaffInvitationConfiguration.cs` |
| ค่าตั้งค่า | `backend/src/HappyPaws.Api/appsettings.json`, `appsettings.Development.json`, `.env.example`, `docker-compose.yml` |
| idempotency | `backend/src/HappyPaws.Infrastructure/Persistence/Idempotency/` |
| รหัส OTP และลิงก์รีเซ็ต | `backend/src/HappyPaws.Application/Identity/CustomerSignIn.cs` (`OneTimeSecrets`), `backend/src/HappyPaws.Domain/Identity/PhoneVerification.cs`, `PasswordResetRequest.cs` |
| URL ของไฟล์ | `backend/src/HappyPaws.Infrastructure/Storage/LocalFileStorage.cs` |
