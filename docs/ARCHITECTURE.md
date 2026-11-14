# Architecture

Pattern: **Clean Architecture** (layers) with a component-based Blazor UI. Components are the views; `HabitState` and the services act as view-model/model (an MVVM-like approach, not a framework).

## Layers

```mermaid
flowchart LR
    Web["HabitTracker.Web<br/>Blazor Server, Identity"] --> Infra["HabitTracker.Infrastructure<br/>EF Core, repository, seeding"]
    Web --> App
    Infra --> App["HabitTracker.Application<br/>services + interfaces"]
    App --> Domain["HabitTracker.Domain<br/>entities"]
```

## Domain model

```mermaid
classDiagram
    class Habit { +int Id +string UserId +string Name +string Emoji +string Color +Weekdays ScheduledDays +DateTime CreatedAt +bool IsArchived +IsScheduledOn(date) bool }
    class CheckIn { +int Id +int HabitId +DateOnly Date }
    class Weekdays { <<flags enum>> Mon Tue Wed Thu Fri Sat Sun Workdays Everyday }
    Habit "1" *-- "many" CheckIn
    Habit --> Weekdays
```

## Services

```mermaid
classDiagram
    class IHabitService { <<interface>> }
    class IHabitRepository { <<interface>> }
    class IClock { <<interface>> }
    class IHabitEvents { <<interface>> }
    HabitService ..|> IHabitService
    HabitRepository ..|> IHabitRepository
    SystemClock ..|> IClock
    HabitEvents ..|> IHabitEvents
    HabitService --> IHabitRepository
    HabitService --> IClock
    HabitService --> IHabitEvents
    HabitService ..> StreakCalculator
    HabitService ..> BadgeService
    HabitState --> IHabitService
    HabitState --> IHabitEvents
```

## Sequence: check-in with live sync

```mermaid
sequenceDiagram
    actor U as User (phone)
    participant C as Today.razor
    participant S as HabitState
    participant H as HabitService
    participant R as HabitRepository
    participant E as HabitEvents (singleton)
    participant L as HabitState (laptop tab)
    U->>C: tap check-in
    C->>S: ToggleAsync(habitId)
    S->>H: ToggleAsync(habitId, userId, today)
    H->>R: add / remove CheckIn
    H->>E: Publish(userId)
    E-->>L: HabitsChanged(userId)
    L->>H: GetTodayAsync (refresh)
    L-->>U: laptop UI re-renders
    S->>C: Changed event, re-render
```

## Use cases

```mermaid
flowchart TB
    User(["User"]) --> UC1["Manage habits"]
    User --> UC2["Check in / undo"]
    User --> UC3["View streaks, heatmap, badges"]
    User --> UC4["Export CSV"]
    Admin(["Admin"]) --> UC5["Manage user roles"]
    Admin --> UC1
```

## Streak rules (StreakCalculator)

- Only **scheduled** days can break a streak; rest days are skipped.
- **Today never breaks** a streak while it is still open.
- A check-in on a rest day counts as a bonus and extends the streak.
- 30-day rate = completed scheduled days / scheduled days (today counted only if done).

## Technology comparison

| | **HabitFlow (ours)** | Habitica | Streaks (iOS) | Google Sheets template |
|---|---|---|---|---|
| Open code, self-hosted | ✔ | partly | ✘ | ✘ |
| Cross-platform (web + PWA) | ✔ | ✔ | iOS only | ✔ |
| Live sync between devices | ✔ | ✔ | iCloud | manual |
| Flexible weekdays per habit | ✔ | ✔ | ✔ | ✘ |
| Heatmap + badges | ✔ | gamified | ✔ | ✘ |
| No account with a third party | ✔ | ✘ | ✔ | ✘ |

| Blazor Server (chosen) | Blazor WebAssembly | MVC / Razor Pages | React + API |
|---|---|---|---|
| Full C# stack, built-in SignalR circuit (live sync), direct DB access | Works offline, but larger download and needs a separate API | Simple, little interactivity | Separate JS/TS stack and API |

## Relevance

Habit formation research shows that visible streaks and fast feedback increase consistency. A small, private, self-hosted tracker is useful for students and teams who do not want another subscription or to share personal data.
