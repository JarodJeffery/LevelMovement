# Level Movement

Vehicle management assessment with a .NET 10 backend and a separate React frontend.

## Domain design and request flow

`Vehicle` is the aggregate root for vehicle management. It has private setters and
exposes `Register`, `UpdateDetails`, `Delete`, and `EnsureEligibleForBooking`.
Registration creates the identity and UTC creation date. Editing preserves those
values and the registration number. The entity validates all proposed details
before changing its state, and deleted vehicles cannot be edited or booked.

Application services in `backend/src/LevelMovement.Application/Services` coordinate
loading entities, invoking their business operations, and mapping responses.
`VehicleRepository` handles EF queries, additions, and staging updates with the
client's expected concurrency stamp. It does not implement vehicle editing or
deletion rules. The former `VehicleBookingManager` has been replaced by the
entity's eligibility operation because that rule depends only on one vehicle.

The existing HTTP upsert contract is unchanged: omit the stamp to create, or send
the latest stamp to update. `UpsertVehicleOrganiser` translates that request into
explicit `RegisterAsync` or `UpdateAsync` application operations. Updates load the
existing entity rather than constructing a temporary entity with an empty ID.
Missing update targets remain 404 responses.

The command pipeline saves once after the handler returns. The EF interceptor
generates the next stamp, and response mapping happens after saving. No-op edits
still issue a version-checked write, stale writes return 409, and soft-deleted rows
remain covered by the unique registration index. These persistence protections
complement the entity's business rules.

This refactor does not change the database model or require a new schema migration.
The registration-normalization data migration below is still required for older
databases. The model represents vehicle management; booking validation does not
reserve a vehicle or implement date availability.

For explanations, code references, and interview practice, see the
[study guide](docs/LevelMovement_Principles_Study_Guide.md) or its
[Word version](docs/LevelMovement_Principles_Study_Guide.docx).

## Registration identity and display

Registration numbers are stored and returned by the API in uppercase with all
whitespace removed. `KW00KBGP`, `KW 00 KB GP`, and `kw 00 kb gp` identify the same
vehicle for creation, lookup, updates, deletion, and booking checks. The 20-character
limit applies after whitespace removal. Punctuation is preserved.

The frontend displays plates matching `AA00AAGP` as `AA 00 AA GP`, including form
values and confirmation messages, while requests and cache keys use the compact
value. Other plate formats retain their compact value without guessed grouping.

The `NormalizeVehicleRegistrations` migration updates existing rows, including
soft-deleted vehicles, and refreshes the stamps of changed rows. It refuses to
proceed if normalization would create duplicate registrations; resolve these
records explicitly before rerunning it. No rows are automatically merged or
deleted. Rolling back the migration does not reconstruct the original spacing.
Apply the migration before running the updated API against an existing database.

## Run the backend with Docker

Prerequisites: Docker Desktop (or Docker Engine with Compose v2), running Linux
containers on an x86-64 machine. Allow at least 2 GB for SQL Server plus capacity
for the API and builds. No local SQL Server is needed. Install the .NET 10 SDK to set up the local HTTPS certificate.

From the repository root, run in PowerShell:

```powershell
cd backend
Copy-Item .env.example .env
# Set a local password in .env
./scripts/Setup-LocalHttps.ps1
docker compose up --build -d
```

Only copy the example on first setup; do not overwrite an existing .env.
Replace its sample password with a strong development password before starting.
On Linux/macOS, use `cp .env.example .env` instead of `Copy-Item`.
The real .env is ignored by Git.

Open [Swagger](https://localhost:5080/swagger) to try the create-vehicle endpoint
with its request and response examples.

Compose starts SQL Server, waits for it to become healthy, applies EF Core
migrations to create the LevelMovement database and Vehicles table, then starts
the API. The initial image downloads and build can take several minutes.

This is a local development setup using SQL Server Developer edition. The Compose
configuration accepts the [SQL Server container license terms](https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker)
through ACCEPT_EULA=Y.

## Database access

To connect through SQL Server Management Studio:

- Server: `localhost,14330`
- Authentication: SQL Server Authentication
- Login: `sa`
- Password: the value in your local `backend/.env`
- Trust the server certificate for this local development connection.

The API container connects to `sqlserver,1433` automatically. A locally launched
API from Visual Studio needs its connection string overridden separately; .NET
does not read Compose's .env automatically. Use user secrets or
`ConnectionStrings__LevelMovement`, with the host address above.

Data persists in a Docker named volume. Changing .env does not rotate the password
in an existing SQL Server volume. Keep the original password or change it through
SQL Server. Existing Windows databases are not imported or modified.

## Stop and troubleshoot

Run these commands from backend:

```powershell
docker compose ps -a
docker compose logs sqlserver migrate api
docker compose down
```

`down` preserves database data. Adding `--volumes` deletes that data.
Change SQL_PORT or API_PORT in .env if the default ports are occupied.
An exited migration container with exit code 0 is expected.

## Frontend

The frontend is independent of the .NET solution and includes vehicle CRUD and booking eligibility.
With Node.js 22.12+ installed, run from the repository root:

```powershell
cd frontend
npm ci
npm run dev
```

Open https://localhost:5173. Run the HTTPS setup script above before the first frontend start.
The proxy defaults to the Visual Studio API at https://localhost:63887. To use Docker,
set `API_TARGET=https://localhost:5080` in `frontend/.env.local` and restart Vite.
Certificate files and passwords stay in ignored `backend/certs` and `backend/.env`.
All web listeners use HTTPS; the proxy verifies the development certificate.

## Backend development

The solution is `backend/LevelMovement.slnx`. With .NET 10 installed:

```powershell
dotnet test backend/LevelMovement.slnx
```

After changing the EF model, generate and review a migration from backend:

```powershell
dotnet tool restore
dotnet ef migrations add DescribeYourChange --project src/LevelMovement.Infrastructure --startup-project src/LevelMovement.Api -- --environment Development
docker compose up --build -d
```

Commit migration files alongside code changes. Repository layers are Domain,
Application, Infrastructure and API; the frontend is not part of the solution.
