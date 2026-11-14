using System.Text;
using System.Text.RegularExpressions;
using HabitTracker.Domain;

namespace HabitTracker.Application;

public record HabitToday(Habit Habit, bool Scheduled, bool DoneToday, StreakInfo Streak);
public record DayActivity(DateOnly Date, int Scheduled, int Completed);
public record Overview(int HabitCount, int ScheduledToday, int DoneToday, int BestStreak, int TotalCheckIns,
                       List<DayActivity> Days, List<Badge> Badges);
public record HabitDetail(Habit Habit, StreakInfo Streak, List<Badge> Badges);

public interface IHabitService
{
    Task<List<HabitToday>> GetTodayAsync(string userId);
    Task<List<Habit>> GetAllAsync(string userId);
    Task<HabitDetail?> GetDetailAsync(int habitId, string userId);
    Task<Habit?> GetForEditAsync(int habitId, string userId);
    Task<Habit> SaveAsync(Habit habit, string userId);
    Task SetArchivedAsync(int habitId, string userId, bool archived);
    Task DeleteAsync(int habitId, string userId);
    Task<bool> ToggleAsync(int habitId, string userId, DateOnly date);
    Task<Overview> GetOverviewAsync(string userId);
    Task<string> ExportCsvAsync(string userId);
    Task<Dictionary<string, int>> CountByUserAsync();
}

public class HabitService : IHabitService
{
    public const int MaxBackfillDays = 30;
    public const int HeatmapDays = 84; // 12 weeks

