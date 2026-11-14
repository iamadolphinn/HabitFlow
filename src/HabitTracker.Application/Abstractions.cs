using HabitTracker.Domain;

namespace HabitTracker.Application;

public interface IHabitRepository
{
    Task<List<Habit>> GetByUserAsync(string userId, bool includeArchived = false);
    Task<Habit?> GetAsync(int id);
    Task<Habit> AddAsync(Habit habit);
    Task UpdateAsync(Habit habit);
    Task DeleteAsync(int id);
    Task<bool> AddCheckInAsync(int habitId, DateOnly date);
    Task<bool> RemoveCheckInAsync(int habitId, DateOnly date);
    Task<Dictionary<string, int>> CountByUserAsync();
}

/// <summary>Lets tests control "today".</summary>
public interface IClock
{
    DateOnly Today { get; }
}

public class SystemClock : IClock
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
}

/// <summary>In-process publish/subscribe so every open tab/device of one user stays in sync.</summary>
public interface IHabitEvents
{
    event Action<string>? HabitsChanged;
    void Publish(string userId);
}

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
