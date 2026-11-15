# Manual QA checklist

Run each item on Chrome, Firefox, Edge and Safari (desktop), and Safari/Chrome on a phone. Tick when it passes.

## Auth and roles
- [x] Sign up, log out, log in
- [x] Wrong password shows the error message
- [x] Anonymous user opening `/today` is redirected to login and returned after login
- [x] Normal user opening `/admin/users` sees "no permission"
- [x] Admin can change a role, and cannot change their own

## Habits
- [x] Create a habit with each weekday preset and a custom set of days
- [x] Validation: empty name, no day selected
- [x] Edit a habit that already has history (history is kept)
- [x] Archive and restore; delete
- [x] A habit not scheduled today appears under "Rest day for"

## Check-ins and stats
- [x] Tap to complete / undo; progress bar and navbar chip update
- [x] Streak increases on consecutive days; a rest day does not break it
- [ ] Heatmap: tapping a past square toggles it; future/too-old dates show a message
- [x] Badges unlock at 3 / 7 / 30-day streaks
- [x] CSV export downloads and opens in Excel

## Live sync
- [x] Open the app in two browsers as the same user: a check-in in one appears in the other within a second
- [x] A second user's check-ins do not appear
## Responsive / cross-platform
- [x] 360 px wide: no horizontal scroll, tap targets easy to hit
- [x] Tablet portrait and landscape
- [ ] Windows, macOS and Linux (`dotnet run`)
- [ ] "Install app" works in Chrome (PWA)

## Resilience
- [x] Stop and restart the server: the page shows the reconnect UI, then recovers
- [x] Double-click the check-in circle quickly: the final state is consistent
