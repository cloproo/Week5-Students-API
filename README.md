# واجب الأسبوع الخامس | ASP.NET Core Web API + JWT + Students CRUD

**مشروع كامل المصدر** باستخدام C# وASP.NET Core (.NET 10)، مع قواعد بيانات فعلية، Swagger UI، مصادقة JWT، وعمليات CRUD. **لا يحتوي المشروع على أي كود Flutter.**


## Live Deployment

**Swagger UI (Live API):**
https://week5-students-api.onrender.com/swagger/index.html

**Health Check:**
https://week5-students-api.onrender.com/health

**GitHub Repository:**
https://github.com/cloproo/Week5-Students-API


## 1) المكونات

- `StudentApi/Controllers/AuthController.cs`: التسجيل وتسجيل الدخول والاستعلام `me`.
- `StudentApi/Controllers/StudentsController.cs`: عمليات الطلاب مع البحث والفلترة والصفحات.
- `StudentApi/Data/AppDbContext.cs`: نموذج قاعدة البيانات بواسطة Entity Framework Core.
- `StudentApi/Models/`: الجداول `Users` و`Students`.
- `StudentApi/Dtos/`: نماذج الإدخال والنتائج والتحقق `[ApiController]` + Data Annotations.
- `StudentApi/Services/JwtTokenService.cs`: إنشاء رمز JWT بتوقيع HMAC-SHA256 وانتهاء محدد.
- `StudentApi/Program.cs`: إعدادات قاعدة البيانات وSwagger والقفل Bearer JWT والحماية.
- `StudentApi/Swagger/JwtAuthorizeOperationFilter.cs`: إظهار القفل على المسارات المحمية فقط دون التسجيل والدخول.
- `.github/workflows/ci.yml`: بناء CI وإجراء اختبارات HTTP تلقائية عند رفع GitHub.
- `Dockerfile`: صورة Docker لرفع API إلى منصة تدعم Docker.
- `compose.yaml`: تشغيل API + PostgreSQL محليًا (قاعدة البيانات محفوظة في Volume).
- `scripts/smoke_test.py`: اختبار سلوك المسارات وحالات الاستجابة تلقائيًا ضد خادم *مشغّل*.
- `postman/Week5_Students_API.postman_collection.json`: مجموعة الطلبات الجاهزة في Postman.
- `DEPLOYMENT.md`: خطوات النشر على الإنترنت.
- `SUBMISSION.md`: تقرير التسليم ومطابقة المتطلبات.

## 2) جميع المسارات

| HTTP | المسار | JWT | حالة النجاح | حالات إضافية |
|---|---|---|---|---|
| POST | `/api/auth/register` | لا | `201 Created` | `400`, `409` |
| POST | `/api/auth/login` | لا | `200 OK` | `400`, `401` |
| GET | `/api/auth/me` | **نعم** | `200 OK` | `401` |
| GET | `/api/students` | **نعم** | `200 OK` | `400`, `401` |
| GET | `/api/students/{id}` | **نعم** | `200 OK` | `401`, `404` |
| POST | `/api/students` | **نعم** | `201 Created` + `Location` | `400`, `401`, `409` |
| PUT | `/api/students/{id}` | **نعم** | `200 OK` | `400`, `401`, `404`, `409` |
| DELETE | `/api/students/{id}` | **نعم** | `204 No Content` | `401`, `404` |
| GET | `/health` | لا | `200 OK` إذا اتصل بقاعدة البيانات | `503` إن فشل الاتصال |

## 3) التشغيل المحلي: .NET SDK 10 + SQLite

تحتاج **.NET 10 SDK**؛ يعمل على Windows وLinux وmacOS.

### Windows PowerShell (داخل المجلد الأساسي للمشروع)

```powershell
# توليد مفتاح عشوائي آمن للنسخة المحلية — لا تضع المفتاح في Git
$bytes = New-Object byte[] 48
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$env:Jwt__Key = [Convert]::ToBase64String($bytes)

# تشغيل بيئة التطوير المحلية
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet restore .\StudentApi\StudentApi.csproj
dotnet run --project .\StudentApi\StudentApi.csproj --launch-profile http
```

**روابط محلية:**
- Swagger: `http://localhost:5080/swagger`
- Health check: `http://localhost:5080/health`

تنشأ قاعدة بيانات SQLite تلقائيًا عند أول تشغيل (`students.db` في مجلد التشغيل). المستخدمون والطلاب محفوظون في قاعدة البيانات **وليست قائمة داخل الذاكرة**.

### Linux / macOS

```bash
export Jwt__Key="$(python3 -c 'import secrets; print(secrets.token_urlsafe(48))')"
export ASPNETCORE_ENVIRONMENT=Development
dotnet restore StudentApi/StudentApi.csproj
dotnet run --project StudentApi/StudentApi.csproj --launch-profile http
```

## 4) التشغيل المحلي: Docker Compose + PostgreSQL

ثبّت Docker وDocker Compose، ثم:

1. انسخ `.env.example` إلى `.env`.
2. غيّر `JWT_SECRET` و`POSTGRES_PASSWORD` إلى قيمتين **قويتين ومختلفتين**.
3. نفّذ:

```bash
docker compose up --build -d
docker compose ps
```

صفحة Swagger: `http://localhost:5080/swagger`. تبقى قاعدة PostgreSQL في volume باسم `pgdata` بعد إعادة تشغيل الحاويات.

