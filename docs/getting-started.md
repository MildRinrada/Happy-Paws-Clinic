# เริ่มต้นใช้งาน

ทำตามหน้านี้ทีละขั้น ครั้งแรกใช้เวลาราว 10 นาที จบแล้วจะได้หน้าแรกของคลินิกที่ http://localhost:3000 และเข้าสู่ระบบพนักงานได้

ระบบมีสามส่วน และต้องเปิดครบทั้งสามส่วนถึงจะใช้งานได้

| ส่วน | รันด้วย | ที่อยู่ |
|---|---|---|
| ฐานข้อมูล PostgreSQL | Docker | `localhost:5432` |
| API (ASP.NET Core) | `dotnet run` | http://localhost:5080/v1 |
| หน้าเว็บ (Next.js) | `npm run dev` | http://localhost:3000 |

ฐานข้อมูลรันอยู่เบื้องหลังใน Docker ส่วน API กับหน้าเว็บรันค้างไว้คนละหน้าต่าง Terminal จึงต้องเปิด Terminal สองหน้าต่าง ทุกคำสั่งในหน้านี้รันจากโฟลเดอร์บนสุดของโปรเจกต์ (โฟลเดอร์ที่มี `README.md`) ใช้ได้ทั้ง PowerShell, Git Bash และ Terminal บน Mac

## 0. ติดตั้งโปรแกรม

| โปรแกรม | รุ่น | ตรวจด้วย |
|---|---|---|
| .NET SDK | 10.0 | `dotnet --version` ต้องขึ้นต้นด้วย `10.` |
| Node.js | 22 | `node --version` ต้องขึ้นต้นด้วย `v22.` |
| Docker Desktop | รุ่นปัจจุบัน และต้องเปิดโปรแกรมค้างไว้ | `docker compose version` |

ไม่ต้องติดตั้ง PostgreSQL บนเครื่อง เพราะใช้ตัวที่อยู่ใน Docker

**ตรวจก่อนหนึ่งอย่าง** เครื่องมี PostgreSQL ติดตั้งอยู่แล้วหรือเปล่า บน PowerShell รัน

```powershell
Get-NetTCPConnection -LocalPort 5432 -State Listen -ErrorAction SilentlyContinue
```

