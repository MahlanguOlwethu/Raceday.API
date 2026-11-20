## Running with Docker

Prerequisites: Docker Desktop, a SQL Server reachable with SQL authentication, and an Azure Storage account.

```bash
cp .env.example .env     # then fill in your connection strings
docker compose up --build
```

- MVC web app: http://localhost:5000
- API + Swagger: http://localhost:5001/swagger

Stop with `docker compose down`.
