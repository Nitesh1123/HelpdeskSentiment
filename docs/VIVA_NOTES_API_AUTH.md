# Viva Notes — Security, Middleware & Web API

**Student:** Harshvardhan Singh  
**Project:** Helpdesk Call Sentiment Analysis (`HelpdeskSentiment`)  
**Role:** Member 3 — Security, Middleware & Web API (`feature/api-auth`)  
**Stack:** ASP.NET Core (.NET 10), C#, REST Web API, HTTP Basic Auth, Cookie Auth  

---

## 1. Middleware Pipeline Order in `Program.cs`

In ASP.NET Core, middleware components execute sequentially like Russian nesting dolls (request travels inward, response travels outward).

```
Incoming Request
      │
      ▼
┌─────────────────────────────────┐
│  UseRequestLogging()            │ ◄── Starts stopwatch, logs total elapsed time & status
└────────────────┬────────────────┘
                 ▼
┌─────────────────────────────────┐
│  UseHttpsRedirection()          │
└────────────────┬────────────────┘
                 ▼
┌─────────────────────────────────┐
│  UseRouting()                   │ ◄── Matches URL to controller/action endpoint
└────────────────┬────────────────┘
                 ▼
┌─────────────────────────────────┐
│  UseSession()                   │ ◄── Loads session state (e.g., recent calls)
└────────────────┬────────────────┘
                 ▼
┌─────────────────────────────────┐
│  UseAuthentication()            │ ◄── Who are you? (Identifies user & creates ClaimsPrincipal)
└────────────────┬────────────────┘
                 ▼
┌─────────────────────────────────┐
│  UseAuthorization()             │ ◄── Are you allowed? (Checks roles / policies)
└────────────────┬────────────────┘
                 ▼
┌─────────────────────────────────┐
│  MapControllers() / Routes      │ ◄── Controller action execution
└─────────────────────────────────┘
```

### Why `UseAuthentication()` MUST come before `UseAuthorization()`
- **Authentication** answers: *"Who are you?"* It inspects cookies or headers, validates credentials, and attaches `HttpContext.User` (`ClaimsPrincipal`).
- **Authorization** answers: *"Are you permitted to access this resource?"* It checks `[Authorize]` or `[Authorize(Roles = "Admin")]` on `HttpContext.User`.
- If `UseAuthorization()` ran first, `HttpContext.User` would still be unauthenticated/empty, causing all protected requests to fail.

### Why `UseRequestLogging()` sits at the very beginning
- Wrapping outer middleware allows the stopwatch to measure the **entire round-trip request lifecycle**, including authentication, model binding, database access, and view rendering.
- Even if downstream middleware rejects a request (e.g. 401 or 403), `RequestLoggingMiddleware`'s `finally` block still executes and logs the outcome.

---

## 2. How `RequestLoggingMiddleware` Works

File: `Middleware/RequestLoggingMiddleware.cs`

1. **Stopwatch start:** Instantiates `Stopwatch.StartNew()` when the request arrives.
2. **Response header hook:** Registers `context.Response.OnStarting()` to inject `X-Response-Time-ms` header before response bytes are sent to the client.
3. **Pipeline continuation:** Calls `await _next(context)` inside a `try` block to pass control to downstream middleware.
4. **Logging in `finally`:** When control returns, it stops the stopwatch and logs:
   - HTTP Method (e.g., `GET`, `POST`, `DELETE`)
   - Path and Query String (e.g., `/api/calls?sentiment=Negative`)
   - HTTP Status Code (e.g., `200`, `201`, `401`, `403`)
   - Elapsed Time in milliseconds (e.g., `14 ms`)
   - Authenticated User identity name or `"anonymous"`

---

## 3. How `BasicAuthenticationHandler` Works

File: `Authentication/BasicAuthenticationHandler.cs`

1. **Header Inspection:** Checks if the incoming request has an `Authorization` header starting with `Basic `. If missing, returns `AuthenticateResult.NoResult()` (allowing anonymous requests or other schemes to proceed).
2. **Base64 Decoding:** Extracts the base64 token and decodes it:
   ```csharp
   byte[] credentialBytes = Convert.FromBase64String(encodedCredentials);
   string[] credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
   ```
3. **User Validation:** Checks username and password against `IUserStore.Validate(username, password)`.
4. **Failure Handling:** If invalid, returns `AuthenticateResult.Fail("Invalid Username or Password")`.
5. **ClaimsPrincipal Construction:** If valid, creates claims:
   - `ClaimTypes.NameIdentifier` = username
   - `ClaimTypes.Name` = display name
   - `ClaimTypes.Role` = role (`"Admin"` or `"Agent"`)
6. **AuthenticationTicket:** Wraps the identity into an `AuthenticationTicket(principal, Scheme.Name)` and returns `AuthenticateResult.Success(ticket)`.

---

## 4. Web API Controllers: Attribute Routing & HTTP Status Codes

Files: `Controllers/Api/CallsApiController.cs`, `Controllers/Api/AgentsApiController.cs`

