# RaceDay - Part 2: RESTful API

An ASP.NET Core Web API (.NET 10) for the RaceDay event management system. It uses
Entity Framework Core with SQL Server, cookie-based sessions with role-based access,
Swagger for testing, xUnit tests, and a GitHub Actions workflow.

## The two roles

* **Organizer** - creates, edits and deletes their own events, defines categories for
those events, captures results (finish time, position, status), and views all
enrolments for their events.
* **Participant** - registers, views events and categories, enrols in an event by
choosing a category, views and cancels their own enrolments, and views their own results.

Organizer-only endpoints return 403 to a Participant, and Participant-only endpoints
return 403 to an Organizer. Anyone who is not logged in gets 401.

## Setup and run

1. Install the .NET 10 SDK and SQL Server Express (with SSMS).
2. In SSMS, open `Part2/Database/RaceDay\_Part2\_Schema.sql` and run it once. It rebuilds
`RaceDayDB` with the Part 2 columns and sample data.
3. Check the connection string in `Part2/RaceDay.Api/appsettings.json`. It defaults to
`Server=localhost\\SQLEXPRESS;Database=RaceDayDB;Trusted\_Connection=True;TrustServerCertificate=True;`
4. From the repository root, start the API:

cd Part2
       dotnet run --project RaceDay.Api --launch-profile http


5. Open `http://localhost:5012/swagger` to view and test every endpoint in the browser.
6. The seeded accounts have placeholder passwords and cannot log in. Create real accounts
with `POST /api/auth/register`, then log in with `POST /api/auth/login`. Swagger keeps
the session cookie, so later requests run as that user.

## Run the tests

&#x20;   cd Part2
    dotnet test


The 28 tests use an in-memory database, so they never touch SQL Server.

## Endpoints

|Area|Endpoints|
|-|-|
|Authentication|`POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/logout`, `GET /api/auth/me`|
|Profile|`GET /api/profile/me`, `PUT /api/profile/me`|
|Events|`GET /api/events`, `GET /api/events/{id}`, `POST /api/events`, `PUT /api/events/{id}`, `DELETE /api/events/{id}`|
|Categories|`GET/POST /api/events/{eventId}/categories`, `PUT/DELETE /api/categories/{id}`|
|Enrolments|`POST /api/categories/{categoryId}/enrollments`, `GET /api/enrollments/me`, `GET /api/events/{eventId}/enrollments`, `DELETE /api/enrollments/{id}`|
|Results|`POST /api/enrollments/{enrollmentId}/results`, `PUT /api/results/{id}`, `GET /api/results/me`, `GET /api/events/{eventId}/results`|

## Changes from the Part 1 plan

* `Event` gained `Distance` and `EventType` (Run, Walk or Cycle), as the Part 2 brief requires.
* `Organizer` and `Participant` gained `Email` and `PasswordHash`. Passwords are hashed with
ASP.NET's password hasher and never stored in plain text.
* Authentication uses cookie sessions (session management) instead of the JWT mentioned in
the Part 1 plan, and does not use ASP.NET Core Identity's tables.
* Viewing events and event results requires a login, because the brief says both roles view
them. The Part 1 plan had these public.
* The Part 1 plan had separate Organizer and Participant profile routes. Both roles now share
`/api/profile/me`, and the code branches on the role.

## CI/CD

A GitHub Actions workflow (`.github/workflows/part2-build-and-test.yml`) builds the solution
and runs all unit tests on every push.

!\[Green build](ci-screenshot-part2.png)

## Video walkthrough

https://youtu.be/E09MozFXvA4?si=rCNRIqhYNCqBfzgk

## AI tool disclosure

AI (Claude) assisted with planning the build order, drafting the code and tests, and
explaining concepts. The code was run, tested and reviewed by the author, who also made the
design decisions and can explain each part in the video.

