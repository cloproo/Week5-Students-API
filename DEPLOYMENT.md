# النشر الحي على الإنترنت (Render Docker + PostgreSQL خارجي)

## النتيجة المطلوبة

`https://<actual-hostname>/swagger` يفتح من أي متصفح، و`https://<actual-hostname>/health` يعيد `200` مع اتصال قاعدة البيانات. اسم المضيف يُحدّد **بعد النشر الحقيقي** ولا يمكن تخمينه مسبقًا.

## المتطلبات والحسابات

- حساب GitHub لرفع **ملفات المشروع** (دون `.env` أو كلمة مرور قاعدة البيانات).
- حساب منصة استضافة ASP.NET Core تدعم Docker (مثال: Render).
- قاعدة PostgreSQL **مستمرة/دائمة** (مثال: Supabase أو مزود PostgreSQL مناسب).
- ينبغي مراجعة **الأسعار والحدود الحالية** في مزود الخدمة قبل الاختيار؛ دعم الخطط المجانية قد يتغير، ولم يتم التحقق منه عبر الإنترنت في بيئة إعداد هذا المشروع.

## الخطوة 1: إنشاء PostgreSQL

1. أنشئ مشروع قاعدة بيانات في مزوّد PostgreSQL.
2. احصل على قيم **Host / Port / Database / Username / Password** من لوحة قاعدة البيانات؛ لا تخمّنها.
3. كوّن connection string بصيغة Npgsql:

```text
Host=ACTUAL_HOST;Port=5432;Database=ACTUAL_DB;Username=ACTUAL_USER;Password=ACTUAL_PASSWORD;SSL Mode=Require
```

> البورت `5432` مثال شائع، استخدم قيمة المزود الحقيقية. لا تستخدم رابط `postgresql://...` كما هو داخل `ConnectionStrings__Postgres`؛ هذا التطبيق ينتظر صيغة Npgsql ذات مفاتيح `Host=...;...`.

## الخطوة 2: رفع المشروع إلى GitHub

- أنشئ مستودعًا جديدًا (يمكن أن يكون Private إذا كانت منصة الاستضافة تستطيع الوصول إليه).
- اجعل `Dockerfile` و`compose.yaml` و`StudentApi/` في **جذر المستودع**.
- لا ترفع `.env`، `students.db`، أو أي Secrets.

```bash
git init
git add .
git commit -m "Week 5: JWT Students CRUD ASP.NET Core API"
git branch -M main
git remote add origin YOUR_REAL_GITHUB_REPOSITORY_URL
git push -u origin main
```

## الخطوة 3: نشر خدمة Docker

مثال لمسار عمل شائع في منصة Render (قد تختلف أسماء الأزرار حسب إصدار الواجهة):

1. اختر خدمة Web Service جديدة مرتبطة بمستودع GitHub.
2. حدّد بيئة بناء **Docker** (يتعرف على `Dockerfile` في جذر المستودع).
3. اجعل إعداد المنفذ `8080` إذا طلبت المنصة تحديده؛ الحاوية تستمع إليه عبر `ASPNETCORE_HTTP_PORTS=8080`.
4. ضع متغيرات البيئة **أسرارًا في إعدادات الخادم لا في المستودع**:

| اسم المتغير | القيمة المطلوبة |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Jwt__Key` | مفتاح عشوائي قوي من 32 بايت على الأقل (يفضل 48 بايت عشوائية ثم Base64) |
| `ConnectionStrings__Postgres` | سلسلة اتصال PostgreSQL الحقيقية بصيغة Npgsql |
| `Jwt__Issuer` | `Week5StudentApi` (افتراضي، يمكن تغييره إذا رغبت) |
| `Jwt__Audience` | `Week5StudentApiClient` (افتراضي، يمكن تغييره إذا رغبت) |
| `Jwt__ExpiryMinutes` | `60` (افتراضي) |

يمكن الاكتفاء بثلاثة متغيرات إلزامية هي `ASPNETCORE_ENVIRONMENT`، `Jwt__Key` و`ConnectionStrings__Postgres` لأن Issuer/Audience/Expiry موجودة في `appsettings.json`.

5. ابدأ النشر وراجع سجل البناء/التشغيل: يجب أن يستعيد NuGet packages، يبني المشروع، ويستطيع الاتصال بقاعدة البيانات. عند أول بدء ينشئ `EnsureCreatedAsync()` الجداول.

## الخطوة 4: تحقق على رابط HTTPS العام

استبدل `<actual-hostname>` بقيمة الرابط الفعلي من المنصة:

1. افتح `https://<actual-hostname>/health` وتأكد من `200` و`"database":"connected"`.
2. افتح `https://<actual-hostname>/swagger` وتأكد من عرض واجهة Swagger والقفل `Authorize`.
3. نفذ `POST /api/auth/register` ثم `login` وأدخل التوكن في **Authorize**.
4. جرّب كامل CRUD من Swagger.
5. نفذ من جهازك `python scripts/smoke_test.py --base https://<actual-hostname>` للتحقق من جميع الحالات.
6. صوّر صفحة Swagger ونتائج الطلبات من الخدمة العامة؛ أخفِ كلمات المرور ورموز JWT في الصور قبل تسليمها.

## إذا فشل النشر

- `Jwt__Key must contain at least 32 UTF-8 bytes`: تأكد أن المتغير مضبوط على الخادم.
- `Production deployment requires ConnectionStrings__Postgres`: لم تُضبط قاعدة PostgreSQL.
- فشل الاتصال بقاعدة البيانات: افحص المضيف والمنفذ والمستخدم وكلمة المرور وSSL وجدار الحماية وتوافر عنوان IPv4/مجمّع اتصالات عند المزود.
- `401 Unauthorized`: استخدم توكنًا حديثًا وادخله في Swagger Authorize؛ تحقق من Issuer/Audience/secret.
- `429 Too Many Requests`: تجاوزت الحد المؤقت لمصادقة الدخول والتسجيل؛ انتظر انتهاء نافذة الدقيقة قبل إعادة التجربة.
- صفحة Swagger لا تظهر: افحص مسار `/swagger` وسجل بدء خدمة Docker.

## حقول التسليم التي يجب تعبئتها بعد النشر

- **Live API base URL:** `_______________________________`
- **Live Swagger URL:** `_______________________________/swagger`
- **Live health URL:** `_______________________________/health`
- **Hosting provider:** `_______________________________`
- **Database provider:** `_______________________________`
- **GitHub repository URL (إن كان مسموحًا):** `_______________________________`

> لا تضع أي رابط افتراضي على أنه الرابط الحقيقي. لا تعتبر الاستضافة مكتملة حتى يُختبر الرابط العام بنجاح.