### Attribute Routing
Instead of relying on convention routes (`{controller}/{action}/{id}`), attribute routing binds HTTP methods and URL paths directly to controller actions:
- `[Route("api/calls")]`
- `[HttpGet]`, `[HttpGet("{id:int}")]`, `[HttpGet("summary")]`
- `[HttpPost]`, `[HttpPut("{id:int}")]`, `[HttpDelete("{id:int}")]`
- `[Route("api/agents")]` -> `[HttpGet]`

### When each HTTP Status Code is returned:
| Status Code | Meaning | Endpoint & Condition |
|---|---|---|
| **200 OK** | Successful read | `GET /api/calls`, `GET /api/calls/{id}`, `GET /api/calls/summary`, `GET /api/agents` |
| **201 Created** | Entity created | `POST /api/calls` returns created call object with `Location` header (`CreatedAtAction`) |
| **204 No Content** | Successful update/delete with no body | `PUT /api/calls/{id}` (after update), `DELETE /api/calls/{id}` (after deletion) |
| **400 Bad Request** | Validation failed | `POST` or `PUT` with invalid body (e.g. invalid phone number regex, missing fields) via automatic `[ApiController]` model validation |
| **401 Unauthorized** | Missing or invalid credentials | Calling protected endpoints without `Authorization: Basic ...` header |
| **403 Forbidden** | Authenticated, but insufficient role | Logged in as `Agent`, but trying to access admin-only endpoints (`DELETE /api/calls/{id}`, `GET /api/agents`) |
| **404 Not Found** | Resource does not exist | `GET /api/calls/{id}`, `PUT /api/calls/{id}`, or `DELETE /api/calls/{id}` when ID is not found in database |

---

## 5. `[Authorize]` vs `[Authorize(Roles = "Admin")]` (401 vs 403)

- **`[Authorize]`**:
  - Requires the caller to be **authenticated** (any valid login: Admin or Agent).
  - If unauthenticated -> returns **401 Unauthorized**.
- **`[Authorize(Roles = Roles.Admin)]`**:
  - Requires the caller to be authenticated **AND** have the `Admin` role claim.
  - If unauthenticated -> returns **401 Unauthorized**.
  - If authenticated as `Agent` -> returns **403 Forbidden** (caller's identity is verified, but their permissions are insufficient).

---

## 6. Top 10 Likely Viva Questions & Answers

### Q1: What is middleware in ASP.NET Core?
**Answer:** Middleware is software assembled into an application's request pipeline to handle requests and responses. Each component can perform logic before and after the next component in the pipeline.

### Q2: Why must `UseAuthentication` precede `UseAuthorization`?
**Answer:** `UseAuthentication` identifies who the user is and creates the `ClaimsPrincipal`. `UseAuthorization` checks if that user has permission. Authorization cannot evaluate permissions if the user's identity has not been established first.

### Q3: How does HTTP Basic Authentication work?
**Answer:** The client sends an `Authorization: Basic <base64(username:password)>` header with each HTTP request. The server decodes the base64 string, validates credentials, and authenticates the request.

### Q4: What is the difference between 401 Unauthorized and 403 Forbidden?
**Answer:** 
- **401 Unauthorized:** The client is unauthenticated (missing or invalid credentials); client must log in.
- **403 Forbidden:** The client is successfully authenticated, but lacks permission/role to access the resource.

### Q5: What is a `ClaimsPrincipal` and why are Claims used?
**Answer:** A `ClaimsPrincipal` represents the current user. A claim is a name-value pair stating a fact about the user (e.g., Name, Role, Email). Claims allow flexible role-based and policy-based authorization without hardcoding user data.

### Q6: Why do you return DTOs (Data Transfer Objects) instead of EF Core entity classes?
**Answer:** DTOs decouple API contracts from the database schema, avoid circular reference loops (e.g., `Agent <-> CallRecord`), and prevent sensitive data exposure (such as password hashes or internal IDs).

### Q7: What is Attribute Routing and why use it for Web APIs?
**Answer:** Attribute routing uses attributes (`[Route]`, `[HttpGet]`, `[HttpPost]`) on controllers and actions to map URLs directly. It provides explicit control over REST URIs and clean parameter constraints (e.g. `{id:int}`).

### Q8: What does `[ApiController]` do automatically?
**Answer:** It enables API-specific behaviors: automatic 400 response on `ModelState.IsValid == false`, inferred parameter binding sources (`[FromBody]` for complex types), and problem details for error responses.

### Q9: Why was custom `RequestLoggingMiddleware` implemented?
**Answer:** To log every HTTP request with execution time in milliseconds, HTTP status code, query parameters, and caller username, while injecting an `X-Response-Time-ms` header for performance monitoring.

### Q10: How does your application support dual authentication (Cookies and Basic Auth)?
**Answer:** In `Program.cs`, Cookie authentication is registered as the default scheme for MVC Razor pages, while HTTP Basic Authentication is registered as a named scheme (`BasicAuthenticationHandler.SchemeName`) used by `[Authorize(AuthenticationSchemes = "Basic")]` on API controllers.
