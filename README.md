# Level Movement

Vehicle management demo using .NET 10, SQL Server, React and TypeScript.
The normal development setup runs **SQL Server in Docker**, the **API locally**
in Visual Studio or a terminal, and the **frontend locally** with Vite.

## Prerequisites

| Tool | Purpose |
| --- | --- |
| .NET 10 SDK | Build and run the API and backend tests |
| Docker Desktop with Linux containers | Host the SQL Server database |
| Node.js 24.15 or later in the 24.x line, with npm | Run the frontend and current test dependencies |
| PowerShell on Windows | Run the HTTPS setup script and commands below |
| Visual Studio with .NET 10 support (optional) | Run and debug the API with F5 |
| SQL Server Management Studio (optional) | Inspect data and run the cleanup script |

Run the following commands from the repository root unless a section says otherwise.

## First-time setup

### 1. Configure and start SQL Server

Copy the environment template only if you do not already have a local file:

```powershell
if (!(Test-Path backend/.env)) {
    Copy-Item backend/.env.example backend/.env
}
```

Edit `backend/.env` and set `MSSQL_SA_PASSWORD` to your development SQL password.
Keep an existing working password when reusing the database volume.

```powershell
docker compose --env-file backend/.env -f backend/compose.yaml up -d sqlserver
docker compose --env-file backend/.env -f backend/compose.yaml ps sqlserver
```

Wait until SQL Server reports healthy. This command starts the database service;
the API runs locally in the following steps. Compose also contains optional
API/migration services, so specify `sqlserver` for this development workflow.

The database files persist in the `sqlserver-data` Docker volume. Editing
`MSSQL_SA_PASSWORD` in the environment file does not change an existing SQL login.

### 2. Prepare HTTPS and restore the backend

```powershell
./backend/scripts/Setup-LocalHttps.ps1
dotnet restore backend/LevelMovement.slnx
```

Accept the Windows certificate trust prompt. The script prepares the localhost
certificate used by the frontend; .NET uses the trusted development certificate
for the local API. Certificate files and passwords are ignored by Git.

### 3. Configure the local API connection

Skip this step if the project already has a working connection in user secrets.
Replace `YOUR_CURRENT_SQL_PASSWORD` with the actual SQL login password:

```powershell
dotnet user-secrets set "ConnectionStrings:LevelMovement" 'Server=tcp:127.0.0.1,14330;Database=LevelMovement;User Id=sa;Password="YOUR_CURRENT_SQL_PASSWORD";Encrypt=True;TrustServerCertificate=True;' --project backend/src/LevelMovement.Api
```

The API does not automatically read Docker Compose's `backend/.env`. User secrets
supply the password for the locally launched API without committing it to source.

For SSMS, use server `localhost,14330`, SQL Server Authentication, login `sa`,
the current SQL password, and trust the server certificate for this local database.

### 4. Create or update the database

```powershell
dotnet run --project backend/src/LevelMovement.Api --no-launch-profile -- --migrate --environment Development
```

This applies the existing EF Core migrations and exits. Normal API startup does
not apply migrations automatically.

For an older database, the registration-normalization migration removes
whitespace and uppercases registrations, including deleted rows. It stops if that
would create duplicates, so resolve conflicting registrations before retrying.
Changed registrations receive fresh concurrency stamps. Rolling back does not
reconstruct the original spacing.

### 5. Seed the demo fleet

```powershell
dotnet run --project backend/src/LevelMovement.Api --no-launch-profile -- --seed-demo --environment Development
```

The command applies migrations, adds missing demo vehicles, and exits. Seeding is
allowed only in Development. The catalog contains 50 registrations,
`DEMO001GP` through `DEMO050GP`, with explicit model years and positive daily rates.
The Fronx entry uses year 2023.

Rerunning preserves existing edits, dates, and deleted vehicles. It skips
registrations that already exist, including soft-deleted ones. It does not reset
the database or restore deleted vehicles. New records use `Vehicle.Register`
and receive their concurrency stamps when saved.

## API-key security

