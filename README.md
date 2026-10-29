# Helpdesk Call Sentiment Analysis

INT402 team project — an ASP.NET Core MVC + Web API application that analyses the sentiment of incoming
helpdesk calls (from the call transcript / agent notes) and gives supervisors a dashboard of caller mood.

See [docs/TEAM_PLAN.md](docs/TEAM_PLAN.md) for the task split between team members.

## Tech used (from the INT402 syllabus)

| Syllabus unit | Where it is used |
|---|---|
| I – .NET Core, C#, .NET CLI, Visual Studio | Solution `HelpdeskSentiment.sln`, `dotnet` CLI commands below |
| II – C# fundamentals & OOP | `Services/Sentiment/*` — abstract class, inheritance, polymorphism, arrays, strings, loops, exceptions |
| III – MVC, Razor, data passing, forms, validation | `Controllers/CallsController`, `DashboardController`, `Views/*` |
| IV – Middleware, DI, Code First, authentication | `Middleware/*`, `Program.cs`, `Data/HelpdeskDbContext`, `Controllers/AccountController` |
| V – SQL Server tables, views, stored procedures | `Database/*.sql` |
| VI – Web API, CRUD, routing, Postman | `Controllers/Api/*`, `docs/HelpdeskSentiment.postman_collection.json` |

## Team

| Member | Role | Branch |
|---|---|---|
| [@Nitesh1123](https://github.com/Nitesh1123) | Core: models, database, sentiment engine | `feature/core-sentiment-engine` |
| [@Priyanshutanwar19](https://github.com/Priyanshutanwar19) | MVC UI: controllers, Razor views, dashboard | `feature/mvc-ui` |
| [@Harshvardhan8091](https://github.com/Harshvardhan8091) | Auth, middleware, Web API, Postman | `feature/api-auth` |

## Running it

Requirements: Visual Studio 2026 (or the .NET 10 SDK) with SQL Server Express LocalDB.

```bash
dotnet tool install --global dotnet-ef
cd HelpdeskSentiment
dotnet ef database update
dotnet run
```

Or in Visual Studio: open `HelpdeskSentiment.sln`, then in *Package Manager Console* run `Update-Database`, then press F5.
(The app also applies migrations and adds sample calls on startup, so this step is optional.)

### Demo logins

| Username | Password | Role | Can do |
|---|---|---|---|
| `admin` | `Admin@123` | Admin | everything, including Dashboard and Delete |
| `agent` | `Agent@123` | Agent | log, view and edit calls |

These are demo values in `appsettings.json` → `DemoUsers`.

### Web API

All `api/calls` endpoints use HTTP Basic auth with the logins above. Import
[docs/HelpdeskSentiment.postman_collection.json](docs/HelpdeskSentiment.postman_collection.json) into Postman to run them.

| Method | Route | Notes |
|---|---|---|
| GET | `/api/calls?sentiment=Negative&search=bill` | list, both filters optional |
| GET | `/api/calls/{id}` | one call |
| GET | `/api/calls/summary` | counts per sentiment, category and agent |
| POST | `/api/calls` | create, sentiment is calculated by the server |
| PUT | `/api/calls/{id}` | update and re-analyse |
| DELETE | `/api/calls/{id}` | Admin only |
| POST | `/api/sentiment/analyze` | `{ "text": "..." }`, no login needed |
