# HabitFlow: Habit Tracker (Blazor / .NET 8)

Track daily habits, grow streaks, see progress on a heatmap, unlock badges. A check-in on one device appears live on every other open tab/device of the same user.

## Features

| Area | What it does |
|---|---|
| Accounts & roles | Sign up / log in (ASP.NET Core Identity). Roles: **User** (own habits only) and **Admin** (manages users) |
| Habits | Name, description, icon, color, repeat days (any weekdays), archive / restore / delete |
| Today view | One-tap check-ins, progress bar, rest-day habits shown separately |
| Streaks | Current + longest streak, 30-day completion rate. Rest days never break a streak |
| Heatmap | 12-week activity calendar; tap a square to add/remove a past check-in (last 30 days) |
| Stats | Weekly completion chart, totals, best streak, **badges** (3 / 7 / 30-day streaks, 100 check-ins) |
| Live sync | Singleton event broker over Blazor's SignalR circuit: all tabs/devices update instantly |
| Export | CSV export (protected against formula injection) |
| PWA | Installable on phone/desktop (manifest + service worker) |
| Responsive | Bootstrap grid + custom media queries, tested at phone width |

## Quick start

Requirements: **.NET 8 SDK** (a newer SDK works too, run with `DOTNET_ROLL_FORWARD=Major` if only a newer runtime is installed).

```bash
dotnet restore
dotnet run --project src/HabitTracker.Web
# open http://localhost:5000
```

The SQLite database (`habits.db`) and demo data are created automatically on first start.

| Role | Email | Password |
|---|---|---|
| User (with 60 days of demo history) | demo@habit.local | Demo123! |
| Admin | admin@habit.local | Admin123! |

> Change or remove the demo accounts before any real deployment.

## Tests

```bash
dotnet test                                   # unit + integration + component tests
dotnet test --collect:"XPlat Code Coverage"   # with coverage

# End-to-end (Playwright), needs the app running:
dotnet run --project src/HabitTracker.Web
pwsh tests/HabitTracker.Tests/bin/Debug/net8.0/playwright.ps1 install chromium   # first time only
E2E_BASE_URL=http://localhost:5000 dotnet test --filter Category=E2E
```

| Test type | Tool | File |
|---|---|---|
| Unit (streaks, badges, validation) | xUnit | `StreakCalculatorTests.cs`, `HabitServiceIntegrationTests.cs` (validation, events) |
| Integration (service + EF Core + SQLite) | xUnit | `HabitServiceIntegrationTests.cs` |
| Component (Blazor UI) | bUnit | `ComponentTests.cs` |
| End-to-end (real browser, desktop + phone viewport) | Playwright | `EndToEndTests.cs` |

End-to-end tests are skipped unless `E2E_BASE_URL` is set and the app is running. Code coverage has not been measured. Manual QA checklist: `docs/QA_CHECKLIST.md` (to be completed per browser/device).

## User guide

1. **Sign up** (or use the demo account), then open **Habits → New habit**: pick a name, icon, color and the days it repeats.
2. Open **Today** and tap the circle next to a habit to complete it. Tap again to undo.
3. Open a habit's **Stats** to see streaks and the heatmap. Tap a past square to fix a missed check-in.
4. **Stats** (top menu) shows weekly completion, badges and CSV export.
5. **Admin → Users** (admin only): change roles.

## Developer notes

Layers (dependencies point inward):

```
Web ─► Infrastructure ─► Application ─► Domain
 Blazor, Identity        EF Core         services, interfaces   entities
```

- `HabitTracker.Domain`: entities (`Habit`, `CheckIn`, `Weekdays`).
- `HabitTracker.Application`: `HabitService`, pure `StreakCalculator` and `BadgeService`, interfaces (`IHabitRepository`, `IClock`, `IHabitEvents`).
- `HabitTracker.Infrastructure`: EF Core `AppDbContext`, `HabitRepository` (via `IDbContextFactory`), seeding.
- `HabitTracker.Web`: Blazor Server UI, auth endpoints, `HabitState` container, `HabitEvents` broker.

More detail and UML: `docs/ARCHITECTURE.md`.

Design choices:
- **SOLID**: services depend on abstractions (`IHabitRepository`, `IClock`, `IHabitEvents`); streak logic is a pure static class with no I/O.
- **State management**: `HabitState` is a scoped container with a `Changed` event; pages and the navbar chip re-render from it. Remote changes arrive through `IHabitEvents`.
- **Reusable components**: `HabitCard`, `WeekdayPicker` (two-way `@bind-Value`), `Heatmap`, `BadgeList`, `BarChart`, built with `[Parameter]` and `EventCallback`.
- **Resilience**: every page catches errors and shows friendly messages, an `ErrorBoundary` wraps the page body, a global exception handler is configured, subscribers can never break a publisher, check-ins are idempotent and unique per day (DB unique index).
- **Security**: Identity (hashed passwords, lockout), ownership checks in the service (users only see/edit their own habits), role-based `[Authorize]`, open-redirect protection, color validation (`#RRGGBB`), CSV formula-injection protection.
- **Efficiency**: `AsNoTracking`, indexes, response compression, a short-lived `DbContext` per operation.

Known limitations / future work: SQLite for simplicity (swap the provider in `AddInfrastructure` for PostgreSQL/SQL Server), `EnsureCreated` instead of migrations, antiforgery is disabled on the login/register form posts (enable for production), reminders/notifications are not implemented.

## Troubleshooting

- *Blank page*: open the site in Safari/Chrome (not an embedded preview) and make sure the terminal says `Hosting environment: Development`. The page shows a warning after 5 s if Blazor could not start.
- *Port in use*: `dotnet run --project src/HabitTracker.Web --urls http://localhost:5080`
- *Reset data*: stop the app and delete `habits.db`.
