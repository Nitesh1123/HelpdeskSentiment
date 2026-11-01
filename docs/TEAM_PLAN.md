# Team Plan — Sentiment Analysis of Incoming Calls on Helpdesk

**Course:** INT402 – Modern Web Programming Tools and Techniques
**Stack:** C# · ASP.NET Core MVC (.NET 10) · Entity Framework Core (Code First) · SQL Server (LocalDB) · ASP.NET Core Web API · Visual Studio
**Deadline:** 1 day

## What the app does

A helpdesk agent logs each incoming call (caller, phone, category, and the call transcript / notes).
The app runs a lexicon‑based sentiment analyser over the transcript and stores a score and a label
(Positive / Neutral / Negative), plus a priority flag for angry callers. Supervisors see a dashboard
with totals per sentiment and category, and all data is also exposed as a REST API that is tested in Postman.

## Team and branches

| Member | GitHub | Role | Branch |
|---|---|---|---|
| Nitesh (lead) | [@Nitesh1123](https://github.com/Nitesh1123) | Core: data model, database, sentiment engine | `feature/core-sentiment-engine` |
| Priyanshu Tanwar | [@Priyanshutanwar19](https://github.com/Priyanshutanwar19) | Frontend: MVC controllers, Razor views, forms, dashboard | `feature/mvc-ui` |
| Harshvardhan | [@Harshvardhan8091](https://github.com/Harshvardhan8091) | Security & API: auth, middleware, Web API, Postman | `feature/api-auth` |

`main` only receives work through pull requests. Merge order: **core → mvc-ui → api-auth**
(UI and API both depend on the models and the `ISentimentAnalyzer` service from core).

## Member 1 — Core / Database / Sentiment engine (`feature/core-sentiment-engine`)

Syllabus: Unit II (C# fundamentals, OOP, strings, arrays, exceptions), Unit IV (DI, Code First), Unit V (SQL Server).

- [ ] Models: `CallRecord`, `Agent`, `SentimentLabel` enum, `CallCategory` enum, with data‑annotation validation
- [ ] `HelpdeskDbContext` + EF Core **Code First** migration, seed data
- [ ] Sentiment engine using OOP:
  - `abstract class SentimentAnalyzerBase` (abstraction) → `LexiconSentimentAnalyzer` (inheritance, `override` = polymorphism)
  - Word lists stored in arrays/dictionaries; string methods to tokenise (`ToLower`, `Split`, `Trim`)
  - Handles negation (“not happy”), intensifiers (“very angry”), loops with `continue`/`break`
  - Exception handling for empty transcripts (custom `SentimentException`)
- [ ] Register `ISentimentAnalyzer` and `ICallService` in DI container (`Program.cs`)
- [ ] `Database/` SQL scripts: tables, constraints, **views** (`vw_SentimentSummary`, `vw_NegativeCallsByAgent` with JOIN),
      **stored procedures** (`sp_GetCallsBySentiment`, `sp_DailySentimentReport`), DML demo script

## Member 2 — MVC Frontend (`feature/mvc-ui`)

Syllabus: Unit III (MVC, Razor, layout, sections, ViewStart, ViewBag/ViewData, TempData, Session, QueryString, forms, model binding, validation).

- [x] `CallsController`: Index (list with **QueryString** filter `?sentiment=Negative&search=`), Details, Create, Edit, Delete
- [x] Create form with **model binding** + **server‑side** (`ModelState`) and **client‑side** (jQuery validation) validation
- [x] After saving, show the detected sentiment with **TempData** flash message
- [x] `DashboardController`: counts per sentiment / category using **ViewBag** and **ViewData**, Chart.js chart
- [x] “Recently viewed calls” stored in **Session**
- [x] `_Layout.cshtml` with navbar, `@RenderSection("Scripts")`, `_ViewStart`, `_ViewImports`, partial view for the sentiment badge
- [x] “Analyse text” playground page (paste text → see score and matched words)

## Member 3 — Security, Middleware & Web API (`feature/api-auth`)

Syllabus: Unit IV (middleware pipeline, custom middleware, DI, authentication & authorization), Unit VI (REST, Web API, CRUD, routing, Postman).

- [x] Cookie **authentication**: `AccountController` Login/Logout, roles `Admin` and `Agent`, `[Authorize]` on MVC controllers,
      `[Authorize(Roles="Admin")]` on Delete and Dashboard
- [x] **Custom middleware** `RequestLoggingMiddleware` (logs method, path, status, elapsed ms) + built‑in middleware ordering
- [x] **Basic authentication** handler for the API (`Authorization: Basic …`) — matches the practical in the syllabus
- [x] `api/calls` Web API with **attribute routing**: GET all, GET by id, POST, PUT, DELETE, plus `POST api/sentiment/analyze`
      and `GET api/calls/summary`
- [x] Postman collection in `docs/HelpdeskSentiment.postman_collection.json`

## Timeline (1 day)

| Time | Everyone |
|---|---|
| Hour 0–1 | Clone repo, open `HelpdeskSentiment.sln` in Visual Studio, run `Update-Database` |
| Hour 1–5 | Each member works on own branch, commits small and often |
| Hour 5–6 | Open PR → review by one teammate → merge into `main` (core first) |
| Hour 6–8 | Integration test, Postman run, screenshots, report/viva prep |

## Git workflow

```bash
git clone https://github.com/Nitesh1123/HelpdeskSentiment.git
cd HelpdeskSentiment
git checkout feature/<your-branch>
# ...work...
git add .
git commit -m "Short description of change"
git push
# then open a Pull Request into main on GitHub
```
