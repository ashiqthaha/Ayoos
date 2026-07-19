# Ayoos

Open, self-hostable telehealth EMR. Practice management, scheduling, video consultations, and FHIR-native clinical records. Built with .NET Clean Architecture + Next.js.

This repository is an initial scaffold. It establishes the application boundaries and local development experience without adding business features.

## Structure

```text
backend/
  src/
    Ayoos.Domain/          Domain entities, value objects, and domain events
    Ayoos.Application/     CQRS application layer, interfaces, and validation
    Ayoos.Infrastructure/  EF Core, PostgreSQL, repositories, and tenancy setup
    Ayoos.Api/             ASP.NET Core minimal API host
  tests/
    Ayoos.UnitTests/       xUnit unit tests
frontend/
  src/
    app/                   Next.js App Router pages and layouts
    components/            Reusable UI components
    lib/                   Frontend configuration and helpers
```

## Run the backend

The backend requires the .NET 10 SDK. From the repository root:

```bash
cd backend
dotnet restore
dotnet build
dotnet run --project src/Ayoos.Api
```

The API runs at `http://localhost:5000`. Its health check is available at `http://localhost:5000/health`, and Swagger UI is available at `http://localhost:5000/swagger`.

The PostgreSQL connection string in `appsettings.json` is a local-development placeholder. The scaffold does not connect to the database while serving the health check.

## Run the frontend

The frontend requires Node.js and npm. In a second terminal, from the repository root:

```bash
cd frontend
cp .env.local.example .env.local
npm install
npm run dev
```

Open `http://localhost:3000`. The landing page checks the backend health endpoint and reports whether it is connected.
