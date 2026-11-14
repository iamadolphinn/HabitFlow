using HabitTracker.Application;

namespace HabitTracker.Web.Services;

/// <summary>
/// State container for the signed-in user's habits (scoped = one per Blazor circuit).
/// Components read from it and re-render when <see cref="Changed"/> fires, either because the user
/// acted in this tab or because another tab/device published a change through <see cref="IHabitEvents"/>.
/// </summary>
public class HabitState : IDisposable
{
    private readonly IHabitService _habits;
    private readonly IHabitEvents _events;
    private readonly CurrentUser _user;
    private string? _userId;
    private bool _disposed;

    public HabitState(IHabitService habits, IHabitEvents events, CurrentUser user)
    {
        _habits = habits;
        _events = events;
        _user = user;
    }

    public IReadOnlyList<HabitToday> Today { get; private set; } = Array.Empty<HabitToday>();
    public bool IsLoaded { get; private set; }
    public int Scheduled => Today.Count(h => h.Scheduled);
    public int Done => Today.Count(h => h.Scheduled && h.DoneToday);
    public int Percent => Scheduled == 0 ? 0 : Done * 100 / Scheduled;

    public event Action? Changed;

    public async Task EnsureLoadedAsync()
    {
        var (id, _, _) = await _user.GetAsync();
        if (string.IsNullOrEmpty(id)) return; // anonymous visitor

        if (_userId != id)
        {
            if (_userId is not null) _events.HabitsChanged -= OnRemoteChange;
            _userId = id;
            _events.HabitsChanged += OnRemoteChange;
        }
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (_userId is null || _disposed) return;
        Today = await _habits.GetTodayAsync(_userId);
        IsLoaded = true;
        Changed?.Invoke();
    }

    public async Task<bool> ToggleAsync(int habitId, DateOnly? date = null)
    {
        if (_userId is null) throw new DomainException("Please log in.");
        var day = date ?? DateOnly.FromDateTime(DateTime.Now);
        var done = await _habits.ToggleAsync(habitId, _userId, day);
        await RefreshAsync();
        return done;
    }

    private void OnRemoteChange(string userId)
    {
        if (userId != _userId) return;
        _ = SafeRefreshAsync();
    }

    private async Task SafeRefreshAsync()
    {
        try { await RefreshAsync(); }
        catch (Exception) { /* circuit may already be gone */ }
    }

    public void Dispose()
    {
        _disposed = true;
        _events.HabitsChanged -= OnRemoteChange;
    }
}
