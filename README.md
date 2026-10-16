# RaceDay Event Management API - Part 2

## Project description
RaceDay is a web-based event management system designed specifically for the South African road running, walking, and cycling community.
The platform allows Event Organisers to create and manage events, categories, and participant results, while Participants can browse upcoming events, enter events, and track their personal performance history.
The system supports role-based access with distinct interfaces for Organisers and Participants.

### CI build screenshot
![Uploading part2 Workflow.png…]()

## Build Status - GREEN
### Part 1: System Planning ✅ (Complete)
- ERD design with 8 entities
- API endpoint planning
- Database schema creation
- CI/CD setup

### Part 2: API Development 
- RESTful API with ASP.NET Core
- Entity Framework Core integration
- Role-based authentication
- Unit testing
- Swagger documentation

### System Roles

### Organiser
- Create, edit, and delete events
- Manage event categories (age groups and distance categories)
- Capture participant results (finish times and positions)
- View all event enrolments
- Upload event banner images

### Participant
- Create an account and manage profile
- Browse upcoming events with filtering options
- Enter events by selecting a category
- View personal enrolment history
- Track personal race results and performance
- Upload profile picture

### How roles are managed
Anyone can register as a Participant. To register as an **Organiser** a user must also supply the **organiser invite code** (`OrganiserInviteCode` in `appsettings.json`, default `RACEDAY-ORGANISER-2026`). Without a valid code the API returns `403 Forbidden`. Change the code before real use, and do not commit a real secret to a public repository (you can override it with an environment variable called `OrganiserInviteCode`).

## Technologies
- C# and ASP.NET Core Web API (.NET 8)
- Entity Framework Core (code-first) with SQL Server
- Swagger / OpenAPI (with XML comments)
- xUnit for automated tests (in-memory database)
- GitHub Actions for continuous integration

## Authentication and session
- Passwords are hashed with ASP.NET Core's `PasswordHasher` before they are stored.
- On login the API signs the user in with a cookie (used by `[Authorize(Roles = "...")]`) **and** creates a **server-side session** that stores the user ID and role. `GET /api/auth/me` shows the values held in the session.
- `POST /api/auth/logout` clears the session and the cookie.
- Role rules are enforced by `[Authorize(Roles = ...)]`, and Organisers can only change data that belongs to their own events.

## Requirements before running
1. Visual Studio 2022 with the ASP.NET and web development workload.
2. The .NET 8 SDK.
3. SQL Server Express (or another SQL Server instance) and SQL Server Management Studio (SSMS).
4. Open `RaceDay.API.sln` from the repository root in Visual Studio.

## Database setup
1. Open `RaceDay.API/appsettings.json`.
2. Check `DefaultConnection` and change the server name if your instance is different. The default is `Server=.\SQLEXPRESS;Database=RaceDay;Trusted_Connection=True;TrustServerCertificate=True;`.
3. Run the API. It calls `EnsureCreated()` to create the database if it does not exist. `EnsureCreated()` does not update an existing schema, so drop the `RaceDay` database if you change the models.

## Run the API
1. Set `RaceDay.API` as the startup project and run the **https** profile.
2. Swagger opens at `https://localhost:7277/swagger`.
3. Register, then log in through Swagger. The session cookie is then sent automatically by the browser for "Try it out" requests.
4. To create an Organiser in Swagger, include the `inviteCode` field in the register request.

The full list of endpoints is in [`docs/Part2-implemented-endpoints.md`](docs/Part2-implemented-endpoints.md).

## Run unit tests
In Visual Studio open **Test > Test Explorer** and select **Run All Tests**, or from a terminal in the repository root:

```bash
dotnet restore RaceDay.API.sln
dotnet build RaceDay.API.sln --configuration Release
dotnet test RaceDay.API.sln --configuration Release
```

The tests use an in-memory database, so they do not need SQL Server. They are split by area: `AuthTests`, `EventTests`, `CategoryTests`, `EnrolmentTests`, `ResultTests` and `ProfileTests`, and cover success and failure cases for both roles.

## GitHub Actions CI
The workflow is `.github/workflows/dotnet.yml`. It restores, builds and tests the solution on every push and pull request.


<!-- ![CI build](docs/ci-build-success.png) -->

## Video presentation
**YouTube video:** TODO - add your unlisted link.

## Reference
I used Claude (Anthropic) to review my API code against the assignment brief and to help refactor controllers to use DTOs, add Swagger comments and expand the unit tests. 