Normal API startup requires a non-blank key, including in Development. Configure
it in user secrets before running with F5 or from a terminal. Replace the example
value with your own private development key:

```powershell
dotnet user-secrets set "Security:ApiKey" "YOUR_DEVELOPMENT_API_KEY" --project backend/src/LevelMovement.Api
```

Alternatively, set `Security__ApiKey` in the environment of the process that runs
the API. All vehicle and booking endpoints require exactly one `X-Api-Key` header
containing that value. Key values are case-sensitive. Missing, empty, incorrect
or duplicate headers return HTTP 401 Problem Details before application work.
The API does not log key values.

Development Swagger remains accessible without a request key. Open Swagger,
click **Authorize**, enter the configured key without a `Bearer` prefix, and
authorize before trying API operations. For a direct PowerShell request:

```powershell
$headers = @{ 'X-Api-Key' = 'YOUR_DEVELOPMENT_API_KEY' }
Invoke-RestMethod 'https://localhost:63887/api/vehicles?pageNumber=1&pageSize=10' -Headers $headers
```

Missing or blank key configuration stops the API with an explanation of the
required setting. The `--migrate` and `--seed-demo` commands do not require an API
key. For the optional Docker API service, set `Security__ApiKey` in `backend/.env`;
Compose passes it to that service. SQL-only Docker startup does not require it.

## Run the app

### API: Visual Studio or a terminal

Open `backend/LevelMovement.slnx`, select `LevelMovement.Api` as the startup
project, choose its `LevelMovement.Api` launch profile, and press F5.

Alternatively:

```powershell
dotnet run --project backend/src/LevelMovement.Api --launch-profile LevelMovement.Api
```

