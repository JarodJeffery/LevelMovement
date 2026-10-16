# Level Movement

Vehicle management assessment with a .NET 10 backend and a separate React frontend.
Docker hosts SQL Server only. Run the API locally from Visual Studio or `dotnet run`.

## Start the database

Prerequisites: .NET 10 locally and Docker Desktop running Linux containers on an
x86-64 machine. Allow at least 2 GB for SQL Server.

For first-time setup, run in PowerShell from the repository root:

```powershell
cd backend
Copy-Item .env.example .env
# Set your own local SQL password in .env.
docker compose up -d
```

Only copy the example on first setup; do not overwrite an existing `.env`.
The real `.env` is ignored by Git. On Linux/macOS, use `cp .env.example .env`.
Wait for `docker compose ps` to report SQL Server as healthy.
For subsequent runs, `docker compose up -d` starts just the database.

This uses SQL Server Developer edition for local development/testing. Compose
accepts the [SQL Server container license terms](https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker)
through `ACCEPT_EULA=Y`.

## Run the API locally

The API reads its development connection from .NET user secrets. Configure it once
from the repository root, replacing `YOUR_PASSWORD` with the value in `backend/.env`:

```powershell
dotnet user-secrets set "ConnectionStrings:LevelMovement" 'Server=tcp:127.0.0.1,14330;Database=LevelMovement;User Id=sa;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True;' --project backend/src/LevelMovement.Api
```

Use the actual port from `.env` if you changed `SQL_PORT`. Secrets stay outside the
repository. .NET does not read Compose's `.env` automatically. An environment
variable named `ConnectionStrings__LevelMovement` can also override the connection.

Apply migrations and seed the demo fleet once:

```powershell
dotnet run --project backend/src/LevelMovement.Api -- --seed-demo --environment Development
```

Then set `LevelMovement.Api` as the Visual Studio startup project and press F5,
or run this from the repository root:

```powershell
dotnet run --project backend/src/LevelMovement.Api
```

The existing launch profile opens Swagger. Its URLs are
[HTTP Swagger](http://localhost:63888/swagger) and
[HTTPS Swagger](https://localhost:63887/swagger).

## Database access

To connect through SQL Server Management Studio:

- Server: `tcp:127.0.0.1,14330`
- Authentication: SQL Server Authentication
- Login: `sa`
- Password: `MSSQL_SA_PASSWORD` in your local `backend/.env`
- Enable Trust server certificate for this local connection.
- Database: `LevelMovement`

The explicit IPv4 address matches the Docker port binding. Database data persists
in the named `levelmovement_sqlserver-data` volume. Existing Windows databases are
not imported or modified.

If `.env` is missing, inspect the SQL container's original environment locally:

```powershell
docker inspect --format '{{range .Config.Env}}{{println .}}{{end}}' levelmovement-sqlserver-1
```

Find `MSSQL_SA_PASSWORD`. This is the initial password and can be outdated if the
SQL login was already changed. Treat the output as a credential.

Changing `.env` alone does not rotate the password in an existing volume. To rotate
a known working password, connect in SSMS as `sa` and execute with your chosen value:

```sql
ALTER LOGIN [sa] WITH PASSWORD = N'YourNewLocalPasswordHere';
```

Update `.env` and the API's user secret to match, then run `docker compose up -d`
from `backend` to refresh SQL Server's environment and health check. Restart your
locally running API to use the new secret. Do not delete the volume to reset a password.

## Demo data

The seeding command adds 50 vehicles (`DEMO001 GP` through `DEMO050 GP`). The fleet
includes small cars, SUVs, bakkies and premium cars, model years from 2020 to 2025,
and varied daily rates. Vehicles are currently the only persisted data area.

Seeding adds missing demo registrations only. Rerunning the command preserves your
records, edits and soft-deleted vehicles without creating duplicates. It applies
pending migrations, seeds and exits. It only runs in Development; normal API
startup does not seed or migrate the database.

In SSMS, verify the fleet with:

```sql
USE LevelMovement;
SELECT * FROM dbo.Vehicles ORDER BY RegistrationNumber;
SELECT COUNT(*) AS DemoVehicles
FROM dbo.Vehicles
WHERE RegistrationNumber LIKE 'DEMO[0-9][0-9][0-9] GP' AND IsDeleted = 0;
```

## Stop and troubleshoot

Stop the local API through Visual Studio or Ctrl+C. From `backend`:

```powershell
docker compose ps -a
docker compose logs sqlserver
docker compose down
```

`down` preserves database data. Adding `--volumes` deletes that data.
Change `SQL_PORT` in `.env` and the API connection if the port is occupied.

## Frontend

The frontend is independent of the .NET solution and is currently a placeholder.
With Node.js 22.12+ installed, run from the repository root:

```powershell
cd frontend
npm ci
npm run dev
```

## Backend development

The solution is `backend/LevelMovement.slnx`. With .NET 10 installed:

```powershell
dotnet test backend/LevelMovement.slnx
```

After changing the EF model, generate and review a migration from `backend`:

```powershell
dotnet tool restore
dotnet ef migrations add DescribeYourChange --project src/LevelMovement.Infrastructure --startup-project src/LevelMovement.Api -- --environment Development
dotnet run --project src/LevelMovement.Api -- --migrate --environment Development
```

Commit migration files alongside code changes. Repository layers are Domain,
Application, Infrastructure and API; the frontend is not part of the solution.
