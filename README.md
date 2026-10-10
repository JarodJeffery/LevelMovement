# Level Movement

Vehicle management assessment with a .NET 10 backend and a separate React frontend.

## Run the backend with Docker

Prerequisites: Docker Desktop (or Docker Engine with Compose v2), running Linux
containers on an x86-64 machine. Allow at least 2 GB for SQL Server plus capacity
for the API and builds. No local SQL Server or .NET SDK is needed for this option.

From the repository root, run in PowerShell:

```powershell
cd backend
Copy-Item .env.example .env
# Set a local password in .env
docker compose up --build -d
```

Only copy the example on first setup; do not overwrite an existing .env.
Replace its sample password with a strong development password before starting.
On Linux/macOS, use `cp .env.example .env` instead of `Copy-Item`.
The real .env is ignored by Git.

Open [Swagger](http://localhost:5080/swagger) to try the create-vehicle endpoint
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

After changing the EF model, generate and review a migration from backend:

```powershell
dotnet tool restore
dotnet ef migrations add DescribeYourChange --project src/LevelMovement.Infrastructure --startup-project src/LevelMovement.Api -- --environment Development
docker compose up --build -d
```

Commit migration files alongside code changes. Repository layers are Domain,
Application, Infrastructure and API; the frontend is not part of the solution.
