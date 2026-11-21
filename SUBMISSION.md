# نموذج تقرير تسليم واجب الأسبوع الخامس

**عنوان المشروع:** Students Management Web API with JWT Authentication & CRUD

**التقنيات:** ASP.NET Core Web API (.NET 10)، C#، EF Core، SQLite للتطوير المحلي، PostgreSQL للاستضافة، JWT Bearer، Swagger UI، Docker.

## ملخص التنفيذ

تم إعداد تطبيق Web API لإدارة حسابات المستخدمين والطلاب. يستخدم تجزئة كلمات المرور عبر `PasswordHasher<AppUser>`، ولا يخزن كلمة مرور صريحة. تُصدر خدمة الدخول رمز JWT موقّعًا بـHS256، ويعيد مسار `me` Claims من الرمز المعتمد دون استعلام قاعدة البيانات. تعتمد عمليات الطلاب على Entity Framework Core وقاعدة بيانات علائقية، مع فهرس فريد للبريد وتقسيم صفحات حقيقي في استعلام قاعدة البيانات. جميع مسارات الطلاب محمية بواسطة `[Authorize]`.

## جدول المطابقة مع معايير الدكتور

| بند المطلوب | التنفيذ في المشروع | حالة الإنجاز |
|---|---|---|
| `POST /api/auth/register` | `AuthController.Register`، تجزئة ورفض تكرار البريد `409` | **منفذ في الكود** |
| `POST /api/auth/login` | `AuthController.Login`، التحقق وإرجاع JWT+الانتهاء+المستخدم | **منفذ في الكود** |
| `GET /api/auth/me` | قراءة JWT claims مباشرة بعد `[Authorize]` | **منفذ في الكود** |
| `GET /api/students` | بحث + Department + minAge/maxAge + page/pageSize | **منفذ في الكود** |
| `GET /api/students/{id}` | `200` أو `404` | **منفذ في الكود** |
| `POST /api/students` | `CreateStudentDto` + Validation + `201 CreatedAtAction` وLocation | **منفذ في الكود** |
| `PUT /api/students/{id}` | `UpdateStudentDto` + `200/404/409` | **منفذ في الكود** |
| `DELETE /api/students/{id}` | `204` أو `404` | **منفذ في الكود** |
| تأمين endpoints | `[Authorize]` + JWT signature/lifetime validation | **منفذ في الكود** |
| Swagger Authorize Bearer | `AddSecurityDefinition` + `JwtAuthorizeOperationFilter` (قفل على المسارات المحمية فقط) | **منفذ في الكود** |
| Live Hosting + Live Database | Dockerfile + PostgreSQL config + `DEPLOYMENT.md` | **مُهيأ، يحتاج نشرًا فعليًا واختبارًا** |

## الفحوصات المطلوبة قبل التسليم

- [ ] تنفيذ `dotnet restore` و`dotnet run` بنجاح على جهاز يحتوي .NET 10 SDK.
- [ ] فتح `swagger` والتأكد من ظهور زر Authorize.
- [ ] تسجيل مستخدم واختبار رفض تكرار البريد.
- [ ] تسجيل الدخول والتحقق من رمز JWT ووقت انتهائه.
- [ ] التحقق من رفض `GET /api/auth/me` وعمليات الطلاب بلا JWT.
- [ ] إنشاء طالب والحصول على `201` وLocation header.
- [ ] عرض طالب موجود وغير موجود، ثم التعديل والحذف والتحقق من `200/404/409/204`.
- [ ] تنفيذ البحث والفلترة وتقسيم الصفحات والتحقق من إجمالي النتائج.
- [ ] نشر Docker API مع PostgreSQL دائم واختبار Swagger عبر **رابط HTTPS حقيقي**.
- [ ] إرفاق الرابط الحقيقي ولقطات اختبار موثوقة مع إخفاء JWT وأسرار الحسابات.

> **تنبيه نزاهة التسليم:** البنود الموجودة في الكود لا تعني تلقائيًا أنها اجتازت اختبار التشغيل. لا تضف علامة إنجاز لشرط الاستضافة حتى تعمل الخدمة على الإنترنت فعليًا.

## رابط المشروع الحي (يُملأ بعد النشر)

- Public API: `________________________`
- Swagger UI: `________________________/swagger`
- Health endpoint: `________________________/health`
