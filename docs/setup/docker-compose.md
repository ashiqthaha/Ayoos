# Run Ayoos with Docker Compose

Docker Compose starts PostgreSQL, Keycloak, the Ayoos API, and the frontend. The values in `.env.example` are development-only defaults.

## Start the services

From the repository root, copy the example environment file:

```powershell
Copy-Item .env.example .env
```

On macOS or Linux, use:

```bash
cp .env.example .env
```

Then build and start all services:

```bash
docker compose up -d
```

The services are available at:

- Frontend: http://localhost:3000
- API: http://localhost:5000
- API health check: http://localhost:5000/health
- Keycloak: http://localhost:8081
- PostgreSQL: localhost:5432

PostgreSQL initializes the `ayoos` application database and a separate `keycloak` database the first time the data volume is created.

## Stop the services

Stop and remove the containers while retaining database data:

```bash
docker compose down
```

## Reset all database data

Stop the services and delete the PostgreSQL data volume:

```bash
docker compose down --volumes
```

The next `docker compose up -d` creates fresh `ayoos` and `keycloak` databases. This reset permanently deletes all local database and Keycloak data stored by this Compose project.