> ملاحظة: تشغيل SQLite يعمل تلقائيًا محليًا عندما لا يُحدد PostgreSQL. في بيئة `Production` يتطلب التطبيق اتصال PostgreSQL دائمًا؛ وذلك حتى لا تُنشأ قاعدة بيانات مؤقتة تتلف بياناتها عند إعادة نشر الخادم.

## 5) تجربة Swagger خطوة بخطوة

### (أ) التسجيل — `POST /api/auth/register`

```json
{
  "fullName": "Ahmed Ali",
  "email": "ahmed.ali@example.com",
  "password": "StrongP@ssw0rd2026"
}
```

- النتيجة المتوقعة `201` وتحتوي على المستخدم **دون PasswordHash**.
- تكرار البريد (حتى باختلاف الأحرف الكبيرة) يعيد `409 Conflict`.

### (ب) تسجيل الدخول — `POST /api/auth/login`

```json
{
  "email": "ahmed.ali@example.com",
  "password": "StrongP@ssw0rd2026"
}
```

- النتيجة `200` تحتوي `token` و`expiresAtUtc` و`user`.
- في Swagger اضغط **Authorize** (أيقونة القفل) والصق قيمة `token` **فقط**. تعريف Swagger هو Bearer HTTP scheme ويضيف كلمة `Bearer` تلقائيًا.
- تحقق من `GET /api/auth/me`؛ يعرض هوية المستخدم من Claims داخل JWT المتحقق من توقيعه وصلاحيته.

### (ج) إضافة طالب — `POST /api/students`

```json
{
  "fullName": "Sara Mohammed",
  "email": "sara.m@example.com",
  "age": 21,
  "department": "Computer Science"
}
```

- النتيجة `201 Created` مع ترويسة `Location: .../api/students/1` **(الرقم يعتمد على قاعدة البيانات)**.
- جرّب `GET /api/students/1` مع استبدال 1 بالـ`id` الحقيقي، ثم `PUT` و`DELETE`.

### (د) البحث والفلترة والتقسيم

```http
GET /api/students?search=sara&department=Computer%20Science&minAge=18&maxAge=30&page=1&pageSize=5
```

الحقول في الاستجابة: `items`، `page`، `pageSize`، `totalCount`، `totalPages`.

### (هـ) تعديل طالب — `PUT /api/students/{id}`

```json
{
  "fullName": "Sara Mohammed Updated",
  "email": "sara.updated@example.com",
  "age": 22,
  "department": "Cybersecurity"
}
```

- `200` عند نجاح التعديل، `404` إذا لم يوجد الطالب، `409` إذا كان البريد مستخدمًا لطالب آخر.
- الحذف `DELETE` يعيد `204` بدون جسم؛ الطلب مجددًا لنفس المعرّف يعيد `404`.

## 6) التحقق Validation والحماية

- النماذج منفصلة: `RegisterDto`, `LoginDto`, `CreateStudentDto`, `UpdateStudentDto`, `StudentsQueryDto`.
- بسبب `[ApiController]` وData Annotations، أخطاء الإدخال مثل البريد غير الصحيح أو العمر خارج `15..110` تُعيد `400 Bad Request` تلقائيًا.
- `User` مخزّن مع **PasswordHash** ناتج من `PasswordHasher<AppUser>`، ولا يُعاد الهاش في أي استجابة عامة.
- JWT يوقَّع بخوارزمية `HS256` وتُفحص `Issuer`, `Audience`, `Signature`, `Lifetime` مع `ClockSkew=0`.
- كل مسارات الطلاب و`/api/auth/me` محمية بـ`[Authorize]`، أما التسجيل والدخول فمفتوحتان.
- البريد فريد بفهرس **Unique Index** في الجدولين، ويُطبّع إلى lowercase لعدم تكرار نفس البريد باختلاف الحروف.
- يوجد حدّ معدل أساسي لطلبات `register` و`login`: 10 طلبات في الدقيقة حسب سياسة limiter المشتركة. هذه إضافة مساعدة وليست منظومة متقدمة لمكافحة المحاولات الموزعة.

## 7) اختبار آلي بعد تشغيل الخادم

```bash
python scripts/smoke_test.py --base http://localhost:5080
```

يفحص الاختبار التسجيل، التكرار، كلمة مرور خاطئة، JWT، رفض الطلب بلا JWT، إضافة طالب، `Location`، البحث والفلترة والصفحات، تعديل الطالب، النزاعات، التحقق `400`، الحذف و`404`.

## 8) الاستضافة المباشرة

راجع **[DEPLOYMENT.md](DEPLOYMENT.md)**. لا يمكن اعتبار التكليف منشورًا ما لم تظهر صفحة Swagger من رابط HTTPS **فعلي** بعد ربط GitHub ومنصة استضافة وقاعدة بيانات PostgreSQL وتجربة الطلبات على الخادم.

**مهم:** تم إعداد المشروع للتشغيل والنشر، لكن إنشاء حساب سحابي ونشر الخدمة وتأكيد الرابط العام يحتاجان وصولًا مصرحًا به إلى حسابات الاستضافة. ملفات المشروع نفسها لا تمثل رابطًا مستضافًا.

## 9) ملاحظة هندسية عن إدارة المخطط

يُنشئ `EnsureCreatedAsync()` مخطط قاعدة البيانات لأول مرة تلقائيًا؛ وهو مناسب لتسليم هذا المشروع الدراسي المستقل. **لا يوفّر ترقيات مخطط إنتاجية أو سجل EF migrations**. في الأنظمة طويلة المدى يجب اعتماد EF Core migrations قبل تغيير الجداول بعد النشر؛ لا تخلط `EnsureCreated` مع migration history على قاعدة بيانات موجودة.