Keep this process running. Swagger is at
[https://localhost:63887/swagger](https://localhost:63887/swagger).
Use its **Authorize** button as described above for the backend demo.

### Frontend: a second terminal

```powershell
cd frontend
npm ci
npm run dev
```

Open [https://localhost:5173](https://localhost:5173). `npm ci` installs Vite and
the other dependencies; run it after cloning or when the lock file changes.
For subsequent starts, `npm run dev` is enough.

The current frontend does not send the required API key, so its API requests
receive HTTP 401 until frontend integration is added. Use authorized Swagger
or direct API requests for the backend demo.

Vite proxies API requests to `https://localhost:63887` and verifies its certificate.
If you deliberately use another HTTPS API port, copy `frontend/.env.example` to
`frontend/.env.local`, set `API_TARGET`, and restart Vite.

### Everyday startup and shutdown

1. Start Docker Desktop and the `sqlserver` service using the command above.
2. Run the API with F5 or the launch-profile command.
3. Run `npm run dev` from `frontend`.
4. Refresh the browser after restarting the API with a changed response contract.

Stop the local API and frontend with Ctrl+C, or stop debugging in Visual Studio.
To stop SQL Server while keeping its container and data:

```powershell
docker compose --env-file backend/.env -f backend/compose.yaml stop sqlserver
```

Do not remove the database volume when you want to preserve your demo records.

## API contract

Every controller endpoint below requires `X-Api-Key`. Authentication does not
change the existing request bodies or response fields. Swagger includes the
header security scheme and the 401 response examples.

| Request | Behavior |
| --- | --- |
| `GET /api/vehicles?pageNumber=1&pageSize=10` | Page of active vehicles with pagination metadata |
| `GET /api/vehicles/{registrationNumber}` | Details and the current edit/delete stamp |
| `POST /api/vehicles` without a stamp | Register a vehicle; returns 201 |
| `POST /api/vehicles` with the latest stamp | Update an existing vehicle; returns 200 |
| `DELETE /api/vehicles/{registrationNumber}` | Soft-delete; JSON body contains `concurrencyStamp` |
| `POST /api/bookings/validate` | Read-only eligibility check; body contains `registrationNumber` |

The list response has four fields:

```json
{
  "items": [],
  "pageNumber": 1,
  "totalPages": 0,
  "count": 0
}
```

`items` contains the requested page. `pageNumber` is the requested page number.
`totalPages` is calculated from **all active vehicles** and the requested page
size. `count` is **only the number of vehicles in the current page**, equal to
`items.length`. For example, 49 active vehicles with size 10 produce five pages;
page 5 returns nine items, `pageNumber: 5`, `totalPages: 5` and `count: 9`.

Page number defaults to 1 and must be positive. Page size defaults to 10 and must
be between 1 and 100; invalid parameters return HTTP 400. An out-of-range page
returns empty items and count 0 while preserving the total pages. An empty fleet
has totalPages 0. Items are sorted by unique registration number. Deleted vehicles
are excluded from the rows and the total-page calculation.

Summary items contain registration, make, model, year, daily rate and creation
date. They omit IDs and concurrency stamps. Load details before editing or
deleting, and use the latest stamp returned by details or a successful save.

Registration numbers are uppercase with whitespace removed. `KW00KBGP`,
`KW 00 KB GP` and `kw 00 kb gp` identify the same vehicle. Punctuation stays intact.
The frontend displays supported GP plates with spacing but sends the compact
identity. Daily rates use two decimal places with banker's rounding. The API
currently validates vehicle years from 1000 through 2026.

Validation failures return 400, missing/deleted details return 404, and stale
stamps or duplicate registrations return 409. Booking checks return 409 for a
deleted vehicle and 404 for an unknown vehicle. Checks do not create bookings
or model date availability.

The frontend reads page and size from the URL, offers 10/25/50 rows, and resets
to page 1 when size changes. It uses the returned `totalPages` for navigation.
Back and Cancel preserve the list URL. Saving invalidates list pages; deletion
clears list/detail caches and returns to a valid page. Missing details show
"This vehicle is no longer available" with a link to the refreshed list.

## Architecture and patterns

| Area / pattern | Responsibility and location |
| --- | --- |
| Layered architecture | `backend/src` separates Domain, Application, Infrastructure and API. Dependency injection wires implementations in `Api/Extensions/ConfigurationExtension.cs`. |
| Domain model / aggregate root | `Domain/Vehicles/Vehicle.cs` owns register, update, delete and booking-eligibility operations. Private setters protect identity, creation date and valid state. |
| Value objects | `RegistrationNumber` normalizes identity; `VehicleYear` and `DailyRate` validate and represent vehicle values. |
| Repository | `IVehicleRepository` abstracts persistence. `VehicleRepository` performs ordered, paged EF queries and stages concurrency-checked updates. Its page method returns a list; a separate scalar count query supports total pages. |
| Application services and organisers | Services load entities, invoke domain operations and map DTOs. Organisers coordinate use cases, including routing an upsert to explicit register/update operations. |
| CQRS with MediatR | Commands handle writes and queries handle reads. Both use the same database; there are no separate read/write databases. |
| Pipeline behaviors | Request context identifies the CQRS operation, FluentValidation rejects invalid inputs, and Unit of Work saves once for successful commands. Read queries do not save. |
| Optimistic concurrency | A GUID stamp detects stale edits/deletes. The EF interceptor generates a new stamp on writes; mapping the save response after persistence returns the latest stamp. |
| Soft deletion | `IsDeleted` preserves the row. An EF global filter hides it from active reads/counts; booking checks deliberately include it to explain why it is unavailable. The unique registration index still reserves deleted registrations. |
| DTOs and API boundary | Responses expose the fields needed by each screen. Central exception handling returns Problem Details and keeps diagnostic information in server logs. Swagger documents examples and status codes. |
| API-key authentication | ASP.NET Core authentication protects every controller endpoint with `X-Api-Key`. Options validation requires startup configuration; comparison uses fixed-length digests and constant-time comparison. Swagger documents how to authorize. |
| React feature structure | `frontend/src/features/vehicles` separates API calls, DTOs, validation, controller hooks and views. |
| Frontend state and validation | React Query manages cached server data; React Hook Form and Zod validate forms and responses. URL parameters own paging state, and query keys include page and size. |

Request flow: controller → MediatR behaviors → handler → organiser → application
service → repository / domain operation → persistence → response mapping.

Edits preserve registration, identity and creation date. A no-op update still
checks the expected stamp. Domain validation runs before mutation, so a rejected
change leaves the entity intact. GUID concurrency protection complements these
domain rules.

## Data cleanup

The seed audit found all 50 catalog records valid after the Fronx year correction.
No catalog entry needs deleting. `DEMO001GP` remains soft-deleted and is intentionally
excluded from the active fleet.

The approved cleanup removes the invalid non-seeded Ford Ranger `XCV334GP`
(year 2029), plus explicitly listed completed browser-test records. The script
never targets the `DEMO001GP`–`DEMO050GP` fleet:

[backend/scripts/Clean-DemoData.sql](backend/scripts/Clean-DemoData.sql)

In SSMS, open the script against the local server and run it with
`@ApplyCleanup = 0` to preview. Change that value to 1 to permanently delete the
listed matching rows. The script uses a transaction, skips corrected/restored
rows, reports removed registrations, and then audits the seed records. Running
it again after cleanup removes zero rows. Reseeding will not recreate these
non-catalog records.

## Verification

Backend build and tests (handler unit tests use fakes; API integration tests use
an isolated SQLite database and an explicitly configured test key):

```powershell
dotnet build backend/LevelMovement.slnx
dotnet test backend/LevelMovement.slnx
```

Frontend unit tests and production build:

```powershell
cd frontend
npm test
npm run build
```

Browser CRUD tests also need the frontend to supply the API key before they can
run against the secured API. The existing frontend test commands are retained
below for that follow-up work. From `frontend`, with the API and SQL Server running:

```powershell
npx playwright install chromium
$env:NODE_EXTRA_CA_CERTS = (Resolve-Path ../backend/certs/localhost.pem).Path
npm run test:e2e
```

Playwright starts or reuses Vite on port 5173. Set `E2E_PORT` to use another port.
CRUD browser tests create their own `E2E-`/`RACE-` registrations and soft-delete
them; they do not reset the seed fleet. Backend coverage includes all 50 seeded
vehicles, active filtering, page boundaries and concurrency. Frontend coverage
includes paging URLs, size changes, return navigation, caches and detail 404s.
Backend security tests cover accepted/rejected headers, startup validation and
anonymous Swagger. Direct command/query handler tests cover routing, normalized
values, pagination, cancellation and error propagation without a database or HTTP host.

## Troubleshooting

| Symptom | Action |
| --- | --- |
| `vite is not recognized` | Run `npm ci` in `frontend`, then `npm run dev`. |
| SQL login fails | Verify the current login password and the local API user-secret connection. Changing `.env` alone does not rotate an existing SQL password. |
| API startup reports `Security:ApiKey` missing | Set the user secret or `Security__ApiKey` environment variable and restart the API. Migration/seed commands do not need it. |
| API returns 401 | Send exactly one correct `X-Api-Key` header. In Swagger, click Authorize and enter the key without a Bearer prefix. |
| SQL container is starting | Inspect `docker compose --env-file backend/.env -f backend/compose.yaml logs sqlserver` and wait for a healthy state. |
| Browser/proxy HTTPS error | Run the HTTPS setup script as the user running the app, accept trust, and restart the API/frontend. Use the HTTPS addresses above. |
| Build files locked by the running API | Stop debugging before rebuilding, or test with `dotnet test backend/LevelMovement.slnx --artifacts-path backend/.artifacts`. |
| Metadata file / CS0006 error | Build the solution and fix the first error in the referenced project; missing reference assemblies are often secondary errors. |
| Save/delete returns 409 | Reload the latest details before saving or deleting again. |
| Details return 404 | The registration is missing or deleted; return to the active list. |
| Vitest workers cannot start | Check the Node version. With a supported runtime, try `npm test -- --pool=threads --maxWorkers=1`. |
| A port is already in use | Stop the old process or choose a new API/Vite port and update `API_TARGET` to match. |