ไม่มีอะไรขึ้นแปลว่าพอร์ตว่าง ทำตามขั้นต่อไปได้เลย ถ้ามีบรรทัดขึ้นมา แปลว่ามีโปรแกรมใช้พอร์ต 5432 อยู่ ให้ทำ [เมื่อพอร์ต 5432 ถูกใช้](#เมื่อพอร์ต-5432-ถูกใช้) ก่อน ไม่อย่างนั้น API จะไปต่อฐานข้อมูลผิดตัว

## 1. คัดลอกไฟล์ตั้งค่า

```sh
cp .env.example .env
cp frontend/.env.example frontend/.env.local
```

ไม่ต้องแก้อะไรในสองไฟล์นี้ ค่าตั้งต้นตรงกับที่ API ใช้ในโหมด Development อยู่แล้ว

## 2. เปิดฐานข้อมูล

```sh
docker compose up -d postgres
docker compose ps
```

รอจนคอลัมน์ `STATUS` ของ `postgres` ขึ้น `healthy` (ไม่กี่วินาที) ถ้าขึ้นว่าพอร์ต 5432 ถูกใช้อยู่ ดู [เมื่อพอร์ต 5432 ถูกใช้](#เมื่อพอร์ต-5432-ถูกใช้)

## 3. สร้างตารางและเจ้าของคลินิก (ครั้งแรกครั้งเดียว)

```sh
dotnet run --project backend/src/HappyPaws.Api -- migrate
dotnet run --project backend/src/HappyPaws.Api -- bootstrap --owner-email owner@example.test --owner-name "คุณเจ้าของ" --clinic-name "Happy Paws Clinic" --branch-code sukhumvit39 --branch-name "สาขาสุขุมวิท 39" --branch-rooms room_1,room_2
dotnet run --project backend/src/HappyPaws.Api -- seed-articles
dotnet run --project backend/src/HappyPaws.Api -- seed-staff
dotnet run --project backend/src/HappyPaws.Api -- seed-treatment-templates
dotnet run --project backend/src/HappyPaws.Api -- seed-contacts
```

- คำสั่งแรกใช้เวลานานหน่อยเพราะต้อง build ก่อน บรรทัดท้ายต้องเป็น `Applied N migration(s)` ถ้าเห็น `fail:` สองบรรทัดก่อนหน้านั้นไม่ต้องตกใจ เป็นเรื่องปกติของฐานข้อมูลว่าง
- `bootstrap` พิมพ์ **รหัสผ่าน** และ **รหัสลับ TOTP** ออกมาครั้งเดียว จดไว้ทั้งสองอย่าง แล้วเพิ่มรหัสลับในแอป authenticator บนมือถือ เช่น Google Authenticator หรือ Microsoft Authenticator (เลือกเพิ่มแบบพิมพ์รหัสเอง)
- `seed-articles` ใส่บทความตัวอย่าง `seed-staff` ใส่บุคลากรตัวอย่าง หน้าแรกและหน้าทีมของเราจึงมีข้อมูลแสดง `seed-treatment-templates` ใส่เทมเพลตแผนรักษาตัวอย่าง และ `seed-contacts` ใส่เบอร์โทร อีเมล แชท และแผนที่ตัวอย่างให้หน้าติดต่อเราและ footer ทั้งสี่ข้ามได้ถ้าไม่ต้องการ (ถ้าข้าม `seed-contacts` footer จะไม่มีข้อมูลติดต่อจนกว่าจะเพิ่มเองที่หน้าข้อมูลติดต่อ)

`--` ในคำสั่งคือตัวแบ่ง ส่วนที่อยู่หลัง `--` ถูกส่งให้โปรแกรมของเรา ไม่ใช่ให้ `dotnet`

## 4. รัน API (Terminal หน้าต่างที่หนึ่ง)

```sh
dotnet run --project backend/src/HappyPaws.Api
```

รอจนเห็นบรรทัด `Now listening on: http://localhost:5080` แล้วปล่อยหน้าต่างนี้ค้างไว้ กด `Ctrl+C` เมื่อต้องการปิด

ลองเปิด http://localhost:5080/v1/clinic ในเบราว์เซอร์ ต้องเห็นข้อมูลคลินิกเป็น JSON

- ที่อยู่ต้องมี `/v1` เสมอ เปิด http://localhost:5080/ เฉย ๆ จะได้ 401 ซึ่งเป็นเรื่องปกติ ไม่ใช่ API พัง
- คำสั่งนี้ใช้ profile `http` ใน `backend/src/HappyPaws.Api/Properties/launchSettings.json` ซึ่งตั้งพอร์ต 5080 และโหมด Development ให้ จึงไม่ต้องตั้งตัวแปรเพิ่ม

## 5. รันหน้าเว็บ (Terminal หน้าต่างที่สอง)

```sh
cd frontend
npm install
npm run dev
```

`npm install` ต้องรันแค่ครั้งแรก และทุกครั้งที่ `package.json` เปลี่ยน รอจนเห็น `Ready` แล้วเปิด http://localhost:3000

## 6. เข้าสู่ระบบพนักงาน

เปิด http://localhost:3000/login ใส่อีเมล `owner@example.test` กับรหัสผ่านจากขั้นที่ 3 แล้วใส่รหัส 6 หลักจากแอป authenticator เข้าได้แล้วระบบพาไปหน้าคิว

ลืมรหัสผ่านหรือทำแอปหาย ใช้คำสั่งนี้ออกรหัสใหม่

```sh
dotnet run --project backend/src/HappyPaws.Api -- reset-credentials --email owner@example.test
```

## ครั้งต่อไป

ไม่ต้องทำขั้น 1 และ 3 ซ้ำ เปิด Docker Desktop แล้ว

```sh
# ฐานข้อมูล
docker compose up -d postgres

# Terminal ที่หนึ่ง
dotnet run --project backend/src/HappyPaws.Api

# Terminal ที่สอง
cd frontend
npm run dev
```

เมื่อดึงโค้ดใหม่ที่มี migration เพิ่ม ให้รัน `migrate` อีกครั้งก่อนเปิด API คำสั่งนี้รันซ้ำได้ ถ้าไม่มีอะไรใหม่จะพิมพ์ `Database is up to date.`

## รันจาก IDE

- **Visual Studio หรือ Rider** เปิด `backend/HappyPaws.slnx` ตั้ง `HappyPaws.Api` เป็น startup project เลือก profile `http` แล้วกดรัน
- **VS Code** ติดตั้งส่วนเสริม C# Dev Kit เปิดโฟลเดอร์โปรเจกต์ แล้วรัน `HappyPaws.Api` จากแถบ Solution Explorer หรือใช้คำสั่ง `dotnet run` ใน Terminal ของ VS Code เหมือนขั้นที่ 4
- คำสั่ง `migrate`, `bootstrap` และคำสั่ง `seed-*` รันจาก Terminal ง่ายที่สุด ถ้าจะรันจาก IDE ให้ใส่ชื่อคำสั่งเป็น command line arguments ของ profile

## เมื่อพอร์ต 5432 ถูกใช้

เกิดเมื่อเครื่องมี PostgreSQL ติดตั้งอยู่แล้ว ให้ Docker ใช้พอร์ตอื่น แล้วบอก API ว่าย้ายไปพอร์ตไหน

1. แก้ `POSTGRES_PORT=5433` ใน `.env` แล้วรัน `docker compose up -d postgres` ใหม่
2. ตั้ง connection string ใน Terminal **ทุกหน้าต่างที่รัน `dotnet`** ก่อนรันคำสั่งในขั้น 3 และ 4

```powershell
# PowerShell
$env:ConnectionStrings__Postgres = "Host=localhost;Port=5433;Database=happypaws;Username=happypaws;Password=happypaws"
```

```sh
# Git Bash หรือ Mac
export ConnectionStrings__Postgres="Host=localhost;Port=5433;Database=happypaws;Username=happypaws;Password=happypaws"
```

ไม่อยากตั้งทุกครั้ง ให้เก็บไว้ใน user secrets ครั้งเดียว

```sh
dotnet user-secrets --project backend/src/HappyPaws.Api set "ConnectionStrings:Postgres" "Host=localhost;Port=5433;Database=happypaws;Username=happypaws;Password=happypaws"
```

## เริ่มใหม่ทั้งหมด

ลบข้อมูลทุกอย่างในฐานข้อมูลแล้วกลับไปขั้นที่ 2

```sh
docker compose down -v
```

ใช้เมื่อทดลองจนข้อมูลเละ หรือเมื่อเปลี่ยนรหัสผ่านฐานข้อมูลใน `.env` หลังจากสร้างฐานข้อมูลไปแล้ว (PostgreSQL อ่านรหัสผ่านตอนสร้างครั้งแรกเท่านั้น)

## อาการที่เจอบ่อย

| อาการ | ทางแก้ |
|---|---|
| `password authentication failed for user "happypaws"` | `.env` เคยตั้งรหัสอื่นไว้ตอนสร้างฐานข้อมูล แก้ `POSTGRES_PASSWORD=happypaws` แล้ว `docker compose down -v` และเริ่มจากขั้นที่ 2 |
| `Failed to connect to 127.0.0.1:5432` | ฐานข้อมูลยังไม่เปิด หรือ Docker Desktop ปิดอยู่ รันขั้นที่ 2 |
| API ล้มตอนเปิด มี `Failed to bind to address http://127.0.0.1:5080` และ `address already in use` | มี API อีกตัวเปิดอยู่แล้ว เช่น Terminal อีกหน้าต่าง หรือ IDE ที่กดรันค้างไว้ ปิดตัวนั้นด้วย `Ctrl+C` ก่อน ถ้าหาไม่เจอ บน PowerShell ใช้ `Get-Process -Id (Get-NetTCPConnection -LocalPort 5080 -State Listen).OwningProcess` ดูว่าโปรแกรมไหนถือพอร์ตอยู่ |
| `docker compose up` ขึ้นว่าพอร์ต 5432 ถูกใช้ หรือ API เข้าสู่ระบบไม่ได้ทั้งที่ทำครบทุกขั้น | เครื่องมี PostgreSQL ติดตั้งอยู่แล้ว API จึงไปต่อฐานข้อมูลตัวนั้นแทนตัวใน Docker ทำตาม [เมื่อพอร์ต 5432 ถูกใช้](#เมื่อพอร์ต-5432-ถูกใช้) |
| API ไม่ยอมเปิด แจ้งเรื่อง `JwtOptions` | API ไม่ได้อยู่ในโหมด Development เช่นรันด้วย `--no-launch-profile` ให้รันตามขั้นที่ 4 |
| หน้าเว็บขึ้น `Missing environment variable: API_BASE_URL` | ไม่มี `frontend/.env.local` ทำขั้นที่ 1 |
| หน้าแรกขึ้นว่ายังโหลดบทความไม่ได้ | API ไม่ได้เปิดอยู่ ดูหน้าต่างของขั้นที่ 4 |
| รหัส 6 หลักไม่ผ่าน | รหัสในแอปเปลี่ยนทุก 30 วินาที ใส่รหัสล่าสุด และตรวจว่าเวลาบนคอมกับมือถือตรงกัน |

อาการอื่นและรายละเอียดเชิงลึก เช่น ตัวแปรแวดล้อมทั้งหมด การทดสอบ และการรันทั้งหมดใน Docker อยู่ใน [การพัฒนา](development.md)