    private static readonly Regex ColorRegex = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);

    private readonly IHabitRepository _repo;
    private readonly IClock _clock;
    private readonly IHabitEvents _events;

    public HabitService(IHabitRepository repo, IClock clock, IHabitEvents events)
    {
        _repo = repo;
        _clock = clock;
        _events = events;
    }

    public async Task<List<HabitToday>> GetTodayAsync(string userId)
    {
        var today = _clock.Today;
        var habits = await _repo.GetByUserAsync(userId);
        return habits
            .Select(h => new HabitToday(h, h.IsScheduledOn(today),
                h.CheckIns.Any(c => c.Date == today), StreakCalculator.Calculate(h, today)))
            .OrderByDescending(x => x.Scheduled)
            .ThenBy(x => x.Habit.CreatedAt)
            .ToList();
    }

    public Task<List<Habit>> GetAllAsync(string userId) => _repo.GetByUserAsync(userId, includeArchived: true);

    public async Task<HabitDetail?> GetDetailAsync(int habitId, string userId)
    {
        var habit = await _repo.GetAsync(habitId);
        if (habit is null || habit.UserId != userId) return null;
        var streak = StreakCalculator.Calculate(habit, _clock.Today);
        return new HabitDetail(habit, streak, BadgeService.Evaluate(streak.Longest, streak.TotalCheckIns));
    }

    public async Task<Habit?> GetForEditAsync(int habitId, string userId)
    {
        var habit = await _repo.GetAsync(habitId);
        return habit is not null && habit.UserId == userId ? habit : null;
    }

    public async Task<Habit> SaveAsync(Habit habit, string userId)
    {
        Validate(habit);

        Habit saved;
        if (habit.Id == 0)
        {
            habit.UserId = userId;
            habit.CreatedAt = DateTime.Now;
            habit.Name = habit.Name.Trim();
            saved = await _repo.AddAsync(habit);
        }
        else
        {
            var existing = await _repo.GetAsync(habit.Id) ?? throw new DomainException("Habit not found.");
            EnsureOwner(existing, userId);
            habit.UserId = existing.UserId;
            habit.Name = habit.Name.Trim();
            await _repo.UpdateAsync(habit);
            saved = habit;
        }

        _events.Publish(userId);
        return saved;
    }

    public async Task SetArchivedAsync(int habitId, string userId, bool archived)
    {
        var habit = await _repo.GetAsync(habitId) ?? throw new DomainException("Habit not found.");
        EnsureOwner(habit, userId);
        habit.IsArchived = archived;
        await _repo.UpdateAsync(habit);
        _events.Publish(userId);
    }

    public async Task DeleteAsync(int habitId, string userId)
    {
        var habit = await _repo.GetAsync(habitId) ?? throw new DomainException("Habit not found.");
        EnsureOwner(habit, userId);
        await _repo.DeleteAsync(habitId);
        _events.Publish(userId);
    }

    public async Task<bool> ToggleAsync(int habitId, string userId, DateOnly date)
    {
        var habit = await _repo.GetAsync(habitId) ?? throw new DomainException("Habit not found.");
        EnsureOwner(habit, userId);

        var today = _clock.Today;
        if (date > today) throw new DomainException("You can't check in for a future day.");
        if (date < today.AddDays(-MaxBackfillDays))
            throw new DomainException($"You can only edit the last {MaxBackfillDays} days.");

        bool nowDone;
        if (habit.CheckIns.Any(c => c.Date == date))
        {
            await _repo.RemoveCheckInAsync(habitId, date);
            nowDone = false;
        }
        else
        {
            await _repo.AddCheckInAsync(habitId, date);
            nowDone = true;
        }

        _events.Publish(userId);
        return nowDone;
    }

    public async Task<Overview> GetOverviewAsync(string userId)
    {
        var today = _clock.Today;
        var habits = await _repo.GetByUserAsync(userId);
        var infos = habits.Select(h => StreakCalculator.Calculate(h, today)).ToList();

        var days = new List<DayActivity>();
        for (var i = HeatmapDays - 1; i >= 0; i--)
        {
            var d = today.AddDays(-i);
            var scheduled = habits.Count(h => h.IsScheduledOn(d) && DateOnly.FromDateTime(h.CreatedAt) <= d);
            var completed = habits.Count(h => h.CheckIns.Any(c => c.Date == d));
            days.Add(new DayActivity(d, Math.Max(scheduled, completed), completed));
        }

        var scheduledToday = habits.Count(h => h.IsScheduledOn(today));
        var doneToday = habits.Count(h => h.IsScheduledOn(today) && h.CheckIns.Any(c => c.Date == today));
        var best = infos.Count == 0 ? 0 : infos.Max(i => i.Current);
        var longest = infos.Count == 0 ? 0 : infos.Max(i => i.Longest);
        var total = infos.Sum(i => i.TotalCheckIns);

        return new Overview(habits.Count, scheduledToday, doneToday, best, total, days,
            BadgeService.Evaluate(longest, total));
    }

    public async Task<string> ExportCsvAsync(string userId)
    {
        var habits = await _repo.GetByUserAsync(userId, includeArchived: true);
        var sb = new StringBuilder("Habit,Date\n");
        foreach (var h in habits)
            foreach (var c in h.CheckIns.OrderBy(c => c.Date))
                sb.Append(Csv(h.Name)).Append(',').Append(c.Date.ToString("yyyy-MM-dd")).Append('\n');
        return sb.ToString();
    }

    public Task<Dictionary<string, int>> CountByUserAsync() => _repo.CountByUserAsync();

    // ---- helpers ----

    private static void EnsureOwner(Habit habit, string userId)
    {
        if (habit.UserId != userId) throw new DomainException("This habit belongs to another user.");
    }

    public static void Validate(Habit habit)
    {
        if (string.IsNullOrWhiteSpace(habit.Name))
            throw new DomainException("Name is required.");
        if (habit.Name.Trim().Length > 80)
            throw new DomainException("Name must be at most 80 characters.");
        if (habit.Description?.Length > 300)
            throw new DomainException("Description must be at most 300 characters.");
        if (habit.ScheduledDays == Weekdays.None)
            throw new DomainException("Choose at least one day of the week.");
        if (!ColorRegex.IsMatch(habit.Color ?? ""))
            throw new DomainException("Color must look like #RRGGBB.");
        if (string.IsNullOrWhiteSpace(habit.Emoji))
            throw new DomainException("Pick an icon.");
    }

    // protects against CSV/formula injection and broken quoting
    private static string Csv(string s)
    {
        if (s.Length > 0 && "=+-@".Contains(s[0])) s = "'" + s;
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }
}
