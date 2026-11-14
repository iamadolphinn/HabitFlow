# Manual QA checklist

Run each item on Chrome, Firefox, Edge and Safari (desktop), and Safari/Chrome on a phone. Tick when it passes.

## Auth and roles
- [ ] Sign up, log out, log in
- [ ] Wrong password shows the error message
- [ ] Anonymous user opening `/today` is redirected to login and returned after login
- [ ] Normal user opening `/admin/users` sees "no permission"
- [ ] Admin can change a role, and cannot change their own

## Habits
- [ ] Create a habit with each weekday preset and a custom set of days
- [ ] Validation: empty name, no day selected
- [ ] Edit a habit that already has history (history is kept)
- [ ] Archive and restore; delete
- [ ] A habit not scheduled today appears under "Rest day for"

## Check-ins and stats
- [ ] Tap to complete / undo; progress bar and navbar chip update
- [ ] Streak increases on consecutive days; a rest day does not break it
- [ ] Heatmap: tapping a past square toggles it; future/too-old dates show a message
- [ ] Badges unlock at 3 / 7 / 30-day streaks
- [ ] CSV export downloads and opens in Excel

## Live sync
- [ ] Open the app in two browsers as the same user: a check-in in one appears in the other within a second
- [ ] A second user's check-ins do not appear

## Responsive / cross-platform
- [ ] 360 px wide: no horizontal scroll, tap targets easy to hit
- [ ] Tablet portrait and landscape
- [ ] Windows, macOS and Linux (`dotnet run`)
- [ ] "Install app" works in Chrome (PWA)

## Resilience
- [ ] Stop and restart the server: the page shows the reconnect UI, then recovers
- [ ] Double-click the check-in circle quickly: the final state is consistent
