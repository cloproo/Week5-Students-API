# Week 5 API — QA / Submission Readiness Review

**Review date:** 2026-11-21

## Verdict

- **Source-code requirement coverage:** Present in reviewed source (static inspection).
- **Static verification:** PASS (`python3 scripts/validate_structure.py`), JSON/XML parse PASS, Python smoke-test source syntax PASS.
- **Compiles on .NET 10:** NOT VERIFIED here: no .NET SDK available in this environment.
- **Automated HTTP smoke tests:** NOT EXECUTED: API not running.
- **Docker build:** NOT EXECUTED: Docker is not available in this environment.
- **Live cloud hosting:** NOT DONE: requires authorization/access to hosting provider, persistent database and real public URL.
- **Final assignment submission:** NOT YET COMPLETE until host and runtime checks pass.

## Assignment-to-source mapping

| Requirement | Source | Static assessment |
| --- | --- | --- |
| POST `/api/auth/register`, hash, 409 | `AuthController.Register`, `AppDbContext.Users` unique index | Present |
| POST `/api/auth/login`, 200, JWT, expiry, user | `AuthController.Login`, `JwtTokenService` | Present |
| GET `/api/auth/me`, JWT claims | `AuthController.Me` | Present |
| GET `/api/students` search / filters / page / pageSize | `StudentsController.GetAll`, `StudentsQueryDto` | Present |
| GET `/api/students/{id}` 200 / 404 | `StudentsController.GetById` | Present |
| POST `/api/students` CreateStudentDto, automatic validation, 201 / Location | `StudentsController.Create`, `[ApiController]` | Present |
| PUT `/api/students/{id}` UpdateStudentDto, 200/404/409 | `StudentsController.Update` | Present |
| DELETE `/api/students/{id}` 204 | `StudentsController.Delete` | Present |
| `[Authorize]`, Swagger Bearer JWT | `Program.cs`, controller attributes, Swagger filter | Present |
| Relational database | `AppDbContext`, EF Core Sqlite/Npgsql | Configured, runtime not yet verified |
| Public live Swagger / online database | `Dockerfile`, `DEPLOYMENT.md` | Infrastructure files exist; NOT DEPLOYED |

## Next mandatory acceptance checks (in order)

1. In a system with .NET 10 SDK installed, set `Jwt__Key` using a private random secret of at least 32 bytes; run:

   ```bash
   dotnet restore StudentApi/StudentApi.csproj
   dotnet build StudentApi/StudentApi.csproj -c Release
   dotnet run --project StudentApi/StudentApi.csproj --launch-profile http
   ```

   Check `http://localhost:5080/swagger` and `http://localhost:5080/health`.

2. With API running, run:

   ```bash
   python3 scripts/smoke_test.py --base http://localhost:5080
   ```

   This test asserts register/login/duplicate 409, invalid inputs 400, JWT protection 401, successful CRUD 201/200/204, Location, search/filter/pagination, and 404/409.

3. Push to GitHub and confirm `.github/workflows/ci.yml` passes on **GitHub Actions** (source build + live HTTP smoke test). CI passing is not proof of cloud deployment.
4. Connect a hosting account, provision a persistent PostgreSQL database, configure production secret and Npgsql connection string, and deploy Docker as described in `DEPLOYMENT.md`.
5. Verify the **real, public HTTPS** `/health` and `/swagger` URLs and repeat the smoke test against the public base URL. Add actual URL and screenshots to `SUBMISSION.md`; protect JWT and passwords.

## Caveats

- Never label the hosting requirement "done" without a working public URL.
- `EnsureCreatedAsync()` bootstraps a new database for academic use; it is not a migrations-based upgrade strategy.
- Hosting providers' current free-plan terms and .NET support were not verified through live web search in this environment.
- No Flutter code is included.
