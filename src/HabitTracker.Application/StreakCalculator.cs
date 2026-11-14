using HabitTracker.Domain;

namespace HabitTracker.Application;

public record StreakInfo(int Current, int Longest, int TotalCheckIns, int Rate30Days);

/// <summary>
/// Pure streak logic (no I/O, easy to unit test).
/// - Only scheduled days can break a streak; rest days are skipped.
/// - Today never breaks a streak while it is still open.
/// </summary>
public static class StreakCalculator
{
    public static StreakInfo Calculate(Habit habit, DateOnly today)
    {
        var done = habit.CheckIns.Select(c => c.Date).ToHashSet();
        var created = DateOnly.FromDateTime(habit.CreatedAt);
        var start = created;
        if (done.Count > 0 && done.Min() < start) start = done.Min();
        if (start > today) start = today;

        int run = 0, longest = 0;
        for (var d = start; d <= today; d = d.AddDays(1))
        {
            if (done.Contains(d)) { run++; longest = Math.Max(longest, run); }
            else if (habit.IsScheduledOn(d) && d != today) run = 0;
        }

        int current = 0;
        for (var d = today; d >= start; d = d.AddDays(-1))
        {
            if (done.Contains(d)) current++;
            else if (habit.IsScheduledOn(d) && d != today) break;
        }

        var windowStart = start > today.AddDays(-29) ? start : today.AddDays(-29);
        int scheduled = 0, completed = 0;
        for (var d = windowStart; d <= today; d = d.AddDays(1))
        {
            var isDone = done.Contains(d);
            if (habit.IsScheduledOn(d) && (d != today || isDone))
            {
                scheduled++;
                if (isDone) completed++;
            }
        }
        var rate = scheduled == 0 ? 0 : (int)Math.Round(completed * 100.0 / scheduled);

        return new StreakInfo(current, longest, done.Count, rate);
    }
}
